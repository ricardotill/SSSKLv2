namespace SSSKLv2.Data.DAL.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    public Task<PaginationObject<Order>> GetAllPagination(int page);
}