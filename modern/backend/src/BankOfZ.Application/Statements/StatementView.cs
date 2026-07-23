using BankOfZ.Domain.Statements;
using BankOfZ.Domain.Transactions;

namespace BankOfZ.Application.Statements;

public sealed record StatementTransactionView(
    DateTimeOffset BookedAt,
    CashTransactionDirection Direction,
    string Reference,
    string Description,
    decimal Amount);

public sealed record StatementView(
    Guid GenerationId,
    string AccountId,
    string CustomerId,
    int Year,
    int Month,
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc,
    DateOnly StatementDate,
    DateTimeOffset GeneratedAt,
    DateTimeOffset DataAsOf,
    string CustomerName,
    string CustomerAddress,
    string? CustomerPhone,
    string SortCode,
    string AccountType,
    string Currency,
    decimal InterestRate,
    int OverdraftLimit,
    decimal OpeningBalance,
    decimal TotalCredits,
    decimal TotalDebits,
    decimal ClosingBalance,
    decimal AvailableBalance,
    int TransactionCount,
    IReadOnlyList<StatementTransactionView> Transactions)
{
    public static StatementView From(StatementSnapshot statement) => new(
        statement.Id,
        statement.AccountId,
        statement.CustomerId,
        statement.Year,
        statement.Month,
        statement.PeriodStartUtc,
        statement.PeriodEndUtc,
        statement.StatementDate,
        statement.GeneratedAt,
        statement.DataAsOf,
        statement.CustomerName,
        statement.CustomerAddress,
        statement.CustomerPhone,
        statement.SortCode,
        statement.AccountType,
        statement.Currency,
        statement.InterestRate,
        statement.OverdraftLimit,
        statement.OpeningBalance,
        statement.TotalCredits,
        statement.TotalDebits,
        statement.ClosingBalance,
        statement.AvailableBalance,
        statement.TransactionCount,
        statement.Transactions
            .OrderBy(transaction => transaction.Sequence)
            .Select(transaction => new StatementTransactionView(
                transaction.BookedAt,
                transaction.Direction,
                transaction.Reference,
                transaction.Description,
                transaction.Amount))
            .ToArray());
}
