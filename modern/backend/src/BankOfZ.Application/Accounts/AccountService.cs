using BankOfZ.Application.Common;
using BankOfZ.Application.Customers;
using BankOfZ.Domain.Accounts;
using BankOfZ.Domain.Customers;

namespace BankOfZ.Application.Accounts;

public sealed class AccountService(
    IAccountRepository repository,
    IAccountAuditWriter auditWriter,
    ICustomerRepository customers,
    IClock clock,
    AccountOptions options)
{
    public async Task<AccountPage> ListAsync(string customerId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var boundedPage = Math.Max(page, 1);
        var boundedSize = Math.Clamp(pageSize, 1, 50);
        var result = await repository.ListAsync(customerId, boundedPage, boundedSize, cancellationToken);
        return new AccountPage(result.Items.Select(AccountView.From).ToArray(), boundedPage, boundedSize, result.Total);
    }

    public async Task<AccountView?> FindAsync(string id, CancellationToken cancellationToken) =>
        await repository.FindAsync(id, false, cancellationToken) is { } account ? AccountView.From(account) : null;

    public async Task<AccountView> CreateAsync(
        string customerId,
        AccountMetadata metadata,
        SourceSystem sourceSystem,
        string? sourceIdentifier,
        string? rawSourceType,
        string actor,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (await customers.FindAsync(customerId, false, cancellationToken) is null)
        {
            throw new CustomerNotFoundException(customerId);
        }
        return await repository.ExecuteSerializableAsync(async () =>
        {
            await repository.LockCustomerAsync(customerId, cancellationToken);
            if (await repository.CountAsync(customerId, cancellationToken) >= AccountRules.MaximumAccountsPerCustomer)
            {
                throw new AccountLimitException();
            }

            var now = clock.UtcNow;
            var id = await repository.AllocateIdAsync(cancellationToken);
            var account = Account.Create(id, customerId, options.SortCode, metadata, sourceSystem, sourceIdentifier, rawSourceType, now);
            repository.Add(account);
            auditWriter.Add(new AccountAuditEntry(actor, now, "AccountCreated", id, customerId, correlationId));
            await repository.SaveChangesAsync(cancellationToken);
            return AccountView.From(account);
        }, cancellationToken);
    }

    public async Task<AccountView> UpdateAsync(
        string id,
        AccountMetadata metadata,
        string version,
        string actor,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var account = await RequiredAsync(id, cancellationToken);
        repository.SetExpectedVersion(account, DecodeVersion(version));
        var now = clock.UtcNow;
        account.UpdateMetadata(metadata, now);
        auditWriter.Add(new AccountAuditEntry(actor, now, "AccountUpdated", id, account.CustomerId, correlationId));
        await repository.SaveChangesAsync(cancellationToken);
        return AccountView.From(account);
    }

    public async Task CloseAsync(
        string id,
        string version,
        string actor,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var account = await RequiredAsync(id, cancellationToken);
        repository.SetExpectedVersion(account, DecodeVersion(version));
        var now = clock.UtcNow;
        account.Close(now);
        auditWriter.Add(new AccountAuditEntry(actor, now, "AccountClosed", id, account.CustomerId, correlationId));
        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<Account> RequiredAsync(string id, CancellationToken cancellationToken) =>
        await repository.FindAsync(id, true, cancellationToken) ?? throw new AccountNotFoundException();

    private static byte[] DecodeVersion(string version)
    {
        try
        {
            return Convert.FromBase64String(version);
        }
        catch (FormatException exception)
        {
            throw new AccountConflictException("Account version is invalid.", exception);
        }
    }
}
