using BankOfZ.Domain.Customers;

namespace BankOfZ.Application.Customers;

public interface ICreditAssessmentProvider
{
    Task<CreditAssessment?> AssessAsync(
        string provider,
        string customerId,
        CustomerDetails details,
        CancellationToken cancellationToken);
}
