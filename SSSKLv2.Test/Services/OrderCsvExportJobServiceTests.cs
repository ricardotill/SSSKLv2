using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SSSKLv2.Dto.Api.v1;
using SSSKLv2.Services;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Test.Services;

[TestClass]
public class OrderCsvExportJobServiceTests
{
    [TestMethod]
    public async Task StartExport_OnlyStartsOneActiveJobAndStoresCompletedCsv()
    {
        var exportReady = new TaskCompletionSource<string>();
        var orderService = Substitute.For<IOrderService>();
        orderService.ExportAllOrdersToCsvAsync().Returns(exportReady.Task);
        var sut = CreateSut(orderService);

        var first = sut.StartExport("admin");
        var second = sut.StartExport("other-admin");

        first.Started.Should().BeTrue();
        second.Started.Should().BeFalse();
        second.Job.Id.Should().Be(first.Job.Id);

        exportReady.SetResult("OrderId\n123");
        var completed = await WaitForTerminalState(sut, first.Job.Id);

        completed!.Status.Should().Be(CsvExportJobStatus.Completed);
        completed.FileName.Should().StartWith("Orders_Export_");
        completed.CompletedAt.Should().NotBeNull();

        var csv = sut.GetCsv(first.Job.Id);
        csv.Should().NotBeNull();
        Encoding.UTF8.GetString(csv!.Value.Bytes).Should().Be("OrderId\n123");
        csv.Value.FileName.Should().Be(completed.FileName);
        await orderService.Received(1).ExportAllOrdersToCsvAsync();
    }

    [TestMethod]
    public async Task RunJob_WhenExportFails_MarksJobFailedAndDoesNotExposeCsv()
    {
        var orderService = Substitute.For<IOrderService>();
        orderService.ExportAllOrdersToCsvAsync().Returns<Task<string>>(_ => throw new InvalidOperationException("database down"));
        var sut = CreateSut(orderService);

        var started = sut.StartExport("admin");
        var failed = await WaitForTerminalState(sut, started.Job.Id);

        failed!.Status.Should().Be(CsvExportJobStatus.Failed);
        failed.ErrorMessage.Should().Be("database down");
        sut.GetCsv(started.Job.Id).Should().BeNull();
    }

    private static OrderCsvExportJobService CreateSut(IOrderService orderService)
    {
        var provider = Substitute.For<IServiceProvider>();
        provider.GetService(typeof(IOrderService)).Returns(orderService);
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(provider);
        var factory = Substitute.For<IServiceScopeFactory>();
        factory.CreateScope().Returns(scope);

        return new OrderCsvExportJobService(factory, Substitute.For<ILogger<OrderCsvExportJobService>>());
    }

    private static async Task<CsvExportJobDto?> WaitForTerminalState(OrderCsvExportJobService sut, Guid jobId)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var status = sut.GetStatus(jobId);
            if (status?.Status is CsvExportJobStatus.Completed or CsvExportJobStatus.Failed)
                return status;
            await Task.Delay(10);
        }

        return sut.GetStatus(jobId);
    }
}
