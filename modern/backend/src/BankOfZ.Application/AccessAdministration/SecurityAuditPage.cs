namespace BankOfZ.Application.AccessAdministration;

public sealed record SecurityAuditPage(
    IReadOnlyList<SecurityAuditView> Items,
    int Page,
    int PageSize,
    int Total);
