using MediatR;
using SSSKL.Contracts.Repositories;
using SSSKL.Contracts.Services;

namespace SSSKL.Application.Features.Events.Commands.DeleteEvent;

internal sealed class DeleteEventHandler(IEventRepository eventRepository, IEventNotifier eventNotifier)
    : IRequestHandler<DeleteEventCommand>
{
    public async Task Handle(DeleteEventCommand request, CancellationToken cancellationToken)
    {
        var e = await eventRepository.GetExisting(request.Id);
        EventAccess.EnsureCanManage(e, request.UserId, request.IsAdmin, "delete");

        await eventRepository.Delete(request.Id);
        await eventNotifier.NotifyEventChangedAsync();
    }
}
