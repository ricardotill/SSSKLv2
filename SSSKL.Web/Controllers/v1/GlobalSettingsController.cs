using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SSSKL.Application.Features.Settings.Commands.UpdateGlobalSetting;
using SSSKL.Application.Features.Settings.Queries.GetGlobalSetting;
using SSSKL.Application.Features.Settings.Queries.GetTurnstileConfig;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Domain.Constants;
using SSSKL.Domain.Entities;

namespace SSSKL.Web.Controllers.v1;

[Route("v1/[controller]")]
[ApiController]
public class GlobalSettingsController(ISender sender) : ControllerBase
{
    // GET v1/globalsettings/public/turnstile - what the login and sign-up pages need to render Turnstile
    [AllowAnonymous]
    [HttpGet("public/turnstile")]
    public async Task<ActionResult<TurnstileConfigDto>> GetTurnstileConfig()
    {
        return Ok(await sender.Send(new GetTurnstileConfigQuery(), HttpContext.RequestAborted));
    }

    // GET v1/globalsettings/{key} - 204 when the setting hasn't been set
    [HttpGet("{key}")]
    public async Task<ActionResult<GlobalSettingDto>> GetSetting(string key)
    {
        var setting = await sender.Send(new GetGlobalSettingQuery(key, User.IsInRole(Roles.Admin)));
        return setting is null ? NoContent() : Ok(setting);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{key}")]
    public async Task<IActionResult> UpdateSetting(string key, [FromBody] GlobalSettingUpdateDto dto)
    {
        await sender.Send(new UpdateGlobalSettingCommand(key, dto.Value));
        return NoContent();
    }

    // Exercises the ASP.NET Core Identity email sender directly, so it stays out of the application layer.
    [Authorize(Roles = Roles.Admin)]
    [HttpPost("test-email")]
    public async Task<IActionResult> SendTestEmail([FromServices] IEmailSender<ApplicationUser> emailSender, [FromServices] UserManager<ApplicationUser> userManager, [FromServices] ILogger<GlobalSettingsController> logger)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null || string.IsNullOrEmpty(user.Email))
        {
            return BadRequest("User or email not found.");
        }

        try
        {
            await emailSender.SendConfirmationLinkAsync(user, user.Email, "https://ssskl.scoutingwilo.nl");
            return Ok(new { message = $"Test email succesvol verzonden naar {user.Email}" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Test email to {UserId} failed", user.Id);
            return StatusCode(500, new { message = "Failed to send email. Check your SMTP settings and logs." });
        }
    }
}
