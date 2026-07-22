using BankOfZ.Domain.Customers;

namespace BankOfZ.Application.Customers;

public interface ICustomerRepository
{
    Task<Customer?> FindAsync(string id, bool tracking, CancellationToken cancellationToken);
    Task<IReadOnlyList<Customer>> SearchAsync(string normalizedName, int page, int pageSize, CancellationToken cancellationToken);
    Task<string> AllocateIdAsync(CancellationToken cancellationToken);
    void Add(Customer customer);
    void SetExpectedVersion(Customer customer, byte[] version);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
