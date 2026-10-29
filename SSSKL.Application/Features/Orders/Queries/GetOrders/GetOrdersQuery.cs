using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Orders.Queries.GetOrders;

public sealed record GetOrdersQuery(int Skip, int Take) : IRequest<PaginationObject<OrderDto>>;
