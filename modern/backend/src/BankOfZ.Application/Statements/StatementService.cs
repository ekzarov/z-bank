using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using BankOfZ.Application.Common;
using BankOfZ.Domain.Statements;
using BankOfZ.Domain.Transactions;

namespace BankOfZ.Application.Statements;

public sealed class StatementService(
    IStatementRepository repository,
    IClock clock,
    StatementOptions options)
{
    public async Task<(StatementView Statement, bool Reused)> GenerateAsync(
        string accountId,
        int year,
        int month,
        string actor,
        CancellationToken cancellationToken)
    {
        var (start, end) = Period(year, month);
        StatementSnapshot? generated = null;
        try
        {
            var source = await repository.LoadSourceAsync(accountId, start, end, cancellationToken)
                ?? throw new StatementNotFoundException();
            var dataVersion = DataVersion(source, year, month, start, end);
            var existing = await repository.FindByVersionAsync(
                accountId, year, month, dataVersion, cancellationToken);
            if (existing is not null)
            {
                await Audit(existing.Id, accountId, actor, "generate", "reused", null, cancellationToken);
                return (StatementView.From(existing), true);
            }

            generated = Build(source, year, month, start, end, dataVersion);
            if (!await repository.TryAddWithAuditAsync(generated, actor, clock.UtcNow, cancellationToken))
            {
                var concurrent = await repository.FindByVersionAsync(
                    accountId, year, month, dataVersion, cancellationToken)
                    ?? throw new StatementGenerationException(
                        "A concurrent statement was created but could not be loaded.");
                await Audit(concurrent.Id, accountId, actor, "generate", "reused", null, cancellationToken);
                return (StatementView.From(concurrent), true);
            }
            return (StatementView.From(generated), false);
        }
        catch (StatementValidationException)
        {
            throw;
        }
        catch (StatementNotFoundException)
        {
            await Audit(null, accountId, actor, "generate", "not-found", null, cancellationToken);
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            await Audit(
                generated?.Id,
                accountId,
                actor,
                "generate",
                "failed",
                exception.Message,
                cancellationToken);
            throw new StatementGenerationException("Statement generation failed.", exception);
        }
    }

    public async Task<StatementView> FindAsync(
        string accountId,
        Guid statementId,
        string actor,
        CancellationToken cancellationToken)
    {
        var statement = await repository.FindAsync(accountId, statementId, cancellationToken)
            ?? throw new StatementNotFoundException();
        await Audit(statement.Id, accountId, actor, "view", "succeeded", null, cancellationToken);
        return StatementView.From(statement);
    }

    public async Task<BulkStatementResult> GenerateBulkAsync(
        int year,
        int month,
        IReadOnlyCollection<string>? requestedAccountIds,
        string actor,
        CancellationToken cancellationToken)
    {
        _ = Period(year, month);
        var availableAccountIds = await repository.ListAccountIdsAsync(options.BankSortCode, cancellationToken);
        var accountIds = requestedAccountIds is null || requestedAccountIds.Count == 0
            ? availableAccountIds
            : requestedAccountIds
                .Distinct(StringComparer.Ordinal)
                .Where(availableAccountIds.Contains)
                .Order(StringComparer.Ordinal)
                .ToArray();
        if (requestedAccountIds is { Count: > 0 } && accountIds.Count != requestedAccountIds.Distinct().Count())
        {
            throw new StatementValidationException(
                "invalid_statement_scope",
                new Dictionary<string, string[]>
                {
                    ["accountIds"] = ["Every requested account must belong to the configured bank sort code."]
                });
        }
        var results = new List<BulkStatementAccountResult>(accountIds.Count);
        foreach (var accountId in accountIds)
        {
            try
            {
                var (statement, reused) = await GenerateAsync(
                    accountId, year, month, actor, cancellationToken);
                results.Add(new(accountId, true, statement.GenerationId, reused, null));
            }
            catch (Exception exception) when (exception is StatementNotFoundException or StatementGenerationException)
            {
                results.Add(new(accountId, false, null, false, exception.Message));
            }
        }

        return new(
            year,
            month,
            results.Count,
            results.Count(result => result.Succeeded),
            results.Count(result => !result.Succeeded),
            results);
    }

    private StatementSnapshot Build(
        StatementSourceData source,
        int year,
        int month,
        DateTimeOffset start,
        DateTimeOffset end,
        string dataVersion)
    {
        var ordered = source.Transactions
            .OrderBy(transaction => transaction.BookedAt)
            .ThenBy(transaction => transaction.Reference, StringComparer.Ordinal)
            .ToArray();
        var credits = ordered
            .Where(transaction => transaction.Direction == CashTransactionDirection.Deposit)
            .Sum(transaction => transaction.Amount);
        var debits = ordered
            .Where(transaction => transaction.Direction == CashTransactionDirection.Withdrawal)
            .Sum(transaction => transaction.Amount);
        var opening = source.PriorClosingBalance
            ?? InferOpeningBalance(ordered, source.ActualBalance, credits, debits);
        var closing = opening + credits - debits;
        if (ordered.Length > 0 &&
            !ordered.Any(transaction => transaction.ResultingActualBalance == closing))
        {
            throw new StatementGenerationException("Booked transaction balances do not reconcile.");
        }

        var generatedAt = clock.UtcNow;
        var dataAsOf = new[]
        {
            source.AccountUpdatedAt,
            source.CustomerUpdatedAt,
            ordered.Select(transaction => transaction.BookedAt).DefaultIfEmpty(start).Max()
        }.Max();
        var items = ordered.Select(transaction => new StatementTransactionInput(
            transaction.BookedAt,
            transaction.Direction,
            transaction.Reference,
            transaction.Direction == CashTransactionDirection.Deposit ? "Credit" : "Debit",
            transaction.Amount)).ToArray();
        return StatementSnapshot.Create(
            Guid.NewGuid(),
            source.AccountId,
            source.CustomerId,
            year,
            month,
            start,
            end,
            generatedAt,
            dataAsOf,
            dataVersion,
            source.CustomerName,
            source.CustomerAddress,
            source.CustomerPhone,
            source.SortCode,
            source.AccountType,
            source.Currency,
            source.InterestRate,
            source.OverdraftLimit,
            opening,
            credits,
            debits,
            closing,
            source.AvailableBalance,
            items);
    }

    private static decimal SignedAmount(StatementSourceTransaction transaction) =>
        transaction.Direction == CashTransactionDirection.Deposit
            ? transaction.Amount
            : -transaction.Amount;

    private static string DataVersion(
        StatementSourceData source,
        int year,
        int month,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc)
    {
        var content = new StringBuilder()
            .Append(source.AccountId).Append('|')
            .Append(year).Append('|').Append(month).Append('|')
            .Append(periodStartUtc.UtcTicks).Append('|')
            .Append(periodEndUtc.UtcTicks).Append('|')
            .Append(source.AccountUpdatedAt.UtcTicks).Append('|')
            .Append(source.CustomerUpdatedAt.UtcTicks);
        foreach (var transaction in source.Transactions
            .OrderBy(item => item.BookedAt)
            .ThenBy(item => item.Reference, StringComparer.Ordinal))
        {
            content.Append('|').Append(transaction.Id)
                .Append(':').Append(transaction.BookedAt.UtcTicks)
                .Append(':').Append(transaction.Amount.ToString(CultureInfo.InvariantCulture))
                .Append(':').Append((int)transaction.Direction);
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content.ToString()))).ToLowerInvariant();
    }

    private static decimal InferOpeningBalance(
        IReadOnlyList<StatementSourceTransaction> transactions,
        decimal currentActualBalance,
        decimal credits,
        decimal debits)
    {
        if (transactions.Count == 0)
        {
            return currentActualBalance;
        }

        var resultingBalances = transactions
            .Select(transaction => transaction.ResultingActualBalance)
            .ToHashSet();
        var candidates = transactions
            .Select(transaction => transaction.ResultingActualBalance - SignedAmount(transaction))
            .Where(balance => !resultingBalances.Contains(balance))
            .Distinct()
            .ToArray();
        return candidates.Length == 1
            ? candidates[0]
            : currentActualBalance - credits + debits;
    }

    private (DateTimeOffset Start, DateTimeOffset End) Period(int year, int month)
    {
        if (year is < 2000 or > 2100 || month is < 1 or > 12)
        {
            throw new StatementValidationException(
                "invalid_statement_period",
                new Dictionary<string, string[]> { ["period"] = ["Use a valid month between 2000 and 2100."] });
        }
        if (!string.Equals(options.TimeZoneId, "UTC", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Statement timezone must be UTC.");
        }
        var localStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var currentMonth = new DateTime(
            clock.UtcNow.Year,
            clock.UtcNow.Month,
            1,
            0,
            0,
            0,
            DateTimeKind.Unspecified);
        if (localStart > currentMonth)
        {
            throw new StatementValidationException(
                "invalid_statement_period",
                new Dictionary<string, string[]> { ["period"] = ["Future statement periods are not available."] });
        }
        var localEnd = localStart.AddMonths(1);
        return (
            new DateTimeOffset(localStart, TimeSpan.Zero),
            new DateTimeOffset(localEnd, TimeSpan.Zero));
    }

    private Task Audit(
        Guid? statementId,
        string? accountId,
        string actor,
        string action,
        string result,
        string? diagnostics,
        CancellationToken cancellationToken) =>
        repository.WriteAuditAsync(
            statementId,
            accountId,
            actor,
            action,
            result,
            diagnostics,
            clock.UtcNow,
            cancellationToken);
}
