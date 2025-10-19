using Microsoft.AspNetCore.SignalR;
using SSSKLv2.Services.Hubs;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Services;

public class PurchaseNotifier : IPurchaseNotifier
{
    private readonly IHubContext<PurchaseHub> _hubContext;

    public PurchaseNotifier(IHubContext<PurchaseHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyUserPurchaseAsync(UserPurchaseDto dto)
    {
        // Broadcast to all connected clients; client method name: "UserPurchase"
        return _hubContext.Clients.All.SendAsync("UserPurchase", dto);
    }
}
