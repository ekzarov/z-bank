using BankOfZ.Application.Common;
using BankOfZ.Application.Statements;
using BankOfZ.Domain.Statements;
using BankOfZ.Domain.Transactions;

namespace BankOfZ.UnitTests.Statements;

public sealed class StatementServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Generates_Leap_Month_With_Exact_Content_And_Reconciled_Totals()
    {
        var source = Source(
            100,
            110,
            null,
            Transaction('a', CashTransactionDirection.Deposit, 25, 125, new(2024, 2, 1, 0, 0, 0, TimeSpan.Zero)),
            Transaction('b', CashTransactionDirection.Withdrawal, 15, 110, new(2024, 2, 29, 23, 59, 59, TimeSpan.Zero)));
        var repository = new FakeStatementRepository(source);
        var service = Create(repository);

        var (statement, reused) = await service.GenerateAsync(
            "10000001", 2024, 2, "operator", default);

        Assert.False(reused);
        Assert.Equal(new DateTimeOffset(2024, 2, 1, 0, 0, 0, TimeSpan.Zero), statement.PeriodStartUtc);
        Assert.Equal(new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero), statement.PeriodEndUtc);
        Assert.Equal("Ms Jamie Customer", statement.CustomerName);
        Assert.Equal("1 Test Street, London, EC1A 1AA, GB", statement.CustomerAddress);
        Assert.Equal(100, statement.OpeningBalance);
        Assert.Equal(25, statement.TotalCredits);
        Assert.Equal(15, statement.TotalDebits);
        Assert.Equal(110, statement.ClosingBalance);
        Assert.Equal(2, statement.TransactionCount);
        Assert.Equal(["Credit", "Debit"], statement.Transactions.Select(item => item.Description));
    }

    [Fact]
    public async Task Empty_Period_Produces_Zero_Totals_And_Stable_Balance()
    {
        var service = Create(new FakeStatementRepository(Source(75, 75, null)));

        var (statement, _) = await service.GenerateAsync(
            "10000001", 2026, 6, "customer", default);

        Assert.Empty(statement.Transactions);
        Assert.Equal(0, statement.TransactionCount);
        Assert.Equal(0, statement.TotalCredits);
        Assert.Equal(0, statement.TotalDebits);
        Assert.Equal(75, statement.OpeningBalance);
        Assert.Equal(75, statement.ClosingBalance);
    }

    [Fact]
    public async Task Prior_Booked_Balance_Is_The_Primary_Opening_Balance()
    {
        var service = Create(new FakeStatementRepository(Source(
            130,
            130,
            100,
            Transaction('p', CashTransactionDirection.Deposit, 30, 130, Now))));

        var (statement, _) = await service.GenerateAsync(
            "10000001", 2026, 7, "operator", default);

        Assert.Equal(100, statement.OpeningBalance);
        Assert.Equal(130, statement.ClosingBalance);
    }

    [Fact]
    public async Task Equal_Timestamps_Use_Financial_Chain_For_Reconciliation_But_Reference_For_Display()
    {
        var timestamp = new DateTimeOffset(2026, 7, 10, 10, 0, 0, TimeSpan.Zero);
        var service = Create(new FakeStatementRepository(Source(
            20,
            20,
            null,
            Transaction('z', CashTransactionDirection.Deposit, 10, 10, timestamp),
            Transaction('a', CashTransactionDirection.Deposit, 10, 20, timestamp))));

        var (statement, _) = await service.GenerateAsync(
            "10000001", 2026, 7, "operator", default);

        Assert.Equal(0, statement.OpeningBalance);
        Assert.Equal(20, statement.ClosingBalance);
        Assert.Equal(
            [new string('a', 32), new string('z', 32)],
            statement.Transactions.Select(transaction => transaction.Reference));
    }

    [Fact]
    public async Task Utc_Defines_Inclusive_Start_Exclusive_End()
    {
        var repository = new FakeStatementRepository(Source(0, 0, null));
        var service = Create(repository);

        var (statement, _) = await service.GenerateAsync(
            "10000001", 2026, 7, "operator", default);

        Assert.Equal(new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero), statement.PeriodStartUtc);
        Assert.Equal(new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero), statement.PeriodEndUtc);
    }

    [Fact]
    public async Task Same_Data_Reuses_Immutable_Snapshot()
    {
        var repository = new FakeStatementRepository(Source(10, 10, null));
        var service = Create(repository);

        var first = await service.GenerateAsync("10000001", 2026, 6, "operator", default);
        var second = await service.GenerateAsync("10000001", 2026, 6, "operator", default);

        Assert.False(first.Reused);
        Assert.True(second.Reused);
        Assert.Equal(first.Statement.GenerationId, second.Statement.GenerationId);
        Assert.Single(repository.Statements);
    }

    [Fact]
    public async Task Concurrent_Unique_Conflict_Reuses_The_Winning_Snapshot()
    {
        var repository = new FakeStatementRepository(Source(10, 10, null))
        {
            SimulateConcurrentConflict = true
        };
        var service = Create(repository);

        var result = await service.GenerateAsync("10000001", 2026, 6, "operator", default);

        Assert.True(result.Reused);
        Assert.Single(repository.Statements);
        Assert.Contains(repository.Audits, audit => audit == ("generate", "reused"));
    }

    [Fact]
    public async Task Future_Period_Is_Rejected()
    {
        var exception = await Assert.ThrowsAsync<StatementValidationException>(() =>
            Create(new FakeStatementRepository(Source(0, 0, null)))
                .GenerateAsync("10000001", 2026, 8, "operator", default));

        Assert.Equal("invalid_statement_period", exception.Code);
    }

    [Fact]
    public async Task Non_Utc_Configuration_Is_Rejected()
    {
        var service = new StatementService(
            new FakeStatementRepository(Source(0, 0, null)),
            new FixedClock(),
            new StatementOptions { TimeZoneId = "Europe/London" });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GenerateAsync("10000001", 2026, 7, "operator", default));
    }

    [Fact]
    public async Task Cancellation_Is_Not_Converted_Into_A_Failed_Statement()
    {
        var repository = new FakeStatementRepository(Source(0, 0, null))
        {
            LoadException = new OperationCanceledException()
        };

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            Create(repository).GenerateAsync("10000001", 2026, 7, "operator", default));

        Assert.Empty(repository.Statements);
        Assert.Empty(repository.Audits);
    }

    [Theory]
    [InlineData(1999, 12)]
    [InlineData(2101, 1)]
    [InlineData(2026, 0)]
    [InlineData(2026, 13)]
    public async Task Invalid_Period_Is_Rejected(int year, int month)
    {
        var exception = await Assert.ThrowsAsync<StatementValidationException>(() =>
            Create(new FakeStatementRepository(Source(0, 0, null)))
                .GenerateAsync("10000001", year, month, "operator", default));

        Assert.Equal("invalid_statement_period", exception.Code);
    }

    [Fact]
    public async Task Mismatched_Booked_Balance_Fails_Without_Publishing()
    {
        var repository = new FakeStatementRepository(Source(
            0,
            99,
            null,
            Transaction('x', CashTransactionDirection.Deposit, 10, 10, Now.AddMinutes(-1)),
            Transaction('y', CashTransactionDirection.Deposit, 10, 99, Now)));

        await Assert.ThrowsAsync<StatementGenerationException>(() =>
            Create(repository).GenerateAsync("10000001", 2026, 7, "operator", default));

        Assert.Empty(repository.Statements);
        Assert.Contains(repository.Audits, audit => audit.Result == "failed");
    }

    private static StatementService Create(FakeStatementRepository repository) =>
        new(repository, new FixedClock(), new StatementOptions());

    private static StatementSourceData Source(
        decimal actual,
        decimal available,
        decimal? prior,
        params StatementSourceTransaction[] transactions) => new(
        "10000001",
        "1000000001",
        "100000",
        "Current",
        "GBP",
        1.25m,
        100,
        actual,
        available,
        Now,
        "Ms Jamie Customer",
        "1 Test Street, London, EC1A 1AA, GB",
        "+44 20 0000 0000",
        Now,
        prior,
        transactions);

    private static StatementSourceTransaction Transaction(
        char reference,
        CashTransactionDirection direction,
        decimal amount,
        decimal balance,
        DateTimeOffset at) => new(
        Guid.NewGuid(),
        new string(reference, 32),
        direction,
        amount,
        balance,
        at);

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeStatementRepository(StatementSourceData? source) : IStatementRepository
    {
        public List<StatementSnapshot> Statements { get; } = [];
        public List<(string Action, string Result)> Audits { get; } = [];
        public bool SimulateConcurrentConflict { get; init; }
        public Exception? LoadException { get; init; }

        public Task<StatementSourceData?> LoadSourceAsync(
            string accountId,
            DateTimeOffset periodStartUtc,
            DateTimeOffset periodEndUtc,
            CancellationToken cancellationToken) =>
            LoadException is null
                ? Task.FromResult(source)
                : Task.FromException<StatementSourceData?>(LoadException);

        public Task<IReadOnlyList<string>> ListAccountIdsAsync(
            string sortCode,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>(source is null ? [] : [source.AccountId]);

        public Task<StatementSnapshot?> FindByVersionAsync(
            string accountId,
            int year,
            int month,
            string dataVersion,
            CancellationToken cancellationToken) =>
            Task.FromResult(Statements.SingleOrDefault(statement =>
                statement.AccountId == accountId &&
                statement.Year == year &&
                statement.Month == month &&
                statement.DataVersion == dataVersion));

        public Task<StatementSnapshot?> FindAsync(
            string accountId,
            Guid statementId,
            CancellationToken cancellationToken) =>
            Task.FromResult(Statements.SingleOrDefault(statement =>
                statement.AccountId == accountId && statement.Id == statementId));

        public Task<bool> TryAddWithAuditAsync(
            StatementSnapshot statement,
            string actor,
            DateTimeOffset occurredAt,
            CancellationToken cancellationToken)
        {
            Statements.Add(statement);
            if (SimulateConcurrentConflict)
            {
                return Task.FromResult(false);
            }
            Audits.Add(("generate", "succeeded"));
            return Task.FromResult(true);
        }

        public Task WriteAuditAsync(
            Guid? statementId,
            string? accountId,
            string actor,
            string action,
            string result,
            string? diagnostics,
            DateTimeOffset occurredAt,
            CancellationToken cancellationToken)
        {
            Audits.Add((action, result));
            return Task.CompletedTask;
        }
    }
}
