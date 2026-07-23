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
public sealed class TransactionHistoryController(
    TransactionHistoryService history,
    AccountService accounts,
    UserManager<ApplicationUser> users) : ControllerBase
{
    [HttpGet("api/accounts/{id}/transactions")]
    public async Task<ActionResult<TransactionHistoryPage>> List(
        string id,
        [FromQuery] string? from = null,
        [FromQuery] string? to = null,
        [FromQuery] int pageSize = TransactionHistoryService.DefaultPageSize,
        [FromQuery] string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        if (!ValidAccountId(id))
        {
            return HistoryProblem(400, "Account identifier is invalid", "history_identifier_invalid");
        }
        if (!await CanAccessAccountAsync(id, cancellationToken))
        {
            return HistoryProblem(404, "Account not found", "history_account_not_found");
        }

        return Ok(await history.ListAsync(id, from, to, pageSize, cursor, cancellationToken));
    }

    [HttpGet("api/accounts/{id}/transactions/{reference}")]
    public async Task<ActionResult<TransactionHistoryView>> Find(
        string id,
        string reference,
        CancellationToken cancellationToken)
    {
        if (!ValidAccountId(id) || string.IsNullOrWhiteSpace(reference))
        {
            return HistoryProblem(400, "Transaction identifier is invalid", "history_identifier_invalid");
        }
        if (!await CanAccessAccountAsync(id, cancellationToken))
        {
            return HistoryProblem(404, "Transaction not found", "transaction_not_found");
        }

        return Ok(await history.FindAsync(id, reference, cancellationToken));
    }

    private async Task<bool> CanAccessAccountAsync(string id, CancellationToken cancellationToken)
    {
        var account = await accounts.FindAsync(id, cancellationToken);
        if (account is null)
        {
            return false;
        }
        if (User.IsInRole(BankRoles.Operator))
        {
            return true;
        }

        var user = await users.GetUserAsync(User);
        return User.IsInRole(BankRoles.Customer) && user?.CustomerId == account.CustomerId;
    }

    private static bool ValidAccountId(string id) =>
        id.Length == AccountRules.IdLength && id.All(char.IsAsciiDigit);

    private ObjectResult HistoryProblem(int status, string title, string code)
        => Problem(
            statusCode: status,
            title: title,
            extensions: new Dictionary<string, object?> { ["code"] = code });
}
