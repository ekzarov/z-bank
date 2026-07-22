namespace BankOfZ.Application.Customers;

public sealed class CustomerNotFoundException(string customerId)
    : Exception($"Customer '{customerId}' was not found.");
