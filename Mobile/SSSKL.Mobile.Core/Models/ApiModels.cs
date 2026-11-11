namespace SSSKL.Mobile.Core.Models;

// Client-side copies of the API DTOs (SSSKL.Contracts), limited to the fields the app uses.
// SSSKL.Contracts isn't referenced because it pulls in Domain, Identity.Stores and MediatR.

public sealed record AccessTokenResponse(string TokenType, string AccessToken, long ExpiresIn, string RefreshToken);

public sealed record RefreshRequest(string RefreshToken);

public sealed record CurrentUser
{
    public string Id { get; init; } = "";
    public string UserName { get; init; } = "";
    public string FullName { get; init; } = "";
    public string? Name { get; init; }
    public string? Surname { get; init; }
    public string? Email { get; init; }
    public bool EmailConfirmed { get; init; }
    public string? PhoneNumber { get; init; }
    public decimal Saldo { get; init; }
    public string? ProfilePictureUrl { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];

    public string DisplayName => string.IsNullOrWhiteSpace(FullName) ? UserName : FullName;
    public bool IsInRole(string role) => Roles.Contains(role, StringComparer.Ordinal);
}

public sealed record OrderInitialize
{
    public IReadOnlyList<OrderProduct> Products { get; init; } = [];
    public IReadOnlyList<OrderUser> Users { get; init; } = [];
}

public sealed record OrderProduct(Guid Id, string Name, int Stock, decimal Price);

public sealed record OrderUser(string Id, string FullName, string? ProfilePictureUrl);

public sealed record OrderSubmit(IReadOnlyList<Guid> Products, IReadOnlyList<Guid> Users, int Amount, bool Split);

/// <summary><c>GET applicationuser/{id}/stats</c> (UserStatDto). Only for yourself, or any user for admins.</summary>
public sealed record UserStats
{
    /// <summary>The API's "no data" value for the minimum-minutes records (int.MaxValue).</summary>
    public const int NoMinutes = int.MaxValue;

    public string UserId { get; init; } = "";
    public int TotalOrders { get; init; }
    public int TotalItemsBought { get; init; }
    public decimal TotalSpent { get; init; }
    public decimal TotalTopUp { get; init; }
    public int QuoteCount { get; init; }
    public int QuoteVotesGiven { get; init; }
    public int QuoteVotesReceived { get; init; }
    public int ReactionCount { get; init; }
    public int CurrentStreak { get; init; }
    public DateTime? LastActivityDate { get; init; }
    public DateTime? MembershipStartDate { get; init; }
    public int MaxOrdersPerHour { get; init; }
    public int MinMinutesBetweenOrders { get; init; }
    public int MinMinutesBetweenTopUp { get; init; }
    public decimal MaxSingleTopUp { get; init; }
    public DateTime? LastOrderDate { get; init; }
    public DateTime? LastTopUpDate { get; init; }
    public DateTime? LastStatsRecalculatedAt { get; init; }
}

public sealed record AchievementEntry
{
    public Guid Id { get; init; }
    public Guid AchievementId { get; init; }
    public string AchievementName { get; init; } = "";
    public string AchievementDescription { get; init; } = "";
    public DateTime DateAdded { get; init; }
    public string? ImageUrl { get; init; }
    public string Tier { get; init; } = "";
    public Guid? ParentAchievementId { get; init; }
    public string? ParentAchievementName { get; init; }
    // Set on the earners list (achievement/{id}/earners).
    public string? UserId { get; init; }
    public string? UserName { get; init; }
    public string? UserFullName { get; init; }
    public string? UserProfilePictureUrl { get; init; }
}

/// <summary>An achievement with one user's progress (<c>achievement/all-for-user/{userId}</c>).</summary>
public sealed record AchievementListing
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public DateTime? DateAdded { get; init; }
    public string? ImageUrl { get; init; }
    public bool Completed { get; init; }
    public string Tier { get; init; } = "";
    public Guid? ParentAchievementId { get; init; }
    public string? ParentAchievementName { get; init; }
}

/// <summary>A page of a list (PaginationObject).</summary>
public sealed record Page<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
}

/// <summary>One of your orders (OrderDto). <see cref="CreatedOn"/> is UTC.</summary>
public sealed record OrderItem
{
    public Guid Id { get; init; }
    public DateTime CreatedOn { get; init; }
    public string ProductName { get; init; } = "";
    public int Amount { get; init; }
    public decimal Paid { get; init; }
}

/// <summary>One of your top-ups (TopUpDto). <see cref="CreatedOn"/> is UTC.</summary>
public sealed record TopUpItem
{
    public Guid Id { get; init; }
    public decimal Saldo { get; init; }
    public DateTime CreatedOn { get; init; }
}

/// <summary>A member as other members see them (ApplicationUserDto): saldo list and profiles.</summary>
public sealed record UserSummary
{
    public string Id { get; init; } = "";
    public string UserName { get; init; } = "";
    public string FullName { get; init; } = "";
    public decimal Saldo { get; init; }
    public string? ProfilePictureUrl { get; init; }
    public string? Description { get; init; }

    public string DisplayName => string.IsNullOrWhiteSpace(FullName) ? UserName : FullName;
}

/// <summary>A product (ProductDto), for the leaderboard's product choice.</summary>
public sealed record Product
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public bool EnableLeaderboard { get; init; }
}

/// <summary>An event (EventDto). Times are UTC.</summary>
public sealed record EventItem
{
    public Guid Id { get; init; }
    public string Title { get; init; } = "";
    /// <summary>HTML from the SPA's editor.</summary>
    public string Description { get; init; } = "";
    public string? ImageUrl { get; init; }
    public DateTime StartDateTime { get; init; }
    public DateTime EndDateTime { get; init; }
    public string CreatorName { get; init; } = "";
    public string CreatorId { get; init; } = "";
    public IReadOnlyList<EventMember> AcceptedUsers { get; init; } = [];
    public IReadOnlyList<EventMember> DeclinedUsers { get; init; } = [];
    /// <summary><see cref="EventResponse.Accepted"/>, <see cref="EventResponse.Declined"/> or null.</summary>
    public string? UserResponse { get; init; }
    public string? LocationName { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public IReadOnlyList<string> RequiredRoles { get; init; } = [];
}

public sealed record EventMember(string UserId, string UserName, string? ProfilePictureUrl);

/// <summary>The API's EventResponseStatus values (sent as strings).</summary>
public static class EventResponse
{
    public const string Accepted = "Accepted";
    public const string Declined = "Declined";
}

public sealed record EventRsvp(string Status);

/// <summary>A quote (QuoteDto).</summary>
public sealed record QuoteItem
{
    public Guid Id { get; init; }
    public string Text { get; init; } = "";
    public DateTime DateSaid { get; init; }
    public UserSummary? CreatedBy { get; init; }
    public IReadOnlyList<QuoteAuthor> Authors { get; init; } = [];
    public int VoteCount { get; init; }
    public int CommentsCount { get; init; }
    public bool HasVoted { get; init; }
}

/// <summary>Who said it: a member (<see cref="ApplicationUser"/>) or a free-text name.</summary>
public sealed record QuoteAuthor
{
    public Guid Id { get; init; }
    public string? ApplicationUserId { get; init; }
    public UserSummary? ApplicationUser { get; init; }
    public string? CustomName { get; init; }

    public string DisplayName => ApplicationUser?.DisplayName ?? CustomName ?? "";
}

/// <summary>
/// A comment or reaction (ReactionDto) on an event, quote or another reaction. Children with 👍 or ❤️ are
/// likes; other children are replies.
/// </summary>
public sealed record Reaction
{
    public Guid Id { get; init; }
    public string UserId { get; init; } = "";
    public string UserName { get; init; } = "";
    public string? ProfilePictureUrl { get; init; }
    public string Content { get; init; } = "";
    public string? TargetUserName { get; init; }
    public DateTime CreatedOn { get; init; }
    public IReadOnlyList<Reaction> Reactions { get; init; } = [];
}

/// <summary>What a reaction is on (ReactionTargetType).</summary>
public static class ReactionTarget
{
    public const string Event = "Event";
    public const string Quote = "Quote";
    public const string Reaction = "Reaction";
}

/// <summary>Adds the reaction, or removes it when you already placed the same one (<c>reactions/toggle</c>).</summary>
public sealed record ToggleReaction(Guid TargetId, string TargetType, string Content);

public enum LeaderboardPeriod
{
    Last12Hours,
    Month,
    AllTime
}

public sealed record LeaderboardEntry
{
    public int Position { get; init; }
    public string FullName { get; init; } = "";
    public int Amount { get; init; }
    public string? ProfilePictureUrl { get; init; }
    public string? UserId { get; init; }
}

/// <summary>A notification (<c>GET notifications</c>). <see cref="CreatedOn"/> is UTC, sent without an offset.</summary>
public sealed record NotificationItem
{
    public Guid Id { get; init; }
    public string Title { get; init; } = "";
    public string Message { get; init; } = "";
    public bool IsRead { get; init; }
    public string? LinkUri { get; init; }
    public DateTime CreatedOn { get; init; }
}

/// <summary><see cref="UpdatedOn"/> is kept as the raw string: it is only compared, never parsed.</summary>
public sealed record GlobalSetting(string Key, string Value, string UpdatedOn);

public sealed record Base64FileUpload(string FileName, string ContentType, string Base64Content);

/// <summary><c>PUT applicationuser/me</c>: null leaves a field unchanged (ApplicationUserSelfUpdateDto).</summary>
public sealed record SelfUpdate(string? Name, string? Surname, string? PhoneNumber, string? Description = null);

/// <summary>An empty body reads the 2FA status (<c>POST identity/manage/2fa</c>).</summary>
public sealed record TwoFactorRequest;

public sealed record TwoFactorStatus
{
    public bool IsTwoFactorEnabled { get; init; }
}

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string User = "User";
    public const string Kiosk = "Kiosk";
    public const string Guest = "Guest";
}
