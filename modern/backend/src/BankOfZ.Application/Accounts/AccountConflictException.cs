namespace BankOfZ.Application.Accounts;

public sealed class AccountConflictException(string message, Exception? innerException = null) : Exception(message, innerException);
