namespace BankOfZ.Application.AccessAdministration;

public sealed record CreateAdministrationUserCommand(
    string UserName,
    string Email,
    string Password,
    string Role,
    string? CustomerId);
