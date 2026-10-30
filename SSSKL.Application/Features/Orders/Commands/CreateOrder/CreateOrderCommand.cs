using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Orders.Commands.CreateOrder;

/// <param name="ActingUserId">The user placing the order; the other users on it get a notification.</param>
public sealed record CreateOrderCommand(OrderSubmitDto Order, string? ActingUserId) : IRequest;
