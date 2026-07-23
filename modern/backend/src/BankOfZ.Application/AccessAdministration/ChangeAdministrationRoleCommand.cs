namespace BankOfZ.Application.AccessAdministration;

public sealed record ChangeAdministrationRoleCommand(
    string Role,
    string? CustomerId,
    string Version);
