using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Api.Models;

namespace Api.Data;

// Dev-only: the SQL seed rows carry placeholder password_hash values (see
// auth-slice-postgres.sql). On startup in Development we overwrite them with
// real hashes for the demo password and print the credentials to the console.
public static class DevSeeder
{
    public const string DemoPassword = "Passw0rd!";

    public static async Task SeedDemoPasswordsAsync(AppDbContext db)
    {
        var adminHasher = new PasswordHasher<AdminUser>();
        var customerHasher = new PasswordHasher<Customer>();

        var admins = await db.AdminUsers.ToListAsync();
        foreach (var admin in admins)
        {
            admin.PasswordHash = adminHasher.HashPassword(admin, DemoPassword);
        }

        var customers = await db.Customers.ToListAsync();
        foreach (var customer in customers)
        {
            customer.PasswordHash = customerHasher.HashPassword(customer, DemoPassword);
        }

        await db.SaveChangesAsync();

        Console.WriteLine("=== DEV SEED: demo credentials (password for all: " + DemoPassword + ") ===");
        foreach (var admin in admins)
        {
            Console.WriteLine($"  Admin    | {admin.Email} | expected: logs in");
        }
        foreach (var customer in customers)
        {
            var expected = customer.Status == "Approved" ? "logs in" : $"rejected (status={customer.Status})";
            Console.WriteLine($"  Customer | {customer.Email} | expected: {expected}");
        }
        Console.WriteLine("===========================================================");
    }
}
