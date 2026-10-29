using System.Collections.Concurrent;
using SSSKL.Contracts.Dto;
using SSSKL.Contracts.Repositories;
using SSSKL.Contracts.Services;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Shared;

internal sealed class AchievementAwarder(
    IAchievementRepository achievementRepository,
    IApplicationUserRepository applicationUserRepository,
    IPurchaseNotifier purchaseNotifier,
    IUserNotifier userNotifier) : IAchievementAwarder
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> AwardLocks = new();

    public Task<bool> AwardToUser(string userId, Guid achievementId)
        => AwardToUserInternal(userId, achievementId, new HashSet<Guid>());

    private async Task<bool> AwardToUserInternal(string userId, Guid achievementId, ISet<Guid> visitedAchievementIds)
    {
        if (!visitedAchievementIds.Add(achievementId))
        {
            return false;
        }

        var lockKey = $"{userId}:{achievementId}";
        var awardLock = AwardLocks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));
        await awardLock.WaitAsync();
        try
        {
            var entries = await achievementRepository.GetAllEntriesOfUser(userId);
            var user = await applicationUserRepository.GetById(userId)
                       ?? new ApplicationUser { Id = userId, UserName = userId };

            if (entries.Any(e => e.Achievement.Id == achievementId))
                return false; // Already awarded

            var achievement = (await achievementRepository.GetAll()).FirstOrDefault(a => a.Id == achievementId);
            if (achievement == null)
                return false;

            // Tiered Sequence: If this has a parent, ensure parent is awarded
            if (achievement.ParentAchievementId.HasValue && !entries.Any(e => e.Achievement.Id == achievement.ParentAchievementId.Value))
            {
                if (visitedAchievementIds.Contains(achievement.ParentAchievementId.Value))
                {
                    return false;
                }

                await AwardToUserInternal(userId, achievement.ParentAchievementId.Value, visitedAchievementIds);
            }

            var achievementEntry = new AchievementEntry
            {
                Id = Guid.NewGuid(),
                Achievement = achievement,
                User = user,
                Tier = achievement.Tier,
                HasSeen = false,
                CreatedOn = DateTime.Now
            };
            await achievementRepository.CreateEntryRange(new List<AchievementEntry> { achievementEntry });
            await Announce([achievementEntry]);
            return true;
        }
        finally
        {
            awardLock.Release();
            visitedAchievementIds.Remove(achievementId);
        }
    }

    public async Task<int> AwardToAllUsers(Guid achievementId)
    {
        var users = await applicationUserRepository.GetAll();
        var achievement = (await achievementRepository.GetAll()).FirstOrDefault(a => a.Id == achievementId);
        if (achievement == null)
            return 0;

        var newEntries = new List<AchievementEntry>();
        foreach (var user in users)
        {
            var entries = await achievementRepository.GetAllEntriesOfUser(user.Id);
            if (entries.All(e => e.Achievement.Id != achievementId))
            {
                newEntries.Add(new AchievementEntry
                {
                    Id = Guid.NewGuid(),
                    Achievement = achievement,
                    User = user,
                    HasSeen = false,
                    CreatedOn = DateTime.Now
                });
            }
        }
        if (newEntries.Any())
            await achievementRepository.CreateEntryRange(newEntries);
        await Announce(newEntries);
        return newEntries.Count;
    }

    private async Task Announce(IEnumerable<AchievementEntry> entries)
    {
        foreach (var entry in entries)
        {
            if (entry.Achievement == null || entry.User == null)
                continue;

            await purchaseNotifier.NotifyAchievementAsync(new AchievementEvent(
                entry.Achievement.Name,
                entry.User.FullName,
                entry.Achievement.Image != null ? $"/api/v1/blob/achievement/image/{entry.Achievement.Image.Id}" : null
            ));

            await userNotifier.NotifyAsync(
                entry.User.Id,
                "Nieuwe prestatie ontgrendeld!",
                $"Gefeliciteerd! Je hebt de prestatie '{entry.Achievement.Name}' ontgrendeld.",
                "/achievements",
                sendPush: true
            );
        }
    }
}
