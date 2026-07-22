using BankOfZ.Domain.Accounts;
using BankOfZ.Domain.Customers;

namespace BankOfZ.Application.Accounts;

public sealed record AccountView(
    string Id,
    string CustomerId,
    string SortCode,
    AccountType Type,
    decimal InterestRate,
    int OverdraftLimit,
    string Currency,
    decimal ActualBalance,
    decimal AvailableBalance,
    DateOnly OpenedOn,
    DateOnly LastStatementOn,
    DateOnly NextStatementOn,
    AccountStatus Status,
    SourceSystem SourceSystem,
    string? SourceIdentifier,
    string? RawSourceType,
    string Version)
{
    public static AccountView From(Account account) => new(
        account.Id,
        account.CustomerId,
        account.SortCode,
        account.Type,
        account.InterestRate,
        account.OverdraftLimit,
        account.Currency,
        account.ActualBalance,
        account.AvailableBalance,
        account.OpenedOn,
        account.LastStatementOn,
        account.NextStatementOn,
        account.Status,
        account.SourceSystem,
        account.SourceIdentifier,
        account.RawSourceType,
        Convert.ToBase64String(account.Version));
}
