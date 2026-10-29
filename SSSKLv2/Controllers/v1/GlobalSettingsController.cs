using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Data;
using SSSKLv2.Data.Constants;
using SSSKLv2.Dto.Api.v1;
using Ganss.Xss;
using Microsoft.AspNetCore.Identity;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Controllers.v1;

[Route("v1/[controller]")]
[ApiController]
public class GlobalSettingsController : ControllerBase
{
    private readonly IGlobalSettingRepository _globalSettingRepository;

    public GlobalSettingsController(IGlobalSettingRepository globalSettingRepository)
    {
        _globalSettingRepository = globalSettingRepository;
    }

    // GET v1/globalsettings/public/turnstile - what the login and sign-up pages need to render Turnstile
    [AllowAnonymous]
    [HttpGet("public/turnstile")]
    public async Task<ActionResult<TurnstileConfigDto>> GetTurnstileConfig([FromServices] ITurnstileService turnstile)
    {
        var config = await turnstile.GetClientConfigAsync(HttpContext.RequestAborted);
        return Ok(new TurnstileConfigDto { Enabled = config.Enabled, SiteKey = config.SiteKey });
    }

    [HttpGet("{key}")]
    public async Task<ActionResult<GlobalSettingDto>> GetSetting(string key)
    {
        // Write-only keys can never be retrieved via the API — not even by admins
        if (GlobalSettingsKeys.WriteOnlyKeys.Contains(key))
        {
            return Forbid();
        }

        // Check if the key is sensitive and requires admin role
        if (GlobalSettingsKeys.SensitiveKeys.Contains(key))
        {
            if (!User.IsInRole(Roles.Admin))
            {
                return Forbid();
            }
        }

        var setting = await _globalSettingRepository.GetByKey(key);

        if (setting == null)
        {
            return NotFound();
        }

        return Ok(new GlobalSettingDto
        {
            Key = setting.Key,
            Value = setting.Value,
            UpdatedOn = setting.UpdatedOn
        });
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{key}")]
    public async Task<IActionResult> UpdateSetting(string key, [FromBody] GlobalSettingUpdateDto dto)
    {
        var sanitizedValue = dto.Value;
        
        // Only sanitize if it's NOT a sensitive key (API keys, passwords, etc.)
        if (!GlobalSettingsKeys.SensitiveKeys.Contains(key)) 
        {
            var sanitizer = new HtmlSanitizer();
            sanitizedValue = sanitizer.Sanitize(dto.Value);
        }

        await _globalSettingRepository.Upsert(key, sanitizedValue);

        return NoContent();
    }

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
