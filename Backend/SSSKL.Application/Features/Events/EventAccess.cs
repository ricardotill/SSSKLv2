using Ganss.Xss;
using SSSKL.Application.Shared;
using SSSKL.Contracts.Dto.Api;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Constants;
using SSSKL.Domain.Entities;
using SSSKL.Domain.Exceptions;

namespace SSSKL.Application.Features.Events;

/// <summary>Lookups and rules shared by the event handlers.</summary>
internal static class EventAccess
{
    public static async Task<Event> GetExisting(this IEventRepository eventRepository, Guid id) =>
        await eventRepository.GetById(id) ?? throw new NotFoundException("Event not found");

    public static async Task<(IList<string>? UserRoles, bool IsAdmin)> GetUserAccess(IUserRoleProvider roleProvider, string? userId)
    {
        if (string.IsNullOrEmpty(userId))
            return (null, false);

        var userRoles = await roleProvider.GetRoles(userId);
        return (userRoles, userRoles.Contains(Roles.Admin));
    }

    public static void EnsureCanManage(Event e, string userId, bool isAdmin, string action)
    {
        if (e.CreatorId != userId && !isAdmin)
            throw new UnauthorizedAccessException($"Only the creator or an admin can {action} this event.");
    }

    public static void ApplyDetails(Event e, EventCreateDto dto)
    {
        var sanitizer = new HtmlSanitizer();

        e.Title = dto.Title;
        e.Description = sanitizer.Sanitize(dto.Description);
        e.StartDateTime = dto.StartDateTime.GetValueOrDefault();
        e.EndDateTime = dto.EndDateTime.GetValueOrDefault();
        e.LocationName = dto.LocationName;
        e.Latitude = dto.Latitude;
        e.Longitude = dto.Longitude;
    }

    public static async Task ApplyRequiredRoles(IRoleRepository roleRepository, Event e, IEnumerable<string>? requiredRoles)
    {
        e.RequiredRoles.Clear();
        if (requiredRoles == null || !requiredRoles.Any())
            return;

        var rolesToFetch = requiredRoles.Except(Roles.AllProtected, StringComparer.OrdinalIgnoreCase);
        var newRoles = await roleRepository.GetByNames(rolesToFetch);
        foreach (var role in newRoles)
        {
            e.RequiredRoles.Add(role);
        }
    }
}
