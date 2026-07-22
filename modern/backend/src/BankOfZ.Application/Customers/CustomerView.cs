using BankOfZ.Domain.Customers;

namespace BankOfZ.Application.Customers;

public sealed record CustomerView(
    string Id,
    string SortCode,
    CustomerDetails Details,
    CustomerStatus Status,
    decimal CreditScore,
    DateOnly CreditReviewDate,
    SourceSystem SourceSystem,
    string? SourceIdentifier,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string Version)
{
    public static CustomerView From(Customer customer) => new(
        customer.Id,
        customer.SortCode,
        new CustomerDetails(
            customer.Title,
            customer.FirstName,
            customer.LastName,
            customer.DateOfBirth,
            customer.AddressLine1,
            customer.AddressLine2,
            customer.City,
            customer.Region,
            customer.PostalCode,
            customer.CountryCode,
            customer.Email,
            customer.Phone),
        customer.Status,
        customer.CreditScore,
        customer.CreditReviewDate,
        customer.SourceSystem,
        customer.SourceIdentifier,
        customer.CreatedAt,
        customer.UpdatedAt,
        Convert.ToBase64String(customer.Version));
}
