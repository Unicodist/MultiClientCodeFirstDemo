using Microsoft.EntityFrameworkCore.Design;

namespace MultiTenantDemo.System.Data;

public sealed class DesignTimeSystemDbContextFactory : IDesignTimeDbContextFactory<SystemDbContext>
{
    public const string ConnectionEnvironmentVariable = "MULTITENANT_SYSTEM_DESIGNTIME_CONNECTION";

    private const string OfflineConnection =
        "Server=design-time.invalid;Database=MultiTenantDemo_System_DesignTime;Integrated Security=true;Connect Timeout=1";

    public SystemDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable(ConnectionEnvironmentVariable);
        return new SystemDbContext(SystemDbContextOptionsBuilder.Build(
            string.IsNullOrWhiteSpace(connection) ? OfflineConnection : connection));
    }
}
