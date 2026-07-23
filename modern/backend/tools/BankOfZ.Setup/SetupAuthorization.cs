namespace BankOfZ.Setup;

public static class SetupAuthorization
{
    public static void EnsureResetAllowed(string environment, string confirmation)
    {
        if (!string.Equals(confirmation, "RESET-BANK-OF-Z", StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("The destructive reset confirmation is invalid.");
        }
        if (string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(
                Environment.GetEnvironmentVariable("BANKOFZ_ALLOW_PRODUCTION_RESET"),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Production reset is disabled.");
        }

        var allowed = (Environment.GetEnvironmentVariable("BANKOFZ_SETUP_ALLOWED_ENVIRONMENTS") ?? "Demo,Development,Test")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!allowed.Contains(environment, StringComparer.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException($"Reset is not authorized for environment '{environment}'.");
        }
    }
}
