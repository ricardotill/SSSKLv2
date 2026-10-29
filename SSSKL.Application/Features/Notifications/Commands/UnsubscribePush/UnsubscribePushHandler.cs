using MediatR;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.Notifications.Commands.UnsubscribePush;

internal sealed class UnsubscribePushHandler(IPushSubscriptionRepository pushSubscriptionRepository)
    : IRequestHandler<UnsubscribePushCommand>
{
    public Task Handle(UnsubscribePushCommand request, CancellationToken cancellationToken) =>
        pushSubscriptionRepository.Delete(request.UserId, request.Endpoint);
}
