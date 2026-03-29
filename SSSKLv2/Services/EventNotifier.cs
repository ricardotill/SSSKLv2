using Microsoft.AspNetCore.SignalR;
using SSSKLv2.Services.Hubs;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Services;

public class EventNotifier : IEventNotifier
{
    private readonly IHubContext<LiveMetricsHub> _hubContext;

    public EventNotifier(IHubContext<LiveMetricsHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyEventChangedAsync()
    {
        return _hubContext.Clients.All.SendAsync("EventChanged");
    }
}
