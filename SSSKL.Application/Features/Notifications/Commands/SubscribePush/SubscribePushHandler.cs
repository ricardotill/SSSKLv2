using MediatR;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Features.Notifications.Commands.SubscribePush;

internal sealed class SubscribePushHandler(IPushSubscriptionRepository pushSubscriptionRepository)
    : IRequestHandler<SubscribePushCommand>
{
    public Task Handle(SubscribePushCommand request, CancellationToken cancellationToken) =>
        pushSubscriptionRepository.Upsert(new PushSubscription
        {
            UserId = request.UserId,
            Endpoint = request.Subscription.Endpoint,
            P256dh = request.Subscription.P256dh,
            Auth = request.Subscription.Auth,
            CreatedOn = DateTime.UtcNow
        });
}
