using BankOfZ.Application.Accounts;
using BankOfZ.Application.Common;
using BankOfZ.Application.Transactions;
using BankOfZ.Domain.Accounts;
using BankOfZ.Domain.Customers;
using BankOfZ.Domain.Transactions;

namespace BankOfZ.UnitTests.Transactions;

public sealed class InternalTransferServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Transfer_Books_Atomic_Pair_With_Distinct_References()
    {
        var source = CreateAccount("10000001");
        source.ApplyCash(CashTransactionDirection.Deposit, 100m, Reference('a'), Now);
        var destination = CreateAccount("10000002");
        var repository = new FakeRepository(source, destination);
        var audit = new FakeAuditWriter();

        var result = await CreateService(repository, audit).TransferAsync(
            source.Id, destination.Id, 40m, "transfer-1", "customer", default);

        Assert.Equal(60m, source.ActualBalance);
        Assert.Equal(40m, destination.ActualBalance);
        Assert.NotEqual(result.Source.Reference, result.Destination.Reference);
        Assert.All(repository.Added, booking => Assert.Equal(result.CorrelationId, booking.TransferCorrelationId));
        Assert.All(repository.Added, booking => Assert.Equal(SourceSystem.Modern, booking.SourceSystem));
        Assert.Equal(2, audit.Entries.Count);
        Assert.True(repository.SaveCalled);
    }

    [Fact]
    public async Task Transfer_Locks_Accounts_In_Deterministic_Order()
    {
        var repository = new FakeRepository(CreateAccount("10000002"), CreateAccount("10000001"));

        await Assert.ThrowsAsync<CashTransactionValidationException>(() =>
            CreateService(repository).TransferAsync(
                "10000002", "10000001", 1m, "transfer-2", "operator", default));

        Assert.Equal(["10000001", "10000002"], repository.Locked);
    }

    [Theory]
    [InlineData("", "10000002", "idempotency_key_invalid")]
    [InlineData("key", "10000001", "transfer_same_account")]
    public async Task Invalid_Request_Fails_Before_Opening_Transaction(
        string key,
        string destinationId,
        string code)
    {
        var repository = new FakeRepository(CreateAccount("10000001"), CreateAccount("10000002"));

        var exception = await Assert.ThrowsAsync<CashTransactionValidationException>(() =>
            CreateService(repository).TransferAsync(
                "10000001", destinationId, 1m, key, "customer", default));

        Assert.Equal(code, exception.Code);
        Assert.False(repository.TransactionOpened);
    }

    [Fact]
    public async Task Same_Request_Replays_Pair_And_Different_Request_Conflicts()
    {
        const string correlation = "0123456789abcdef0123456789abcdef";
        var source = CreateAccount("10000001");
        var destination = CreateAccount("10000002");
        var sourceBooking = Booking(source, CashTransactionDirection.Withdrawal, "same-key", correlation, "transfer:10000002:10");
        var destinationBooking = Booking(destination, CashTransactionDirection.Deposit, correlation, correlation, "transfer:10000002:10");
        var repository = new FakeRepository(source, destination, sourceBooking, destinationBooking);
        var service = CreateService(repository);

        var replay = await service.TransferAsync(
            source.Id, destination.Id, 10.00m, "same-key", "customer", default);
        var exception = await Assert.ThrowsAsync<CashTransactionConflictException>(() =>
            service.TransferAsync(source.Id, destination.Id, 11m, "same-key", "customer", default));

        Assert.Equal(correlation, replay.CorrelationId);
        Assert.Equal("idempotency_conflict", exception.Code);
        Assert.Empty(repository.Added);
        Assert.False(repository.SaveCalled);
    }

    private static InternalTransferService CreateService(
        FakeRepository repository,
        FakeAuditWriter? audit = null) => new(repository, audit ?? new FakeAuditWriter(), new FakeClock());

    private static Account CreateAccount(string id, string currency = "GBP") => Account.Create(
        id,
        "1000000001",
        "100000",
        new AccountMetadata(AccountType.Current, 0, 0, currency),
        SourceSystem.Modern,
        null,
        null,
        Now);

    private static BookedTransaction Booking(
        Account account,
        CashTransactionDirection direction,
        string key,
        string correlation,
        string fingerprint) => BookedTransaction.Create(
            Reference(direction == CashTransactionDirection.Deposit ? 'b' : 'c'),
            account.Id,
            account.CustomerId,
            direction,
            10m,
            account.Currency,
            account.ActualBalance,
            account.AvailableBalance,
            key,
            fingerprint,
            Now,
            correlation);

    private static string Reference(char character) => new(character, CashTransactionRules.ReferenceLength);

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeAuditWriter : IAccountAuditWriter
    {
        public List<AccountAuditEntry> Entries { get; } = [];
        public void Add(AccountAuditEntry entry) => Entries.Add(entry);
    }

    private sealed class FakeRepository(
        Account first,
        Account second,
        params BookedTransaction[] existing) : IInternalTransferRepository
    {
        private readonly Dictionary<string, Account> accounts = new()
        {
            [first.Id] = first,
            [second.Id] = second
        };

        public bool TransactionOpened { get; private set; }
        public bool SaveCalled { get; private set; }
        public List<string> Locked { get; } = [];
        public List<BookedTransaction> Added { get; } = [];

        public async Task<T> ExecuteSerializableAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
        {
            TransactionOpened = true;
            return await operation();
        }

        public Task<bool> LockAccountAsync(string accountId, CancellationToken cancellationToken)
        {
            Locked.Add(accountId);
            return Task.FromResult(accounts.ContainsKey(accountId));
        }

        public Task<Account?> FindAccountAsync(string accountId, CancellationToken cancellationToken) =>
            Task.FromResult(accounts.GetValueOrDefault(accountId));

        public Task<BookedTransaction?> FindByIdempotencyAsync(
            string sourceAccountId,
            string idempotencyKey,
            CancellationToken cancellationToken) => Task.FromResult(existing.SingleOrDefault(
                transaction => transaction.AccountId == sourceAccountId && transaction.IdempotencyKey == idempotencyKey));

        public Task<IReadOnlyList<BookedTransaction>> FindTransferAsync(
            string correlationId,
            CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<BookedTransaction>>(
                existing.Where(transaction => transaction.TransferCorrelationId == correlationId).ToArray());

        public void AddRange(BookedTransaction source, BookedTransaction destination) => Added.AddRange([source, destination]);

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCalled = true;
            return Task.CompletedTask;
        }
    }
}
