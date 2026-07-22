using BankOfZ.Domain.Security;

namespace BankOfZ.Application.Security;

public sealed class CustomerAccessEvaluator
{
    public bool CanAccessCustomer(
        IReadOnlyCollection<string> roles,
        string? associatedCustomerId,
        string requestedCustomerId)
    {
        if (roles.Contains(BankRoles.Operator) || roles.Contains(BankRoles.Administrator))
        {
            return true;
        }

        return roles.Contains(BankRoles.Customer)
            && associatedCustomerId is not null
            && string.Equals(associatedCustomerId, requestedCustomerId, StringComparison.Ordinal);
    }
}
