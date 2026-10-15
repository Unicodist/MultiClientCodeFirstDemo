using Microsoft.EntityFrameworkCore;

namespace MultiTenantDemo.Client.Data;

internal static class ClientDbContextOptionsBuilder
{
    public static DbContextOptions<ClientDbContext> Build(string connectionString) =>
        new DbContextOptionsBuilder<ClientDbContext>()
            .UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(ClientDbContext).Assembly.GetName().Name);
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "dbo");
            })
            .Options;
}
