using BankOfZ.Api.Contracts;
using BankOfZ.Api.Security;
using BankOfZ.Application.Customers;
using BankOfZ.Domain.Security;
using BankOfZ.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BankOfZ.Api.Controllers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController(
    CustomerService customers,
    UserManager<ApplicationUser> users) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.Operator)]
    public async Task<ActionResult<IReadOnlyList<CustomerView>>> Search(
        [FromQuery] string name,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await customers.SearchAsync(name, page, pageSize, cancellationToken));

    [HttpGet("{id}")]
    [Authorize(Policy = AuthorizationPolicies.Operator)]
    public async Task<ActionResult<CustomerView>> Find(string id, CancellationToken cancellationToken)
    {
        var customer = await customers.FindAsync(id, cancellationToken);
        return customer is null ? Problem(statusCode: 404, title: "Customer not found") : Ok(customer);
    }

    [HttpGet("me")]
    [Authorize(Policy = AuthorizationPolicies.Customer)]
    public async Task<ActionResult<CustomerView>> Me(CancellationToken cancellationToken)
    {
        var customerId = (await users.GetUserAsync(User))?.CustomerId;
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return Problem(statusCode: 404, title: "Customer not found");
        }

        var customer = await customers.FindAsync(customerId, cancellationToken);
        return customer is null ? Problem(statusCode: 404, title: "Customer not found") : Ok(customer);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.Operator)]
    public async Task<ActionResult<CustomerView>> Create(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = await customers.CreateAsync(
            request.Details.ToDomain(),
            request.SourceSystem,
            request.SourceIdentifier,
            Actor(),
            HttpContext.TraceIdentifier,
            cancellationToken);
        return CreatedAtAction(nameof(Find), new { id = customer.Id }, customer);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.Operator)]
    public async Task<ActionResult<CustomerView>> Update(
        string id,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken) =>
        Ok(await customers.UpdateAsync(
            id,
            request.Details.ToDomain(),
            request.Version,
            Actor(),
            HttpContext.TraceIdentifier,
            cancellationToken));

    [HttpPost("{id}/retire")]
    [Authorize(Policy = AuthorizationPolicies.Operator)]
    public async Task<IActionResult> Retire(
        string id,
        RetireCustomerRequest request,
        CancellationToken cancellationToken)
    {
        await customers.RetireAsync(id, request.Version, Actor(), HttpContext.TraceIdentifier, cancellationToken);
        return NoContent();
    }

    private string Actor() => User.Identity?.Name ?? "unknown";
}
