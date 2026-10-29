using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSSKL.Application.Features.Notifications.Commands.MarkAllNotificationsRead;
using SSSKL.Application.Features.Notifications.Commands.MarkNotificationRead;
using SSSKL.Application.Features.Notifications.Commands.SendCustomNotification;
using SSSKL.Application.Features.Notifications.Commands.SubscribePush;
using SSSKL.Application.Features.Notifications.Commands.UnsubscribePush;
using SSSKL.Application.Features.Notifications.Queries.GetNotifications;
using SSSKL.Application.Features.Notifications.Queries.GetUnreadNotificationCount;
using SSSKL.Contracts.Dto;
using SSSKL.Domain.Constants;

namespace SSSKL.Web.Controllers.v1;

[ApiController]
[Route("v1/[controller]")]
[Authorize]
public class NotificationsController(ISender sender, IConfiguration configuration) : ControllerBase
{
    [HttpGet("vapid-public-key")]
    [AllowAnonymous]
    public ActionResult<string> GetVapidPublicKey()
    {
        var publicKey = configuration["VAPID_PUBLIC_KEY"] ?? configuration["VapidDetails:PublicKey"];
        if (string.IsNullOrEmpty(publicKey))
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "VAPID Public Key is not configured on the server.");
        }
        return Content(publicKey, "text/plain");
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> GetNotifications([FromQuery] bool unreadOnly = false, [FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        return Ok(await sender.Send(new GetNotificationsQuery(userId, unreadOnly, skip, take)));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        return Ok(await sender.Send(new GetUnreadNotificationCountQuery(userId)));
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await sender.Send(new MarkNotificationReadCommand(id, userId));
        return Ok();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await sender.Send(new MarkAllNotificationsReadCommand(userId));
        return Ok();
    }

    [HttpPost("custom")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> SendCustomNotification(CreateCustomNotificationDto dto)
    {
        await sender.Send(new SendCustomNotificationCommand(dto));
        return Ok();
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe(PushSubscriptionDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await sender.Send(new SubscribePushCommand(userId, dto));
        return Ok();
    }

    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await sender.Send(new UnsubscribePushCommand(userId, dto.Endpoint));
        return Ok();
    }
}
