namespace BankOfZ.Application.AccessAdministration;

public sealed record SecurityAuditView(
    long Id,
    DateTimeOffset OccurredAt,
    string EventName,
    string? ActorId,
    string? SubjectId,
    bool Succeeded,
    string Outcome,
    string CorrelationId);
