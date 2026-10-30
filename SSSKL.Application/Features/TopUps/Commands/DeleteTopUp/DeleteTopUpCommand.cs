using MediatR;

namespace SSSKL.Application.Features.TopUps.Commands.DeleteTopUp;

public sealed record DeleteTopUpCommand(Guid Id) : IRequest;
