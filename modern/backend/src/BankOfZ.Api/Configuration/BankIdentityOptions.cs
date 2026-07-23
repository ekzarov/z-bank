using System.ComponentModel.DataAnnotations;

namespace BankOfZ.Api.Configuration;

public sealed class BankIdentityOptions
{
    public const string SectionName = "Bank";

    [Required, StringLength(80, MinimumLength = 2)]
    public string DisplayName { get; init; } = "Bank of Z";

    [Required, RegularExpression(@"^\d{6}$")]
    public string SortCode { get; init; } = "100000";
}
