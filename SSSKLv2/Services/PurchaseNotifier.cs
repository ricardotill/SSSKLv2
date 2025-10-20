using Microsoft.AspNetCore.SignalR;
using SSSKLv2.Services.Hubs;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Services;

public class PurchaseNotifier : IPurchaseNotifier
{
    private readonly IHubContext<LiveMetricsHub> _hubContext;

    public PurchaseNotifier(IHubContext<LiveMetricsHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyUserPurchaseAsync(UserPurchaseEvent @event)
    {
        return _hubContext.Clients.All.SendAsync("UserPurchase", @event);
    }
    
    public Task NotifyAchievementAsync(AchievementEvent @event)
    {
        return _hubContext.Clients.All.SendAsync("Achievement", @event);
    }
}
