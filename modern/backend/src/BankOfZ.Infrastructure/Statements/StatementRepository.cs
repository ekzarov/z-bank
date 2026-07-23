using BankOfZ.Application.Statements;
using BankOfZ.Domain.Accounts;
using BankOfZ.Domain.Statements;
using BankOfZ.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BankOfZ.Infrastructure.Statements;

public sealed class StatementRepository(BankOfZIdentityContext context) : IStatementRepository
{
    public async Task<StatementSourceData?> LoadSourceAsync(
        string accountId,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
        CancellationToken cancellationToken)
    {
        var source = await (
            from account in context.Accounts.AsNoTracking()
            join customer in context.Customers.AsNoTracking() on account.CustomerId equals customer.Id
            where account.Id == accountId
            select new
            {
                Account = account,
                Customer = customer
            }).SingleOrDefaultAsync(cancellationToken);
        if (source is null)
        {
            return null;
        }

        var transactions = await context.BookedTransactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.AccountId == accountId &&
                transaction.CreatedAt >= periodStartUtc &&
                transaction.CreatedAt < periodEndUtc)
            .OrderBy(transaction => transaction.CreatedAt)
            .ThenBy(transaction => transaction.Reference)
            .Select(transaction => new StatementSourceTransaction(
                transaction.Id,
                transaction.Reference,
                transaction.Direction,
                transaction.Amount,
                transaction.ResultingActualBalance,
                transaction.CreatedAt))
            .ToArrayAsync(cancellationToken);
        var prior = await context.BookedTransactions
            .AsNoTracking()
            .Where(transaction => transaction.AccountId == accountId && transaction.CreatedAt < periodStartUtc)
            .OrderByDescending(transaction => transaction.CreatedAt)
            .ThenByDescending(transaction => transaction.Reference)
            .Select(transaction => (decimal?)transaction.ResultingActualBalance)
            .FirstOrDefaultAsync(cancellationToken);
        var customerEntity = source.Customer;
        var accountEntity = source.Account;
        var address = string.Join(
            ", ",
            new[]
            {
                customerEntity.AddressLine1,
                customerEntity.AddressLine2,
                customerEntity.City,
                customerEntity.Region,
                customerEntity.PostalCode,
                customerEntity.CountryCode
            }.Where(value => !string.IsNullOrWhiteSpace(value)));
        return new(
            accountEntity.Id,
            accountEntity.CustomerId,
            accountEntity.SortCode,
            accountEntity.Type.ToString(),
            accountEntity.Currency,
            accountEntity.InterestRate,
            accountEntity.OverdraftLimit,
            accountEntity.ActualBalance,
            accountEntity.AvailableBalance,
            accountEntity.UpdatedAt,
            $"{customerEntity.Title} {customerEntity.FirstName} {customerEntity.LastName}",
            address,
            customerEntity.Phone,
            customerEntity.UpdatedAt,
            prior,
            transactions);
    }

    public async Task<IReadOnlyList<string>> ListAccountIdsAsync(
        string sortCode,
        CancellationToken cancellationToken) =>
        await context.Accounts
            .AsNoTracking()
            .Where(account => account.SortCode == sortCode)
            .OrderBy(account => account.Id)
            .Select(account => account.Id)
            .ToArrayAsync(cancellationToken);

    public Task<StatementSnapshot?> FindByVersionAsync(
        string accountId,
        int year,
        int month,
        string dataVersion,
        CancellationToken cancellationToken) =>
        context.StatementSnapshots
            .AsNoTracking()
            .Include(statement => statement.Transactions)
            .SingleOrDefaultAsync(
                statement =>
                    statement.AccountId == accountId &&
                    statement.Year == year &&
                    statement.Month == month &&
                    statement.DataVersion == dataVersion,
                cancellationToken);

    public Task<StatementSnapshot?> FindAsync(
        string accountId,
        Guid statementId,
        CancellationToken cancellationToken) =>
        context.StatementSnapshots
            .AsNoTracking()
            .Include(statement => statement.Transactions)
            .SingleOrDefaultAsync(
                statement => statement.AccountId == accountId && statement.Id == statementId,
                cancellationToken);

    public async Task<bool> TryAddWithAuditAsync(
        StatementSnapshot statement,
        string actor,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            context.StatementSnapshots.Add(statement);
            context.StatementAuditEntries.Add(Audit(
                statement.Id,
                statement.AccountId,
                actor,
                "generate",
                "succeeded",
                null,
                occurredAt));
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            await transaction.RollbackAsync(cancellationToken);
            context.ChangeTracker.Clear();
            return false;
        }
    }

    public async Task WriteAuditAsync(
        Guid? statementId,
        string? accountId,
        string actor,
        string action,
        string result,
        string? diagnostics,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        context.ChangeTracker.Clear();
        context.StatementAuditEntries.Add(Audit(
            statementId,
            accountId,
            actor,
            action,
            result,
            diagnostics,
            occurredAt));
        await context.SaveChangesAsync(cancellationToken);
    }

    private static StatementAuditRecord Audit(
        Guid? statementId,
        string? accountId,
        string actor,
        string action,
        string result,
        string? diagnostics,
        DateTimeOffset occurredAt) => new()
        {
            Id = Guid.NewGuid(),
            StatementId = statementId,
            AccountId = accountId,
            Actor = actor,
            Action = action,
            Result = result,
            Diagnostics = diagnostics?[..Math.Min(diagnostics.Length, CatalogModelConstants.Lengths.StatementFailure)],
            OccurredAt = occurredAt
        };
}
