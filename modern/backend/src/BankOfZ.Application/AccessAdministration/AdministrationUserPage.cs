namespace BankOfZ.Application.AccessAdministration;

public sealed record AdministrationUserPage(
    IReadOnlyList<AdministrationUserView> Items,
    int Page,
    int PageSize,
    int Total);
