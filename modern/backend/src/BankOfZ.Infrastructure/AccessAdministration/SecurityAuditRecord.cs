namespace BankOfZ.Infrastructure.AccessAdministration;

public sealed class SecurityAuditRecord
{
    public long Id { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public required string EventName { get; set; }
    public string? ActorId { get; set; }
    public string? SubjectId { get; set; }
    public bool Succeeded { get; set; }
    public required string Outcome { get; set; }
    public required string CorrelationId { get; set; }
}
