namespace BankOfZ.Application.AccessAdministration;

public sealed class AccessAdministrationValidationException(
    IReadOnlyDictionary<string, string[]> errors) : Exception("Access administration data is invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
