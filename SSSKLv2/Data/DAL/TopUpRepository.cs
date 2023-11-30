using SSSKLv2.Data.DAL.Interfaces;

namespace SSSKLv2.Data.DAL;

public class TopUpRepository : ITopUpRepository
{
    public Task<TopUp> GetById()
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<TopUp>> GetAll()
    {
        throw new NotImplementedException();
    }

    public Task Create(TopUp obj)
    {
        throw new NotImplementedException();
    }

    public Task Update(TopUp obj)
    {
        throw new NotImplementedException();
    }

    public Task Delete(Guid id)
    {
        throw new NotImplementedException();
    }
}