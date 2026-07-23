namespace BankOfZ.Application.Statements;

public sealed class StatementValidationException(
    string code,
    IReadOnlyDictionary<string, string[]> errors) : Exception("Statement validation failed.")
{
    public string Code { get; } = code;
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
