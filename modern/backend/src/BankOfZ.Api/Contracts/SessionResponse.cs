namespace BankOfZ.Api.Contracts;

public sealed record SessionResponse(
    bool IsAuthenticated,
    string? UserName,
    string? CustomerId,
    IReadOnlyCollection<string> Roles);
