using BankOfZ.Domain.Customers;

namespace BankOfZ.Domain.Transactions;

public sealed class BookedTransaction
{
    private BookedTransaction()
    {
    }

    public Guid Id { get; private set; }
    public string Reference { get; private set; } = null!;
    public string AccountId { get; private set; } = null!;
    public string CustomerId { get; private set; } = null!;
    public CashTransactionDirection Direction { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = null!;
    public decimal ResultingActualBalance { get; private set; }
    public decimal ResultingAvailableBalance { get; private set; }
    public string IdempotencyKey { get; private set; } = null!;
    public string RequestFingerprint { get; private set; } = null!;
    public string? TransferCorrelationId { get; private set; }
    public SourceSystem SourceSystem { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static BookedTransaction Create(
        string reference,
        string accountId,
        string customerId,
        CashTransactionDirection direction,
        decimal amount,
        string currency,
        decimal resultingActualBalance,
        decimal resultingAvailableBalance,
        string idempotencyKey,
        string requestFingerprint,
        DateTimeOffset createdAt,
        string? transferCorrelationId = null) => new()
        {
            Id = Guid.NewGuid(),
            Reference = reference,
            AccountId = accountId,
            CustomerId = customerId,
            Direction = direction,
            Amount = amount,
            Currency = currency,
            ResultingActualBalance = resultingActualBalance,
            ResultingAvailableBalance = resultingAvailableBalance,
            IdempotencyKey = idempotencyKey,
            RequestFingerprint = requestFingerprint,
            TransferCorrelationId = transferCorrelationId,
            SourceSystem = SourceSystem.Modern,
            CreatedAt = createdAt
        };
}
