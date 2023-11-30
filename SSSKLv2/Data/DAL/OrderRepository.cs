using SSSKLv2.Data.DAL.Interfaces;

namespace SSSKLv2.Data.DAL;

public class OrderRepository : IOrderRepository
{
    public Task<Order> GetById()
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Order>> GetAll()
    {
        throw new NotImplementedException();
    }

    public Task Create(Order obj)
    {
        throw new NotImplementedException();
    }

    public Task Update(Order obj)
    {
        throw new NotImplementedException();
    }

    public Task Delete(Guid id)
    {
        throw new NotImplementedException();
    }
}