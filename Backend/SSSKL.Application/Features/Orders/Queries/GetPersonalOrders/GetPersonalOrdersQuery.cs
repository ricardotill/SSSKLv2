using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Orders.Queries.GetPersonalOrders;

public sealed record GetPersonalOrdersQuery(string Username, int Skip, int Take) : IRequest<PaginationObject<OrderDto>>;
