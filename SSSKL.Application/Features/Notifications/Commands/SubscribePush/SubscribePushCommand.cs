using MediatR;
using SSSKL.Contracts.Dto;

namespace SSSKL.Application.Features.Notifications.Commands.SubscribePush;

public sealed record SubscribePushCommand(string UserId, PushSubscriptionDto Subscription) : IRequest;
