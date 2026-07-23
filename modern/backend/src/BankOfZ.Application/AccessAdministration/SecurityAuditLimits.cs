namespace BankOfZ.Application.AccessAdministration;

public static class SecurityAuditLimits
{
    public const int CorrelationIdMaxLength = 64;

    public static string NormalizeCorrelationId(string? value)
    {
        var correlationId = value ?? string.Empty;
        return correlationId.Length <= CorrelationIdMaxLength
            ? correlationId
            : correlationId[..CorrelationIdMaxLength];
    }
}
