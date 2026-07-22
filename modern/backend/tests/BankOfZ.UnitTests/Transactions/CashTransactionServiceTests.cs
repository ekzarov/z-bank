using BankOfZ.Application.Accounts;
using BankOfZ.Application.Common;
using BankOfZ.Application.Transactions;
using BankOfZ.Domain.Accounts;
using BankOfZ.Domain.Customers;
using BankOfZ.Domain.Transactions;

namespace BankOfZ.UnitTests.Transactions;

public sealed class CashTransactionServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 22, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("")]
    [InlineData("contains space")]
    public async Task Deposit_Rejects_Invalid_Idempotency_Key_Before_Opening_Transaction(string key)
    {
        var repository = new FakeRepository(CreateAccount());
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CashTransactionValidationException>(() =>
            service.DepositAsync("10000001", 50m, key, "operator", "correlation", default));

        Assert.Equal("idempotency_key_invalid", exception.Code);
        Assert.False(repository.TransactionOpened);
    }

    [Fact]
    public async Task Same_Idempotency_Request_Replays_Existing_Modern_Booking()
    {
        var existing = BookedTransaction.Create(
            "0123456789abcdef0123456789abcdef", "10000001", "1000000001",
            CashTransactionDirection.Deposit, 50m, "GBP", 50m, 50m,
            "same-key", "Deposit:50", Now);
        var repository = new FakeRepository(CreateAccount(), existing);
        var service = CreateService(repository);

        var result = await service.DepositAsync("10000001", 50.00m, "same-key", "operator", "correlation", default);

        Assert.Equal(existing.Reference, result.Reference);
        Assert.Equal(SourceSystem.Modern, result.SourceSystem);
        Assert.Null(repository.Added);
        Assert.False(repository.SaveCalled);
    }

    [Fact]
    public async Task Reused_Idempotency_Key_With_Different_Request_Conflicts()
    {
        var existing = BookedTransaction.Create(
            "0123456789abcdef0123456789abcdef", "10000001", "1000000001",
            CashTransactionDirection.Deposit, 50m, "GBP", 50m, 50m,
            "same-key", "Deposit:50", Now);
        var service = CreateService(new FakeRepository(CreateAccount(), existing));

        var exception = await Assert.ThrowsAsync<CashTransactionConflictException>(() =>
            service.DepositAsync("10000001", 51m, "same-key", "operator", "correlation", default));

        Assert.Equal("idempotency_conflict", exception.Code);
    }

    private static CashTransactionService CreateService(FakeRepository repository) =>
        new(repository, new FakeAuditWriter(), new FakeClock());

    private static Account CreateAccount() => Account.Create(
        "10000001", "1000000001", "100000",
        new AccountMetadata(AccountType.Current, 0, 500, "GBP"),
        SourceSystem.Modern, null, null, Now);

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeAuditWriter : IAccountAuditWriter
    {
        public void Add(AccountAuditEntry entry)
        {
        }
    }

    private sealed class FakeRepository(Account account, BookedTransaction? existing = null) : ICashTransactionRepository
    {
        public bool TransactionOpened { get; private set; }
        public bool SaveCalled { get; private set; }
        public BookedTransaction? Added { get; private set; }

        public async Task<T> ExecuteSerializableAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
        {
            TransactionOpened = true;
            return await operation();
        }

        public Task<bool> LockAccountAsync(string accountId, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<Account?> FindAccountAsync(string accountId, CancellationToken cancellationToken) => Task.FromResult<Account?>(account);
        public Task<BookedTransaction?> FindByIdempotencyAsync(string accountId, string idempotencyKey, CancellationToken cancellationToken) =>
            Task.FromResult(existing is not null && existing.IdempotencyKey == idempotencyKey ? existing : null);
        public void Add(BookedTransaction transaction) => Added = transaction;
        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCalled = true;
            return Task.CompletedTask;
        }
    }
}
