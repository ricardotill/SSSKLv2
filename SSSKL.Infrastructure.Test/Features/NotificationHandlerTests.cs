using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SSSKL.Application.Features.Notifications.Commands.MarkAllNotificationsRead;
using SSSKL.Application.Features.Notifications.Commands.MarkNotificationRead;
using SSSKL.Application.Features.Notifications.Commands.SendCustomNotification;
using SSSKL.Application.Features.Notifications.Commands.SubscribePush;
using SSSKL.Application.Features.Notifications.Commands.UnsubscribePush;
using SSSKL.Application.Features.Notifications.Queries.GetNotifications;
using SSSKL.Application.Features.Notifications.Queries.GetUnreadNotificationCount;
using SSSKL.Application.Shared;
using SSSKL.Contracts.Dto;
using SSSKL.Contracts.Services;
using SSSKL.Domain.Entities;
using SSSKL.Infrastructure.Persistence;
using SSSKL.Infrastructure.Persistence.Repositories;
using SSSKL.Infrastructure.Test.Util;

namespace SSSKL.Infrastructure.Test.Features;

[TestClass]
public class NotificationHandlerTests : RepositoryTest
{
    public TestContext TestContext { get; set; } = null!;

    private ApplicationDbContext _dbContext = null!;
    private IWebPushService _webPushService = null!;
    private NotificationRepository _notificationRepository = null!;
    private PushSubscriptionRepository _pushSubscriptionRepository = null!;
    private UserNotifier _userNotifier = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        InitializeDatabase();
        _dbContext = new ApplicationDbContext(GetOptions());
        _webPushService = Substitute.For<IWebPushService>();
        _notificationRepository = new NotificationRepository(_dbContext);
        _pushSubscriptionRepository = new PushSubscriptionRepository(_dbContext);
        _userNotifier = new UserNotifier(
            _notificationRepository,
            new ApplicationUserRepository(new MockDbContextFactory(GetOptions())),
            _webPushService);
    }

    // The tests below were written against NotificationService; these helpers send the same calls to the handlers.

    private Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(string userId, bool unreadOnly, int skip, int take) =>
        new GetNotificationsHandler(_notificationRepository).Handle(new GetNotificationsQuery(userId, unreadOnly, skip, take), CancellationToken.None);

    private Task<int> GetUnreadCountAsync(string userId) =>
        new GetUnreadNotificationCountHandler(_notificationRepository).Handle(new GetUnreadNotificationCountQuery(userId), CancellationToken.None);

    private Task MarkAsReadAsync(Guid id, string userId) =>
        new MarkNotificationReadHandler(_notificationRepository).Handle(new MarkNotificationReadCommand(id, userId), CancellationToken.None);

    private Task MarkAllAsReadAsync(string userId) =>
        new MarkAllNotificationsReadHandler(_notificationRepository).Handle(new MarkAllNotificationsReadCommand(userId), CancellationToken.None);

    private Task SubscribeAsync(string userId, PushSubscriptionDto dto) =>
        new SubscribePushHandler(_pushSubscriptionRepository).Handle(new SubscribePushCommand(userId, dto), CancellationToken.None);

    private Task UnsubscribeAsync(string userId, string endpoint) =>
        new UnsubscribePushHandler(_pushSubscriptionRepository).Handle(new UnsubscribePushCommand(userId, endpoint), CancellationToken.None);

    private Task CreateNotificationAsync(string userId, string title, string message, string? linkUri = null, bool sendPush = false) =>
        _userNotifier.NotifyAsync(userId, title, message, linkUri, sendPush);

    private Task CreateCustomNotificationAsync(CreateCustomNotificationDto dto) =>
        new SendCustomNotificationHandler(_userNotifier).Handle(new SendCustomNotificationCommand(dto), CancellationToken.None);

    [TestCleanup]
    public void TestCleanup()
    {
        CleanupDatabase();
        _dbContext.Dispose();
    }

    // ── GetNotificationsAsync ──────────────────────────────────────────────────

    [TestMethod]
    public async Task GetNotificationsAsync_ShouldReturnNotificationsForUser()
    {
        // Arrange
        var otherUser = new ApplicationUser { Id = "other", UserName = "other", Name = "Other", Surname = "User", Email = "other@test.com" };
        _dbContext.Users.Add(otherUser);
        
        _dbContext.Notification.AddRange(
            new Notification { UserId = TestUser.Id, Title = "T1", Message = "M1", CreatedOn = DateTime.UtcNow.AddMinutes(-5) },
            new Notification { UserId = "other",    Title = "T2", Message = "M2", CreatedOn = DateTime.UtcNow }
        );
        await _dbContext.SaveChangesAsync(TestContext.CancellationToken);

        // Act
        var result = await GetNotificationsAsync(TestUser.Id, false, 0, 10);

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("T1");
    }

    [TestMethod]
    public async Task GetNotificationsAsync_UnreadOnly_ShouldFilterCorrectly()
    {
        // Arrange
        _dbContext.Notification.AddRange(
            new Notification { UserId = TestUser.Id, Title = "Read",   Message = "M1", IsRead = true,  CreatedOn = DateTime.UtcNow.AddMinutes(-5) },
            new Notification { UserId = TestUser.Id, Title = "Unread", Message = "M2", IsRead = false, CreatedOn = DateTime.UtcNow }
        );
        await _dbContext.SaveChangesAsync(TestContext.CancellationToken);

        // Act
        var result = await GetNotificationsAsync(TestUser.Id, true, 0, 10);

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Unread");
    }

    [TestMethod]
    public async Task GetNotificationsAsync_Pagination_ShouldWork()
    {
        // Arrange
        for (int i = 1; i <= 5; i++)
        {
            _dbContext.Notification.Add(new Notification 
            { 
                UserId = TestUser.Id, Title = "T" + i, Message = "M", CreatedOn = DateTime.UtcNow.AddMinutes(i) 
            });
        }
        await _dbContext.SaveChangesAsync(TestContext.CancellationToken);

        // Act
        var result = await GetNotificationsAsync(TestUser.Id, false, 1, 2);

        // Assert
        var list = result.ToList();
        list.Should().HaveCount(2);
        // Ordered by descending CreatedOn: T5, T4, T3, T2, T1
        // Skip 1, Take 2 -> T4, T3
        list[0].Title.Should().Be("T4");
        list[1].Title.Should().Be("T3");
    }

    // ── GetUnreadCountAsync ────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetUnreadCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var otherUser = new ApplicationUser { Id = "other", UserName = "other", Name = "Other", Surname = "User", Email = "other@test.com" };
        _dbContext.Users.Add(otherUser);
        
        _dbContext.Notification.AddRange(
            new Notification { UserId = TestUser.Id, Title = "R", Message = "M", IsRead = true },
            new Notification { UserId = TestUser.Id, Title = "U1", Message = "M", IsRead = false },
            new Notification { UserId = TestUser.Id, Title = "U2", Message = "M", IsRead = false },
            new Notification { UserId = "other",    Title = "U3", Message = "M", IsRead = false }
        );
        await _dbContext.SaveChangesAsync(TestContext.CancellationToken);

        // Act
        var count = await GetUnreadCountAsync(TestUser.Id);

        // Assert
        count.Should().Be(2);
    }

    // ── MarkAsReadAsync ────────────────────────────────────────────────────────

    [TestMethod]
    public async Task MarkAsReadAsync_ExistingUnread_ShouldUpdate()
    {
        // Arrange
        var n = new Notification { UserId = TestUser.Id, Title = "T", Message = "M", IsRead = false };
        _dbContext.Notification.Add(n);
        await _dbContext.SaveChangesAsync(TestContext.CancellationToken);

        // Act
        await MarkAsReadAsync(n.Id, TestUser.Id);

        // Assert
        var updated = await _dbContext.Notification.FindAsync([n.Id], TestContext.CancellationToken);
        updated!.IsRead.Should().BeTrue();
    }

    [TestMethod]
    public async Task MarkAsReadAsync_WrongUser_ShouldNotUpdate()
    {
        // Arrange
        var otherUser = new ApplicationUser { Id = "other", UserName = "other", Name = "Other", Surname = "User", Email = "other@test.com" };
        _dbContext.Users.Add(otherUser);
        
        var n = new Notification { UserId = "other", Title = "T", Message = "M", IsRead = false };
        _dbContext.Notification.Add(n);
        await _dbContext.SaveChangesAsync(TestContext.CancellationToken);

        // Act
        await MarkAsReadAsync(n.Id, TestUser.Id);

        // Assert
        var updated = await _dbContext.Notification.FindAsync([n.Id], TestContext.CancellationToken);
        updated!.IsRead.Should().BeFalse();
    }

    // ── MarkAllAsReadAsync ─────────────────────────────────────────────────────

    [TestMethod]
    public async Task MarkAllAsReadAsync_ShouldUpdateAllUnreadForUser()
    {
        // Arrange
        var otherUser = new ApplicationUser { Id = "other", UserName = "other", Name = "Other", Surname = "User", Email = "other@test.com" };
        _dbContext.Users.Add(otherUser);
        
        _dbContext.Notification.AddRange(
            new Notification { UserId = TestUser.Id, Title = "T1", IsRead = false },
            new Notification { UserId = TestUser.Id, Title = "T2", IsRead = false },
            new Notification { UserId = "other",    Title = "T3", IsRead = false }
        );
        await _dbContext.SaveChangesAsync(TestContext.CancellationToken);

        // Act
        await MarkAllAsReadAsync(TestUser.Id);

        // ExecuteUpdateAsync bypasses the change tracker, so drop stale tracked entities before re-querying
        _dbContext.ChangeTracker.Clear();

        // Assert
        var userNotifs = await _dbContext.Notification.Where(n => n.UserId == TestUser.Id).ToListAsync(TestContext.CancellationToken);
        userNotifs.Should().AllSatisfy(n => n.IsRead.Should().BeTrue());
        
        var otherNotif = await _dbContext.Notification.FirstAsync(n => n.UserId == "other", TestContext.CancellationToken);
        otherNotif.IsRead.Should().BeFalse();
    }

    // ── SubscribeAsync ─────────────────────────────────────────────────────────

    [TestMethod]
    public async Task SubscribeAsync_NewEndpoint_ShouldSaveSubscription()
    {
        // Arrange
        var dto = new PushSubscriptionDto { Endpoint = "https://push.example.com/1", P256dh = "key1", Auth = "auth1" };

        // Act
        await SubscribeAsync(TestUser.Id, dto);

        // Assert
        var sub = await _dbContext.PushSubscription
            .FirstOrDefaultAsync(s => s.UserId == TestUser.Id && s.Endpoint == dto.Endpoint, TestContext.CancellationToken);
        sub.Should().NotBeNull();
        sub.P256dh.Should().Be("key1");
        sub.Auth.Should().Be("auth1");
    }

    [TestMethod]
    public async Task SubscribeAsync_ExistingEndpoint_ShouldUpdateKeys()
    {
        // Arrange – pre-existing subscription
        _dbContext.PushSubscription.Add(new PushSubscription
        {
            UserId = TestUser.Id, Endpoint = "https://push.example.com/1",
            P256dh = "old-key", Auth = "old-auth", CreatedOn = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync(TestContext.CancellationToken);

        var dto = new PushSubscriptionDto { Endpoint = "https://push.example.com/1", P256dh = "new-key", Auth = "new-auth" };

        // Act
        await SubscribeAsync(TestUser.Id, dto);

        // Assert – still one record but with updated keys
        var subs = await _dbContext.PushSubscription
            .Where(s => s.UserId == TestUser.Id && s.Endpoint == dto.Endpoint)
            .ToListAsync(TestContext.CancellationToken);
        subs.Should().HaveCount(1);
        subs.Single().P256dh.Should().Be("new-key");
        subs.Single().Auth.Should().Be("new-auth");
    }

    // ── UnsubscribeAsync ───────────────────────────────────────────────────────

    [TestMethod]
    public async Task UnsubscribeAsync_ExistingSubscription_ShouldRemoveIt()
    {
        // Arrange
        var endpoint = "https://push.example.com/to-remove";
        _dbContext.PushSubscription.Add(new PushSubscription
        {
            UserId = TestUser.Id, Endpoint = endpoint,
            P256dh = "key", Auth = "auth", CreatedOn = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync(TestContext.CancellationToken);

        // Act
        await UnsubscribeAsync(TestUser.Id, endpoint);

        // Assert
        var sub = await _dbContext.PushSubscription.FirstOrDefaultAsync(s => s.Endpoint == endpoint, TestContext.CancellationToken);
        sub.Should().BeNull();
    }

    // ── CreateNotificationAsync + Push ─────────────────────────────────────────

    [TestMethod]
    public async Task CreateNotificationAsync_WithSendPush_ShouldCallWebPushService()
    {
        // Act
        await CreateNotificationAsync(TestUser.Id, "Title", "Body", "/link", sendPush: true);

        // Assert
        await _webPushService.Received(1).SendNotificationAsync(TestUser.Id, "Title", "Body", "/link");
    }

    [TestMethod]
    public async Task CreateCustomNotificationAsync_FanOut_WithSendPush_ShouldCallForEachUser()
    {
        // Arrange – create a second user
        var user2 = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "user2", Name = "User", Surname = "Two", Email = "u2@test.com" };
        _dbContext.Users.Add(user2);
        await _dbContext.SaveChangesAsync(TestContext.CancellationToken);

        var dto = new CreateCustomNotificationDto
        {
            Title = "Broadcast",
            Message = "Hello everyone!",
            FanOut = true,
            SendPush = true
        };

        // Act
        await CreateCustomNotificationAsync(dto);

        // Assert – called once for each user in the database
        await _webPushService.Received(2).SendNotificationAsync(Arg.Any<string>(), "Broadcast", "Hello everyone!", Arg.Any<string>());
    }

    [TestMethod]
    public async Task CreateCustomNotificationAsync_TargetedUsers_EmptyList_ShouldNotThrow()
    {
        // Arrange
        var dto = new CreateCustomNotificationDto
        {
            Title = "T",
            Message = "M",
            FanOut = false,
            UserIds = null // Should handle null as empty list
        };

        // Act & Assert
        await this.Invoking(s => s.CreateCustomNotificationAsync(dto))
            .Should().NotThrowAsync();
    }
}
