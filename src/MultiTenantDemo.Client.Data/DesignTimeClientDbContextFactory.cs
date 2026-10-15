using Microsoft.EntityFrameworkCore.Design;

namespace MultiTenantDemo.Client.Data;

public sealed class DesignTimeClientDbContextFactory : IDesignTimeDbContextFactory<ClientDbContext>
{
    public const string ConnectionEnvironmentVariable = "MULTITENANT_DESIGNTIME_CONNECTION";

    private const string OfflineConnection =
        "Server=design-time.invalid;Database=MultiTenantDemo_DesignTime;Integrated Security=true;Connect Timeout=1";

    public ClientDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable(ConnectionEnvironmentVariable);
        return new ClientDbContext(ClientDbContextOptionsBuilder.Build(
            string.IsNullOrWhiteSpace(connection) ? OfflineConnection : connection));
    }
}
