namespace BankOfZ.Api.Contracts;

public sealed record ChangeAdministrationRoleRequest(
    string Role,
    string? CustomerId,
    string Version);
