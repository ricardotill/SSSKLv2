using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSSKLv2.Services.Interfaces;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Dto.Api.v1;
using SSSKLv2.Validators.Announcement;

namespace SSSKLv2.Controllers.v1;

[Authorize]
[Route("v1/[controller]")]
[ApiController]
public class AnnouncementController : ControllerBase
{
    private readonly IAnnouncementService _announcementService;

    public AnnouncementController(IAnnouncementService announcementService)
    {
        _announcementService = announcementService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Announcement>>> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 15)
    {
        var list = await _announcementService.GetAllAnnouncements(skip, take);
        var totalCount = await _announcementService.GetCount();

        return Ok(new PaginationObject<Announcement>()
        {
            Items = list,
            TotalCount = totalCount
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Announcement?>> GetById(Guid id)
    {
        try
        {
            var item = await _announcementService.GetAnnouncementById(id);
            if (item is null)
                return NotFound();
            return Ok(item);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<Announcement>> Create([FromBody] AnnouncementCreateDto? dto)
    {
        if (dto is null)
            return BadRequest();

        // Map DTO to domain model
        var announcement = new Announcement
        {
            Message = dto.Message,
            Description = dto.Description,
            Order = dto.Order,
            IsScheduled = dto.IsScheduled,
            PlannedFrom = dto.PlannedFrom,
            PlannedTill = dto.PlannedTill
        };

        var validator = new AnnouncementValidator();
        var validationResult = await validator.ValidateAsync(announcement);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
        }

        await _announcementService.CreateAnnouncement(announcement);

        return CreatedAtAction(nameof(GetById), new { id = announcement.Id }, announcement);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AnnouncementUpdateDto? dto)
    {
        if (dto is null)
            return BadRequest();

        try
        {
            var existing = await _announcementService.GetAnnouncementById(id);
            if (existing is null)
                return NotFound();

            // Map update DTO onto existing
            existing.Message = dto.Message;
            existing.Description = dto.Description;
            existing.Order = dto.Order;
            existing.IsScheduled = dto.IsScheduled;
            existing.PlannedFrom = dto.PlannedFrom;
            existing.PlannedTill = dto.PlannedTill;

            var validator = new AnnouncementValidator();
            var validationResult = await validator.ValidateAsync(existing);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
            }

            await _announcementService.UpdateAnnouncement(existing);
            return NoContent();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var existing = await _announcementService.GetAnnouncementById(id);
            if (existing is null)
                return NotFound();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }

        await _announcementService.DeleteAnnouncement(id);
        return NoContent();
    }
}