using MediatR;
using SSSKL.Contracts.Dto;

namespace SSSKL.Application.Features.Notifications.Queries.GetNotifications;

public sealed record GetNotificationsQuery(string UserId, bool UnreadOnly, int Skip, int Take) : IRequest<IReadOnlyList<NotificationDto>>;
