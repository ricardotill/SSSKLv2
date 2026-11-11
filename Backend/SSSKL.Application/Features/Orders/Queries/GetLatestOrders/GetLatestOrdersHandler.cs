using MediatR;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.Orders.Queries.GetLatestOrders;

internal sealed class GetLatestOrdersHandler(IOrderRepository orderRepository)
    : IRequestHandler<GetLatestOrdersQuery, IReadOnlyList<OrderDto>>
{
    public async Task<IReadOnlyList<OrderDto>> Handle(GetLatestOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await orderRepository.GetLatest(request.Take);
        return orders.Select(o => o.ToDto()).ToList();
    }
}
