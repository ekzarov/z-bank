namespace BankOfZ.Domain.Accounts;

public sealed class AccountValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("Account data is invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
