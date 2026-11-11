using MediatR;
using SSSKL.Contracts.Repositories;
using SSSKL.Contracts.Services;

namespace SSSKL.Application.Features.Events.Commands.UpdateEvent;

internal sealed class UpdateEventHandler(
    IEventRepository eventRepository,
    IRoleRepository roleRepository,
    IEventNotifier eventNotifier) : IRequestHandler<UpdateEventCommand>
{
    public async Task Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        var e = await eventRepository.GetExisting(request.Id);
        EventAccess.EnsureCanManage(e, request.UserId, request.IsAdmin, "update");

        EventAccess.ApplyDetails(e, request.Event);
        await EventAccess.ApplyRequiredRoles(roleRepository, e, request.Event.RequiredRoles);

        await eventRepository.Update(e);
        await eventNotifier.NotifyEventChangedAsync();
    }
}
