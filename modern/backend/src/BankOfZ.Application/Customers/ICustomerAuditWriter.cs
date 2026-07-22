namespace BankOfZ.Application.Customers;

public interface ICustomerAuditWriter
{
    void Add(CustomerAuditEntry entry);
}
