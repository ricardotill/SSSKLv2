using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SSSKL.Application.Features.Orders.Export;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Test.Features.Orders;

[TestClass]
public class OrderCsvExportJobTests
{
    [TestMethod]
    public async Task StartExport_OnlyStartsOneActiveJobAndStoresCompletedCsv()
    {
        var ordersReady = new TaskCompletionSource<IList<Order>>();
        var orderRepository = Substitute.For<IOrderRepository>();
        orderRepository.GetAllAsync().Returns(ordersReady.Task);
        var sut = CreateSut(orderRepository);

        var first = sut.StartExport("admin");
        var second = sut.StartExport("other-admin");

        first.Started.Should().BeTrue();
        second.Started.Should().BeFalse();
        second.Job.Id.Should().Be(first.Job.Id);

        var order = new Order { Id = Guid.NewGuid(), User = new ApplicationUser { UserName = "jan" }, ProductNaam = "Cola", Amount = 1, Paid = 1.5m };
        ordersReady.SetResult(new List<Order> { order });
        var completed = await WaitForTerminalState(sut, first.Job.Id);

        completed!.Status.Should().Be(CsvExportJobStatus.Completed);
        completed.FileName.Should().StartWith("Orders_Export_");
        completed.CompletedAt.Should().NotBeNull();

        var csv = sut.GetCsv(first.Job.Id);
        csv.Should().NotBeNull();
        Encoding.UTF8.GetString(csv!.Bytes).Should().Contain($"{order.Id},\"jan\"");
        csv.FileName.Should().Be(completed.FileName);
        await orderRepository.Received(1).GetAllAsync();
    }

    [TestMethod]
    public async Task RunJob_WhenExportFails_MarksJobFailedAndDoesNotExposeCsv()
    {
        var orderRepository = Substitute.For<IOrderRepository>();
        orderRepository.GetAllAsync().Returns<Task<IList<Order>>>(_ => throw new InvalidOperationException("database down"));
        var sut = CreateSut(orderRepository);

        var started = sut.StartExport("admin");
        var failed = await WaitForTerminalState(sut, started.Job.Id);

        failed!.Status.Should().Be(CsvExportJobStatus.Failed);
        failed.ErrorMessage.Should().Be("database down");
        sut.GetCsv(started.Job.Id).Should().BeNull();
    }

    private static OrderCsvExportJob CreateSut(IOrderRepository orderRepository)
    {
        var provider = Substitute.For<IServiceProvider>();
        provider.GetService(typeof(IOrderRepository)).Returns(orderRepository);
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(provider);
        var factory = Substitute.For<IServiceScopeFactory>();
        factory.CreateScope().Returns(scope);

        return new OrderCsvExportJob(factory, Substitute.For<ILogger<OrderCsvExportJob>>());
    }

    private static async Task<CsvExportJobDto?> WaitForTerminalState(OrderCsvExportJob sut, Guid jobId)
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
