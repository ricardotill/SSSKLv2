using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSSKLv2.Services.Interfaces;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Dto.Api;
using SSSKLv2.Dto.Api.v1;
using System.Security.Claims;
using SSSKLv2.Data;
using System.Net.Mime;

namespace SSSKLv2.Controllers.v1;

[Authorize]
[Route("v1/[controller]")]
[ApiController]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly ILogger<EventsController> _logger;

    public EventsController(IEventService eventService, ILogger<EventsController> logger)
    {
        _eventService = eventService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PaginationObject<EventDto>>> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 15, [FromQuery] bool futureOnly = false)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var items = await _eventService.GetAllEvents(skip, take, futureOnly, userId);
        var totalCount = await _eventService.GetCount(futureOnly);

        return Ok(new PaginationObject<EventDto>
        {
            Items = items,
            TotalCount = totalCount
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EventDto>> GetById(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        try
        {
            var e = await _eventService.GetEventById(id, userId);
            return Ok(e);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<EventDto>> Create([FromForm] EventCreateDto dto, [FromForm] IFormFile? image)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        if (image != null)
        {
            dto.ImageContentType = new ContentType(image.ContentType);
            dto.ImageContent = image.OpenReadStream();
        }

        var eventId = await _eventService.CreateEvent(dto, userId);
        var createdEvent = await _eventService.GetEventById(eventId, userId);
        return CreatedAtAction(nameof(GetById), new { id = eventId }, createdEvent);
    }

    [HttpPut("{id:guid}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(Guid id, [FromForm] EventCreateDto dto, [FromForm] IFormFile? image)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var isAdmin = User.IsInRole("Admin");

        if (image != null)
        {
            dto.ImageContentType = new ContentType(image.ContentType);
            dto.ImageContent = image.OpenReadStream();
        }

        try
        {
            await _eventService.UpdateEvent(id, dto, userId, isAdmin);
            return NoContent();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var isAdmin = User.IsInRole("Admin");

        try
        {
            await _eventService.DeleteEvent(id, userId, isAdmin);
            return NoContent();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("{id:guid}/rsvp")]
    public async Task<IActionResult> Respond(Guid id, [FromBody] EventResponseDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        try
        {
            await _eventService.RespondToEvent(id, userId, dto.Status);
            return Ok();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }
}
