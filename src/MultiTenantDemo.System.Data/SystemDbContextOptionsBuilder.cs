using Microsoft.EntityFrameworkCore;

namespace MultiTenantDemo.System.Data;

internal static class SystemDbContextOptionsBuilder
{
    public static DbContextOptions<SystemDbContext> Build(string connectionString) =>
        new DbContextOptionsBuilder<SystemDbContext>()
            .UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(SystemDbContext).Assembly.GetName().Name);
                sql.MigrationsHistoryTable("__EFMigrationsHistory", SystemDbContext.Schema);
            })
            .Options;
}
