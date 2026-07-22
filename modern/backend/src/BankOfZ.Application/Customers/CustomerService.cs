using BankOfZ.Application.Common;
using BankOfZ.Domain.Customers;

namespace BankOfZ.Application.Customers;

public sealed class CustomerService(
    ICustomerRepository repository,
    ICreditAssessmentProvider creditProvider,
    ICustomerAccountStatusReader accountStatusReader,
    ICustomerAuditWriter auditWriter,
    IClock clock,
    CustomerOptions options)
{
    public async Task<CustomerView?> FindAsync(string id, CancellationToken cancellationToken) =>
        await repository.FindAsync(id, false, cancellationToken) is { } customer
            ? CustomerView.From(customer)
            : null;

    public async Task<IReadOnlyList<CustomerView>> SearchAsync(
        string name,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalized = Customer.NormalizeName(name);
        var customers = await repository.SearchAsync(normalized, Math.Max(1, page), Math.Clamp(pageSize, 1, 50), cancellationToken);
        return customers.Select(CustomerView.From).ToArray();
    }

    public async Task<CustomerView> CreateAsync(
        CustomerDetails details,
        SourceSystem sourceSystem,
        string? sourceIdentifier,
        string actor,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (options.CreditProviders.Length != 5 || options.CreditProviders.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 5)
        {
            throw new InvalidOperationException("Exactly five distinct credit providers must be configured.");
        }

        var id = await repository.AllocateIdAsync(cancellationToken);
        var assessments = await Task.WhenAll(options.CreditProviders.Select(provider =>
            creditProvider.AssessAsync(provider, id, details, cancellationToken)));
        var successful = assessments.OfType<CreditAssessment>().ToArray();
        if (successful.Length == 0)
        {
            throw new CreditAssessmentUnavailableException();
        }

        var now = clock.UtcNow;
        var score = successful.Average(assessment => (decimal)assessment.Score);
        var customer = Customer.Create(
            id,
            options.SortCode,
            details,
            score,
            DateOnly.FromDateTime(now.UtcDateTime).AddDays(21),
            sourceSystem,
            sourceIdentifier,
            now);
        repository.Add(customer);
        auditWriter.Add(new CustomerAuditEntry(actor, now, "CustomerCreated", id, "Succeeded", correlationId));
        await repository.SaveChangesAsync(cancellationToken);
        return CustomerView.From(customer);
    }

    public async Task<CustomerView> UpdateAsync(
        string id,
        CustomerDetails details,
        string version,
        string actor,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var customer = await GetTrackedAsync(id, cancellationToken);
        repository.SetExpectedVersion(customer, DecodeVersion(version));
        customer.Update(details, clock.UtcNow);
        auditWriter.Add(new CustomerAuditEntry(actor, clock.UtcNow, "CustomerUpdated", id, "Succeeded", correlationId));
        await SaveWithConflictAsync(cancellationToken);
        return CustomerView.From(customer);
    }

    public async Task RetireAsync(
        string id,
        string version,
        string actor,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var customer = await GetTrackedAsync(id, cancellationToken);
        repository.SetExpectedVersion(customer, DecodeVersion(version));
        var accountStatus = await accountStatusReader.GetAsync(id, cancellationToken);
        customer.Retire(accountStatus.HasActiveAccounts, accountStatus.HasUnresolvedObligations, clock.UtcNow);
        auditWriter.Add(new CustomerAuditEntry(actor, clock.UtcNow, "CustomerRetired", id, "Succeeded", correlationId));
        await SaveWithConflictAsync(cancellationToken);
    }

    private async Task<Customer> GetTrackedAsync(string id, CancellationToken cancellationToken) =>
        await repository.FindAsync(id, true, cancellationToken) ?? throw new CustomerNotFoundException(id);

    private async Task SaveWithConflictAsync(CancellationToken cancellationToken)
    {
        try
        {
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch (CustomerConflictException)
        {
            throw;
        }
    }

    private static byte[] DecodeVersion(string value)
    {
        try
        {
            return Convert.FromBase64String(value);
        }
        catch (FormatException)
        {
            throw new CustomerConflictException("Customer version is invalid.");
        }
    }
}
