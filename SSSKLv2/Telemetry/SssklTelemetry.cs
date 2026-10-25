using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SSSKLv2.Telemetry;

/// <summary>
/// Business metrics and spans for SSSKLv2. The meter and activity source share the application name,
/// which ServiceDefaults subscribes to, so everything here flows to OTLP / Application Insights.
/// </summary>
public static class SssklTelemetry
{
    public const string Name = "SSSKLv2";

    public static readonly ActivitySource ActivitySource = new(Name);
    private static readonly Meter Meter = new(Name);

    private static readonly Counter<long> OrdersCreated =
        Meter.CreateCounter<long>("sssklv2.orders.created", "{order}", "Order lines saved");
    private static readonly Counter<double> OrderRevenue =
        Meter.CreateCounter<double>("sssklv2.orders.revenue", "EUR", "Amount charged to user saldo for orders");
    private static readonly Counter<long> TopUpsCreated =
        Meter.CreateCounter<long>("sssklv2.topups.created", "{topup}", "Top-ups saved");
    private static readonly Counter<double> TopUpAmount =
        Meter.CreateCounter<double>("sssklv2.topups.amount", "EUR", "Amount added to user saldo by top-ups");
    private static readonly Counter<long> PushSent =
        Meter.CreateCounter<long>("sssklv2.push.sent", "{message}", "Web push deliveries by outcome");
    private static readonly Counter<long> SideEffectFailures =
        Meter.CreateCounter<long>("sssklv2.side_effects.failed", "{failure}",
            "Failures in work that runs after a change is committed (event handlers, notifications, live updates)");
    private static readonly Histogram<double> JobDuration =
        Meter.CreateHistogram<double>("sssklv2.jobs.duration", "s", "Background job duration by job and outcome");

    public static void RecordOrders(IEnumerable<Data.Order> orders)
    {
        foreach (var order in orders)
        {
            var product = new KeyValuePair<string, object?>("product", order.ProductNaam);
            OrdersCreated.Add(1, product);
            OrderRevenue.Add((double)order.Paid, product);
        }
    }

    public static void RecordTopUp(decimal amount)
    {
        TopUpsCreated.Add(1);
        TopUpAmount.Add((double)amount);
    }

    /// <param name="outcome">success, expired or failed</param>
    public static void RecordPush(string outcome) =>
        PushSent.Add(1, new KeyValuePair<string, object?>("outcome", outcome));

    public static void RecordSideEffectFailure(string source) =>
        SideEffectFailures.Add(1, new KeyValuePair<string, object?>("source", source));

    public static void RecordJob(string job, string outcome, TimeSpan duration) =>
        JobDuration.Record(duration.TotalSeconds,
            new KeyValuePair<string, object?>("job", job),
            new KeyValuePair<string, object?>("outcome", outcome));
}
