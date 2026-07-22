namespace BankOfZ.Api.Contracts;

public sealed record LoginRequest(string UserName, string Password, bool RememberMe = false);
