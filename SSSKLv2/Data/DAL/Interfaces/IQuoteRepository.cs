using SSSKLv2.Data;

namespace SSSKLv2.Data.DAL.Interfaces;

public interface IQuoteRepository
{
    Task<IEnumerable<Quote>> GetAll(int skip = 0, int take = 15, IList<string>? userRoles = null, bool isAdmin = false, string? targetUserId = null);
    Task<Quote?> GetById(Guid id);
    Task Add(Quote quote);
    Task Update(Quote quote);
    Task Delete(Guid id);
}
