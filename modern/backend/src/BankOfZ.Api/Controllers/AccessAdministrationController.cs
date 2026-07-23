using BankOfZ.Api.Contracts;
using BankOfZ.Api.Security;
using BankOfZ.Application.AccessAdministration;
using BankOfZ.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BankOfZ.Api.Controllers;

[ApiController]
[Route("api/administration")]
[Authorize(Policy = AuthorizationPolicies.Administrator)]
public sealed class AccessAdministrationController(
    IAccessAdministrationService administration,
    UserManager<ApplicationUser> users) : ControllerBase
{
    [HttpGet("users")]
    public async Task<ActionResult<AdministrationUserPage>> SearchUsers(
        [FromQuery] string? query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await administration.SearchUsersAsync(query, page, pageSize, cancellationToken));

    [HttpGet("users/{id:guid}")]
    public async Task<ActionResult<AdministrationUserView>> FindUser(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await administration.FindUserAsync(id, cancellationToken));

    [HttpPost("users")]
    public async Task<ActionResult<AdministrationUserView>> CreateUser(
        CreateAdministrationUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await administration.CreateUserAsync(
            new(
                request.UserName,
                request.Email,
                request.Password,
                request.Role,
                request.CustomerId),
            ActorId(),
            SecurityAuditLimits.NormalizeCorrelationId(HttpContext.TraceIdentifier),
            cancellationToken);
        return CreatedAtAction(nameof(FindUser), new { id = result.Id }, result);
    }

    [HttpPut("users/{id:guid}/role")]
    public async Task<ActionResult<AdministrationUserView>> ChangeRole(
        Guid id,
        ChangeAdministrationRoleRequest request,
        CancellationToken cancellationToken) =>
        Ok(await administration.ChangeRoleAsync(
            id,
            new(request.Role, request.CustomerId, request.Version),
            ActorId(),
            SecurityAuditLimits.NormalizeCorrelationId(HttpContext.TraceIdentifier),
            cancellationToken));

    [HttpPut("users/{id:guid}/lockout")]
    public async Task<ActionResult<AdministrationUserView>> ChangeLockout(
        Guid id,
        ChangeAdministrationLockoutRequest request,
        CancellationToken cancellationToken) =>
        Ok(await administration.ChangeLockoutAsync(
            id,
            new(request.Locked, request.Version),
            ActorId(),
            SecurityAuditLimits.NormalizeCorrelationId(HttpContext.TraceIdentifier),
            cancellationToken));

    [HttpGet("security-audit")]
    public async Task<ActionResult<SecurityAuditPage>> SearchSecurityAudit(
        [FromQuery] string? eventName,
        [FromQuery] string? actorOrSubject,
        [FromQuery] bool? succeeded,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await administration.SearchSecurityAuditAsync(
            eventName,
            actorOrSubject,
            succeeded,
            from,
            to,
            page,
            pageSize,
            cancellationToken));

    private Guid ActorId() =>
        Guid.TryParse(users.GetUserId(User), out var id)
            ? id
            : throw new InvalidOperationException("Authenticated Administrator identifier is missing.");
}
