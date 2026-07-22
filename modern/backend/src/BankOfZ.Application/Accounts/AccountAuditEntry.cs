namespace BankOfZ.Application.Accounts;

public sealed record AccountAuditEntry(
    string Actor,
    DateTimeOffset Timestamp,
    string Action,
    string AccountId,
    string CustomerId,
    string CorrelationId);
