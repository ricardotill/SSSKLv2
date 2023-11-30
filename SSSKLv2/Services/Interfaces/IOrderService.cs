using SSSKLv2.Data;

namespace SSSKLv2.Services.Interfaces;

public interface IOrderService
{
    public Task<Order> GetOrderById(Guid id);
    public Task<PaginationObject<Order>> GetAllPagination(int page);
    public Task DeleteOrder(Guid id);
}