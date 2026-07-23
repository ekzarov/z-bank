namespace BankOfZ.Api.Contracts;

public sealed record GenerateStatementRequest(int Year, int Month, string[]? AccountIds = null);
