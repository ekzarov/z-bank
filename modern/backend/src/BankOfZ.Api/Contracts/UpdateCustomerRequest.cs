namespace BankOfZ.Api.Contracts;

public sealed record UpdateCustomerRequest(CustomerDetailsRequest Details, string Version);
