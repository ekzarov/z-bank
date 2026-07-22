using BankOfZ.Domain.Accounts;
using BankOfZ.Domain.Customers;

namespace BankOfZ.UnitTests.Accounts;

public sealed class AccountTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 22, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("CURRENT", AccountType.Current)]
    [InlineData("CHECKING", AccountType.Current)]
    [InlineData("CURRENT_", AccountType.Current)]
    [InlineData("SAVINGS", AccountType.Saving)]
    [InlineData("ISA", AccountType.Isa)]
    [InlineData("LOAN", AccountType.Loan)]
    [InlineData("MORTGAGE", AccountType.Mortgage)]
    public void Legacy_Type_Mapping_Normalizes_All_Known_Values(string raw, AccountType expected)
    {
        Assert.Equal(expected, LegacyAccountTypeMapper.Map(raw));
    }

    [Fact]
    public void Creation_Assigns_System_Managed_Identity_Dates_And_Zero_Balances()
    {
        var account = Create();

        Assert.Equal("10000001", account.Id);
        Assert.Equal("100000", account.SortCode);
        Assert.Equal(new DateOnly(2026, 7, 22), account.OpenedOn);
        Assert.Equal(account.OpenedOn, account.LastStatementOn);
        Assert.Equal(new DateOnly(2026, 8, 22), account.NextStatementOn);
        Assert.Equal(0, account.ActualBalance);
        Assert.Equal(0, account.AvailableBalance);
        Assert.Equal(AccountStatus.Active, account.Status);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(10000)]
    [InlineData(1.001)]
    public void Interest_Outside_Exact_Legacy_Boundary_Is_Rejected(decimal rate)
    {
        Assert.Throws<AccountValidationException>(() => Create(interestRate: rate));
    }

    [Theory]
    [InlineData(AccountType.Loan)]
    [InlineData(AccountType.Mortgage)]
    public void Lending_Products_Require_Non_Zero_Interest(AccountType type)
    {
        Assert.Throws<AccountValidationException>(() => Create(type, 0));
    }

    [Fact]
    public void Metadata_Update_Cannot_Mutate_Balances_Or_Statement_Dates()
    {
        var account = Create();
        var lastStatement = account.LastStatementOn;
        var nextStatement = account.NextStatementOn;

        account.UpdateMetadata(new AccountMetadata(AccountType.Isa, 4.25m, 0, "gbp"), Now.AddDays(1));

        Assert.Equal(0, account.ActualBalance);
        Assert.Equal(0, account.AvailableBalance);
        Assert.Equal(lastStatement, account.LastStatementOn);
        Assert.Equal(nextStatement, account.NextStatementOn);
        Assert.Equal("GBP", account.Currency);
    }

    [Fact]
    public void Eligible_Account_Closes_As_Soft_State_Transition()
    {
        var account = Create();

        account.Close(Now.AddDays(1));

        Assert.Equal(AccountStatus.Closed, account.Status);
        Assert.Throws<AccountValidationException>(() =>
            account.UpdateMetadata(new AccountMetadata(AccountType.Current, 0, 0, "GBP"), Now.AddDays(2)));
    }

    [Fact]
    public void Invalid_Identifiers_And_Negative_Overdraft_Are_Rejected()
    {
        Assert.Throws<AccountValidationException>(() => Account.Create(
            "123", "1000000001", "100000",
            new AccountMetadata(AccountType.Current, 0, -1, "GBP"),
            SourceSystem.Modern, null, null, Now));
    }

    private static Account Create(AccountType type = AccountType.Current, decimal interestRate = 0) => Account.Create(
        "10000001",
        "1000000001",
        "100000",
        new AccountMetadata(type, interestRate, 500, "GBP"),
        SourceSystem.Modern,
        "unit-test",
        type.ToString().ToUpperInvariant(),
        Now);
}
