namespace BankOfZ.Domain.Accounts;

public static class AccountRules
{
    public const int IdLength = 8;
    public const int SortCodeLength = 6;
    public const int CurrencyLength = 3;
    public const int RawSourceValueMaxLength = 32;
    public const int SourceIdentifierMaxLength = 128;
    public const int MaximumAccountsPerCustomer = 10;
    public const decimal MaximumInterestRate = 9999.99m;
}
