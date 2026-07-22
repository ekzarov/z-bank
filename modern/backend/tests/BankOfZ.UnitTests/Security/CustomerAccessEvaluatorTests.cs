using BankOfZ.Application.Security;
using BankOfZ.Domain.Security;

namespace BankOfZ.UnitTests.Security;

public sealed class CustomerAccessEvaluatorTests
{
    private readonly CustomerAccessEvaluator evaluator = new();

    [Fact]
    public void Customer_Can_Access_Own_Record()
    {
        var allowed = evaluator.CanAccessCustomer(
            [BankRoles.Customer],
            "1000000001",
            "1000000001");

        Assert.True(allowed);
    }

    [Fact]
    public void Customer_Cannot_Access_Another_Record()
    {
        var allowed = evaluator.CanAccessCustomer(
            [BankRoles.Customer],
            "1000000001",
            "1000000002");

        Assert.False(allowed);
    }

    [Theory]
    [InlineData(BankRoles.Operator)]
    [InlineData(BankRoles.Administrator)]
    public void Staff_Roles_Can_Access_Customer_Record(string role)
    {
        var allowed = evaluator.CanAccessCustomer([role], null, "1000000001");

        Assert.True(allowed);
    }

    [Fact]
    public void Customer_Without_Association_Is_Denied()
    {
        var allowed = evaluator.CanAccessCustomer(
            [BankRoles.Customer],
            null,
            "1000000001");

        Assert.False(allowed);
    }
}
