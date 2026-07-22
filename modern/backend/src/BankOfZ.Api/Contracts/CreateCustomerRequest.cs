using BankOfZ.Domain.Customers;

namespace BankOfZ.Api.Contracts;

public sealed record CreateCustomerRequest(
    CustomerDetailsRequest Details,
    SourceSystem SourceSystem = SourceSystem.Modern,
    string? SourceIdentifier = null);
