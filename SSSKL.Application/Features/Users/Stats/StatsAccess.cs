using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Exceptions;

namespace SSSKL.Application.Features.Users.Stats;

internal static class StatsAccess
{
    public static async Task EnsureCanAccess(IApplicationUserRepository applicationUserRepository, string userId, string? requesterId, bool isAdmin)
    {
        _ = await applicationUserRepository.FindById(userId) ?? throw new NotFoundException("ApplicationUser not found");

        if (!isAdmin && requesterId != userId)
            throw new UnauthorizedAccessException("Only the user or an admin can access these statistics.");
    }
}
