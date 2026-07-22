using BankOfZ.Application.Customers;
using BankOfZ.Domain.Accounts;
using BankOfZ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BankOfZ.Infrastructure.Accounts;

public sealed class CustomerAccountStatusReader(BankOfZIdentityContext context) : ICustomerAccountStatusReader
{
    public async Task<CustomerAccountStatus> GetAsync(string customerId, CancellationToken cancellationToken)
    {
        var accounts = context.Accounts.Where(account => account.CustomerId == customerId);
        return new CustomerAccountStatus(
            await accounts.AnyAsync(account => account.Status == AccountStatus.Active, cancellationToken),
            await accounts.AnyAsync(account => account.HasPendingWork, cancellationToken));
    }
}
