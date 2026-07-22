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
    public void Create_Accepts_Maximum_Age_Boundary()
    {
        var customer = CreateCustomer("0000000042", Details(dateOfBirth: new DateOnly(1906, 7, 22)));

        Assert.Equal(new DateOnly(1906, 7, 22), customer.DateOfBirth);
    }

    [Fact]
    public void Create_Rejects_Customer_Older_Than_Maximum()
    {
        Assert.Throws<CustomerValidationException>(() =>
            CreateCustomer("0000000042", Details(dateOfBirth: new DateOnly(1905, 7, 22))));
    }

    [Theory]
    [InlineData("42")]
    [InlineData("000000004A")]
    public void Create_Rejects_Invalid_Customer_Identifier(string id)
    {
        Assert.Throws<CustomerValidationException>(() => CreateCustomer(id, Details()));
    }

    [Theory]
    [InlineData("10000")]
    [InlineData("10000A")]
    public void Create_Rejects_Invalid_Sort_Code(string sortCode)
    {
        Assert.Throws<CustomerValidationException>(() => CreateCustomer("0000000042", Details(), sortCode));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(999.01)]
    public void Create_Rejects_Credit_Score_Outside_Range(double score)
    {
        Assert.Throws<CustomerValidationException>(() =>
            CreateCustomer("0000000042", Details(), creditScore: (decimal)score));
    }

    [Fact]
    public void Create_Accepts_Exact_Text_Length_Boundaries()
    {
        var customer = CreateCustomer("0000000042", Details(
            firstName: new string('F', CustomerRules.NameMaxLength),
            lastName: new string('L', CustomerRules.NameMaxLength),
            addressLine1: new string('A', CustomerRules.AddressMaxLength),
            city: new string('C', CustomerRules.CityMaxLength),
            region: new string('R', CustomerRules.RegionMaxLength),
            postalCode: new string('P', CustomerRules.PostalCodeMaxLength),
            countryCode: "GB",
            phone: new string('1', CustomerRules.PhoneMaxLength)));

        Assert.Equal(CustomerRules.NameMaxLength, customer.FirstName.Length);
        Assert.Equal(CustomerRules.AddressMaxLength, customer.AddressLine1.Length);
    }

    [Fact]
    public void Create_Rejects_Required_And_Overlong_Fields()
    {
        Assert.Throws<CustomerValidationException>(() => CreateCustomer(
            "0000000042",
            Details(addressLine1: " ")));
        Assert.Throws<CustomerValidationException>(() => CreateCustomer(
            "0000000042",
            Details(firstName: new string('F', CustomerRules.NameMaxLength + 1))));
        Assert.Throws<CustomerValidationException>(() => CreateCustomer(
            "0000000042",
            Details(postalCode: new string('P', CustomerRules.PostalCodeMaxLength + 1))));
    }

    [Theory]
    [InlineData("G")]
    [InlineData("GBR")]
    public void Create_Rejects_Country_Code_Without_Exact_Length(string countryCode)
    {
        Assert.Throws<CustomerValidationException>(() =>
            CreateCustomer("0000000042", Details(countryCode: countryCode)));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("")]
    public void Create_Rejects_Invalid_Email(string email)
    {
        Assert.Throws<CustomerValidationException>(() =>
            CreateCustomer("0000000042", Details(email: email)));
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

    private static Customer CreateCustomer(
        string id,
        CustomerDetails details,
        string sortCode = "100000",
        decimal creditScore = 700) => Customer.Create(
        id,
        sortCode,
        details,
        creditScore,
        new DateOnly(2026, 8, 12),
        SourceSystem.Modern,
        null,
        Now);

    internal static CustomerDetails Details(
        string title = "Ms",
        string firstName = "Jamie",
        string lastName = "Doe",
        DateOnly? dateOfBirth = null,
        string addressLine1 = "1 Test Street",
        string? addressLine2 = null,
        string city = "London",
        string? region = null,
        string postalCode = "EC1A 1AA",
        string countryCode = "GB",
        string email = "Jamie@Example.Test",
        string? phone = null) => new(
        title,
        firstName,
        lastName,
        dateOfBirth ?? new DateOnly(1990, 5, 12),
        addressLine1,
        addressLine2,
        city,
        region,
        postalCode,
        countryCode,
        email,
        phone);
}
