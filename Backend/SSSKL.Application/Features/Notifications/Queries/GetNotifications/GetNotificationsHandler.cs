using MediatR;
using SSSKL.Contracts.Dto;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.Notifications.Queries.GetNotifications;

internal sealed class GetNotificationsHandler(INotificationRepository notificationRepository)
    : IRequestHandler<GetNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    public async Task<IReadOnlyList<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var notifications = await notificationRepository.GetForUser(request.UserId, request.UnreadOnly, request.Skip, request.Take);

        return notifications
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                Title = n.Title,
                Message = n.Message,
                IsRead = n.IsRead,
                LinkUri = n.LinkUri,
                CreatedOn = n.CreatedOn
            })
            .ToList();
    }
}
