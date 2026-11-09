using MediatR;

namespace SSSKL.Application.Features.Notifications.Queries.GetUnreadNotificationCount;

public sealed record GetUnreadNotificationCountQuery(string UserId) : IRequest<int>;
