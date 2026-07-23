using BankOfZ.Application.Statements;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BankOfZ.Api.ErrorHandling;

public sealed class StatementExceptionHandler(IProblemDetailsService problemDetails)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, code, errors) = exception switch
        {
            StatementValidationException validation =>
                (400, "Statement request is invalid", validation.Code, validation.Errors),
            StatementNotFoundException =>
                (404, "Statement not found", "statement_not_found", null),
            StatementGenerationException =>
                (503, "Statement generation failed", "statement_generation_failed", null),
            _ => (0, string.Empty, string.Empty, null)
        };
        if (status == 0)
        {
            return false;
        }

        context.Response.StatusCode = status;
        var details = new ProblemDetails
        {
            Status = status,
            Title = title
        };
        details.Extensions["code"] = code;
        if (errors is not null)
        {
            details.Extensions["errors"] = errors;
        }
        return await problemDetails.TryWriteAsync(new()
        {
            HttpContext = context,
            ProblemDetails = details,
            Exception = exception
        });
    }
}
