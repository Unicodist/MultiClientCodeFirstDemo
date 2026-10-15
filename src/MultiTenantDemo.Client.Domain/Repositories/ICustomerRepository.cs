namespace MultiTenantDemo.Client.Domain.Repositories;

public interface ICustomerRepository
{
    Task<IReadOnlyList<Customer>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);

    Task<CustomerSummary> GetSummaryAsync(DateTime? createdSinceUtc, CancellationToken cancellationToken = default);
}
