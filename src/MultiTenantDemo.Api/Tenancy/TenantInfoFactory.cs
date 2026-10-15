using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using MultiTenantDemo.System.Domain;

namespace MultiTenantDemo.Api.Tenancy;

public sealed partial class TenantInfoFactory
{
    private readonly TenantConnectionOptions _options;

    public TenantInfoFactory(IOptions<TenantConnectionOptions> options)
    {
        _options = options.Value;
    }

    public TenantInfo Create(Tenant tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        if (!TenantId.IsValid(tenant.TenantId))
        {
            throw Invalid("registry returned a malformed tenant id");
        }

        SqlConnectionStringBuilder builder;
        try
        {
            builder = new SqlConnectionStringBuilder(tenant.ConnectionString);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or KeyNotFoundException or InvalidOperationException)
        {
            throw Invalid("connection string could not be parsed");
        }

        if (string.IsNullOrWhiteSpace(builder.DataSource)
            || !_options.AllowedServers.Contains(builder.DataSource, StringComparer.OrdinalIgnoreCase))
        {
            throw Invalid("database server is not in the allowed server list");
        }

        if (string.IsNullOrWhiteSpace(builder.InitialCatalog) || !DatabaseName().IsMatch(builder.InitialCatalog)
            || IsSystemDatabase(builder.InitialCatalog))
        {
            throw Invalid("database name is missing or not allowed");
        }

        if (builder.TrustServerCertificate && !_options.AllowTrustServerCertificate)
        {
            throw Invalid("TrustServerCertificate is not allowed");
        }

        builder.ApplicationName = "MultiTenantDemo";

        var databaseKey = $"{builder.DataSource}/{builder.InitialCatalog}".ToLowerInvariant();
        return new TenantInfo(tenant.TenantId, databaseKey, builder.ConnectionString);

        TenantResolutionException Invalid(string reason) =>
            new(TenantResolutionFailure.InvalidConnectionMetadata, $"Tenant {tenant.TenantId}: {reason}.");
    }

    private static bool IsSystemDatabase(string name) =>
        name.Equals("master", StringComparison.OrdinalIgnoreCase)
        || name.Equals("model", StringComparison.OrdinalIgnoreCase)
        || name.Equals("msdb", StringComparison.OrdinalIgnoreCase)
        || name.Equals("tempdb", StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex("^[A-Za-z0-9_-]{1,128}$", RegexOptions.CultureInvariant)]
    internal static partial Regex DatabaseName();
}
