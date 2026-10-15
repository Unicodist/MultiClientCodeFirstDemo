using Microsoft.AspNetCore.Mvc;
using MultiTenantDemo.Client.Domain;

namespace MultiTenantDemo.Api.Controllers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController : ControllerBase
{
    private readonly ICustomerService _customers;

    public CustomersController(ICustomerService customers)
    {
        _customers = customers;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> List(CancellationToken cancellationToken) =>
        Ok(await _customers.ListAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var created = await _customers.CreateAsync(request, cancellationToken);
        return Created($"/api/customers/{created.Id}", created);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<CustomerSummaryDto>> Summary([FromQuery] DateTime? createdSinceUtc, CancellationToken cancellationToken) =>
        Ok(await _customers.GetSummaryAsync(createdSinceUtc?.ToUniversalTime(), cancellationToken));
}
