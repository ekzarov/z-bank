namespace BankOfZ.Api.Security;

public interface ISecurityAudit
{
    void Record(string eventName, string? subjectId, bool succeeded, string outcome);
}
