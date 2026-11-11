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

    Task<Page<OrderItem>> GetPersonalOrdersAsync(int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>Deletes one of your own orders (admins: any order); the saldo is refunded.</summary>
    Task DeleteOrderAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Page<TopUpItem>> GetPersonalTopUpsAsync(int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>All members with their saldo (the saldo list).</summary>
    Task<Page<UserSummary>> GetUsersAsync(int skip, int take, CancellationToken cancellationToken = default);

    Task<UserSummary> GetUserProfileAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaderboardEntry>> GetLeaderboardAsync(Guid productId, LeaderboardPeriod period, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recalculates a user's statistics. Users may do this once every seven days: a 429 <see cref="ApiException"/> otherwise.
    /// </summary>
    Task<UserStats> RecalculateStatsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Every achievement with the user's progress.</summary>
    Task<IReadOnlyList<AchievementListing>> GetAchievementListingAsync(string userId, CancellationToken cancellationToken = default);

    /// <param name="futureOnly">Only events that haven't ended yet.</param>
    Task<Page<EventItem>> GetEventsAsync(int skip, int take, bool futureOnly, CancellationToken cancellationToken = default);
    Task<EventItem> GetEventAsync(Guid id, CancellationToken cancellationToken = default);
    Task RespondToEventAsync(Guid id, string status, CancellationToken cancellationToken = default);
    Task UpdateEventImageAsync(Guid id, Base64FileUpload image, CancellationToken cancellationToken = default);
    Task DeleteEventAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Page<QuoteItem>> GetQuotesAsync(int skip, int take, CancellationToken cancellationToken = default);
    Task<QuoteItem> GetQuoteAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Votes, or takes your vote back. Returns whether you voted now.</summary>
    Task<bool> ToggleQuoteVoteAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteQuoteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Comments on an event, quote or reaction, each with its replies and likes.</summary>
    Task<IReadOnlyList<Reaction>> GetReactionsAsync(Guid targetId, string targetType, CancellationToken cancellationToken = default);
    Task ToggleReactionAsync(ToggleReaction reaction, CancellationToken cancellationToken = default);
    /// <summary>Deletes your own reaction (admins: any), with its replies.</summary>
    Task DeleteReactionAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Everyone who earned an achievement, with their name and picture.</summary>
    Task<IReadOnlyList<AchievementEntry>> GetAchievementEarnersAsync(Guid achievementId, CancellationToken cancellationToken = default);
}
