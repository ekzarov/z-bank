namespace BankOfZ.Domain.Transactions;

public sealed class CashTransactionValidationException(
    string code,
    IReadOnlyDictionary<string, string[]> errors) : Exception(code)
{
    public string Code { get; } = code;
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
