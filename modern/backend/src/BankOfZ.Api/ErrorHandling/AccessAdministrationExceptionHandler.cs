using BankOfZ.Application.AccessAdministration;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BankOfZ.Api.ErrorHandling;

public sealed class AccessAdministrationExceptionHandler(
    IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var details = exception switch
        {
            AccessAdministrationValidationException validation => new ValidationProblemDetails(
                validation.Errors.ToDictionary(error => error.Key, error => error.Value))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Access administration data is invalid"
            },
            AccessAdministrationNotFoundException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "User not found"
            },
            AccessAdministrationConflictException => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "User access changed"
            },
            _ => null
        };
        if (details is null)
        {
            return false;
        }

        httpContext.Response.StatusCode = details.Status!.Value;
        return await problemDetails.TryWriteAsync(new()
        {
            HttpContext = httpContext,
            ProblemDetails = details,
            Exception = exception
        });
    }
}
