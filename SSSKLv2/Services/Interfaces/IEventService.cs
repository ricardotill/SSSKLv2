using SSSKLv2.Data;
using SSSKLv2.Dto.Api;

namespace SSSKLv2.Services.Interfaces;

public interface IEventService
{
    Task<IEnumerable<EventDto>> GetAllEvents(int skip = 0, int take = 15, bool futureOnly = false, string? userId = null);
    Task<int> GetCount(bool futureOnly = false);
    Task<EventDto> GetEventById(Guid id, string? userId = null);
    Task<Guid> CreateEvent(EventCreateDto dto, string creatorId);
    Task UpdateEvent(Guid id, EventCreateDto dto, string userId, bool isAdmin);
    Task DeleteEvent(Guid id, string userId, bool isAdmin);
    Task RespondToEvent(Guid id, string userId, EventResponseStatus status);
}
