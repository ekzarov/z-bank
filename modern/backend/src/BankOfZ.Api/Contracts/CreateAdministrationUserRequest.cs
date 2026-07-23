namespace BankOfZ.Api.Contracts;

public sealed record CreateAdministrationUserRequest(
    string UserName,
    string Email,
    string Password,
    string Role,
    string? CustomerId);
