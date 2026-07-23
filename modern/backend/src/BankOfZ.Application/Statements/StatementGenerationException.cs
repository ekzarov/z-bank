namespace BankOfZ.Application.Statements;

public sealed class StatementGenerationException(string message, Exception? inner = null)
    : Exception(message, inner);
