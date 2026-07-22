namespace BankOfZ.Domain.Customers;

public sealed record CustomerDetails(
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
    string? Phone);
