using Microsoft.EntityFrameworkCore;
using MultiTenantDemo.System.Data.Configurations;
using MultiTenantDemo.System.Domain;

namespace MultiTenantDemo.System.Data;

public class SystemDbContext : DbContext
{
    public const string Schema = "system";

    public SystemDbContext(DbContextOptions<SystemDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfiguration(new TenantConfiguration());
    }
}
