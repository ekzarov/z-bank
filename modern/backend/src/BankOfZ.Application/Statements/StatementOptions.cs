namespace BankOfZ.Application.Statements;

public sealed class StatementOptions
{
    public const string SectionName = "Statements";
    public string TimeZoneId { get; set; } = "UTC";
    public string BankSortCode { get; set; } = "100000";
}
