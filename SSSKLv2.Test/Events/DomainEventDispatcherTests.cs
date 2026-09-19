using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SSSKLv2.Data;
using SSSKLv2.Events;

namespace SSSKLv2.Test.Events;

[TestClass]
public class DomainEventDispatcherTests
{
    [TestMethod]
    public async Task DispatchAsync_InvokesEveryRegisteredHandler()
    {
        var first = Substitute.For<IDomainEventHandler<OrderPlacedEvent>>();
        var second = Substitute.For<IDomainEventHandler<OrderPlacedEvent>>();
        var services = new ServiceCollection()
            .AddSingleton(first)
            .AddSingleton(second)
            .BuildServiceProvider();
        var sut = new DomainEventDispatcher(services);
        var domainEvent = new OrderPlacedEvent(new Order { User = new ApplicationUser { Id = "user" } });

        await sut.DispatchAsync(domainEvent);

        await first.Received(1).HandleAsync(domainEvent);
        await second.Received(1).HandleAsync(domainEvent);
    }
}