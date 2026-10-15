using Microsoft.EntityFrameworkCore;
using MultiTenantDemo.System.Data;
using MultiTenantDemo.System.Domain;

namespace MultiTenantDemo.IntegrationTests.Infrastructure;

public sealed record TestTenant(string TenantId, string DatabaseName, bool IsActive = true);

internal static class TenantConfiguration
{
    public static Dictionary<string, string?> For(SqlServerFixture sql, string systemDatabase) => new()
    {
        ["SystemDatabase:ConnectionString"] = sql.ConnectionStringFor(systemDatabase),
        ["TenantConnections:AllowedServers:0"] = sql.DataSource,
        ["TenantConnections:AllowTrustServerCertificate"] = "true",
    };

    public static async Task SeedAsync(SqlServerFixture sql, string systemDatabase, IEnumerable<TestTenant> tenants)
    {
        await using var db = new SystemDbContext(new DbContextOptionsBuilder<SystemDbContext>()
            .UseSqlServer(sql.ConnectionStringFor(systemDatabase), o => o.MigrationsHistoryTable("__EFMigrationsHistory", SystemDbContext.Schema))
            .Options);
        await db.Database.EnsureCreatedAsync();

        foreach (var tenant in tenants)
        {
            var existing = await db.Tenants.FindAsync(tenant.TenantId);
            if (existing is null)
            {
                db.Tenants.Add(new Tenant
                {
                    TenantId = tenant.TenantId,
                    Name = tenant.TenantId,
                    ConnectionString = sql.ConnectionStringFor(tenant.DatabaseName),
                    IsActive = tenant.IsActive,
                });
            }
        }

        await db.SaveChangesAsync();
    }
}
