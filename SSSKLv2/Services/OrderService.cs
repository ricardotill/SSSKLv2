using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Services;

public class OrderService(IOrderRepository _orderRepository) : IOrderService
{
    public async Task<Order> GetOrderById(Guid id)
    {
        return await _orderRepository.GetById(id);
    }

    public async Task<PaginationObject<Order>> GetAllPagination(int page)
    {
        return await _orderRepository.GetAllPagination(page);
    }

    public async Task DeleteOrder(Guid id)
    {
        await _orderRepository.Delete(id);
    }
}