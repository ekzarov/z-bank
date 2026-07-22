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
public sealed class CashTransactionsController(
    CashTransactionService cashTransactions,
    AccountService accounts,
    UserManager<ApplicationUser> users) : ControllerBase
{
    [HttpPost("api/accounts/{id}/deposits")]
    public Task<ActionResult<CashTransactionView>> Deposit(
        string id,
        CashTransactionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken) => Execute(
            id, request, idempotencyKey, true, cancellationToken);

    [HttpPost("api/accounts/{id}/withdrawals")]
    public Task<ActionResult<CashTransactionView>> Withdraw(
        string id,
        CashTransactionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken) => Execute(
            id, request, idempotencyKey, false, cancellationToken);

    private async Task<ActionResult<CashTransactionView>> Execute(
        string id,
        CashTransactionRequest request,
        string? idempotencyKey,
        bool deposit,
        CancellationToken cancellationToken)
    {
        if (id.Length != AccountRules.IdLength || id.Any(character => !char.IsAsciiDigit(character)))
        {
            return CashProblem(400, "Account identifier is invalid", "cash_identifier_invalid");
        }

        var account = await accounts.FindAsync(id, cancellationToken);
        if (account is null || !await CanAccessCustomerAsync(account.CustomerId))
        {
            return CashProblem(404, "Account not found", "cash_account_not_found");
        }

        var result = deposit
            ? await cashTransactions.DepositAsync(
                id, request.Amount, idempotencyKey ?? string.Empty, Actor(), HttpContext.TraceIdentifier, cancellationToken)
            : await cashTransactions.WithdrawAsync(
                id, request.Amount, idempotencyKey ?? string.Empty, Actor(), HttpContext.TraceIdentifier, cancellationToken);
        return Ok(result);
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

    private ObjectResult CashProblem(int status, string title, string code)
    {
        var details = new ProblemDetails { Status = status, Title = title };
        details.Extensions["code"] = code;
        return StatusCode(status, details);
    }
}
