using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data;
using SSSKLv2.Data.Constants;
using SSSKLv2.Dto.Api.v1;
using Ganss.Xss;
using Microsoft.AspNetCore.Identity;

namespace SSSKLv2.Controllers.v1;

[Route("v1/[controller]")]
[ApiController]
public class GlobalSettingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public GlobalSettingsController(ApplicationDbContext context)
    {
        _context = context;
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

        var setting = await _context.GlobalSetting
            .FirstOrDefaultAsync(s => s.Key == key);

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

        var setting = await _context.GlobalSetting
            .FirstOrDefaultAsync(s => s.Key == key);

        if (setting == null)
        {
            // Create new if it doesn't exist
            setting = new GlobalSetting
            {
                Key = key,
                Value = sanitizedValue,
                UpdatedOn = DateTime.UtcNow
            };
            _context.GlobalSetting.Add(setting);
        }
        else
        {
            setting.Value = sanitizedValue;
            setting.UpdatedOn = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost("test-email")]
    public async Task<IActionResult> SendTestEmail([FromServices] IEmailSender<ApplicationUser> emailSender, [FromServices] UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null || string.IsNullOrEmpty(user.Email))
        {
            return BadRequest("User or email not found.");
        }

        try
        {
            var testMessage = $@"
                <h2>Test Geslaagd! 🚀</h2>
                <p>Gefeliciteerd! Je SMTP-instellingen zijn correct geconfigureerd.</p>
                <p>Deze email is verzonden naar <b>{user.Email}</b> om de verbinding met de mailserver te verifiëren.</p>
                <p>Je kunt nu veilig doorgaan met het gebruik van de applicatie.</p>";

            await emailSender.SendConfirmationLinkAsync(user, user.Email, "https://ssskl.scoutingwilo.nl"); 
            return Ok(new { message = $"Test email succesvol verzonden naar {user.Email}" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to send email. Check your SMTP settings and logs.", error = ex.Message });
        }
    }
}
