using SSSKL.Domain.Entities;


namespace SSSKL.Contracts.Repositories;

public interface IUserStatRepository
{
    Task<UserStat> GetByUserId(string userId);
    Task<UserStat> GetOrCreateByUserId(string userId);
    Task<UserStat> RecalculateByUserId(string userId);
    Task<bool> TryMarkRecalculatedAtIfEligible(string userId, DateTime claimedAtUtc, TimeSpan minInterval);
    Task Update(UserStat userStat);
}
