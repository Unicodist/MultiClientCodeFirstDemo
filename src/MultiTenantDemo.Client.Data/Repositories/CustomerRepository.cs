using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MultiTenantDemo.Client.Domain;
using MultiTenantDemo.Client.Domain.Repositories;

namespace MultiTenantDemo.Client.Data.Repositories;

internal sealed class CustomerRepository : ICustomerRepository
{
    private readonly IClientDbContextFactory _contextFactory;

    public CustomerRepository(IClientDbContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<Customer>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.Customers
            .AsNoTracking()
            .OrderBy(c => c.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(customer);

        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        db.Customers.Add(customer);

        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new CustomerConflictException("A customer with this email already exists.", ex);
        }
    }

    public async Task<CustomerSummary> GetSummaryAsync(DateTime? createdSinceUtc, CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var since = new SqlParameter("@CreatedSinceUtc", SqlDbType.DateTime2)
        {
            Value = (object?)createdSinceUtc ?? DBNull.Value,
        };

        var rows = await db.Database
            .SqlQueryRaw<CustomerSummaryRow>("EXEC dbo.GetCustomerSummary @CreatedSinceUtc", since)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var row = rows.Single();
        return new CustomerSummary(row.TotalCustomers, row.ActiveCustomers, row.CreatedSince, row.LatestCreatedUtc);
    }

    private sealed record CustomerSummaryRow(int TotalCustomers, int ActiveCustomers, int CreatedSince, DateTime? LatestCreatedUtc);
}
