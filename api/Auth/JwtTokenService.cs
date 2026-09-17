using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Api.Auth;

// Issues JWTs for the login slice. The signing key comes from config (env var
// Jwt__Key), never hard-coded.
public class JwtTokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config)
    {
        _config = config;
    }

    public string CreateToken(string subjectId, string email, string role, string displayName)
    {
        var key = _config["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured (set env var Jwt__Key).");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, subjectId),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim("name", displayName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "stockroom-login-slice",
            audience: _config["Jwt:Audience"] ?? "stockroom-login-slice",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
