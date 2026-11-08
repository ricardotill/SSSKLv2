using SSSKL.Mobile.Core.Models;

namespace SSSKL.Mobile.Core.Api;

/// <summary>The part of the SSSKL API (<c>/api/v1</c>) that the native screens use.</summary>
public interface ISssklApi
{
    Task<CurrentUser> GetMeAsync(CancellationToken cancellationToken = default);
    Task<OrderInitialize> GetOrderInitializeAsync(CancellationToken cancellationToken = default);
    Task SubmitOrderAsync(OrderSubmit order, CancellationToken cancellationToken = default);
    Task<UserStats> GetStatsAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AchievementEntry>> GetAchievementEntriesAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Returns the unseen achievement entries. The server marks them seen in the same call.</summary>
    Task<IReadOnlyList<AchievementEntry>> GetUnseenAchievementsAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns null when the setting doesn't exist.</summary>
    Task<GlobalSetting?> GetGlobalSettingAsync(string key, CancellationToken cancellationToken = default);

    Task UploadProfilePictureAsync(Base64FileUpload file, CancellationToken cancellationToken = default);
    Task DeleteProfilePictureAsync(CancellationToken cancellationToken = default);
}
