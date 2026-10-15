using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MultiTenantDemo.System.Domain;
using MultiTenantDemo.System.Domain.Repositories;

namespace MultiTenantDemo.System.Data.Repositories;

internal sealed class TenantRepository : ITenantRepository
{
    private readonly ISystemDbContextFactory _contextFactory;

    public TenantRepository(ISystemDbContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Tenant?> FindAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var db = _contextFactory.CreateDbContext();
            return await db.Tenants.AsNoTracking()
                .SingleOrDefaultAsync(t => t.TenantId == tenantId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException { InnerException: SqlException })
        {
            throw new TenantRepositoryUnavailableException("The system database could not be queried for tenants.", ex);
        }
    }
}
