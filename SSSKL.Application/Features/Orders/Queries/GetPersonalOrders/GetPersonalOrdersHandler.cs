using MediatR;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.Orders.Queries.GetPersonalOrders;

internal sealed class GetPersonalOrdersHandler(IOrderRepository orderRepository)
    : IRequestHandler<GetPersonalOrdersQuery, PaginationObject<OrderDto>>
{
    public async Task<PaginationObject<OrderDto>> Handle(GetPersonalOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await orderRepository.GetPersonal(request.Username, request.Skip, request.Take);
        var totalCount = await orderRepository.GetPersonalCount(request.Username);

        return new PaginationObject<OrderDto>
        {
            Items = orders.Select(o => o.ToDto()).ToList(),
            TotalCount = totalCount
        };
    }
}
