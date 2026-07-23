namespace BankOfZ.Infrastructure.Persistence;

public sealed class StatementAuditRecord
{
    public Guid Id { get; set; }
    public Guid? StatementId { get; set; }
    public string? AccountId { get; set; }
    public string Actor { get; set; } = null!;
    public string Action { get; set; } = null!;
    public string Result { get; set; } = null!;
    public string? Diagnostics { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
