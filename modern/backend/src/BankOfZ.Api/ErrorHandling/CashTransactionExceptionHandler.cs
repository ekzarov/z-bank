using BankOfZ.Application.Transactions;
using BankOfZ.Domain.Transactions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BankOfZ.Api.ErrorHandling;

public sealed class CashTransactionExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var details = exception switch
        {
            CashTransactionValidationException validation => WithCode(new ValidationProblemDetails(
                validation.Errors.ToDictionary(error => error.Key, error => error.Value))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Cash transaction is invalid"
            }, validation.Code),
            CashTransactionNotFoundException => WithCode(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Account not found"
            }, "cash_account_not_found"),
            CashTransactionConflictException conflict => WithCode(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Cash transaction conflict",
                Detail = conflict.Message
            }, conflict.Code),
            _ => null
        };

        if (details is null)
        {
            return false;
        }

        httpContext.Response.StatusCode = details.Status!.Value;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = details,
            Exception = exception
        });
    }

    private static T WithCode<T>(T details, string code) where T : ProblemDetails
    {
        details.Extensions["code"] = code;
        return details;
    }
}
