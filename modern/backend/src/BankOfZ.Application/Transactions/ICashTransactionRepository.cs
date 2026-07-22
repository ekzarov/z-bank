using BankOfZ.Domain.Accounts;
using BankOfZ.Domain.Transactions;

namespace BankOfZ.Application.Transactions;

public interface ICashTransactionRepository
{
    Task<T> ExecuteSerializableAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken);
    Task<bool> LockAccountAsync(string accountId, CancellationToken cancellationToken);
    Task<Account?> FindAccountAsync(string accountId, CancellationToken cancellationToken);
    Task<BookedTransaction?> FindByIdempotencyAsync(string accountId, string idempotencyKey, CancellationToken cancellationToken);
    void Add(BookedTransaction transaction);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
