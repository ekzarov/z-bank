using BankOfZ.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankOfZ.Api.Controllers;

[ApiController]
[Route("api/access")]
public sealed class AccessProbeController : ControllerBase
{
    [Authorize(Policy = AuthorizationPolicies.Customer)]
    [HttpGet("customer")]
    public IActionResult Customer() => Ok(new { access = "customer" });

    [Authorize(Policy = AuthorizationPolicies.Operator)]
    [HttpGet("operator")]
    public IActionResult Operator() => Ok(new { access = "operator" });

    [Authorize(Policy = AuthorizationPolicies.Administrator)]
    [HttpGet("administrator")]
    public IActionResult Administrator() => Ok(new { access = "administrator" });
}
