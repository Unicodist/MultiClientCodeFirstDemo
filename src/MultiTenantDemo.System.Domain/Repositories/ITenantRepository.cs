namespace MultiTenantDemo.System.Domain.Repositories;

public interface ITenantRepository
{
    Task<Tenant?> FindAsync(string tenantId, CancellationToken cancellationToken = default);
}

public sealed class TenantRepositoryUnavailableException : Exception
{
    public TenantRepositoryUnavailableException()
        : base("The tenant repository is unavailable.")
    {
    }

    public TenantRepositoryUnavailableException(string message)
        : base(message)
    {
    }

    public TenantRepositoryUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
