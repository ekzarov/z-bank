using BankOfZ.Application.Transactions;
using BankOfZ.Domain.Customers;
using BankOfZ.Domain.Transactions;

namespace BankOfZ.UnitTests.Transactions;

public sealed class TransactionHistoryServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 23, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Cursor_Pages_Without_Duplicates_And_Binds_Query_Context()
    {
        var repository = new FakeRepository(
            Record('3', Now),
            Record('2', Now),
            Record('1', Now.AddMinutes(-1)));
        var service = CreateService(repository);

        var first = await service.ListAsync(
            "10000001", "2026-07-01", "2026-08-01", 2, null, default);
        var second = await service.ListAsync(
            "10000001", "2026-07-01T00:00:00Z", "2026-08-01T00:00:00Z",
            2, first.NextCursor, default);

        Assert.Equal([new string('3', 32), new string('2', 32)], first.Items.Select(item => item.Reference));
        Assert.Equal([new string('1', 32)], second.Items.Select(item => item.Reference));
        Assert.Null(second.NextCursor);
        var accountError = await Assert.ThrowsAsync<TransactionHistoryValidationException>(() =>
            service.ListAsync("10000002", "2026-07-01", "2026-08-01", 2, first.NextCursor, default));
        var filterError = await Assert.ThrowsAsync<TransactionHistoryValidationException>(() =>
            service.ListAsync("10000001", "2026-07-02", "2026-08-01", 2, first.NextCursor, default));
        Assert.Equal("invalid_history_cursor", accountError.Code);
        Assert.Equal("invalid_history_cursor", filterError.Code);
    }

    [Fact]
    public async Task Invalid_Cursor_Page_Size_And_Date_Ranges_Are_Rejected()
    {
        var service = CreateService(new FakeRepository());

        Assert.Equal(
            "invalid_history_cursor",
            (await Assert.ThrowsAsync<TransactionHistoryValidationException>(() =>
                service.ListAsync("10000001", null, null, 50, "not-a-cursor", default))).Code);
        Assert.Equal(
            "invalid_history_page_size",
            (await Assert.ThrowsAsync<TransactionHistoryValidationException>(() =>
                service.ListAsync("10000001", null, null, 0, null, default))).Code);
        Assert.Equal(
            "invalid_history_page_size",
            (await Assert.ThrowsAsync<TransactionHistoryValidationException>(() =>
                service.ListAsync("10000001", null, null, 201, null, default))).Code);
        Assert.Equal(
            "invalid_history_range",
            (await Assert.ThrowsAsync<TransactionHistoryValidationException>(() =>
                service.ListAsync("10000001", "2026-08-01", "2026-07-01", 50, null, default))).Code);
        Assert.Equal(
            "invalid_history_range",
            (await Assert.ThrowsAsync<TransactionHistoryValidationException>(() =>
                service.ListAsync("10000001", "not-a-date", null, 50, null, default))).Code);
    }

    [Fact]
    public async Task Mapping_Exposes_Description_Related_Reference_And_Provenance()
    {
        var record = Record(
            'a',
            Now,
            CashTransactionDirection.Withdrawal,
            SourceSystem.Cics,
            transferCorrelationId: "correlation",
            relatedReference: new string('b', 32),
            sourceIdentifier: "CICS-00042");
        var service = CreateService(new FakeRepository(record));

        var page = await service.ListAsync("10000001", null, null, 50, null, default);

        var item = Assert.Single(page.Items);
        Assert.Equal("Internal transfer sent", item.Description);
        Assert.Equal("booked", item.Status);
        Assert.Equal(new string('b', 32), item.RelatedTransferReference);
        Assert.Equal(SourceSystem.Cics, item.SourceSystem);
        Assert.Equal("CICS-00042", item.SourceIdentifier);
    }

    [Fact]
    public async Task Missing_Detail_Is_Not_Found()
    {
        var service = CreateService(new FakeRepository());

        await Assert.ThrowsAsync<TransactionHistoryNotFoundException>(() =>
            service.FindAsync("10000001", new string('f', 32), default));
    }

    private static TransactionHistoryService CreateService(ITransactionHistoryRepository repository) =>
        new(repository, new HistoryCursorCodec());

    private static TransactionHistoryRecord Record(
        char reference,
        DateTimeOffset bookedAt,
        CashTransactionDirection direction = CashTransactionDirection.Deposit,
        SourceSystem sourceSystem = SourceSystem.Modern,
        string? transferCorrelationId = null,
        string? relatedReference = null,
        string? sourceIdentifier = null) => new(
            new string(reference, 32),
            "10000001",
            direction,
            10,
            "GBP",
            100,
            100,
            bookedAt,
            transferCorrelationId,
            relatedReference,
            sourceSystem,
            sourceIdentifier);

    private sealed class FakeRepository(params TransactionHistoryRecord[] records)
        : ITransactionHistoryRepository
    {
        public Task<IReadOnlyList<TransactionHistoryRecord>> ListAsync(
            string accountId,
            DateTimeOffset? from,
            DateTimeOffset? to,
            DateTimeOffset? afterBookedAt,
            string? afterReference,
            int take,
            CancellationToken cancellationToken)
        {
            var query = records
                .Where(record => record.AccountId == accountId)
                .Where(record => from is null || record.BookedAt >= from)
                .Where(record => to is null || record.BookedAt < to)
                .Where(record => afterBookedAt is null ||
                    record.BookedAt < afterBookedAt ||
                    record.BookedAt == afterBookedAt &&
                    string.CompareOrdinal(record.Reference, afterReference) < 0)
                .OrderByDescending(record => record.BookedAt)
                .ThenByDescending(record => record.Reference, StringComparer.Ordinal)
                .Take(take)
                .ToArray();
            return Task.FromResult<IReadOnlyList<TransactionHistoryRecord>>(query);
        }

        public Task<TransactionHistoryRecord?> FindAsync(
            string accountId,
            string reference,
            CancellationToken cancellationToken) => Task.FromResult(
                records.SingleOrDefault(record =>
                    record.AccountId == accountId && record.Reference == reference));
    }
}
