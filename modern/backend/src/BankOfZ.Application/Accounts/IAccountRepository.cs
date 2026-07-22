using BankOfZ.Domain.Accounts;

namespace BankOfZ.Application.Accounts;

public interface IAccountRepository
{
    Task<Account?> FindAsync(string id, bool tracking, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Account> Items, int Total)> ListAsync(string customerId, int page, int pageSize, CancellationToken cancellationToken);
    Task<int> CountAsync(string customerId, CancellationToken cancellationToken);
    Task LockCustomerAsync(string customerId, CancellationToken cancellationToken);
    Task<string> AllocateIdAsync(CancellationToken cancellationToken);
    Task<T> ExecuteSerializableAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken);
    void Add(Account account);
    void SetExpectedVersion(Account account, byte[] version);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
