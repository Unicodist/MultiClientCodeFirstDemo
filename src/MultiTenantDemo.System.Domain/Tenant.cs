namespace MultiTenantDemo.System.Domain;

public sealed class Tenant
{
    public string TenantId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string ConnectionString { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedUtc { get; set; }

    public override string ToString() => $"Tenant {TenantId} ({(IsActive ? "active" : "inactive")})";
}
