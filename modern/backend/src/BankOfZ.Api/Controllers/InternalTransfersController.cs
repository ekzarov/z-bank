using BankOfZ.Api.Contracts;
using BankOfZ.Application.Accounts;
using BankOfZ.Application.Transactions;
using BankOfZ.Domain.Accounts;
using BankOfZ.Domain.Security;
using BankOfZ.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BankOfZ.Api.Controllers;

[ApiController]
[Authorize]
public sealed class InternalTransfersController(
    InternalTransferService transfers,
    AccountService accounts,
    UserManager<ApplicationUser> users) : ControllerBase
{
    [HttpPost("api/accounts/{sourceAccountId}/transfers")]
    public async Task<ActionResult<InternalTransferView>> Transfer(
        string sourceAccountId,
        InternalTransferRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!ValidId(sourceAccountId) || !ValidId(request.DestinationAccountId))
        {
            return TransferProblem(400, "Account identifier is invalid", "transfer_identifier_invalid");
        }

        var source = await accounts.FindAsync(sourceAccountId, cancellationToken);
        var destination = await accounts.FindAsync(request.DestinationAccountId, cancellationToken);
        if (source is null || destination is null ||
            !await CanAccessCustomerAsync(source.CustomerId) ||
            !await CanAccessCustomerAsync(destination.CustomerId))
        {
            return TransferProblem(404, "Account not found", "transfer_account_not_found");
        }

        return Ok(await transfers.TransferAsync(
            sourceAccountId,
            request.DestinationAccountId,
            request.Amount,
            idempotencyKey ?? string.Empty,
            User.Identity?.Name ?? "unknown",
            cancellationToken));
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

    private static bool ValidId(string id) =>
        id.Length == AccountRules.IdLength && id.All(char.IsAsciiDigit);

    private ObjectResult TransferProblem(int status, string title, string code)
    {
        var details = new ProblemDetails { Status = status, Title = title };
        details.Extensions["code"] = code;
        return StatusCode(status, details);
    }
}
