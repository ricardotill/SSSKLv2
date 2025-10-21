using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using SSSKLv2.Services;
using SSSKLv2.Services.Hubs;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Test.Services
{
    [TestClass]
    public class PurchaseNotifierTests
    {
        [TestMethod]
        public async Task NotifyUserPurchaseAsync_Calls_ClientsAll_SendCoreAsync_With_Dto()
        {
            // Arrange
            var hubContext = Substitute.For<IHubContext<LiveMetricsHub>>();
            var hubClients = Substitute.For<IHubClients>();
            var clientProxy = Substitute.For<IClientProxy>();

            hubContext.Clients.Returns(hubClients);
            hubClients.All.Returns(clientProxy);

            var notifier = new PurchaseNotifier(hubContext);

            var dto = new UserPurchaseEvent("user1", "product1", 2, DateTime.UtcNow);

            // Act
            await notifier.NotifyUserPurchaseAsync(dto);

            // Assert: SendCoreAsync will be invoked by the SendAsync extension
            clientProxy.Received(1).SendCoreAsync(
                Arg.Is<string>(s => s == "UserPurchase"),
                Arg.Is<object?[]>(arr => arr != null && arr.Length == 1 && arr[0] == dto),
                Arg.Any<CancellationToken>());
        }

        [TestMethod]
        public async Task NotifyUserPurchaseAsync_When_SendCoreAsync_Throws_Propagates_Exception()
        {
            // Arrange
            var hubContext = Substitute.For<IHubContext<LiveMetricsHub>>();
            var hubClients = Substitute.For<IHubClients>();
            var clientProxy = Substitute.For<IClientProxy>();

            hubContext.Clients.Returns(hubClients);
            hubClients.All.Returns(clientProxy);

            // Make SendCoreAsync throw when called
            clientProxy.When(x => x.SendCoreAsync(Arg.Any<string>(), Arg.Any<object?[]>(), Arg.Any<CancellationToken>()))
                       .Do(ci => throw new InvalidOperationException("boom"));

            var notifier = new PurchaseNotifier(hubContext);
            var dto = new UserPurchaseEvent("user", "product", 1, DateTime.UtcNow);

            // Act
            Func<Task> act = () => notifier.NotifyUserPurchaseAsync(dto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
        }
        
        [TestMethod]
        public async Task NotifyAchievementAsync_Calls_ClientsAll_SendCoreAsync_With_Dto()
        {
            // Arrange
            var hubContext = Substitute.For<IHubContext<LiveMetricsHub>>();
            var hubClients = Substitute.For<IHubClients>();
            var clientProxy = Substitute.For<IClientProxy>();

            hubContext.Clients.Returns(hubClients);
            hubClients.All.Returns(clientProxy);

            var notifier = new PurchaseNotifier(hubContext);

            var dto = new AchievementEvent("achievement1", "username1", "https://testurl.nl/image.png");

            // Act
            await notifier.NotifyAchievementAsync(dto);

            // Assert: SendCoreAsync will be invoked by the SendAsync extension
            clientProxy.Received(1).SendCoreAsync(
                Arg.Is<string>(s => s == "Achievement"),
                Arg.Is<object?[]>(arr => arr != null && arr.Length == 1 && arr[0] == dto),
                Arg.Any<CancellationToken>());
        }

        [TestMethod]
        public async Task NotifyAchievementAsync_When_SendCoreAsync_Throws_Propagates_Exception()
        {
            // Arrange
            var hubContext = Substitute.For<IHubContext<LiveMetricsHub>>();
            var hubClients = Substitute.For<IHubClients>();
            var clientProxy = Substitute.For<IClientProxy>();

            hubContext.Clients.Returns(hubClients);
            hubClients.All.Returns(clientProxy);

            // Make SendCoreAsync throw when called
            clientProxy.When(x => x.SendCoreAsync(Arg.Any<string>(), Arg.Any<object?[]>(), Arg.Any<CancellationToken>()))
                       .Do(ci => throw new InvalidOperationException("boom"));

            var notifier = new PurchaseNotifier(hubContext);
            var dto = new AchievementEvent("achievement1", "username1", "https://testurl.nl/image.png");

            // Act
            Func<Task> act = () => notifier.NotifyAchievementAsync(dto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
        }
    }
}
