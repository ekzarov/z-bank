using System.Security.Cryptography;
using System.Text;
using BankOfZ.Domain.Accounts;
using BankOfZ.Domain.Customers;
using BankOfZ.Domain.Transactions;
using BankOfZ.Infrastructure.DataInitialization;

namespace BankOfZ.Setup;

public static class DemoPackageFactory
{
    public static ImportPackage Create(DateOnly start, DateOnly end, int stepDays, int seed)
    {
        if (end < start)
        {
            throw new ArgumentException("Demo end date cannot precede start date.");
        }
        if (stepDays is < 1 or > 365)
        {
            throw new ArgumentOutOfRangeException(nameof(stepDays), "Step days must be between 1 and 365.");
        }

        var createdAt = AtUtc(start);
        var transactions = new List<ImportTransaction>();
        decimal balance = 0;
        var index = 0;
        for (var day = start; day <= end; day = day.AddDays(stepDays))
        {
            var amount = DeterministicAmount(seed, index);
            balance += amount;
            var sourceId = $"demo-history-{seed}-{index:D6}";
            transactions.Add(new ImportTransaction(
                sourceId,
                Reference(sourceId),
                "10000000",
                "1000000001",
                CashTransactionDirection.Deposit,
                amount,
                "GBP",
                balance,
                balance,
                AtUtc(day).AddHours(12)));
            index++;
        }

        var lastTransactionReference = transactions.Count == 0 ? null : transactions[^1].Reference;
        return new ImportPackage(
            ImportPackageJson.CurrentSchemaVersion,
            $"deterministic-demo:{seed}",
            [
                new ImportCustomer(
                    "1000000001", "100000", "Ms", "Jamie", "Customer",
                    new DateOnly(1990, 5, 12), "1 Demo Street", null, "London", null,
                    "EC1A 1AA", "GB", "customer@bankofz.demo", "+44 20 0000 0000",
                    CustomerStatus.Active, 720, end, SourceSystem.Ims,
                    $"demo-customer-{seed}", createdAt, AtUtc(end))
            ],
            [
                new ImportAccount(
                    "10000000", "1000000001", "100000", AccountType.Current, 0.25m, 500,
                    "GBP", balance, balance, start, start, start.AddMonths(1), AccountStatus.Active,
                    false, SourceSystem.Ims, $"demo-account-current-{seed}", "CURRENT",
                    lastTransactionReference, createdAt, AtUtc(end)),
                new ImportAccount(
                    "10000099", "1000000001", "100000", AccountType.Saving, 1.50m, 0,
                    "GBP", 0, 0, start, start, start.AddMonths(1), AccountStatus.Active,
                    false, SourceSystem.Ims, $"demo-account-saving-{seed}", "SAVING",
                    null, createdAt, AtUtc(end))
            ],
            transactions,
            [
                new ImportLegacyTransactionRun(
                    $"demo-run-{seed}", "COMPLETED", createdAt, AtUtc(end).AddHours(23), "1000000001")
            ],
            [
                new ImportReference("ACCOUNT_TYPE", "CURRENT", "Current account", "demo-reference"),
                new ImportReference("ACCOUNT_TYPE", "SAVING", "Savings account", "demo-reference"),
                new ImportReference("TRANSACTION_STATUS", "COMPLETED", "Completed", "demo-reference")
            ]);
    }

    private static decimal DeterministicAmount(int seed, int index)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{seed}:{index}"));
        return 10m + (hash[0] * 256 + hash[1]) % 9000 / 100m;
    }

    private static string Reference(string sourceIdentifier) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sourceIdentifier)))[..32];

    private static DateTimeOffset AtUtc(DateOnly date) =>
        new(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
}
