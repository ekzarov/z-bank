using BankOfZ.Api.Contracts;
using BankOfZ.Api.Security;
using BankOfZ.Application.AccessAdministration;
using BankOfZ.Infrastructure.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BankOfZ.Api.Controllers;

[ApiController]
[Route("api/session")]
public sealed class SessionController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    IAntiforgery antiforgery,
    ISecurityAuditWriter audit,
    InvalidCredentialWorkFactor invalidCredentialWorkFactor) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<SessionResponse>> GetCurrent()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Ok(new SessionResponse(false, null, null, []));
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Ok(new SessionResponse(false, null, null, []));
        }

        var roles = await userManager.GetRolesAsync(user);
        return Ok(new SessionResponse(true, user.UserName, user.CustomerId, roles.ToArray()));
    }

    [AllowAnonymous]
    [HttpGet("csrf")]
    public IActionResult GetCsrfToken()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        Response.Cookies.Append("BankOfZ.XSRF-TOKEN", tokens.RequestToken!, new CookieOptions
        {
            HttpOnly = false,
            IsEssential = true,
            Path = "/",
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps
        });
        return Ok(new { token = tokens.RequestToken });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<SessionResponse>> Login(LoginRequest request)
    {
        var user = await userManager.FindByNameAsync(request.UserName);
        if (user is null)
        {
            invalidCredentialWorkFactor.Verify(request.Password);
            await audit.WriteAsync(new(
                "login",
                null,
                null,
                false,
                "invalid-credentials",
                SecurityAuditLimits.NormalizeCorrelationId(HttpContext.TraceIdentifier)));
            return Unauthorized(GenericLoginProblem());
        }

        var result = await signInManager.PasswordSignInAsync(
            user,
            request.Password,
            request.RememberMe,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            await audit.WriteAsync(new(
                "login",
                null,
                user.Id.ToString(),
                false,
                result.IsLockedOut ? "locked-out" : "invalid-credentials",
                SecurityAuditLimits.NormalizeCorrelationId(HttpContext.TraceIdentifier)));
            return Unauthorized(GenericLoginProblem());
        }

        var roles = await userManager.GetRolesAsync(user);
        await audit.WriteAsync(new(
            "login",
            user.Id.ToString(),
            user.Id.ToString(),
            true,
            "authenticated",
            SecurityAuditLimits.NormalizeCorrelationId(HttpContext.TraceIdentifier)));
        return Ok(new SessionResponse(true, user.UserName, user.CustomerId, roles.ToArray()));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var subjectId = userManager.GetUserId(User);
        await signInManager.SignOutAsync();
        await audit.WriteAsync(new(
            "logout",
            subjectId,
            subjectId,
            true,
            "session-revoked",
            SecurityAuditLimits.NormalizeCorrelationId(HttpContext.TraceIdentifier)));
        return NoContent();
    }

    private static ProblemDetails GenericLoginProblem() => new()
    {
        Status = StatusCodes.Status401Unauthorized,
        Title = "Sign-in failed",
        Detail = "The supplied credentials could not be accepted."
    };
}
