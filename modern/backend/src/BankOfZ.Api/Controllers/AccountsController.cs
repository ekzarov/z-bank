using BankOfZ.Api.Contracts;
using BankOfZ.Api.Security;
using BankOfZ.Application.Accounts;
using BankOfZ.Domain.Accounts;
using BankOfZ.Domain.Customers;
using BankOfZ.Domain.Security;
using BankOfZ.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BankOfZ.Api.Controllers;

[ApiController]
[Authorize]
public sealed class AccountsController(
    AccountService accounts,
    UserManager<ApplicationUser> users) : ControllerBase
{
    [HttpGet("api/customers/{customerId}/accounts")]
    public async Task<ActionResult<AccountPage>> List(
        string customerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!ValidId(customerId, CustomerRules.IdLength))
        {
            return InvalidIdentifier("customerId", CustomerRules.IdLength);
        }
        if (!await CanAccessCustomerAsync(customerId))
        {
            return Problem(statusCode: 404, title: "Customer not found");
        }

        return Ok(await accounts.ListAsync(customerId, page, pageSize, cancellationToken));
    }

    [HttpGet("api/accounts/{id}")]
    public async Task<ActionResult<AccountView>> Find(string id, CancellationToken cancellationToken)
    {
        if (!ValidId(id, AccountRules.IdLength))
        {
            return InvalidIdentifier("id", AccountRules.IdLength);
        }
        var account = await accounts.FindAsync(id, cancellationToken);
        if (account is null || !await CanAccessCustomerAsync(account.CustomerId))
        {
            return Problem(statusCode: 404, title: "Account not found");
        }
        return Ok(account);
    }

    [HttpPost("api/customers/{customerId}/accounts")]
    [Authorize(Policy = AuthorizationPolicies.Operator)]
    public async Task<ActionResult<AccountView>> Create(
        string customerId,
        CreateAccountRequest request,
        CancellationToken cancellationToken)
    {
        if (!ValidId(customerId, CustomerRules.IdLength))
        {
            return InvalidIdentifier("customerId", CustomerRules.IdLength);
        }
        var account = await accounts.CreateAsync(
            customerId,
            request.Metadata.ToDomain(),
            request.SourceSystem,
            request.SourceIdentifier,
            request.RawSourceType,
            Actor(),
            HttpContext.TraceIdentifier,
            cancellationToken);
        return CreatedAtAction(nameof(Find), new { id = account.Id }, account);
    }

    [HttpPut("api/accounts/{id}")]
    [Authorize(Policy = AuthorizationPolicies.Operator)]
    public async Task<ActionResult<AccountView>> Update(
        string id,
        UpdateAccountRequest request,
        CancellationToken cancellationToken)
    {
        if (!ValidId(id, AccountRules.IdLength))
        {
            return InvalidIdentifier("id", AccountRules.IdLength);
        }
        return Ok(await accounts.UpdateAsync(
            id,
            request.Metadata.ToDomain(),
            request.Version,
            Actor(),
            HttpContext.TraceIdentifier,
            cancellationToken));
    }

    [HttpPost("api/accounts/{id}/close")]
    [Authorize(Policy = AuthorizationPolicies.Operator)]
    public async Task<IActionResult> Close(
        string id,
        CloseAccountRequest request,
        CancellationToken cancellationToken)
    {
        if (!ValidId(id, AccountRules.IdLength))
        {
            return InvalidIdentifier("id", AccountRules.IdLength);
        }
        await accounts.CloseAsync(id, request.Version, Actor(), HttpContext.TraceIdentifier, cancellationToken);
        return NoContent();
    }

    private async Task<bool> CanAccessCustomerAsync(string customerId)
    {
        if (User.IsInRole(BankRoles.Operator))
        {
            return true;
        }

        var user = await users.GetUserAsync(User);
        return User.IsInRole(BankRoles.Customer) && user?.CustomerId == customerId;
    }

    private string Actor() => User.Identity?.Name ?? "unknown";

    private static bool ValidId(string value, int length) =>
        value.Length == length && value.All(char.IsAsciiDigit);

    private ObjectResult InvalidIdentifier(string field, int length) => BadRequest(new ValidationProblemDetails(
        new Dictionary<string, string[]> { [field] = [$"{field} must contain exactly {length} digits."] })
    {
        Title = "Identifier is invalid",
        Status = StatusCodes.Status400BadRequest
    });
}
