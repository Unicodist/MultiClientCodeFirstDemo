namespace MultiTenantDemo.System.Data;

public sealed class SystemDatabaseOptions
{
    public const string SectionName = "SystemDatabase";

    public string? ConnectionString { get; set; }
}
