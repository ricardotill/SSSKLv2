using MediatR;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.Orders.Commands.DeleteOrder;

internal sealed class DeleteOrderHandler(IOrderRepository orderRepository, IUserStatRepository userStatRepository)
    : IRequestHandler<DeleteOrderCommand>
{
    public async Task Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetById(request.Id);

        if (!request.IsAdmin && !string.Equals(order.User?.Id, request.UserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Only admins can delete orders of other users");
        }

        await orderRepository.Delete(request.Id);
        await userStatRepository.RecalculateByUserId(order.User!.Id);
    }
}
