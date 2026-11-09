using MediatR;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.Notifications.Commands.MarkAllNotificationsRead;

internal sealed class MarkAllNotificationsReadHandler(INotificationRepository notificationRepository)
    : IRequestHandler<MarkAllNotificationsReadCommand>
{
    public Task Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken) =>
        notificationRepository.MarkAllAsRead(request.UserId);
}
