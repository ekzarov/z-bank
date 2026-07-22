using System.Security.Cryptography;
using System.Text;
using BankOfZ.Application.Customers;
using BankOfZ.Domain.Customers;

namespace BankOfZ.Infrastructure.Customers;

public sealed class DeterministicCreditAssessmentProvider(CustomerOptions options) : ICreditAssessmentProvider
{
    public Task<CreditAssessment?> AssessAsync(
        string provider,
        string customerId,
        CustomerDetails details,
        CancellationToken cancellationToken)
    {
        if (options.FailedCreditProviders.Contains(provider, StringComparer.OrdinalIgnoreCase))
        {
            return Task.FromResult<CreditAssessment?>(null);
        }

        var input = Encoding.UTF8.GetBytes($"{provider}|{customerId}|{details.DateOfBirth:O}");
        var hash = SHA256.HashData(input);
        var score = 300 + BitConverter.ToUInt16(hash, 0) % 551;
        return Task.FromResult<CreditAssessment?>(new CreditAssessment(provider, score));
    }
}
