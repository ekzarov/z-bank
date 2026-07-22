namespace BankOfZ.Api.Security;

public sealed class SecurityAudit(ILogger<SecurityAudit> logger) : ISecurityAudit
{
    public void Record(string eventName, string? subjectId, bool succeeded, string outcome)
    {
        logger.LogInformation(
            "SecurityEvent {EventName} SubjectId={SubjectId} Succeeded={Succeeded} Outcome={Outcome}",
            eventName,
            subjectId ?? "anonymous",
            succeeded,
            outcome);
    }
}
