namespace BankOfZ.Application.Accounts;

public sealed record AccountPage(IReadOnlyList<AccountView> Items, int Page, int PageSize, int Total);
