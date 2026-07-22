using System.Data;
using BankOfZ.Application.Transactions;
using BankOfZ.Domain.Accounts;
using BankOfZ.Domain.Transactions;
using BankOfZ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BankOfZ.Infrastructure.Transactions;

public sealed class CashTransactionRepository(BankOfZIdentityContext context) : ICashTransactionRepository
{
    public async Task<T> ExecuteSerializableAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var result = await operation();
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<bool> LockAccountAsync(string accountId, CancellationToken cancellationToken)
    {
        var transaction = context.Database.CurrentTransaction
            ?? throw new InvalidOperationException("A transaction is required before locking an account.");
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = "SELECT Id FROM Accounts WITH (UPDLOCK, HOLDLOCK) WHERE Id = @accountId";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@accountId";
        parameter.Value = accountId;
        command.Parameters.Add(parameter);
        return await command.ExecuteScalarAsync(cancellationToken) is not null;
    }

    public Task<Account?> FindAccountAsync(string accountId, CancellationToken cancellationToken) =>
        context.Accounts.SingleOrDefaultAsync(account => account.Id == accountId, cancellationToken);

    public Task<BookedTransaction?> FindByIdempotencyAsync(
        string accountId,
        string idempotencyKey,
        CancellationToken cancellationToken) => context.BookedTransactions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                transaction => transaction.AccountId == accountId && transaction.IdempotencyKey == idempotencyKey,
                cancellationToken);

    public void Add(BookedTransaction transaction) => context.BookedTransactions.Add(transaction);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new CashTransactionConflictException("cash_concurrency_conflict", "The account changed during booking.", exception);
        }
    }
}
