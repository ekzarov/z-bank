namespace BankOfZ.Domain.Transactions;

public static class CashTransactionRules
{
    public const int ReferenceLength = 32;
    public const int IdempotencyKeyMaxLength = 64;
    public const int RequestFingerprintMaxLength = 128;
    public const decimal MaximumAmount = 9999999999999999.99m;
}
