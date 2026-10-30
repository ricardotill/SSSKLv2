using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Orders.Queries.GetOrderInitialize;

public sealed record GetOrderInitializeQuery : IRequest<OrderInitializeDto>;
