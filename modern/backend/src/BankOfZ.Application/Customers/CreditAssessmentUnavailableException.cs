namespace BankOfZ.Application.Customers;

public sealed class CreditAssessmentUnavailableException()
    : Exception("No credit assessment provider returned a score.");
