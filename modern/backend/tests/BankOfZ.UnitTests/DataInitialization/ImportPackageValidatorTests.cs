using BankOfZ.Infrastructure.DataInitialization;
using BankOfZ.Setup;

namespace BankOfZ.UnitTests.DataInitialization;

public sealed class ImportPackageValidatorTests
{
    [Fact]
    public void Deterministic_demo_package_is_valid_and_reproducible()
    {
        var first = DemoPackageFactory.Create(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            7,
            42);
        var second = DemoPackageFactory.Create(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            7,
            42);

        Assert.Empty(ImportPackageValidator.Validate(first));
        Assert.Equal(
            System.Text.Json.JsonSerializer.Serialize(first, ImportPackageJson.Options),
            System.Text.Json.JsonSerializer.Serialize(second, ImportPackageJson.Options));
    }

    [Fact]
    public void Validator_rejects_history_that_disagrees_with_account_balance()
    {
        var package = DemoPackageFactory.Create(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            7,
            42);
        var accounts = package.Accounts.ToArray();
        accounts[0] = accounts[0] with { ActualBalance = accounts[0].ActualBalance + 1 };

        var errors = ImportPackageValidator.Validate(package with { Accounts = accounts });

        Assert.Contains(errors, error => error.Contains("latest history balance", StringComparison.Ordinal));
    }

    [Fact]
    public void Validator_rejects_broken_intermediate_running_balance()
    {
        var package = DemoPackageFactory.Create(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            7,
            42);
        var transactions = package.Transactions.ToArray();
        transactions[1] = transactions[1] with
        {
            ResultingActualBalance = transactions[1].ResultingActualBalance + 1,
            ResultingAvailableBalance = transactions[1].ResultingAvailableBalance + 1
        };

        var errors = ImportPackageValidator.Validate(package with { Transactions = transactions });

        Assert.Contains(errors, error => error.Contains("running balance", StringComparison.Ordinal));
    }

    [Fact]
    public void Demo_factory_rejects_invalid_range_and_step()
    {
        Assert.Throws<ArgumentException>(() => DemoPackageFactory.Create(
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 1, 1),
            1,
            1));
        Assert.Throws<ArgumentOutOfRangeException>(() => DemoPackageFactory.Create(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 2, 1),
            0,
            1));
    }

    [Fact]
    public void Completed_import_run_cannot_be_restaged()
    {
        var startedAt = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMinutes(1));
        var run = ImportRun.Stage("v1", "fingerprint", "Test", "{}", 1, 0, startedAt);
        run.BeginPromotion(startedAt);
        run.Succeed("migration", startedAt.AddSeconds(1));

        Assert.Throws<InvalidOperationException>(() =>
            run.Restage("{}", 1, 0, DateTimeOffset.UtcNow));
    }
}
