using System.ComponentModel.DataAnnotations;

namespace MultiTenantDemo.Client.Domain;

public interface ICustomerService
{
    Task<IReadOnlyList<CustomerDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);

    Task<CustomerSummaryDto> GetSummaryAsync(DateTime? createdSinceUtc, CancellationToken cancellationToken = default);
}

public sealed record CustomerDto(int Id, string Name, string Email, bool IsActive, DateTime CreatedUtc);

public sealed record CustomerSummaryDto(int TotalCustomers, int ActiveCustomers, int CreatedSince, DateTime? LatestCreatedUtc);

public sealed class CreateCustomerRequest
{
    [Required]
    [StringLength(Customer.NameMaxLength, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(Customer.EmailMaxLength)]
    public string Email { get; set; } = string.Empty;
}

public sealed class CustomerConflictException : Exception
{
    public CustomerConflictException()
    {
    }

    public CustomerConflictException(string message)
        : base(message)
    {
    }

    public CustomerConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
