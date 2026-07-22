using BankOfZ.Application.Accounts;
using BankOfZ.Domain.Accounts;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BankOfZ.Api.ErrorHandling;

public sealed class AccountExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var details = exception switch
        {
            AccountValidationException validation => new ValidationProblemDetails(
                validation.Errors.ToDictionary(error => error.Key, error => error.Value))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Account data is invalid"
            },
            AccountNotFoundException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Account not found"
            },
            AccountConflictException => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Account changed"
            },
            AccountLimitException => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Account limit reached",
                Detail = "A customer can have at most ten active accounts."
            },
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
}
