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

    /// <summary>Changes the signed-in user's name and phone number; null fields stay as they are.</summary>
    Task UpdateMeAsync(SelfUpdate update, CancellationToken cancellationToken = default);

    Task<TwoFactorStatus> GetTwoFactorStatusAsync(CancellationToken cancellationToken = default);

    Task<int> GetUnreadNotificationCountAsync(CancellationToken cancellationToken = default);

    /// <summary>Read and unread notifications, newest first.</summary>
    Task<IReadOnlyList<NotificationItem>> GetNotificationsAsync(int skip, int take, CancellationToken cancellationToken = default);

    Task MarkNotificationReadAsync(Guid id, CancellationToken cancellationToken = default);
    Task MarkAllNotificationsReadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether the user may see quotes (they are limited to some roles), probed like the SPA menu does:
    /// only a 403 hides them.
    /// </summary>
    Task<bool> CanAccessQuotesAsync(CancellationToken cancellationToken = default);
}
