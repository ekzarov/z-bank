using System.Net.Mail;

namespace BankOfZ.Domain.Customers;

public sealed class Customer
{
    private Customer()
    {
    }

    public string Id { get; private set; } = null!;
    public string SortCode { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string NormalizedName { get; private set; } = null!;
    public DateOnly DateOfBirth { get; private set; }
    public string AddressLine1 { get; private set; } = null!;
    public string? AddressLine2 { get; private set; }
    public string City { get; private set; } = null!;
    public string? Region { get; private set; }
    public string PostalCode { get; private set; } = null!;
    public string CountryCode { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string? Phone { get; private set; }
    public CustomerStatus Status { get; private set; }
    public decimal CreditScore { get; private set; }
    public DateOnly CreditReviewDate { get; private set; }
    public SourceSystem SourceSystem { get; private set; }
    public string? SourceIdentifier { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public byte[] Version { get; private set; } = [];

    public static Customer Create(
        string id,
        string sortCode,
        CustomerDetails details,
        decimal creditScore,
        DateOnly creditReviewDate,
        SourceSystem sourceSystem,
        string? sourceIdentifier,
        DateTimeOffset now)
    {
        ValidateIdentifier(id, CustomerRules.IdLength, nameof(id));
        ValidateIdentifier(sortCode, CustomerRules.SortCodeLength, nameof(sortCode));
        ValidateDetails(details, DateOnly.FromDateTime(now.UtcDateTime));
        ValidateText(sourceIdentifier, CustomerRules.SourceIdentifierMaxLength, nameof(sourceIdentifier), false);
        if (creditScore is < 0 or > 999)
        {
            throw Validation(nameof(creditScore), "Credit score must be between 0 and 999.");
        }

        var customer = new Customer
        {
            Id = id,
            SortCode = sortCode,
            Status = CustomerStatus.Active,
            CreditScore = decimal.Round(creditScore, 2),
            CreditReviewDate = creditReviewDate,
            SourceSystem = sourceSystem,
            SourceIdentifier = NormalizeOptional(sourceIdentifier),
            CreatedAt = now,
            UpdatedAt = now
        };
        customer.Apply(details);
        return customer;
    }

    public void Update(CustomerDetails details, DateTimeOffset now)
    {
        EnsureActive();
        ValidateDetails(details, DateOnly.FromDateTime(now.UtcDateTime));
        Apply(details);
        UpdatedAt = now;
    }

    public void Retire(bool hasBlockingAccounts, bool hasUnresolvedObligations, DateTimeOffset now)
    {
        EnsureActive();
        if (hasBlockingAccounts || hasUnresolvedObligations)
        {
            throw Validation(nameof(Status), "Customer has active accounts or unresolved obligations.");
        }

        Status = CustomerStatus.Retired;
        UpdatedAt = now;
    }

    public static string NormalizeName(string value) =>
        string.Join(' ', value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();

    private void Apply(CustomerDetails details)
    {
        Title = details.Title.Trim();
        FirstName = details.FirstName.Trim();
        LastName = details.LastName.Trim();
        NormalizedName = NormalizeName($"{FirstName} {LastName}");
        DateOfBirth = details.DateOfBirth;
        AddressLine1 = details.AddressLine1.Trim();
        AddressLine2 = NormalizeOptional(details.AddressLine2);
        City = details.City.Trim();
        Region = NormalizeOptional(details.Region);
        PostalCode = details.PostalCode.Trim();
        CountryCode = details.CountryCode.Trim().ToUpperInvariant();
        Email = details.Email.Trim().ToLowerInvariant();
        Phone = NormalizeOptional(details.Phone);
    }

    private void EnsureActive()
    {
        if (Status != CustomerStatus.Active)
        {
            throw Validation(nameof(Status), "Retired customers cannot be changed.");
        }
    }

    private static void ValidateDetails(CustomerDetails details, DateOnly today)
    {
        if (!CustomerRules.SupportedTitles.Contains(details.Title, StringComparer.OrdinalIgnoreCase))
        {
            throw Validation(nameof(details.Title), "Title is not supported.");
        }

        ValidateText(details.FirstName, CustomerRules.NameMaxLength, nameof(details.FirstName), true);
        ValidateText(details.LastName, CustomerRules.NameMaxLength, nameof(details.LastName), true);
        ValidateText(details.AddressLine1, CustomerRules.AddressMaxLength, nameof(details.AddressLine1), true);
        ValidateText(details.AddressLine2, CustomerRules.AddressMaxLength, nameof(details.AddressLine2), false);
        ValidateText(details.City, CustomerRules.CityMaxLength, nameof(details.City), true);
        ValidateText(details.Region, CustomerRules.RegionMaxLength, nameof(details.Region), false);
        ValidateText(details.PostalCode, CustomerRules.PostalCodeMaxLength, nameof(details.PostalCode), true);
        ValidateText(details.CountryCode, CustomerRules.CountryCodeLength, nameof(details.CountryCode), true, exact: true);
        ValidateText(details.Email, CustomerRules.EmailMaxLength, nameof(details.Email), true);
        ValidateText(details.Phone, CustomerRules.PhoneMaxLength, nameof(details.Phone), false);

        try
        {
            _ = new MailAddress(details.Email);
        }
        catch (FormatException)
        {
            throw Validation(nameof(details.Email), "Email format is invalid.");
        }

        var age = today.Year - details.DateOfBirth.Year;
        if (details.DateOfBirth > today.AddYears(-age))
        {
            age--;
        }
        if (age is < CustomerRules.MinimumAge or > CustomerRules.MaximumAge)
        {
            throw Validation(nameof(details.DateOfBirth), "Customer age is outside the supported range.");
        }
    }

    private static void ValidateIdentifier(string value, int length, string field)
    {
        if (value.Length != length || value.Any(character => !char.IsAsciiDigit(character)))
        {
            throw Validation(field, $"{field} must contain exactly {length} digits.");
        }
    }

    private static void ValidateText(string? value, int maxLength, string field, bool required, bool exact = false)
    {
        var normalized = value?.Trim();
        if (required && string.IsNullOrEmpty(normalized))
        {
            throw Validation(field, $"{field} is required.");
        }
        if (normalized is not null && (normalized.Length > maxLength || exact && normalized.Length != maxLength))
        {
            throw Validation(field, exact ? $"{field} must be exactly {maxLength} characters." : $"{field} exceeds {maxLength} characters.");
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static CustomerValidationException Validation(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
