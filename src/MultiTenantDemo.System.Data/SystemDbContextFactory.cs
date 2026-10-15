using Microsoft.Extensions.Options;

namespace MultiTenantDemo.System.Data;

public interface ISystemDbContextFactory
{
    SystemDbContext CreateDbContext();
}

internal sealed class SystemDbContextFactory : ISystemDbContextFactory
{
    private readonly SystemDatabaseOptions _options;

    public SystemDbContextFactory(IOptions<SystemDatabaseOptions> options)
    {
        _options = options.Value;
    }

    public SystemDbContext CreateDbContext()
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new InvalidOperationException($"{SystemDatabaseOptions.SectionName}:ConnectionString is not configured.");
        }

        return new SystemDbContext(SystemDbContextOptionsBuilder.Build(_options.ConnectionString));
    }
}
