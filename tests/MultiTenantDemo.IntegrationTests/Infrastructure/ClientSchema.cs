using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MultiTenantDemo.Client.Data;

namespace MultiTenantDemo.IntegrationTests.Infrastructure;

internal static class ClientSchema
{
    public static async Task MigrateAsync(SqlServerFixture sql, string database, string? targetMigration = null)
    {
        await using var db = new ClientDbContext(ClientDbContextOptionsBuilder.Build(sql.ConnectionStringFor(database)));
        await db.GetService<IMigrator>().MigrateAsync(targetMigration);
    }

    public static async Task<string[]> AppliedMigrationsAsync(SqlServerFixture sql, string database)
    {
        await using var db = new ClientDbContext(ClientDbContextOptionsBuilder.Build(sql.ConnectionStringFor(database)));
        return [.. await db.Database.GetAppliedMigrationsAsync()];
    }
}
