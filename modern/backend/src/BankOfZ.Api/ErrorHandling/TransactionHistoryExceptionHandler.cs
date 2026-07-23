using BankOfZ.Application.Transactions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BankOfZ.Api.ErrorHandling;

public sealed class TransactionHistoryExceptionHandler(IProblemDetailsService problemDetails)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var details = exception switch
        {
            TransactionHistoryValidationException validation => WithCode(new ValidationProblemDetails(
                validation.Errors.ToDictionary(error => error.Key, error => error.Value))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Transaction history query is invalid"
            }, validation.Code),
            TransactionHistoryNotFoundException => WithCode(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Transaction not found"
            }, "transaction_not_found"),
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
