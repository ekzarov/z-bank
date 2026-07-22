using BankOfZ.Application.Customers;
using BankOfZ.Infrastructure.Persistence;

namespace BankOfZ.Infrastructure.Customers;

public sealed class CustomerAuditWriter(BankOfZIdentityContext context) : ICustomerAuditWriter
{
    public void Add(CustomerAuditEntry entry) => context.CustomerAuditEntries.Add(new CustomerAuditRecord
    {
        Actor = entry.Actor,
        Timestamp = entry.Timestamp,
        Action = entry.Action,
        CustomerId = entry.CustomerId,
        Result = entry.Result,
        CorrelationId = entry.CorrelationId
    });
}
