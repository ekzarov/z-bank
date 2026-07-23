namespace BankOfZ.Application.AccessAdministration;

public sealed record SecurityAuditEntry(
    string EventName,
    string? ActorId,
    string? SubjectId,
    bool Succeeded,
    string Outcome,
    string CorrelationId);
