using MediatR;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.Notifications.Queries.GetUnreadNotificationCount;

internal sealed class GetUnreadNotificationCountHandler(INotificationRepository notificationRepository)
    : IRequestHandler<GetUnreadNotificationCountQuery, int>
{
    public Task<int> Handle(GetUnreadNotificationCountQuery request, CancellationToken cancellationToken) =>
        notificationRepository.GetUnreadCount(request.UserId);
}
