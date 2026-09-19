using SSSKLv2.Data;

namespace SSSKLv2.Data.DAL.Interfaces;

public interface IUserStatRepository
{
    Task<UserStat> GetByUserId(string userId);
    Task<UserStat> GetOrCreateByUserId(string userId);
    Task<UserStat> RecalculateByUserId(string userId);
    Task Update(UserStat userStat);
}
