using BankOfZ.Api.Contracts;
using BankOfZ.Application.Accounts;
using BankOfZ.Application.Statements;
using BankOfZ.Domain.Accounts;
using BankOfZ.Domain.Security;
using BankOfZ.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BankOfZ.Api.Controllers;

[ApiController]
[Authorize]
public sealed class StatementsController(
    StatementService statements,
    AccountService accounts,
    UserManager<ApplicationUser> users) : ControllerBase
{
    [HttpPost("api/accounts/{id}/statements")]
    public async Task<ActionResult<StatementView>> Generate(
        string id,
        GenerateStatementRequest request,
        CancellationToken cancellationToken)
    {
        if (!ValidAccountId(id))
        {
            return StatementProblem(400, "Account identifier is invalid", "statement_identifier_invalid");
        }
        if (!await CanAccessAccountAsync(id, cancellationToken))
        {
            return StatementProblem(404, "Account not found", "statement_account_not_found");
        }

        var (statement, reused) = await statements.GenerateAsync(
            id,
            request.Year,
            request.Month,
            Actor(),
            cancellationToken);
        Response.Headers["X-Statement-Reused"] = reused ? "true" : "false";
        return reused
            ? Ok(statement)
            : CreatedAtAction(nameof(Find), new { id, statementId = statement.GenerationId }, statement);
    }

    [HttpGet("api/accounts/{id}/statements/{statementId:guid}")]
    public async Task<ActionResult<StatementView>> Find(
        string id,
        Guid statementId,
        CancellationToken cancellationToken)
    {
        if (!ValidAccountId(id) || !await CanAccessAccountAsync(id, cancellationToken))
        {
            return StatementProblem(404, "Statement not found", "statement_not_found");
        }

        return Ok(await statements.FindAsync(id, statementId, Actor(), cancellationToken));
    }

    [HttpPost("api/statements/bulk")]
    [Authorize(Roles = BankRoles.Operator)]
    public async Task<ActionResult<BulkStatementResult>> GenerateBulk(
        GenerateStatementRequest request,
        CancellationToken cancellationToken) =>
        Ok(await statements.GenerateBulkAsync(
            request.Year,
            request.Month,
            request.AccountIds,
            Actor(),
            cancellationToken));

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

    private string Actor() => User.Identity?.Name ?? "unknown";

    private static bool ValidAccountId(string id) =>
        id.Length == AccountRules.IdLength && id.All(char.IsAsciiDigit);

    private ObjectResult StatementProblem(int status, string title, string code) =>
        Problem(
            statusCode: status,
            title: title,
            extensions: new Dictionary<string, object?> { ["code"] = code });
}
