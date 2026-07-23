using BankOfZ.Domain.Customers;
using BankOfZ.Domain.Transactions;

namespace BankOfZ.Application.Transactions;

public sealed record TransactionHistoryView(
    string Reference,
    string AccountId,
    CashTransactionDirection Direction,
    decimal Amount,
    string Currency,
    decimal ResultingActualBalance,
    decimal ResultingAvailableBalance,
    string Status,
    string Description,
    DateTimeOffset BookedAt,
    string? TransferCorrelationId,
    string? RelatedTransferReference,
    SourceSystem SourceSystem,
    string? SourceIdentifier)
{
    public static TransactionHistoryView From(TransactionHistoryRecord transaction) => new(
        transaction.Reference,
        transaction.AccountId,
        transaction.Direction,
        transaction.Amount,
        transaction.Currency,
        transaction.ResultingActualBalance,
        transaction.ResultingAvailableBalance,
        "booked",
        BuildDescription(transaction),
        transaction.BookedAt,
        transaction.TransferCorrelationId,
        transaction.RelatedTransferReference,
        transaction.SourceSystem,
        transaction.SourceIdentifier);

    private static string BuildDescription(TransactionHistoryRecord transaction) =>
        transaction.TransferCorrelationId is not null
            ? transaction.Direction == CashTransactionDirection.Withdrawal
                ? "Internal transfer sent"
                : "Internal transfer received"
            : transaction.Direction == CashTransactionDirection.Withdrawal
                ? "Cash withdrawal"
                : "Cash deposit";
}
