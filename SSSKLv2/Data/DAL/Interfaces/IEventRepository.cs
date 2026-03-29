using SSSKLv2.Data;

namespace SSSKLv2.Data.DAL.Interfaces;

public interface IEventRepository
{
    Task<IList<Event>> GetAll(int skip = 0, int take = 15, bool futureOnly = false, IList<string>? userRoles = null, bool isAdmin = false);
    Task<int> GetCount(bool futureOnly = false, IList<string>? userRoles = null, bool isAdmin = false);
    Task<Event?> GetById(Guid id);
    Task Add(Event entity);
    Task Update(Event entity);
    Task Delete(Guid id);
    Task<EventResponse?> GetResponse(Guid eventId, string userId);
    Task AddResponse(EventResponse response);
    Task UpdateResponse(EventResponse response);
    Task DeleteResponse(EventResponse response);
}
