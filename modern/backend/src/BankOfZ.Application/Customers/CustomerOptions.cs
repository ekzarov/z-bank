namespace BankOfZ.Application.Customers;

public sealed class CustomerOptions
{
    public const string SectionName = "Customers";
    public static readonly string[] DefaultCreditProviders = ["North", "South", "East", "West", "Central"];

    public string SortCode { get; set; } = "100000";
    public string[] CreditProviders { get; set; } = [];
    public string[] FailedCreditProviders { get; set; } = [];
}
