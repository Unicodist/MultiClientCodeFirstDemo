namespace MultiTenantDemo.System.Domain;

public sealed class TenantInfo
{
    public TenantInfo(string tenantId, string databaseKey, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        TenantId = tenantId;
        DatabaseKey = databaseKey;
        ConnectionString = connectionString;
    }

    public string TenantId { get; }

    public string DatabaseKey { get; }

    public string ConnectionString { get; }

    public override string ToString() => $"Tenant {TenantId} ({DatabaseKey})";
}
