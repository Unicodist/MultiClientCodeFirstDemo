namespace MultiTenantDemo.System.Domain;

public sealed class TenantResolutionException : Exception
{
    public TenantResolutionException(TenantResolutionFailure reason, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Reason = reason;
    }

    public TenantResolutionException()
        : this(TenantResolutionFailure.Missing, "Tenant could not be resolved.")
    {
    }

    public TenantResolutionException(string message)
        : this(TenantResolutionFailure.Missing, message)
    {
    }

    public TenantResolutionException(string message, Exception innerException)
        : this(TenantResolutionFailure.Missing, message, innerException)
    {
    }

    public TenantResolutionFailure Reason { get; }
}

public enum TenantResolutionFailure
{
    Missing,

    Malformed,

    Unknown,

    Inactive,

    RegistryUnavailable,

    InvalidConnectionMetadata,
}
