using System.Data;
using BankOfZ.Application.Transactions;
using BankOfZ.Domain.Accounts;
using BankOfZ.Domain.Transactions;
using BankOfZ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BankOfZ.Infrastructure.Transactions;

public sealed class InternalTransferRepository(BankOfZIdentityContext context) : IInternalTransferRepository
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
        string sourceAccountId,
        string idempotencyKey,
        CancellationToken cancellationToken) => context.BookedTransactions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                transaction => transaction.AccountId == sourceAccountId && transaction.IdempotencyKey == idempotencyKey,
                cancellationToken);

    public async Task<IReadOnlyList<BookedTransaction>> FindTransferAsync(
        string correlationId,
        CancellationToken cancellationToken) => await context.BookedTransactions
            .AsNoTracking()
            .Where(transaction => transaction.TransferCorrelationId == correlationId)
            .ToArrayAsync(cancellationToken);

    public void AddRange(BookedTransaction source, BookedTransaction destination) =>
        context.BookedTransactions.AddRange(source, destination);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new CashTransactionConflictException(
                "transfer_concurrency_conflict",
                "An account changed during transfer booking.",
                exception);
        }
    }
}
