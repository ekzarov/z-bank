namespace BankOfZ.Application.Transactions;

public sealed record HistoryCursor(DateTimeOffset BookedAt, string Reference);
