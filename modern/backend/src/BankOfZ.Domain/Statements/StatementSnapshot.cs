using BankOfZ.Domain.Transactions;

namespace BankOfZ.Domain.Statements;

public sealed class StatementSnapshot
{
    private StatementSnapshot()
    {
    }

    public Guid Id { get; private set; }
    public string AccountId { get; private set; } = null!;
    public string CustomerId { get; private set; } = null!;
    public int Year { get; private set; }
    public int Month { get; private set; }
    public DateTimeOffset PeriodStartUtc { get; private set; }
    public DateTimeOffset PeriodEndUtc { get; private set; }
    public DateOnly StatementDate { get; private set; }
    public DateTimeOffset GeneratedAt { get; private set; }
    public DateTimeOffset DataAsOf { get; private set; }
    public string DataVersion { get; private set; } = null!;
    public string CustomerName { get; private set; } = null!;
    public string CustomerAddress { get; private set; } = null!;
    public string? CustomerPhone { get; private set; }
    public string SortCode { get; private set; } = null!;
    public string AccountType { get; private set; } = null!;
    public string Currency { get; private set; } = null!;
    public decimal InterestRate { get; private set; }
    public int OverdraftLimit { get; private set; }
    public decimal OpeningBalance { get; private set; }
    public decimal TotalCredits { get; private set; }
    public decimal TotalDebits { get; private set; }
    public decimal ClosingBalance { get; private set; }
    public decimal AvailableBalance { get; private set; }
    public int TransactionCount { get; private set; }
    public IReadOnlyCollection<StatementTransactionSnapshot> Transactions => transactions;

    private readonly List<StatementTransactionSnapshot> transactions = [];

    public static StatementSnapshot Create(
        Guid id,
        string accountId,
        string customerId,
        int year,
        int month,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
        DateTimeOffset generatedAt,
        DateTimeOffset dataAsOf,
        string dataVersion,
        string customerName,
        string customerAddress,
        string? customerPhone,
        string sortCode,
        string accountType,
        string currency,
        decimal interestRate,
        int overdraftLimit,
        decimal openingBalance,
        decimal totalCredits,
        decimal totalDebits,
        decimal closingBalance,
        decimal availableBalance,
        IReadOnlyList<StatementTransactionInput> items)
    {
        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month));
        }
        if (openingBalance + totalCredits - totalDebits != closingBalance)
        {
            throw new InvalidOperationException("Statement reconciliation failed.");
        }

        var snapshot = new StatementSnapshot
        {
            Id = id,
            AccountId = accountId,
            CustomerId = customerId,
            Year = year,
            Month = month,
            PeriodStartUtc = periodStartUtc,
            PeriodEndUtc = periodEndUtc,
            StatementDate = new DateOnly(year, month, DateTime.DaysInMonth(year, month)),
            GeneratedAt = generatedAt,
            DataAsOf = dataAsOf,
            DataVersion = dataVersion,
            CustomerName = customerName,
            CustomerAddress = customerAddress,
            CustomerPhone = customerPhone,
            SortCode = sortCode,
            AccountType = accountType,
            Currency = currency,
            InterestRate = interestRate,
            OverdraftLimit = overdraftLimit,
            OpeningBalance = openingBalance,
            TotalCredits = totalCredits,
            TotalDebits = totalDebits,
            ClosingBalance = closingBalance,
            AvailableBalance = availableBalance,
            TransactionCount = items.Count
        };
        snapshot.transactions.AddRange(items.Select((item, index) =>
            StatementTransactionSnapshot.Create(id, index + 1, item)));
        return snapshot;
    }
}
