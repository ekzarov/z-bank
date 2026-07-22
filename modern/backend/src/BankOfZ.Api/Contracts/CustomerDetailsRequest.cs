using BankOfZ.Domain.Customers;

namespace BankOfZ.Api.Contracts;

public sealed record CustomerDetailsRequest(
    string Title,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? Region,
    string PostalCode,
    string CountryCode,
    string Email,
    string? Phone)
{
    public CustomerDetails ToDomain() => new(
        Title,
        FirstName,
        LastName,
        DateOfBirth,
        AddressLine1,
        AddressLine2,
        City,
        Region,
        PostalCode,
        CountryCode,
        Email,
        Phone);
}
