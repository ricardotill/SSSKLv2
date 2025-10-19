namespace SSSKLv2.Services.Interfaces;

public interface IPurchaseNotifier
{
    Task NotifyUserPurchaseAsync(UserPurchaseDto dto);
}

public record UserPurchaseDto(string UserName, string ProductName, int Quantity, DateTime Timestamp);
