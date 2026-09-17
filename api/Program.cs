using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Api.Auth;
using Api.Data;
using Api.Models;

var builder = WebApplication.CreateBuilder(args);

// --- Connection string & DbContext (Npgsql / Render Postgres) ---------------
var connectionString = builder.Configuration["ConnectionStrings:Default"]
    ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured (set env var ConnectionStrings__Default).");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// --- JWT auth -----------------------------------------------------------
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured (set env var Jwt__Key).");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "stockroom-login-slice";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "stockroom-login-slice";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddSingleton<JwtTokenService>();

// --- CORS: admin + storefront origins ------------------------------------
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173", "http://localhost:5174" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AppCors", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Dev-only: stamp real password hashes onto the seeded rows from
    // auth-slice-postgres.sql and print demo credentials to the console.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        await DevSeeder.SeedDemoPasswordsAsync(db);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"DEV SEED skipped (is the DB reachable and has auth-slice-postgres.sql been loaded?): {ex.Message}");
    }
}

app.UseCors("AppCors");
app.UseAuthentication();
app.UseAuthorization();

// --- Health check: proves the API can really reach Postgres ---------------
app.MapGet("/health", async (AppDbContext db) =>
{
    var canConnect = await db.Database.CanConnectAsync();
    return canConnect
        ? Results.Ok(new { status = "ok" })
        : Results.Problem("Database unreachable", statusCode: StatusCodes.Status503ServiceUnavailable);
});

// --- Admin login ------------------------------------------------------------
app.MapPost("/api/admin/login", async (LoginRequest request, AppDbContext db, JwtTokenService tokens) =>
{
    var admin = await db.AdminUsers.SingleOrDefaultAsync(a => a.Email == request.Email);
    var hasher = new PasswordHasher<AdminUser>();

    if (admin is null || admin.PasswordHash is null ||
        hasher.VerifyHashedPassword(admin, admin.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
    {
        return Results.Json(new LoginError("Invalid email or password."), statusCode: StatusCodes.Status401Unauthorized);
    }

    if (admin.Status != "Active")
    {
        return Results.Json(new LoginError("This admin account is inactive."), statusCode: StatusCodes.Status401Unauthorized);
    }

    var token = tokens.CreateToken(admin.AdminId.ToString(), admin.Email, admin.Role, admin.FullName ?? admin.Email);
    return Results.Ok(new LoginResponse(token, admin.Email, admin.FullName ?? admin.Email, admin.Role));
});

// --- Customer login -----------------------------------------------------
app.MapPost("/api/customer/login", async (LoginRequest request, AppDbContext db, JwtTokenService tokens) =>
{
    var customer = await db.Customers.SingleOrDefaultAsync(c => c.Email == request.Email);
    var hasher = new PasswordHasher<Customer>();

    if (customer is null || customer.PasswordHash is null ||
        hasher.VerifyHashedPassword(customer, customer.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
    {
        return Results.Json(new LoginError("Invalid email or password."), statusCode: StatusCodes.Status401Unauthorized);
    }

    if (customer.Status != "Approved")
    {
        var message = customer.Status switch
        {
            "Pending" => "Your account is still awaiting approval. We'll email you once it's active.",
            "OnHold" => "Your account is on hold. Please contact us for details.",
            "Rejected" => "Your account application was not approved.",
            _ => "Your account is not active."
        };
        return Results.Json(new LoginError(message), statusCode: StatusCodes.Status403Forbidden);
    }

    var token = tokens.CreateToken(customer.CustomerId.ToString(), customer.Email, "Customer", customer.BusinessName);
    return Results.Ok(new LoginResponse(token, customer.Email, customer.BusinessName, "Customer"));
});

app.Run();
