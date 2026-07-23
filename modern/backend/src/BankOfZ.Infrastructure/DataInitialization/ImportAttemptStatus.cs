namespace BankOfZ.Infrastructure.DataInitialization;

public enum ImportAttemptStatus
{
    Validating,
    Promoting,
    Succeeded,
    FailedValidation,
    FailedPromotion,
    RejectedActive
}
