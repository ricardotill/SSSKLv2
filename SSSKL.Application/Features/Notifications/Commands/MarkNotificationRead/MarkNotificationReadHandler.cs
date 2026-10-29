using MediatR;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.Notifications.Commands.MarkNotificationRead;

internal sealed class MarkNotificationReadHandler(INotificationRepository notificationRepository)
    : IRequestHandler<MarkNotificationReadCommand>
{
    public Task Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken) =>
        notificationRepository.MarkAsRead(request.Id, request.UserId);
}
