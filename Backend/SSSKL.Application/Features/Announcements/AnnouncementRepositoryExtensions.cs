using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Entities;
using SSSKL.Domain.Exceptions;

namespace SSSKL.Application.Features.Announcements;

internal static class AnnouncementRepositoryExtensions
{
    /// <summary>Loads an announcement, throwing <see cref="NotFoundException"/> when it doesn't exist.</summary>
    public static async Task<Announcement> GetExisting(this IAnnouncementRepository repository, Guid id) =>
        await repository.GetById(id) ?? throw new NotFoundException("Announcement not found");
}
