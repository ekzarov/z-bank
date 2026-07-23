namespace BankOfZ.Application.Transactions;

public interface ITransactionHistoryRepository
{
    Task<IReadOnlyList<TransactionHistoryRecord>> ListAsync(
        string accountId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        DateTimeOffset? afterBookedAt,
        string? afterReference,
        int take,
        CancellationToken cancellationToken);

    Task<TransactionHistoryRecord?> FindAsync(
        string accountId,
        string reference,
        CancellationToken cancellationToken);
}
