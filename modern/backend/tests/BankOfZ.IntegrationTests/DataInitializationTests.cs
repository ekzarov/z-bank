using System.Text.Json;
using System.Security.Cryptography;
using BankOfZ.Domain.Accounts;
using BankOfZ.Domain.Customers;
using BankOfZ.Domain.Transactions;
using BankOfZ.Infrastructure.DataInitialization;
using BankOfZ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BankOfZ.IntegrationTests;

[Trait("Category", DatabaseIntegrationTestCategory.Name)]
[Collection(DatabaseTestCollection.Name)]
public sealed class DataInitializationTests(BankOfZTestsFixture fixture)
    : DatabaseIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Import_preserves_legacy_data_and_identical_rerun_is_idempotent()
    {
        var package = Package();
        Assert.Empty(ImportPackageValidator.Validate(package));
        var bytes = JsonSerializer.SerializeToUtf8Bytes(package, ImportPackageJson.Options);
        await using var context = Context();
        var service = new DataImportService(context);

        var first = await service.ImportAsync(bytes, "migration-operator", "Test");
        context.ChangeTracker.Clear();
        var second = await service.ImportAsync(bytes, "migration-operator", "Test");

        Assert.Equal(ImportRunStatus.Succeeded, first.Status);
        Assert.False(first.AlreadyApplied);
        Assert.True(second.AlreadyApplied);
        Assert.Equal(first.RunId, second.RunId);
        Assert.Equal(2, await context.Customers.CountAsync());
        Assert.Equal(1, await context.Accounts.CountAsync());
        Assert.Equal(1, await context.BookedTransactions.CountAsync());
        Assert.Equal(1, await context.LegacyTransactionRuns.CountAsync());
        Assert.Equal(1, await context.ImportReferenceValues.CountAsync());
        Assert.Equal(1, await context.ImportRuns.CountAsync());
        Assert.Equal(2, await context.ImportAttempts.CountAsync());
        Assert.Equal(5, await context.ImportStagedRecords.CountAsync());
        Assert.Equal(0, second.PromotedCount);

        var account = await context.Accounts.SingleAsync();
        Assert.Equal(123.45m, account.ActualBalance);
        Assert.Equal(SourceSystem.Ims, account.SourceSystem);
        Assert.Equal("legacy-account-1", account.SourceIdentifier);
        var transaction = await context.BookedTransactions.SingleAsync();
        Assert.Equal("legacy-history-1", transaction.SourceIdentifier);
        Assert.Equal(new DateTimeOffset(2025, 1, 2, 12, 0, 0, TimeSpan.Zero), transaction.CreatedAt);
    }

    [Fact]
    public async Task Conflicting_source_rolls_back_all_trusted_rows_and_records_failed_run()
    {
        await using var context = Context();
        var service = new DataImportService(context);
        var original = Package();
        await service.ImportAsync(
            JsonSerializer.SerializeToUtf8Bytes(original, ImportPackageJson.Options),
            "migration-operator",
            "Test");
        context.ChangeTracker.Clear();

        var conflictingCustomers = original.Customers.ToArray();
        conflictingCustomers[0] = conflictingCustomers[0] with { FirstName = "Different" };
        var conflictingReferences = original.ReferenceData.Append(
            new ImportReference("ACCOUNT_TYPE", "SAVING", "Savings", "legacy-ref-2")).ToArray();
        var conflict = original with
        {
            Source = "conflicting-source",
            Customers = conflictingCustomers,
            ReferenceData = conflictingReferences
        };

        var exception = await Assert.ThrowsAsync<ImportPackageException>(() => service.ImportAsync(
            JsonSerializer.SerializeToUtf8Bytes(conflict, ImportPackageJson.Options),
            "migration-operator",
            "Test"));

        Assert.Equal("customer_source_conflict", exception.Code);
        context.ChangeTracker.Clear();
        Assert.Equal(1, await context.ImportReferenceValues.CountAsync());
        Assert.Equal(2, await context.ImportRuns.CountAsync());
        Assert.Equal(ImportRunStatus.Failed, (await context.ImportRuns.OrderBy(run => run.StartedAt).LastAsync()).Status);
    }

    [Fact]
    public async Task Invalid_relationship_is_rejected_before_trusted_mutation()
    {
        var package = Package();
        var accounts = package.Accounts.ToArray();
        accounts[0] = accounts[0] with { CustomerId = "9999999999" };
        var invalid = package with { Accounts = accounts };
        await using var context = Context();

        var exception = await Assert.ThrowsAsync<ImportPackageException>(() =>
            new DataImportService(context).ImportAsync(
                JsonSerializer.SerializeToUtf8Bytes(invalid, ImportPackageJson.Options),
                "migration-operator",
                "Test"));

        Assert.Equal("package_validation_failed", exception.Code);
        Assert.Empty(await context.Accounts.ToListAsync());
        Assert.Empty(await context.BookedTransactions.ToListAsync());
        var run = await context.ImportRuns.SingleAsync();
        Assert.Equal(ImportRunStatus.Failed, run.Status);
        Assert.DoesNotContain("Import Customer", run.StagedManifest, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unexpired_active_run_is_rejected_and_audited()
    {
        var package = Package();
        var bytes = JsonSerializer.SerializeToUtf8Bytes(package, ImportPackageJson.Options);
        var fingerprint = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        await using var context = Context();
        context.ImportRuns.Add(ImportRun.Stage(
            package.SchemaVersion,
            fingerprint,
            "Test",
            """{"staged":true}""",
            5,
            0,
            DateTimeOffset.UtcNow));
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ImportPackageException>(() =>
            new DataImportService(context).ImportAsync(bytes, "migration-operator", "Test"));

        Assert.Equal("import_already_active", exception.Code);
        var attempt = await context.ImportAttempts.SingleAsync();
        Assert.Equal(ImportAttemptStatus.RejectedActive, attempt.Status);
    }

    [Fact]
    public async Task Expired_active_run_resumes_under_same_logical_run()
    {
        var package = Package();
        var bytes = JsonSerializer.SerializeToUtf8Bytes(package, ImportPackageJson.Options);
        var fingerprint = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        await using var context = Context();
        var staleRun = ImportRun.Stage(
            package.SchemaVersion,
            fingerprint,
            "Test",
            """{"staged":true}""",
            5,
            0,
            DateTimeOffset.UtcNow.Subtract(ImportRun.LeaseDuration).AddMinutes(-1));
        context.ImportRuns.Add(staleRun);
        await context.SaveChangesAsync();

        var result = await new DataImportService(context).ImportAsync(bytes, "migration-operator", "Test");

        Assert.Equal(staleRun.Id, result.RunId);
        Assert.Equal(ImportRunStatus.Succeeded, result.Status);
        Assert.Equal(1, result.AttemptNumber);
    }

    [Fact]
    public async Task Latest_data_initialization_migration_is_applied()
    {
        await using var context = Context();

        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.Contains(
            await context.Database.GetAppliedMigrationsAsync(),
            migration => migration.EndsWith("_AddDataInitialization", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Oversized_package_is_rejected_before_parsing_or_trusted_mutation()
    {
        var bytes = new byte[ImportPackageJson.MaximumPackageBytes + 1];
        await using var context = Context();
        var customerCount = await context.Customers.CountAsync();
        var accountCount = await context.Accounts.CountAsync();
        var transactionCount = await context.BookedTransactions.CountAsync();

        var exception = await Assert.ThrowsAsync<ImportPackageException>(() =>
            new DataImportService(context).ImportAsync(bytes, "migration-operator", "Test"));

        Assert.Equal("package_too_large", exception.Code);
        Assert.Equal(customerCount, await context.Customers.CountAsync());
        Assert.Equal(accountCount, await context.Accounts.CountAsync());
        Assert.Equal(transactionCount, await context.BookedTransactions.CountAsync());
        Assert.Equal(ImportRunStatus.Failed, (await context.ImportRuns.SingleAsync()).Status);
        Assert.Equal(
            ImportAttemptStatus.FailedValidation,
            (await context.ImportAttempts.SingleAsync()).Status);
    }

    private BankOfZIdentityContext Context() =>
        new(new DbContextOptionsBuilder<BankOfZIdentityContext>()
            .UseSqlServer(Fixture.ConnectionString)
            .Options);

    private static ImportPackage Package()
    {
        var created = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        return new ImportPackage(
            ImportPackageJson.CurrentSchemaVersion,
            "integration-test",
            [
                new ImportCustomer(
                    "1000000002", "100000", "Mr", "Import", "Customer",
                    new DateOnly(1985, 1, 1), "2 Test Street", null, "London", null,
                    "EC1A 1BB", "GB", "import@example.test", null, CustomerStatus.Active,
                    650.25m, new DateOnly(2025, 1, 1), SourceSystem.Ims, "legacy-customer-1",
                    created, created)
            ],
            [
                new ImportAccount(
                    "10000002", "1000000002", "100000", AccountType.Current, 0.25m, 500,
                    "GBP", 123.45m, 123.45m, new DateOnly(2025, 1, 1),
                    new DateOnly(2025, 1, 1), new DateOnly(2025, 2, 1), AccountStatus.Active,
                    false, SourceSystem.Ims, "legacy-account-1", "CURRENT", "ABCDEF1234567890ABCDEF1234567890",
                    created, created.AddDays(1))
            ],
            [
                new ImportTransaction(
                    "legacy-history-1", "ABCDEF1234567890ABCDEF1234567890", "10000002", "1000000002",
                    CashTransactionDirection.Deposit, 123.45m, "GBP", 123.45m, 123.45m,
                    created.AddDays(1).AddHours(12))
            ],
            [
                new ImportLegacyTransactionRun(
                    "legacy-run-1", "COMPLETED", created, created.AddMinutes(1), "1000000002")
            ],
            [
                new ImportReference("ACCOUNT_TYPE", "CURRENT", "Current account", "legacy-ref-1")
            ]);
    }
}
