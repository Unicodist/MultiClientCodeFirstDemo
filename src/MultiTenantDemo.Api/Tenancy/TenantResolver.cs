using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MultiTenantDemo.System.Domain;
using MultiTenantDemo.System.Domain.Repositories;

namespace MultiTenantDemo.Api.Tenancy;

public sealed class TenantResolver : ITenantResolver
{
    public const string HeaderName = "X-Tenant-Id";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITenantRepository _tenants;
    private readonly TenantInfoFactory _tenantInfoFactory;
    private readonly ILogger<TenantResolver> _logger;
    private TenantInfo? _resolved;

    public TenantResolver(
        IHttpContextAccessor httpContextAccessor,
        ITenantRepository tenants,
        TenantInfoFactory tenantInfoFactory,
        ILogger<TenantResolver> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _tenants = tenants;
        _tenantInfoFactory = tenantInfoFactory;
        _logger = logger;
    }

    public async Task<TenantInfo> ResolveAsync(CancellationToken cancellationToken = default)
    {
        if (_resolved is not null)
        {
            return _resolved;
        }

        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("TenantResolver requires an HTTP request.");

        var values = httpContext.Request.Headers[HeaderName];
        if (values.Count == 0 || string.IsNullOrWhiteSpace(values[0]))
        {
            throw new TenantResolutionException(TenantResolutionFailure.Missing, $"The {HeaderName} header is required.");
        }

        var tenantId = values[0]!;
        if (values.Count > 1 || !TenantId.IsValid(tenantId))
        {
            throw new TenantResolutionException(TenantResolutionFailure.Malformed, $"The {HeaderName} header is malformed.");
        }

        Tenant? tenant;
        try
        {
            tenant = await _tenants.FindAsync(tenantId, cancellationToken).ConfigureAwait(false);
        }
        catch (TenantRepositoryUnavailableException ex)
        {
            _logger.LogError(ex, "Tenant repository unavailable while resolving tenant {TenantId}", tenantId);
            throw new TenantResolutionException(TenantResolutionFailure.RegistryUnavailable, "The tenant registry is unavailable.", ex);
        }

        if (tenant is null)
        {
            throw new TenantResolutionException(TenantResolutionFailure.Unknown, $"Tenant {tenantId} is not registered.");
        }

        if (!tenant.IsActive)
        {
            throw new TenantResolutionException(TenantResolutionFailure.Inactive, $"Tenant {tenantId} is not active.");
        }

        try
        {
            _resolved = _tenantInfoFactory.Create(tenant);
        }
        catch (TenantResolutionException ex)
        {
            _logger.LogError("Rejected connection metadata for tenant {TenantId}: {Reason}", tenantId, ex.Message);
            throw;
        }

        return _resolved;
    }
}
