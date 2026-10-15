
namespace MultiTenantDemo.System.Domain;

public interface ITenantResolver
{
    Task<TenantInfo> ResolveAsync(CancellationToken cancellationToken = default);
}
