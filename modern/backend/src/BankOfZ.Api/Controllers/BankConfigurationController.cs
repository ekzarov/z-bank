using BankOfZ.Api.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BankOfZ.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/configuration/bank")]
public sealed class BankConfigurationController(IOptions<BankIdentityOptions> options) : ControllerBase
{
    [HttpGet]
    public ActionResult<BankConfigurationView> Get() =>
        Ok(new BankConfigurationView(options.Value.DisplayName, options.Value.SortCode));
}

public sealed record BankConfigurationView(string DisplayName, string SortCode);
