namespace BankOfZ.Application.Customers;

public interface ICustomerAccountStatusReader
{
    Task<CustomerAccountStatus> GetAsync(string customerId, CancellationToken cancellationToken);
}
