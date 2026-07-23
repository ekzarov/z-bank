namespace BankOfZ.Application.AccessAdministration;

public sealed record AdministrationUserView(
    Guid Id,
    string UserName,
    string Email,
    string Role,
    string? CustomerId,
    bool IsLockedOut,
    DateTimeOffset? LockoutEnd,
    int AccessFailedCount,
    string Version);
