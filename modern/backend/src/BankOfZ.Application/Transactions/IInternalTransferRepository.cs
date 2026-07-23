using BankOfZ.Domain.Accounts;
using BankOfZ.Domain.Transactions;

namespace BankOfZ.Application.Transactions;

public interface IInternalTransferRepository
{
    Task<T> ExecuteSerializableAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken);
    Task<bool> LockAccountAsync(string accountId, CancellationToken cancellationToken);
    Task<Account?> FindAccountAsync(string accountId, CancellationToken cancellationToken);
    Task<BookedTransaction?> FindByIdempotencyAsync(
        string sourceAccountId,
        string idempotencyKey,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<BookedTransaction>> FindTransferAsync(
        string correlationId,
        CancellationToken cancellationToken);
    void AddRange(BookedTransaction source, BookedTransaction destination);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
