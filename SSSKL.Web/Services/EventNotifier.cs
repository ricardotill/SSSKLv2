using Microsoft.AspNetCore.SignalR;
using SSSKL.Contracts.Services;
using SSSKL.Web.Hubs;

namespace SSSKL.Web.Services;

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
