using BankOfZ.Domain.Customers;
using BankOfZ.Domain.Transactions;

namespace BankOfZ.Application.Transactions;

public sealed record TransactionHistoryRecord(
    string Reference,
    string AccountId,
    CashTransactionDirection Direction,
    decimal Amount,
    string Currency,
    decimal ResultingActualBalance,
    decimal ResultingAvailableBalance,
    DateTimeOffset BookedAt,
    string? TransferCorrelationId,
    string? RelatedTransferReference,
    SourceSystem SourceSystem,
    string? SourceIdentifier);
