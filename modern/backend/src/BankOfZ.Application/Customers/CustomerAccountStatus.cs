namespace BankOfZ.Application.Customers;

public sealed record CustomerAccountStatus(bool HasActiveAccounts, bool HasUnresolvedObligations);
