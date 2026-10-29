using MediatR;
using SSSKL.Contracts.Telemetry;

namespace SSSKL.Application.Events;

/// <summary>
/// Runs every notification handler in turn. Domain events are published after the triggering change is
/// committed, so a failing handler (achievements, stats) must not turn an already-saved order or top-up
/// into an error response or stop the other handlers.
/// </summary>
public sealed class ResilientNotificationPublisher(ILogger<ResilientNotificationPublisher> logger) : INotificationPublisher
{
    public async Task Publish(IEnumerable<NotificationHandlerExecutor> handlerExecutors, INotification notification, CancellationToken cancellationToken)
    {
        foreach (var executor in handlerExecutors)
        {
            try
            {
                await executor.HandlerCallback(notification, cancellationToken);
            }
            catch (Exception ex)
            {
                var handlerName = executor.HandlerInstance.GetType().Name;
                SssklTelemetry.RecordSideEffectFailure(handlerName);
                logger.LogError(ex, "Domain event handler {Handler} failed for {Event}",
                    handlerName, notification.GetType().Name);
            }
        }
    }
}
