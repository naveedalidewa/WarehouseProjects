using Microsoft.EntityFrameworkCore;
using Api.Models;

namespace Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminUser>(e =>
        {
            e.ToTable("admin_users");
            e.HasKey(x => x.AdminId);
            e.Property(x => x.AdminId).HasColumnName("admin_id");
            e.Property(x => x.Email).HasColumnName("email");
            e.Property(x => x.PasswordHash).HasColumnName("password_hash");
            e.Property(x => x.FullName).HasColumnName("full_name");
            e.Property(x => x.Role).HasColumnName("role");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Customer>(e =>
        {
            e.ToTable("customers");
            e.HasKey(x => x.CustomerId);
            e.Property(x => x.CustomerId).HasColumnName("customer_id");
            e.Property(x => x.AccountNumber).HasColumnName("account_number");
            e.Property(x => x.BusinessName).HasColumnName("business_name");
            e.Property(x => x.Email).HasColumnName("email");
            e.Property(x => x.PasswordHash).HasColumnName("password_hash");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasIndex(x => x.Email).IsUnique();
        });
    }
}
