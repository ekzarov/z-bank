namespace BankOfZ.Application.Accounts;

public interface IAccountAuditWriter
{
    void Add(AccountAuditEntry entry);
}
