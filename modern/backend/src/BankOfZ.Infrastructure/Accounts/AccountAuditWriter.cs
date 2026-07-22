using BankOfZ.Application.Accounts;
using BankOfZ.Infrastructure.Persistence;

namespace BankOfZ.Infrastructure.Accounts;

public sealed class AccountAuditWriter(BankOfZIdentityContext context) : IAccountAuditWriter
{
    public void Add(AccountAuditEntry entry) => context.AccountAuditEntries.Add(new AccountAuditRecord
    {
        Actor = entry.Actor,
        Timestamp = entry.Timestamp,
        Action = entry.Action,
        AccountId = entry.AccountId,
        CustomerId = entry.CustomerId,
        Result = "Succeeded",
        CorrelationId = entry.CorrelationId
    });
}
