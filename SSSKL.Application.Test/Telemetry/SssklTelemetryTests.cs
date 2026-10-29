using System.Diagnostics.Metrics;
using FluentAssertions;
using SSSKL.Contracts.Telemetry;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Test.Telemetry;

[TestClass]
public class SssklTelemetryTests
{
    [TestMethod]
    public void RecordOrders_EmitsCountAndRevenuePerProduct()
    {
        var measurements = new List<(string Instrument, double Value, string? Product)>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == SssklTelemetry.Name) l.EnableMeasurementEvents(instrument);
        };
        void Capture(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            string? product = null;
            foreach (var tag in tags) if (tag.Key == "product") product = tag.Value as string;
            measurements.Add((instrument.Name, value, product));
        }
        listener.SetMeasurementEventCallback<long>((i, v, t, _) => Capture(i, v, t));
        listener.SetMeasurementEventCallback<double>((i, v, t, _) => Capture(i, v, t));
        listener.Start();

        SssklTelemetry.RecordOrders([
            new Order { ProductNaam = "Cola", Paid = 1.5m, User = new ApplicationUser() },
            new Order { ProductNaam = "Cola", Paid = 1.5m, User = new ApplicationUser() }
        ]);

        measurements.Where(m => m.Instrument == "sssklv2.orders.created" && m.Product == "Cola")
            .Sum(m => m.Value).Should().Be(2);
        measurements.Where(m => m.Instrument == "sssklv2.orders.revenue" && m.Product == "Cola")
            .Sum(m => m.Value).Should().Be(3.0);
    }
}
