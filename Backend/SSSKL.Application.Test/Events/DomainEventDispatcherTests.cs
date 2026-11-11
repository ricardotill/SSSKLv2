using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SSSKL.Application.Events;
using SSSKL.Domain.Entities;
using SSSKL.Domain.Events;

namespace SSSKL.Application.Test.Events;

[TestClass]
public class DomainEventDispatcherTests
{
    // MediatR collapses handlers that share an implementation type, so each fake needs its own class.
    private sealed class FirstHandler : INotificationHandler<OrderPlacedEvent>
    {
        public List<OrderPlacedEvent> Received { get; } = [];

        public Task Handle(OrderPlacedEvent notification, CancellationToken cancellationToken)
        {
            Received.Add(notification);
            return Task.CompletedTask;
        }
    }

    private sealed class SecondHandler : INotificationHandler<OrderPlacedEvent>
    {
        public List<OrderPlacedEvent> Received { get; } = [];

        public Task Handle(OrderPlacedEvent notification, CancellationToken cancellationToken)
        {
            Received.Add(notification);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingHandler : INotificationHandler<OrderPlacedEvent>
    {
        public Task Handle(OrderPlacedEvent notification, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("boom");
    }

    private static ServiceProvider BuildProvider(params INotificationHandler<OrderPlacedEvent>[] handlers)
    {
        var services = new ServiceCollection().AddLogging();
        services.AddMediatR(cfg =>
        {
            // Scan an assembly without handlers; the fakes below are registered as instances.
            cfg.RegisterServicesFromAssemblyContaining<IMediator>();
            cfg.NotificationPublisherType = typeof(ResilientNotificationPublisher);
        });
        foreach (var handler in handlers)
            services.AddSingleton(handler);
        return services.BuildServiceProvider();
    }

    private static OrderPlacedEvent CreateEvent() =>
        new(new Order { User = new ApplicationUser { Id = "user" } });

    [TestMethod]
    public async Task DispatchAsync_InvokesEveryRegisteredHandler()
    {
        var first = new FirstHandler();
        var second = new SecondHandler();
        await using var provider = BuildProvider(first, second);
        var sut = new DomainEventDispatcher(provider.GetRequiredService<IServiceScopeFactory>());
        var domainEvent = CreateEvent();

        await sut.DispatchAsync(domainEvent);

        first.Received.Should().ContainSingle().Which.Should().BeSameAs(domainEvent);
        second.Received.Should().ContainSingle().Which.Should().BeSameAs(domainEvent);
    }

    [TestMethod]
    public async Task DispatchAsync_WhenHandlerThrows_StillInvokesOtherHandlersAndDoesNotThrow()
    {
        var second = new SecondHandler();
        await using var provider = BuildProvider(new ThrowingHandler(), second);
        var sut = new DomainEventDispatcher(provider.GetRequiredService<IServiceScopeFactory>());
        var domainEvent = CreateEvent();

        var act = () => sut.DispatchAsync(domainEvent);

        await act.Should().NotThrowAsync();
        second.Received.Should().ContainSingle().Which.Should().BeSameAs(domainEvent);
    }
}
