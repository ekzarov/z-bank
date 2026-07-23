using BankOfZ.Setup;

namespace BankOfZ.UnitTests.DataInitialization;

public sealed class SetupAuthorizationTests
{
    private static readonly object EnvironmentLock = new();

    [Fact]
    public void Reset_requires_exact_destructive_confirmation()
    {
        lock (EnvironmentLock)
        {
            WithEnvironment("BANKOFZ_SETUP_ALLOWED_ENVIRONMENTS", "Demo", () =>
                Assert.Throws<UnauthorizedAccessException>(() =>
                    SetupAuthorization.EnsureResetAllowed("Demo", "yes")));
        }
    }

    [Fact]
    public void Reset_rejects_unauthorized_environment()
    {
        lock (EnvironmentLock)
        {
            WithEnvironment("BANKOFZ_SETUP_ALLOWED_ENVIRONMENTS", "Demo", () =>
                Assert.Throws<UnauthorizedAccessException>(() =>
                    SetupAuthorization.EnsureResetAllowed("Test", "RESET-BANK-OF-Z")));
        }
    }

    [Fact]
    public void Production_reset_is_disabled_by_default()
    {
        lock (EnvironmentLock)
        {
            WithEnvironment("BANKOFZ_SETUP_ALLOWED_ENVIRONMENTS", "Production", () =>
                WithEnvironment("BANKOFZ_ALLOW_PRODUCTION_RESET", null, () =>
                    Assert.Throws<UnauthorizedAccessException>(() =>
                        SetupAuthorization.EnsureResetAllowed("Production", "RESET-BANK-OF-Z"))));
        }
    }

    [Fact]
    public void Authorized_demo_reset_is_allowed()
    {
        lock (EnvironmentLock)
        {
            WithEnvironment("BANKOFZ_SETUP_ALLOWED_ENVIRONMENTS", "Demo", () =>
                SetupAuthorization.EnsureResetAllowed("Demo", "RESET-BANK-OF-Z"));
        }
    }

    private static void WithEnvironment(string name, string? value, Action action)
    {
        var original = Environment.GetEnvironmentVariable(name);
        try
        {
            Environment.SetEnvironmentVariable(name, value);
            action();
        }
        finally
        {
            Environment.SetEnvironmentVariable(name, original);
        }
    }
}
