using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Orders.Queries.GetLatestOrders;

public sealed record GetLatestOrdersQuery(int Take) : IRequest<IReadOnlyList<OrderDto>>;
