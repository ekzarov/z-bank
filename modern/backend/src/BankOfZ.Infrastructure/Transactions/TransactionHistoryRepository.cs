using BankOfZ.Application.Transactions;
using BankOfZ.Domain.Transactions;
using BankOfZ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BankOfZ.Infrastructure.Transactions;

public sealed class TransactionHistoryRepository(BankOfZIdentityContext context)
    : ITransactionHistoryRepository
{
    public async Task<IReadOnlyList<TransactionHistoryRecord>> ListAsync(
        string accountId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        DateTimeOffset? afterBookedAt,
        string? afterReference,
        int take,
        CancellationToken cancellationToken)
    {
        var query = context.BookedTransactions
            .AsNoTracking()
            .Where(transaction => transaction.AccountId == accountId);
        if (from is not null)
        {
            query = query.Where(transaction => transaction.CreatedAt >= from);
        }
        if (to is not null)
        {
            query = query.Where(transaction => transaction.CreatedAt < to);
        }
        if (afterBookedAt is not null && afterReference is not null)
        {
            query = query.Where(transaction =>
                transaction.CreatedAt < afterBookedAt ||
                transaction.CreatedAt == afterBookedAt &&
                string.Compare(transaction.Reference, afterReference) < 0);
        }

        var transactions = await query
            .OrderByDescending(transaction => transaction.CreatedAt)
            .ThenByDescending(transaction => transaction.Reference)
            .Take(take)
            .ToArrayAsync(cancellationToken);
        return await MapAsync(transactions, cancellationToken);
    }

    public async Task<TransactionHistoryRecord?> FindAsync(
        string accountId,
        string reference,
        CancellationToken cancellationToken)
    {
        var transaction = await context.BookedTransactions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.AccountId == accountId && candidate.Reference == reference,
                cancellationToken);
        return transaction is null
            ? null
            : (await MapAsync([transaction], cancellationToken)).Single();
    }

    private async Task<IReadOnlyList<TransactionHistoryRecord>> MapAsync(
        IReadOnlyList<BookedTransaction> transactions,
        CancellationToken cancellationToken)
    {
        var correlations = transactions
            .Select(transaction => transaction.TransferCorrelationId)
            .Where(correlation => correlation is not null)
            .Distinct()
            .ToArray();
        var related = correlations.Length == 0
            ? []
            : await context.BookedTransactions
                .AsNoTracking()
                .Where(transaction => correlations.Contains(transaction.TransferCorrelationId))
                .Select(transaction => new
                {
                    transaction.TransferCorrelationId,
                    transaction.Reference
                })
                .ToArrayAsync(cancellationToken);

        return transactions.Select(transaction => new TransactionHistoryRecord(
            transaction.Reference,
            transaction.AccountId,
            transaction.Direction,
            transaction.Amount,
            transaction.Currency,
            transaction.ResultingActualBalance,
            transaction.ResultingAvailableBalance,
            transaction.CreatedAt,
            transaction.TransferCorrelationId,
            transaction.TransferCorrelationId is null
                ? null
                : related.FirstOrDefault(candidate =>
                    candidate.TransferCorrelationId == transaction.TransferCorrelationId &&
                    candidate.Reference != transaction.Reference)?.Reference,
            transaction.SourceSystem,
            transaction.SourceIdentifier)).ToArray();
    }
}
