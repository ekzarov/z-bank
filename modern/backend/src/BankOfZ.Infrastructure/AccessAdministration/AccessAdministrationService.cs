using System.Data;
using BankOfZ.Application.AccessAdministration;
using BankOfZ.Application.Common;
using BankOfZ.Domain.Customers;
using BankOfZ.Domain.Security;
using BankOfZ.Infrastructure.Identity;
using BankOfZ.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BankOfZ.Infrastructure.AccessAdministration;

public sealed class AccessAdministrationService(
    BankOfZIdentityContext context,
    UserManager<ApplicationUser> users,
    ISecurityAuditWriter audit,
    IClock clock) : IAccessAdministrationService
{
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 50;

    public async Task<AdministrationUserPage> SearchUsersAsync(
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ValidatePage(page, pageSize);
        var normalized = string.IsNullOrWhiteSpace(query)
            ? null
            : users.NormalizeName(query.Trim());
        var source = context.Users.AsNoTracking();
        if (normalized is not null)
        {
            source = source.Where(user =>
                (user.NormalizedUserName != null && user.NormalizedUserName.Contains(normalized)) ||
                (user.NormalizedEmail != null && user.NormalizedEmail.Contains(normalized)));
        }

        var total = await source.CountAsync(cancellationToken);
        var pageUsers = await source
            .OrderBy(user => user.NormalizedUserName)
            .ThenBy(user => user.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
        return new(
            await MapUsersAsync(pageUsers, cancellationToken),
            page,
            pageSize,
            total);
    }

    public async Task<AdministrationUserView> FindUserAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var user = await context.Users.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            ?? throw new AccessAdministrationNotFoundException();
        return (await MapUsersAsync([user], cancellationToken))[0];
    }

    public async Task<AdministrationUserView> CreateUserAsync(
        CreateAdministrationUserCommand command,
        Guid actorId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var subjectId = Guid.NewGuid();
        try
        {
            return await ExecuteMutationAsync(async () =>
            {
                var role = ValidateRole(command.Role);
                var customerId = await ValidateCustomerAssociationAsync(
                    role,
                    command.CustomerId,
                    cancellationToken);
                var user = new ApplicationUser
                {
                    Id = subjectId,
                    UserName = command.UserName.Trim(),
                    Email = command.Email.Trim(),
                    EmailConfirmed = true,
                    CustomerId = customerId,
                    LockoutEnabled = true
                };
                EnsureSucceeded(await users.CreateAsync(user, command.Password));
                EnsureSucceeded(await users.AddToRoleAsync(user, role));
                await audit.WriteAsync(new(
                    "user-created",
                    actorId.ToString(),
                    user.Id.ToString(),
                    true,
                    "created",
                    correlationId), cancellationToken);
                return await MapUserAsync(user, role, cancellationToken);
            }, cancellationToken);
        }
        catch (Exception exception) when (IsExpected(exception))
        {
            context.ChangeTracker.Clear();
            await WriteRejectedAsync(
                "user-created",
                actorId,
                subjectId,
                exception,
                correlationId,
                cancellationToken);
            throw;
        }
    }

    public async Task<AdministrationUserView> ChangeRoleAsync(
        Guid id,
        ChangeAdministrationRoleCommand command,
        Guid actorId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await ExecuteMutationAsync(async () =>
            {
                EnsureNotSelf(id, actorId);
                var user = await LoadUserAsync(id, cancellationToken);
                EnsureVersion(user, command.Version);
                var currentRoles = await users.GetRolesAsync(user);
                var currentRole = SingleRole(currentRoles);
                var newRole = ValidateRole(command.Role);
                var customerId = await ValidateCustomerAssociationAsync(
                    newRole,
                    command.CustomerId,
                    cancellationToken);
                if (currentRole == BankRoles.Administrator && newRole != BankRoles.Administrator)
                {
                    await EnsureAnotherUnlockedAdministratorAsync(id, cancellationToken);
                }

                user.CustomerId = customerId;
                EnsureSucceeded(await users.UpdateAsync(user));
                if (!string.Equals(currentRole, newRole, StringComparison.Ordinal))
                {
                    EnsureSucceeded(await users.RemoveFromRoleAsync(user, currentRole));
                    EnsureSucceeded(await users.AddToRoleAsync(user, newRole));
                }
                EnsureSucceeded(await users.UpdateSecurityStampAsync(user));
                await audit.WriteAsync(new(
                    "role-changed",
                    actorId.ToString(),
                    id.ToString(),
                    true,
                    newRole,
                    correlationId), cancellationToken);
                return await MapUserAsync(user, newRole, cancellationToken);
            }, cancellationToken);
        }
        catch (Exception exception) when (IsExpected(exception))
        {
            context.ChangeTracker.Clear();
            await WriteRejectedAsync(
                "role-changed",
                actorId,
                id,
                exception,
                correlationId,
                cancellationToken);
            throw;
        }
    }

    public async Task<AdministrationUserView> ChangeLockoutAsync(
        Guid id,
        ChangeAdministrationLockoutCommand command,
        Guid actorId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await ExecuteMutationAsync(async () =>
            {
                EnsureNotSelf(id, actorId);
                var user = await LoadUserAsync(id, cancellationToken);
                EnsureVersion(user, command.Version);
                var role = SingleRole(await users.GetRolesAsync(user));
                if (command.Locked && role == BankRoles.Administrator)
                {
                    await EnsureAnotherUnlockedAdministratorAsync(id, cancellationToken);
                }

                EnsureSucceeded(await users.SetLockoutEnabledAsync(user, true));
                EnsureSucceeded(await users.SetLockoutEndDateAsync(
                    user,
                    command.Locked ? clock.UtcNow.AddYears(100) : null));
                if (!command.Locked)
                {
                    EnsureSucceeded(await users.ResetAccessFailedCountAsync(user));
                }
                EnsureSucceeded(await users.UpdateSecurityStampAsync(user));
                await audit.WriteAsync(new(
                    command.Locked ? "user-locked" : "user-unlocked",
                    actorId.ToString(),
                    id.ToString(),
                    true,
                    command.Locked ? "locked" : "unlocked",
                    correlationId), cancellationToken);
                return await MapUserAsync(user, role, cancellationToken);
            }, cancellationToken);
        }
        catch (Exception exception) when (IsExpected(exception))
        {
            context.ChangeTracker.Clear();
            await WriteRejectedAsync(
                command.Locked ? "user-locked" : "user-unlocked",
                actorId,
                id,
                exception,
                correlationId,
                cancellationToken);
            throw;
        }
    }

    public async Task<SecurityAuditPage> SearchSecurityAuditAsync(
        string? eventName,
        string? actorOrSubject,
        bool? succeeded,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ValidatePage(page, pageSize);
        if (from.HasValue && to.HasValue && from > to)
        {
            throw Validation("dateRange", "From must be before or equal to To.");
        }

        var source = context.SecurityAuditEntries.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(eventName))
        {
            var value = eventName.Trim();
            source = source.Where(entry => entry.EventName == value);
        }
        if (!string.IsNullOrWhiteSpace(actorOrSubject))
        {
            var value = actorOrSubject.Trim();
            source = source.Where(entry => entry.ActorId == value || entry.SubjectId == value);
        }
        if (succeeded.HasValue)
        {
            source = source.Where(entry => entry.Succeeded == succeeded);
        }
        if (from.HasValue)
        {
            source = source.Where(entry => entry.OccurredAt >= from);
        }
        if (to.HasValue)
        {
            source = source.Where(entry => entry.OccurredAt <= to);
        }

        var total = await source.CountAsync(cancellationToken);
        var entries = await source
            .OrderByDescending(entry => entry.OccurredAt)
            .ThenByDescending(entry => entry.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(entry => new SecurityAuditView(
                entry.Id,
                entry.OccurredAt,
                entry.EventName,
                entry.ActorId,
                entry.SubjectId,
                entry.Succeeded,
                entry.Outcome,
                entry.CorrelationId))
            .ToArrayAsync(cancellationToken);
        return new(entries, page, pageSize, total);
    }

    private async Task<T> ExecuteMutationAsync<T>(
        Func<Task<T>> mutation,
        CancellationToken cancellationToken)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);
            var result = await mutation();
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }

    private async Task<ApplicationUser> LoadUserAsync(Guid id, CancellationToken cancellationToken) =>
        await context.Users.SingleOrDefaultAsync(user => user.Id == id, cancellationToken)
        ?? throw new AccessAdministrationNotFoundException();

    private async Task<string?> ValidateCustomerAssociationAsync(
        string role,
        string? customerId,
        CancellationToken cancellationToken)
    {
        if (role == BankRoles.Customer)
        {
            var value = customerId?.Trim();
            if (string.IsNullOrWhiteSpace(value) ||
                !await context.Customers.AnyAsync(
                    customer => customer.Id == value && customer.Status == CustomerStatus.Active,
                    cancellationToken))
            {
                throw Validation("customerId", "Customer role requires an existing active customer.");
            }
            return value;
        }
        if (!string.IsNullOrWhiteSpace(customerId))
        {
            throw Validation("customerId", "Staff roles cannot have a customer association.");
        }
        return null;
    }

    private async Task EnsureAnotherUnlockedAdministratorAsync(
        Guid excludedUserId,
        CancellationToken cancellationToken)
    {
        var normalizedRole = users.NormalizeName(BankRoles.Administrator);
        var roleId = await context.Roles
            .Where(role => role.NormalizedName == normalizedRole)
            .Select(role => role.Id)
            .SingleAsync(cancellationToken);
        var remaining = await (
            from userRole in context.UserRoles
            join user in context.Users on userRole.UserId equals user.Id
            where userRole.RoleId == roleId &&
                  user.Id != excludedUserId &&
                  (!user.LockoutEnabled || user.LockoutEnd == null || user.LockoutEnd <= clock.UtcNow)
            select user.Id)
            .AnyAsync(cancellationToken);
        if (!remaining)
        {
            throw new AccessAdministrationConflictException(
                "At least one unlocked Administrator must remain.");
        }
    }

    private async Task<IReadOnlyList<AdministrationUserView>> MapUsersAsync(
        IReadOnlyCollection<ApplicationUser> source,
        CancellationToken cancellationToken)
    {
        if (source.Count == 0)
        {
            return [];
        }
        var ids = source.Select(user => user.Id).ToArray();
        var roleRows = await (
            from userRole in context.UserRoles.AsNoTracking()
            join role in context.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where ids.Contains(userRole.UserId)
            select new { userRole.UserId, Role = role.Name! })
            .ToArrayAsync(cancellationToken);
        var roles = roleRows.GroupBy(row => row.UserId)
            .ToDictionary(group => group.Key, group => SingleRole(group.Select(row => row.Role).ToArray()));
        return source.Select(user =>
            MapUser(user, roles.GetValueOrDefault(user.Id)
                ?? throw new AccessAdministrationConflictException("User has no governed business role.")))
            .ToArray();
    }

    private Task<AdministrationUserView> MapUserAsync(
        ApplicationUser user,
        string role,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(MapUser(user, role));
    }

    private AdministrationUserView MapUser(ApplicationUser user, string role) => new(
        user.Id,
        user.UserName ?? string.Empty,
        user.Email ?? string.Empty,
        role,
        user.CustomerId,
        user.LockoutEnabled && user.LockoutEnd > clock.UtcNow,
        user.LockoutEnd,
        user.AccessFailedCount,
        user.ConcurrencyStamp ?? string.Empty);

    private static string SingleRole(IEnumerable<string> roles)
    {
        var values = roles.ToArray();
        if (values.Length != 1 || !BankRoles.All.Contains(values[0], StringComparer.Ordinal))
        {
            throw new AccessAdministrationConflictException(
                "User must have exactly one governed business role.");
        }
        return values[0];
    }

    private static string ValidateRole(string role)
    {
        if (!BankRoles.All.Contains(role, StringComparer.Ordinal))
        {
            throw Validation("role", "Role is not supported.");
        }
        return role;
    }

    private static void ValidatePage(int page, int pageSize)
    {
        if (page < 1)
        {
            throw Validation("page", "Page must be at least 1.");
        }
        if (pageSize is < 1 or > MaximumPageSize)
        {
            throw Validation("pageSize", $"Page size must be between 1 and {MaximumPageSize}.");
        }
    }

    private static void EnsureNotSelf(Guid id, Guid actorId)
    {
        if (id == actorId)
        {
            throw new AccessAdministrationConflictException(
                "Administrators cannot change their own role or lockout state.");
        }
    }

    private static void EnsureVersion(ApplicationUser user, string version)
    {
        if (string.IsNullOrWhiteSpace(version) ||
            !string.Equals(user.ConcurrencyStamp, version, StringComparison.Ordinal))
        {
            throw new AccessAdministrationConflictException("User changed.");
        }
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new AccessAdministrationValidationException(
                result.Errors
                    .GroupBy(error => string.IsNullOrWhiteSpace(error.Code) ? "identity" : error.Code)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(error => error.Description).ToArray()));
        }
    }

    private async Task WriteRejectedAsync(
        string eventName,
        Guid actorId,
        Guid subjectId,
        Exception exception,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var outcome = exception switch
        {
            AccessAdministrationNotFoundException => "not-found",
            AccessAdministrationConflictException => "conflict",
            AccessAdministrationValidationException => "invalid",
            _ => "failed"
        };
        await audit.WriteAsync(new(
            eventName,
            actorId.ToString(),
            subjectId.ToString(),
            false,
            outcome,
            correlationId), cancellationToken);
    }

    private static bool IsExpected(Exception exception) =>
        exception is AccessAdministrationNotFoundException or
            AccessAdministrationConflictException or
            AccessAdministrationValidationException;

    private static AccessAdministrationValidationException Validation(
        string key,
        string message) => new(new Dictionary<string, string[]>
        {
            [key] = [message]
        });
}
