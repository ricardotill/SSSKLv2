using MediatR;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Events;
using SSSKL.Contracts.Repositories;
using SSSKL.Contracts.Telemetry;
using SSSKL.Domain.Entities;
using SSSKL.Domain.Events;

namespace SSSKL.Application.Features.TopUps.Commands.CreateTopUp;

internal sealed class CreateTopUpHandler(
    ITopUpRepository topUpRepository,
    IApplicationUserRepository applicationUserRepository,
    IDomainEventDispatcher eventDispatcher)
    : IRequestHandler<CreateTopUpCommand, TopUpDto>
{
    public async Task<TopUpDto> Handle(CreateTopUpCommand request, CancellationToken cancellationToken)
    {
        // Load the real user so EF Core can track and update it correctly.
        // Throws NotFoundException for an unknown username.
        var user = await applicationUserRepository.GetByUsername(request.UserName);

        var topUp = new TopUp
        {
            User = user,
            Saldo = request.Saldo
        };

        await topUpRepository.Create(topUp);
        SssklTelemetry.RecordTopUp(topUp.Saldo);
        await eventDispatcher.DispatchAsync(new TopUpEvent(topUp));

        return topUp.ToDto();
    }
}
