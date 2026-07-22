namespace BankOfZ.Application.Customers;

public sealed record CustomerAuditEntry(
    string Actor,
    DateTimeOffset Timestamp,
    string Action,
    string CustomerId,
    string Result,
    string CorrelationId);
