using BankOfZ.Api.Contracts;
using BankOfZ.Api.Security;
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
    ISecurityAudit audit) : ControllerBase
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
            audit.Record("login", null, false, "invalid-credentials");
            return Unauthorized(GenericLoginProblem());
        }

        var result = await signInManager.PasswordSignInAsync(
            user,
            request.Password,
            request.RememberMe,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            audit.Record("login", user.Id.ToString(), false,
                result.IsLockedOut ? "locked-out" : "invalid-credentials");
            return Unauthorized(GenericLoginProblem());
        }

        var roles = await userManager.GetRolesAsync(user);
        audit.Record("login", user.Id.ToString(), true, "authenticated");
        return Ok(new SessionResponse(true, user.UserName, user.CustomerId, roles.ToArray()));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var subjectId = userManager.GetUserId(User);
        await signInManager.SignOutAsync();
        audit.Record("logout", subjectId, true, "session-revoked");
        return NoContent();
    }

    private static ProblemDetails GenericLoginProblem() => new()
    {
        Status = StatusCodes.Status401Unauthorized,
        Title = "Sign-in failed",
        Detail = "The supplied credentials could not be accepted."
    };
}
