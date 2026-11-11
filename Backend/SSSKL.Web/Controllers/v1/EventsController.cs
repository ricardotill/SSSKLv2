using System.Net.Mime;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSSKL.Application.Features.Events.Commands.CreateEvent;
using SSSKL.Application.Features.Events.Commands.DeleteEvent;
using SSSKL.Application.Features.Events.Commands.RespondToEvent;
using SSSKL.Application.Features.Events.Commands.UpdateEvent;
using SSSKL.Application.Features.Events.Commands.UpdateEventImage;
using SSSKL.Application.Features.Events.Queries.GetEventById;
using SSSKL.Application.Features.Events.Queries.GetEvents;
using SSSKL.Application.Util;
using SSSKL.Contracts.Dto.Api;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Web.Controllers.v1;

[Authorize]
[Route("v1/[controller]")]
[ApiController]
public class EventsController(ISender sender, ILogger<EventsController> logger) : ControllerBase
{
    private const string UnsupportedImageMessage = "Unsupported image content type. Only JPEG, PNG, WebP, HEIC, and HEIF are allowed.";

    [HttpGet]
    public async Task<ActionResult<PaginationObject<EventDto>>> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 15, [FromQuery] bool futureOnly = false, [FromQuery] string? requiredRole = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Ok(await sender.Send(new GetEventsQuery(skip, take, futureOnly, userId, requiredRole)));
    }

    [AllowAnonymous]
    [HttpGet("public")]
    public async Task<ActionResult<PaginationObject<EventDto>>> GetPublic([FromQuery] int skip = 0, [FromQuery] int take = 15, [FromQuery] bool futureOnly = false, [FromQuery] string? requiredRole = null)
    {
        return Ok(await sender.Send(new GetEventsQuery(skip, take, futureOnly, null, requiredRole)));
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EventDto>> GetById(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Ok(await sender.Send(new GetEventByIdQuery(id, userId)));
    }

    [Authorize(Roles = "User,Admin")]
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<EventDto>> Create([FromForm] EventCreateDto dto, [FromForm] IFormFile? image)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        if (image != null)
        {
            var contentType = ContentTypeToExtensionMapper.NormalizeContentType(image.ContentType, image.FileName);
            if (contentType == null)
            {
                logger.LogWarning(
                    "Event image create rejected: unsupported content type. UserId={UserId}, FileName={FileName}, RawContentType={RawContentType}, Length={Length}",
                    userId, image.FileName, image.ContentType, image.Length);
                return BadRequest(UnsupportedImageMessage);
            }

            logger.LogInformation(
                "Event image create accepted: UserId={UserId}, FileName={FileName}, RawContentType={RawContentType}, NormalizedContentType={NormalizedContentType}, Length={Length}",
                userId, image.FileName, image.ContentType, contentType, image.Length);
            dto.ImageContentType = new ContentType(contentType);
            dto.ImageContent = image.OpenReadStream();
        }

        var createdEvent = await sender.Send(new CreateEventCommand(dto, userId));
        return CreatedAtAction(nameof(GetById), new { id = createdEvent.Id }, createdEvent);
    }

    [Authorize(Roles = "User,Admin")]
    [HttpPut("{id:guid}")]
    [HttpPost("{id:guid}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(Guid id, [FromForm] EventCreateDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await sender.Send(new UpdateEventCommand(id, dto, userId, User.IsInRole("Admin")));
        return NoContent();
    }

    // Accepts a JSON base64 payload rather than multipart/form-data: iOS PWAs with an
    // active service worker are known to strip the body from multipart POST requests.
    [Authorize(Roles = "User,Admin")]
    [HttpPost("{id:guid}/image")]
    public async Task<IActionResult> UpdateImage(Guid id, [FromBody] Base64FileUploadDto? image)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        if (image == null || !Base64FileDecoder.TryDecode(image.Base64Content, out var bytes))
        {
            logger.LogWarning(
                "Event image update rejected: no file content was received. EventId={EventId}, UserId={UserId}, RequestContentLength={RequestContentLength}",
                id, userId, Request.ContentLength);
            return BadRequest("Image file is required.");
        }

        var contentType = ContentTypeToExtensionMapper.NormalizeContentType(image.ContentType, image.FileName);
        if (contentType == null)
        {
            logger.LogWarning(
                "Event image update rejected: unsupported content type. EventId={EventId}, UserId={UserId}, FileName={FileName}, RawContentType={RawContentType}, Length={Length}",
                id, userId, image.FileName, image.ContentType, bytes.Length);
            return BadRequest(UnsupportedImageMessage);
        }

        var isAdmin = User.IsInRole("Admin");
        logger.LogInformation(
            "Event image update accepted for processing: EventId={EventId}, UserId={UserId}, IsAdmin={IsAdmin}, FileName={FileName}, RawContentType={RawContentType}, NormalizedContentType={NormalizedContentType}, Length={Length}",
            id, userId, isAdmin, image.FileName, image.ContentType, contentType, bytes.Length);

        try
        {
            using var stream = new MemoryStream(bytes);
            await sender.Send(new UpdateEventImageCommand(id, userId, isAdmin, stream, contentType));
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(
                ex,
                "Event image update rejected by handler: {Reason}. EventId={EventId}, UserId={UserId}, FileName={FileName}, RawContentType={RawContentType}, NormalizedContentType={NormalizedContentType}, Length={Length}",
                ex.Message, id, userId, image.FileName, image.ContentType, contentType, bytes.Length);
            return BadRequest(UnsupportedImageMessage);
        }
    }

    [Authorize(Roles = "User,Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await sender.Send(new DeleteEventCommand(id, userId, User.IsInRole("Admin")));
        return NoContent();
    }

    [Authorize(Roles = "User,Admin")]
    [HttpPost("{id:guid}/rsvp")]
    public async Task<IActionResult> Respond(Guid id, [FromBody] EventResponseDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await sender.Send(new RespondToEventCommand(id, userId, dto.Status));
        return Ok();
    }
}
