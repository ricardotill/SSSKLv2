namespace SSSKL.Contracts.Services;

public interface IPurchaseNotifier
{
    Task NotifyUserPurchaseAsync(UserPurchaseEvent @event);
    Task NotifyAchievementAsync(AchievementEvent @event);
}

public record UserPurchaseEvent(string UserName, string ProductName, int Quantity, DateTime Timestamp);
public record AchievementEvent(string AchievementName, string UserFullName, string? ImageUrl);