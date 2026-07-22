namespace BankOfZ.Domain.Customers;

public sealed class CustomerValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("Customer data is invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
