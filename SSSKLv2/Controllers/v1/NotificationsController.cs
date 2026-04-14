using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSSKLv2.Data.Constants;
using SSSKLv2.Dto;
using SSSKLv2.Services.Interfaces;
using System.Security.Claims;

namespace SSSKLv2.Controllers.v1;

[ApiController]
[Route("v1/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> GetNotifications([FromQuery] bool unreadOnly = false, [FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var notifications = await _notificationService.GetNotificationsAsync(userId, unreadOnly, skip, take);
        return Ok(notifications);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var count = await _notificationService.GetUnreadCountAsync(userId);
        return Ok(count);
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await _notificationService.MarkAsReadAsync(id, userId);
        return Ok();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await _notificationService.MarkAllAsReadAsync(userId);
        return Ok();
    }

    [HttpPost("custom")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> SendCustomNotification(CreateCustomNotificationDto dto)
    {
        await _notificationService.CreateCustomNotificationAsync(dto);
        return Ok();
    }
}
