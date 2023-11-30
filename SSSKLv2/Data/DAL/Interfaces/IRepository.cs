namespace SSSKLv2.Data.DAL.Interfaces;

public interface IRepository<T>
{
    public Task<T> GetById(Guid id);
    public Task<IEnumerable<T>> GetAll();
    public Task Create(T obj);
    public Task Update(T obj);
    public Task Delete(Guid id);
}