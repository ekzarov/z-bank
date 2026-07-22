namespace BankOfZ.Domain.Customers;

public static class CustomerRules
{
    public const int IdLength = 10;
    public const int SortCodeLength = 6;
    public const int TitleMaxLength = 8;
    public const int NameMaxLength = 64;
    public const int AddressMaxLength = 128;
    public const int CityMaxLength = 64;
    public const int RegionMaxLength = 64;
    public const int PostalCodeMaxLength = 16;
    public const int CountryCodeLength = 2;
    public const int EmailMaxLength = 254;
    public const int PhoneMaxLength = 32;
    public const int SourceIdentifierMaxLength = 64;
    public const int MinimumAge = 18;
    public const int MaximumAge = 120;

    public static readonly string[] SupportedTitles = ["Mr", "Mrs", "Ms", "Miss", "Dr"];
}
