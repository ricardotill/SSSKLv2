using MediatR;

namespace SSSKL.Application.Features.Notifications.Commands.MarkAllNotificationsRead;

public sealed record MarkAllNotificationsReadCommand(string UserId) : IRequest;
