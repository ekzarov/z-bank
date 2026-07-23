using BankOfZ.Domain.Transactions;

namespace BankOfZ.Application.Statements;

public sealed record StatementSourceTransaction(
    Guid Id,
    string Reference,
    CashTransactionDirection Direction,
    decimal Amount,
    decimal ResultingActualBalance,
    DateTimeOffset BookedAt);

public sealed record StatementSourceData(
    string AccountId,
    string CustomerId,
    string SortCode,
    string AccountType,
    string Currency,
    decimal InterestRate,
    int OverdraftLimit,
    decimal ActualBalance,
    decimal AvailableBalance,
    DateTimeOffset AccountUpdatedAt,
    string CustomerName,
    string CustomerAddress,
    string? CustomerPhone,
    DateTimeOffset CustomerUpdatedAt,
    decimal? PriorClosingBalance,
    IReadOnlyList<StatementSourceTransaction> Transactions);
