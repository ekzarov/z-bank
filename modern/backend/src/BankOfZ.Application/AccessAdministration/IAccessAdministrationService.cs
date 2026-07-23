namespace BankOfZ.Application.AccessAdministration;

public interface IAccessAdministrationService
{
    Task<AdministrationUserPage> SearchUsersAsync(
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<AdministrationUserView> FindUserAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<AdministrationUserView> CreateUserAsync(
        CreateAdministrationUserCommand command,
        Guid actorId,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<AdministrationUserView> ChangeRoleAsync(
        Guid id,
        ChangeAdministrationRoleCommand command,
        Guid actorId,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<AdministrationUserView> ChangeLockoutAsync(
        Guid id,
        ChangeAdministrationLockoutCommand command,
        Guid actorId,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<SecurityAuditPage> SearchSecurityAuditAsync(
        string? eventName,
        string? actorOrSubject,
        bool? succeeded,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
