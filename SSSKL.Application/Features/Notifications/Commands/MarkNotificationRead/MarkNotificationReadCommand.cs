using MediatR;

namespace SSSKL.Application.Features.Notifications.Commands.MarkNotificationRead;

public sealed record MarkNotificationReadCommand(Guid Id, string UserId) : IRequest;
