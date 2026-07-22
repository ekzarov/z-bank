using System.Data;
using BankOfZ.Application.Accounts;
using BankOfZ.Domain.Accounts;
using BankOfZ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BankOfZ.Infrastructure.Accounts;

public sealed class AccountRepository(BankOfZIdentityContext context) : IAccountRepository
{
    public Task<Account?> FindAsync(string id, bool tracking, CancellationToken cancellationToken)
    {
        var query = tracking ? context.Accounts : context.Accounts.AsNoTracking();
        return query.SingleOrDefaultAsync(account => account.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<Account> Items, int Total)> ListAsync(
        string customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = context.Accounts.AsNoTracking().Where(account => account.CustomerId == customerId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(account => account.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
        return (items, total);
    }

    public Task<int> CountAsync(string customerId, CancellationToken cancellationToken) =>
        context.Accounts.CountAsync(account => account.CustomerId == customerId && account.Status == AccountStatus.Active, cancellationToken);

    public async Task LockCustomerAsync(string customerId, CancellationToken cancellationToken)
    {
        var transaction = context.Database.CurrentTransaction
            ?? throw new InvalidOperationException("A transaction is required before locking a customer.");
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = "SELECT Id FROM Customers WITH (UPDLOCK, HOLDLOCK) WHERE Id = @customerId";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@customerId";
        parameter.Value = customerId;
        command.Parameters.Add(parameter);
        if (await command.ExecuteScalarAsync(cancellationToken) is null)
        {
            throw new InvalidOperationException("Customer disappeared while creating an account.");
        }
    }

    public async Task<T> ExecuteSerializableAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var result = await operation();
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<string> AllocateIdAsync(CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            if (context.Database.CurrentTransaction is { } transaction)
            {
                command.Transaction = transaction.GetDbTransaction();
            }
            command.CommandText = $"SELECT NEXT VALUE FOR dbo.{CatalogModelConstants.Sequences.AccountNumber}";
            var value = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
            return value.ToString($"D{AccountRules.IdLength}");
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    public void Add(Account account) => context.Accounts.Add(account);

    public void SetExpectedVersion(Account account, byte[] version) =>
        context.Entry(account).Property(entity => entity.Version).OriginalValue = version;

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new AccountConflictException("Account was changed by another request.", exception);
        }
    }
}
