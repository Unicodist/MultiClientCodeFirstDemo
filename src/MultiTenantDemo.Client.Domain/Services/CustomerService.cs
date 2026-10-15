using MultiTenantDemo.Client.Domain.Repositories;

namespace MultiTenantDemo.Client.Domain.Services;

public sealed class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customers;
    private readonly TimeProvider _timeProvider;

    public CustomerService(ICustomerRepository customers, TimeProvider timeProvider)
    {
        _customers = customers;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<CustomerDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var customers = await _customers.ListAsync(cancellationToken).ConfigureAwait(false);
        return customers.Select(ToDto).ToList();
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var customer = new Customer(request.Name, request.Email, now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMillisecond)));
        await _customers.AddAsync(customer, cancellationToken).ConfigureAwait(false);
        return ToDto(customer);
    }

    public async Task<CustomerSummaryDto> GetSummaryAsync(DateTime? createdSinceUtc, CancellationToken cancellationToken = default)
    {
        var summary = await _customers.GetSummaryAsync(createdSinceUtc, cancellationToken).ConfigureAwait(false);
        return new CustomerSummaryDto(summary.TotalCustomers, summary.ActiveCustomers, summary.CreatedSince, summary.LatestCreatedUtc);
    }

    private static CustomerDto ToDto(Customer customer) =>
        new(customer.Id, customer.Name, customer.Email, customer.IsActive, customer.CreatedUtc);
}
