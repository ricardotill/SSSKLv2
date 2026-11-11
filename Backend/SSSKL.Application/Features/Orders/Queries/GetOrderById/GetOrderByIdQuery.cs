using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Orders.Queries.GetOrderById;

/// <summary>Admins can read any order; other users only their own.</summary>
public sealed record GetOrderByIdQuery(Guid Id, string? UserId, bool IsAdmin) : IRequest<OrderDto>;
