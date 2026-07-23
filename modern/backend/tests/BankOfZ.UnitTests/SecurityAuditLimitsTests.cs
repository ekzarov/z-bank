using BankOfZ.Application.AccessAdministration;

namespace BankOfZ.UnitTests;

public sealed class SecurityAuditLimitsTests
{
    [Fact]
    public void NormalizeCorrelationId_Bounds_External_Values()
    {
        var externalValue = new string('x', SecurityAuditLimits.CorrelationIdMaxLength + 10);

        var result = SecurityAuditLimits.NormalizeCorrelationId(externalValue);

        Assert.Equal(SecurityAuditLimits.CorrelationIdMaxLength, result.Length);
        Assert.Equal(string.Empty, SecurityAuditLimits.NormalizeCorrelationId(null));
    }
}
