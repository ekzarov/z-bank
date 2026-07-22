namespace BankOfZ.Api.Security;

public static class AuthorizationPolicies
{
    public const string Customer = nameof(Customer);
    public const string Operator = nameof(Operator);
    public const string Administrator = nameof(Administrator);
}
