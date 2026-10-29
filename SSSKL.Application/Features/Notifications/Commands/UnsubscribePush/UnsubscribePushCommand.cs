using MediatR;

namespace SSSKL.Application.Features.Notifications.Commands.UnsubscribePush;

public sealed record UnsubscribePushCommand(string UserId, string Endpoint) : IRequest;
