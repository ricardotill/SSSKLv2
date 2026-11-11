using MediatR;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.Orders.Queries.GetOrderById;

internal sealed class GetOrderByIdHandler(IOrderRepository orderRepository)
    : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetById(request.Id);

        if (!request.IsAdmin && !string.Equals(order.User?.Id, request.UserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Only admins can view orders of other users");
        }

        return order.ToDto();
    }
}
