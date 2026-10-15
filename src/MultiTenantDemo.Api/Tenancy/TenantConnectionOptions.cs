namespace MultiTenantDemo.Api.Tenancy;

public sealed class TenantConnectionOptions
{
    public const string SectionName = "TenantConnections";

    public IList<string> AllowedServers { get; } = new List<string>();

    public bool AllowTrustServerCertificate { get; set; }
}
