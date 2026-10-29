using MediatR;
using SSSKL.Application.Shared;

namespace SSSKL.Application.Features.Notifications.Commands.SendCustomNotification;

internal sealed class SendCustomNotificationHandler(IUserNotifier userNotifier)
    : IRequestHandler<SendCustomNotificationCommand>
{
    public Task Handle(SendCustomNotificationCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Notification;
        // Fan-out targets every user; otherwise only the listed ones
        var userIds = dto.FanOut ? null : dto.UserIds ?? [];
        return userNotifier.NotifyManyAsync(userIds, dto.Title, dto.Message, dto.LinkUri, dto.SendPush);
    }
}
