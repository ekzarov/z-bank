using BankOfZ.Domain.Transactions;
using BankOfZ.Domain.Customers;

namespace BankOfZ.Application.Transactions;

public sealed record CashTransactionView(
    string Reference,
    string AccountId,
    CashTransactionDirection Direction,
    decimal Amount,
    string Currency,
    decimal ActualBalance,
    decimal AvailableBalance,
    SourceSystem SourceSystem,
    DateTimeOffset BookedAt)
{
    public static CashTransactionView From(BookedTransaction transaction) => new(
        transaction.Reference,
        transaction.AccountId,
        transaction.Direction,
        transaction.Amount,
        transaction.Currency,
        transaction.ResultingActualBalance,
        transaction.ResultingAvailableBalance,
        transaction.SourceSystem,
        transaction.CreatedAt);
}
