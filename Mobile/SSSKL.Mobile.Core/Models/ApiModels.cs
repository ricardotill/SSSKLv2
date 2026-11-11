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

public sealed record UserStats
{
    public int TotalOrders { get; init; }
    public int TotalItemsBought { get; init; }
    public decimal TotalSpent { get; init; }
    public int CurrentStreak { get; init; }
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
}

/// <summary><see cref="UpdatedOn"/> is kept as the raw string: it is only compared, never parsed.</summary>
public sealed record GlobalSetting(string Key, string Value, string UpdatedOn);

public sealed record Base64FileUpload(string FileName, string ContentType, string Base64Content);

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string User = "User";
    public const string Kiosk = "Kiosk";
    public const string Guest = "Guest";
}
