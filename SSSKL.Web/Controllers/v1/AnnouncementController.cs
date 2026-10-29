using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSSKL.Application.Features.Announcements.Commands.CreateAnnouncement;
using SSSKL.Application.Features.Announcements.Commands.DeleteAnnouncement;
using SSSKL.Application.Features.Announcements.Commands.UpdateAnnouncement;
using SSSKL.Application.Features.Announcements.Queries.GetAnnouncementById;
using SSSKL.Application.Features.Announcements.Queries.GetAnnouncements;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Domain.Entities;

namespace SSSKL.Web.Controllers.v1;

[Authorize]
[Route("v1/[controller]")]
[ApiController]
public class AnnouncementController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginationObject<Announcement>>> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 15)
    {
        return Ok(await sender.Send(new GetAnnouncementsQuery(skip, take)));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Announcement>> GetById(Guid id)
    {
        return Ok(await sender.Send(new GetAnnouncementByIdQuery(id)));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<Announcement>> Create([FromBody] AnnouncementCreateDto? dto)
    {
        if (dto is null)
            return BadRequest();

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var announcement = await sender.Send(new CreateAnnouncementCommand(dto));
        return CreatedAtAction(nameof(GetById), new { id = announcement.Id }, announcement);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AnnouncementUpdateDto? dto)
    {
        if (dto is null)
            return BadRequest();

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        await sender.Send(new UpdateAnnouncementCommand(id, dto));
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await sender.Send(new DeleteAnnouncementCommand(id));
        return NoContent();
    }
}
