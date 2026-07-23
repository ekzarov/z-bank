using BankOfZ.Application.AccessAdministration;
using BankOfZ.Application.Common;
using BankOfZ.Infrastructure.Persistence;

namespace BankOfZ.Infrastructure.AccessAdministration;

public sealed class SecurityAuditWriter(
    BankOfZIdentityContext context,
    IClock clock) : ISecurityAuditWriter
{
    public async Task WriteAsync(
        SecurityAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        context.SecurityAuditEntries.Add(new SecurityAuditRecord
        {
            OccurredAt = clock.UtcNow,
            EventName = entry.EventName,
            ActorId = entry.ActorId,
            SubjectId = entry.SubjectId,
            Succeeded = entry.Succeeded,
            Outcome = entry.Outcome,
            CorrelationId = entry.CorrelationId
        });
        await context.SaveChangesAsync(cancellationToken);
    }
}
