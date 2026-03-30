using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data;
using SSSKLv2.Dto.Api.v1;

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

    [Authorize(Roles = "Admin")]
    [HttpPut("{key}")]
    public async Task<IActionResult> UpdateSetting(string key, [FromBody] GlobalSettingUpdateDto dto)
    {
        var setting = await _context.GlobalSetting
            .FirstOrDefaultAsync(s => s.Key == key);

        if (setting == null)
        {
            // Create new if it doesn't exist
            setting = new GlobalSetting
            {
                Key = key,
                Value = dto.Value,
                UpdatedOn = DateTime.UtcNow
            };
            _context.GlobalSetting.Add(setting);
        }
        else
        {
            setting.Value = dto.Value;
            setting.UpdatedOn = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }
}
