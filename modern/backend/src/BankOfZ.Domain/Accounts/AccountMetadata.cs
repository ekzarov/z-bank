namespace BankOfZ.Domain.Accounts;

public sealed record AccountMetadata(
    AccountType Type,
    decimal InterestRate,
    int OverdraftLimit,
    string Currency);
