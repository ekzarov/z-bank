namespace BankOfZ.Application.AccessAdministration;

public interface ISecurityAuditWriter
{
    Task WriteAsync(SecurityAuditEntry entry, CancellationToken cancellationToken = default);
}
