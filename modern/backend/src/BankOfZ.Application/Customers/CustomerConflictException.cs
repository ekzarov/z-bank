namespace BankOfZ.Application.Customers;

public sealed class CustomerConflictException(string message, Exception? innerException = null)
    : Exception(message, innerException);
