using MultiTenantDemo.Client.Data;
using MultiTenantDemo.System.Domain;

namespace MultiTenantDemo.Api.Tenancy;

internal sealed class TenantConnectionStringProvider : IClientConnectionStringProvider
{
    private readonly ITenantResolver _tenantResolver;

    public TenantConnectionStringProvider(ITenantResolver tenantResolver)
    {
        _tenantResolver = tenantResolver;
    }

    public async Task<string> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantResolver.ResolveAsync(cancellationToken).ConfigureAwait(false);
        return tenant.ConnectionString;
    }
}
