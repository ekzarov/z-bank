namespace BankOfZ.Domain.Accounts;

public static class LegacyAccountTypeMapper
{
    public static AccountType Map(string value) => value.Trim().ToUpperInvariant() switch
    {
        "ISA" => AccountType.Isa,
        "CURRENT" or "CURRENT_" or "CHECKING" => AccountType.Current,
        "LOAN" => AccountType.Loan,
        "SAVING" or "SAVINGS" => AccountType.Saving,
        "MORTGAGE" => AccountType.Mortgage,
        _ => throw new AccountValidationException(new Dictionary<string, string[]>
        {
            ["accountType"] = [$"Unsupported legacy account type '{value}'."]
        })
    };
}
