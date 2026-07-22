using BankOfZ.Domain.Customers;

namespace BankOfZ.UnitTests.Customers;

public sealed class CustomerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 22, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Preserves_Leading_Zeroes_And_Normalizes_Name()
    {
        var customer = CreateCustomer("0000000042", Details(firstName: "  Jamie ", lastName: " van   Doe "));

        Assert.Equal("0000000042", customer.Id);
        Assert.Equal("JAMIE VAN DOE", customer.NormalizedName);
        Assert.Equal("jamie@example.test", customer.Email);
    }

    [Theory]
    [InlineData("Professor")]
    [InlineData("")]
    public void Create_Rejects_Unsupported_Title(string title)
    {
        Assert.Throws<CustomerValidationException>(() => CreateCustomer("0000000042", Details(title: title)));
    }

    [Fact]
    public void Create_Accepts_Minimum_Age_Boundary()
    {
        var customer = CreateCustomer("0000000042", Details(dateOfBirth: new DateOnly(2008, 7, 22)));

        Assert.Equal(new DateOnly(2008, 7, 22), customer.DateOfBirth);
    }

    [Fact]
    public void Create_Rejects_Customer_Younger_Than_Minimum()
    {
        Assert.Throws<CustomerValidationException>(() =>
            CreateCustomer("0000000042", Details(dateOfBirth: new DateOnly(2008, 7, 23))));
    }

    [Fact]
    public void Retire_Rejects_Blocking_Accounts_And_Remains_Active()
    {
        var customer = CreateCustomer("0000000042", Details());

        Assert.Throws<CustomerValidationException>(() => customer.Retire(true, false, Now.AddDays(1)));
        Assert.Equal(CustomerStatus.Active, customer.Status);
    }

    [Fact]
    public void Retire_Is_A_Soft_State_Transition()
    {
        var customer = CreateCustomer("0000000042", Details());

        customer.Retire(false, false, Now.AddDays(1));

        Assert.Equal(CustomerStatus.Retired, customer.Status);
        Assert.Equal("Jamie", customer.FirstName);
    }

    private static Customer CreateCustomer(string id, CustomerDetails details) => Customer.Create(
        id,
        "100000",
        details,
        700,
        new DateOnly(2026, 8, 12),
        SourceSystem.Modern,
        null,
        Now);

    internal static CustomerDetails Details(
        string title = "Ms",
        string firstName = "Jamie",
        string lastName = "Doe",
        DateOnly? dateOfBirth = null) => new(
        title,
        firstName,
        lastName,
        dateOfBirth ?? new DateOnly(1990, 5, 12),
        "1 Test Street",
        null,
        "London",
        null,
        "EC1A 1AA",
        "GB",
        "Jamie@Example.Test",
        null);
}
