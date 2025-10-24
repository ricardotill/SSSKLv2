using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using SSSKLv2.Services.Interfaces;
using SSSKLv2.Dto.Api.v1;
using SSSKLv2.Dto;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Exceptions;

namespace SSSKLv2.Controllers.v1;

[Authorize]
[Route("v1/[controller]")]
[ApiController]
public class AchievementController : ControllerBase
{
    private readonly IAchievementService _achievementService;

    public AchievementController(IAchievementService achievementService)
    {
        _achievementService = achievementService;
    }

    private static AchievementResponseDto MapToDto(Achievement a) => new AchievementResponseDto
    {
        Id = a.Id,
        Name = a.Name ?? string.Empty,
        Description = a.Description ?? string.Empty,
        AutoAchieve = a.AutoAchieve,
        Action = a.Action,
        ComparisonOperator = a.ComparisonOperator,
        ComparisonValue = a.ComparisonValue,
        Image = a.Image == null ? null : new AchievementImageDto
        {
            Id = a.Image.Id,
            FileName = a.Image.FileName,
            Uri = a.Image.Uri,
            ContentType = a.Image.ContentType
        }
    };

    private static AchievementEntryDto MapEntryToDto(AchievementEntry e) => new AchievementEntryDto
    {
        Id = e.Id,
        AchievementId = e.Achievement?.Id ?? Guid.Empty,
        AchievementName = e.Achievement?.Name ?? string.Empty,
        AchievementDescription = e.Achievement?.Description ?? string.Empty,
        DateAdded = e.CreatedOn,
        ImageUrl = e.Achievement?.Image?.Uri,
        HasSeen = e.HasSeen,
        UserId = e.User?.Id
    };

    // GET v1/achievement
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 15)
    {
        var list = await _achievementService.GetAchievements(skip, take);
        var totalCount = await _achievementService.GetCount();
        return Ok(new PaginationObject<AchievementResponseDto>()
        {
            Items = list.Select(MapToDto),
            TotalCount = totalCount
        });
    }

    // GET v1/achievement/{id}
    [Authorize(Roles = "Admin")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var achievement = await _achievementService.GetAchievementById(id);
            return Ok(MapToDto(achievement));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    // POST v1/achievement
    // Accept multipart/form-data for image upload
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] AchievementDto dto, [FromForm] IFormFile? image)
    {
        // Let [ApiController] + FluentValidation handle ModelState and automatic 400 responses.

        if (image != null)
        {
            dto.ImageContentType = new ContentType(image.ContentType);
            dto.ImageContent = image.OpenReadStream();
        }

        await _achievementService.AddAchievement(dto);
        return StatusCode(StatusCodes.Status201Created);
    }

    // PUT v1/achievement/{id}
    [Authorize(Roles = "Admin")]
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] AchievementUpdateDto dto)
    {
        try
        {
            // Map DTO -> Achievement domain model
            var achievement = new Achievement
            {
                Id = dto.Id,
                Name = dto.Name,
                Description = dto.Description,
                AutoAchieve = dto.AutoAchieve,
                Action = dto.Action,
                ComparisonOperator = dto.ComparisonOperator,
                ComparisonValue = dto.ComparisonValue
            };

            if (dto.Image != null)
            {
                achievement.Image = new AchievementImage
                {
                    Id = dto.Image.Id,
                    FileName = dto.Image.FileName,
                    Uri = dto.Image.Uri,
                    ContentType = dto.Image.ContentType,
                    CreatedOn = DateTime.Now
                };
            }

            await _achievementService.UpdateAchievement(achievement);
            return NoContent();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    // DELETE v1/achievement/{id}
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _achievementService.DeleteAchievement(id);
        return NoContent();
    }

    // GET v1/achievement/personal
    [Authorize]
    [HttpGet("personal")]
    public async Task<IActionResult> GetPersonal()
    {
        var username = User?.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
        {
            return Unauthorized();
        }

        var list = await _achievementService.GetPersonalAchievementsByUsername(username);
        return Ok(list);
    }

    // GET v1/achievement/entries/{userId}
    [HttpGet("entries/{userId}")]
    public async Task<IActionResult> GetPersonalEntries(string userId)
    {
        var list = await _achievementService.GetPersonalAchievementEntries(userId);
        return Ok(list.Select(MapEntryToDto));
    }

    // GET v1/achievement/entries/personal
    [Authorize]
    [HttpGet("entries/personal")]
    public async Task<IActionResult> GetPersonalEntries()
    {
        var username = User?.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
        {
            return Unauthorized();
        }

        var list = await _achievementService.GetPersonalAchievementEntriesByUsername(username);
        return Ok(list.Select(MapEntryToDto));
    }

    // GET v1/achievement/entries/unseen
    [Authorize]
    [HttpGet("entries/unseen")]
    public async Task<IActionResult> GetPersonalUnseenEntriesForCurrentUser()
    {
        var username = User?.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
        {
            return Unauthorized();
        }

        var list = await _achievementService.GetPersonalUnseenAchievementEntries(username);
        return Ok(list.Select(MapEntryToDto));
    }

    // POST v1/achievement/entries/delete
    [Authorize(Roles = "Admin")]
    [HttpPost("entries/delete")]
    public async Task<IActionResult> DeleteEntries([FromBody] IEnumerable<Guid>? entryIds)
    {
        var ids = entryIds?.ToList();
        if (ids == null || !ids.Any()) return BadRequest();

        var entries = ids.Select(id => new AchievementEntry { Id = id });
        await _achievementService.DeleteAchievementEntryRange(entries);
        return NoContent();
    }

    // POST v1/achievement/award/{userId}/{achievementId}
    [Authorize(Roles = "Admin")]
    [HttpPost("award/{userId}/{achievementId:guid}")]
    public async Task<IActionResult> AwardToUser(string userId, Guid achievementId)
    {
        var ok = await _achievementService.AwardAchievementToUser(userId, achievementId);
        if (!ok) return Conflict();
        return Ok(true);
    }

    // POST v1/achievement/award/all/{achievementId}
    [Authorize(Roles = "Admin")]
    [HttpPost("award/all/{achievementId:guid}")]
    public async Task<IActionResult> AwardToAll(Guid achievementId)
    {
        var count = await _achievementService.AwardAchievementToAllUsers(achievementId);
        return Ok(count);
    }
}