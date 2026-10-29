using MediatR;
using SSSKL.Application.Shared;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Constants;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Features.Events.Commands.RespondToEvent;

internal sealed class RespondToEventHandler(IEventRepository eventRepository, IUserRoleProvider roleProvider)
    : IRequestHandler<RespondToEventCommand>
{
    public async Task Handle(RespondToEventCommand request, CancellationToken cancellationToken)
    {
        var e = await eventRepository.GetExisting(request.Id);

        if (e.RequiredRoles.Count > 0)
        {
            var userRoles = await roleProvider.GetRoles(request.UserId);
            if (!userRoles.Contains(Roles.Admin) && !e.RequiredRoles.Any(r => userRoles.Contains(r.Name!)))
            {
                throw new UnauthorizedAccessException("You don't have the required role to RSVP to this event.");
            }
        }

        var response = await eventRepository.GetResponse(request.Id, request.UserId);
        if (response == null)
        {
            await eventRepository.AddResponse(new EventResponse
            {
                EventId = request.Id,
                UserId = request.UserId,
                Status = request.Status
            });
            return;
        }

        response.Status = request.Status;
        await eventRepository.UpdateResponse(response);
    }
}
