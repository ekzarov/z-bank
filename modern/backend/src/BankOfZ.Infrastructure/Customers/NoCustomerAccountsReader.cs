using BankOfZ.Application.Customers;

namespace BankOfZ.Infrastructure.Customers;

public sealed class NoCustomerAccountsReader : ICustomerAccountStatusReader
{
    public Task<CustomerAccountStatus> GetAsync(string customerId, CancellationToken cancellationToken) =>
        Task.FromResult(new CustomerAccountStatus(false, false));
}
