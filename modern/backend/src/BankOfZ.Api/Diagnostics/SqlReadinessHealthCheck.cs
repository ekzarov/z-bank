using BankOfZ.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BankOfZ.Api.Diagnostics;

public sealed class SqlReadinessHealthCheck(
    IServiceScopeFactory scopeFactory,
    ILogger<SqlReadinessHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>().Database;
            return await database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Required SQL connectivity is unavailable.");
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "SQL readiness check failed");
            return HealthCheckResult.Unhealthy("Required SQL connectivity is unavailable.");
        }
    }
}
