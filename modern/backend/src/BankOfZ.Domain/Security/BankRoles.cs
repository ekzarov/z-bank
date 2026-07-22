namespace BankOfZ.Domain.Security;

public static class BankRoles
{
    public const string Customer = nameof(Customer);
    public const string Operator = nameof(Operator);
    public const string Administrator = nameof(Administrator);

    public static readonly IReadOnlyList<string> All =
        [Customer, Operator, Administrator];
}
