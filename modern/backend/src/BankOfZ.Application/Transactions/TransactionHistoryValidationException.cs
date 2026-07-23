namespace BankOfZ.Application.Transactions;

public sealed class TransactionHistoryValidationException(
    string code,
    IReadOnlyDictionary<string, string[]> errors) : Exception(code)
{
    public string Code { get; } = code;
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
