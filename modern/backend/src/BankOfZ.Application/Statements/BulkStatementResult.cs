namespace BankOfZ.Application.Statements;

public sealed record BulkStatementAccountResult(
    string AccountId,
    bool Succeeded,
    Guid? GenerationId,
    bool Reused,
    string? Error);

public sealed record BulkStatementResult(
    int Year,
    int Month,
    int Total,
    int Succeeded,
    int Failed,
    IReadOnlyList<BulkStatementAccountResult> Accounts);
