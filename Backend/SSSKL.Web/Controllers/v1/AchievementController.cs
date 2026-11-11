using System.Net.Mime;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSSKL.Application.Features.Achievements.Commands.AwardAchievement;
using SSSKL.Application.Features.Achievements.Commands.CreateAchievement;
using SSSKL.Application.Features.Achievements.Commands.DeleteAchievement;
using SSSKL.Application.Features.Achievements.Commands.DeleteAchievementEntries;
using SSSKL.Application.Features.Achievements.Commands.MarkUnseenEntriesSeen;
using SSSKL.Application.Features.Achievements.Commands.UpdateAchievement;
using SSSKL.Application.Features.Achievements.Queries.GetAchievementById;
using SSSKL.Application.Features.Achievements.Queries.GetAchievementEarners;
using SSSKL.Application.Features.Achievements.Queries.GetAchievementEntries;
using SSSKL.Application.Features.Achievements.Queries.GetAchievementListing;
using SSSKL.Application.Features.Achievements.Queries.GetAchievements;
using SSSKL.Application.Util;
using SSSKL.Contracts.Dto;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Web.Controllers.v1;

[Authorize]
[Route("v1/achievement")]
[ApiController]
public class AchievementController(ISender sender) : ControllerBase
{
    private const string UnsupportedImageMessage = "Unsupported image content type. Only JPEG, PNG, WebP, HEIC, and HEIF are allowed.";

    // GET v1/achievement
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 15)
    {
        return Ok(await sender.Send(new GetAchievementsQuery(skip, take)));
    }

    // GET v1/achievement/{id}
    [Authorize(Roles = "Admin")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        return Ok(await sender.Send(new GetAchievementByIdQuery(id)));
    }

    // POST v1/achievement
    // Accepts a JSON base64 payload rather than multipart/form-data: iOS PWAs with an
    // active service worker are known to strip the body from multipart POST requests.
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AchievementCreateDto dto)
    {
        // Let [ApiController] + FluentValidation handle ModelState and automatic 400 responses.

        if (dto.Image == null || !Base64FileDecoder.TryDecode(dto.Image.Base64Content, out var bytes))
        {
            return BadRequest("Afbeelding is verplicht."); // "Image is required" in Dutch
        }

        var contentType = ContentTypeToExtensionMapper.NormalizeContentType(dto.Image.ContentType, dto.Image.FileName);
        if (contentType == null)
        {
            return BadRequest(UnsupportedImageMessage);
        }

        using var stream = new MemoryStream(bytes);
        var achievementDto = new AchievementDto
        {
            Name = dto.Name,
            Description = dto.Description,
            AutoAchieve = dto.AutoAchieve,
            Action = dto.Action,
            ComparisonOperator = dto.ComparisonOperator,
            ComparisonValue = dto.ComparisonValue,
            ImageContent = stream,
            ImageContentType = new ContentType(contentType)
        };

        await sender.Send(new CreateAchievementCommand(achievementDto));
        return StatusCode(StatusCodes.Status201Created);
    }

    // PUT v1/achievement/{id}
    // Image replacement is sent as base64 rather than multipart/form-data: iOS PWAs with an
    // active service worker are known to strip the body from multipart POST requests.
    [Authorize(Roles = "Admin")]
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] AchievementUpdateDto dto)
    {
        try
        {
            if (dto.NewImage != null)
            {
                if (!Base64FileDecoder.TryDecode(dto.NewImage.Base64Content, out var bytes))
                {
                    return BadRequest(UnsupportedImageMessage);
                }

                using var stream = new MemoryStream(bytes);
                await sender.Send(new UpdateAchievementCommand(dto, stream, dto.NewImage.ContentType));
                return NoContent();
            }

            await sender.Send(new UpdateAchievementCommand(dto));
            return NoContent();
        }
        catch (ArgumentException)
        {
            return BadRequest(UnsupportedImageMessage);
        }
    }

    // DELETE v1/achievement/{id}
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await sender.Send(new DeleteAchievementCommand(id));
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

        return Ok(await sender.Send(new GetAchievementListingQuery(Username: username)));
    }

    // GET v1/achievement/entries/{userId}
    [HttpGet("entries/{userId}")]
    public async Task<IActionResult> GetPersonalEntries(string userId)
    {
        return Ok(await sender.Send(new GetAchievementEntriesQuery(UserId: userId)));
    }

    // GET v1/achievement/all-for-user/{userId}
    [HttpGet("all-for-user/{userId}")]
    public async Task<IActionResult> GetAllForUser(string userId)
    {
        return Ok(await sender.Send(new GetAchievementListingQuery(UserId: userId)));
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

        return Ok(await sender.Send(new GetAchievementEntriesQuery(Username: username)));
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

        return Ok(await sender.Send(new MarkUnseenEntriesSeenCommand(username)));
    }

    // POST v1/achievement/entries/delete
    [Authorize(Roles = "Admin")]
    [HttpPost("entries/delete")]
    public async Task<IActionResult> DeleteEntries([FromBody] IEnumerable<Guid>? entryIds)
    {
        var ids = entryIds?.ToList();
        if (ids == null || ids.Count == 0) return BadRequest();

        await sender.Send(new DeleteAchievementEntriesCommand(ids));
        return NoContent();
    }

    // GET v1/achievement/{id}/earners
    [HttpGet("{id:guid}/earners")]
    public async Task<IActionResult> GetEarners(Guid id)
    {
        return Ok(await sender.Send(new GetAchievementEarnersQuery(id)));
    }

    // POST v1/achievement/award/{userId}/{achievementId}
    [Authorize(Roles = "Admin")]
    [HttpPost("award/{userId}/{achievementId:guid}")]
    public async Task<IActionResult> AwardToUser(string userId, Guid achievementId)
    {
        var ok = await sender.Send(new AwardAchievementToUserCommand(userId, achievementId));
        if (!ok) return Conflict();
        return Ok(true);
    }

    // POST v1/achievement/award/all/{achievementId}
    [Authorize(Roles = "Admin")]
    [HttpPost("award/all/{achievementId:guid}")]
    public async Task<IActionResult> AwardToAll(Guid achievementId)
    {
        return Ok(await sender.Send(new AwardAchievementToAllUsersCommand(achievementId)));
    }

    // GET v1/achievement/rarity/{id}
    [HttpGet("rarity/{id:guid}")]
    public async Task<IActionResult> GetRarity(Guid id)
    {
        var entries = await sender.Send(new GetAchievementEarnersQuery(id));
        return Ok(new { Count = entries.Count });
    }
}
