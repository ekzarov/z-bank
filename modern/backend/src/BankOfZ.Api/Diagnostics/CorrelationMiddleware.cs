using System.Diagnostics;
using System.Text.RegularExpressions;

namespace BankOfZ.Api.Diagnostics;

public sealed partial class CorrelationMiddleware(
    RequestDelegate next,
    ILogger<CorrelationMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var supplied = context.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = supplied is not null && ValidCorrelationId().IsMatch(supplied)
            ? supplied
            : Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });
        var started = Stopwatch.GetTimestamp();

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            try
            {
                await next(context);
            }
            finally
            {
                var level = context.Response.StatusCode >= StatusCodes.Status500InternalServerError
                    ? LogLevel.Error
                    : LogLevel.Information;
                logger.Log(
                    level,
                    "HTTP {Method} {Path} completed with {StatusCode} in {ElapsedMilliseconds} ms; correlation {CorrelationId}",
                    context.Request.Method,
                    context.Request.Path.Value,
                    context.Response.StatusCode,
                    Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                    correlationId);
            }
        }
    }

    [GeneratedRegex(@"^[A-Za-z0-9_-]{1,64}$", RegexOptions.CultureInvariant)]
    private static partial Regex ValidCorrelationId();
}
