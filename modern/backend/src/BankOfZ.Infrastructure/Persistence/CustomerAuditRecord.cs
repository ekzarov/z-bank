namespace BankOfZ.Infrastructure.Persistence;

public sealed class CustomerAuditRecord
{
    public long Id { get; set; }
    public string Actor { get; set; } = null!;
    public DateTimeOffset Timestamp { get; set; }
    public string Action { get; set; } = null!;
    public string CustomerId { get; set; } = null!;
    public string Result { get; set; } = null!;
    public string CorrelationId { get; set; } = null!;
}
