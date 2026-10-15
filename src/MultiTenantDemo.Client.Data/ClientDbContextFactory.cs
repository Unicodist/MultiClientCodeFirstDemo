namespace MultiTenantDemo.Client.Data;

public interface IClientConnectionStringProvider
{
    Task<string> GetConnectionStringAsync(CancellationToken cancellationToken = default);
}

public interface IClientDbContextFactory
{
    Task<ClientDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default);
}

internal sealed class ClientDbContextFactory : IClientDbContextFactory
{
    private readonly IClientConnectionStringProvider _connectionStringProvider;

    public ClientDbContextFactory(IClientConnectionStringProvider connectionStringProvider)
    {
        _connectionStringProvider = connectionStringProvider;
    }

    public async Task<ClientDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = await _connectionStringProvider.GetConnectionStringAsync(cancellationToken).ConfigureAwait(false);
        return new ClientDbContext(ClientDbContextOptionsBuilder.Build(connectionString));
    }
}
