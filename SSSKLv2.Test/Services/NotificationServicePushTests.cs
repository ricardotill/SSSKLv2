using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SSSKLv2.Data;
using SSSKLv2.Dto;
using SSSKLv2.Services;
using SSSKLv2.Services.Interfaces;
using SSSKLv2.Test.Util;

namespace SSSKLv2.Test.Services;

[TestClass]
public class NotificationServiceTests : RepositoryTest
{
    private ApplicationDbContext _dbContext = null!;
    private IWebPushService _webPushService = null!;
    private NotificationService _sut = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        InitializeDatabase();
        _dbContext = new ApplicationDbContext(GetOptions());
        _webPushService = Substitute.For<IWebPushService>();
        _sut = new NotificationService(_dbContext, _webPushService);
    }

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
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetNotificationsAsync(TestUser.Id, false, 0, 10);

        // Assert
        result.Should().HaveCount(1);
        result.First().Title.Should().Be("T1");
    }

    [TestMethod]
    public async Task GetNotificationsAsync_UnreadOnly_ShouldFilterCorrectly()
    {
        // Arrange
        _dbContext.Notification.AddRange(
            new Notification { UserId = TestUser.Id, Title = "Read",   Message = "M1", IsRead = true,  CreatedOn = DateTime.UtcNow.AddMinutes(-5) },
            new Notification { UserId = TestUser.Id, Title = "Unread", Message = "M2", IsRead = false, CreatedOn = DateTime.UtcNow }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetNotificationsAsync(TestUser.Id, true, 0, 10);

        // Assert
        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Unread");
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
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetNotificationsAsync(TestUser.Id, false, 1, 2);

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
        await _dbContext.SaveChangesAsync();

        // Act
        var count = await _sut.GetUnreadCountAsync(TestUser.Id);

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
        await _dbContext.SaveChangesAsync();

        // Act
        await _sut.MarkAsReadAsync(n.Id, TestUser.Id);

        // Assert
        var updated = await _dbContext.Notification.FindAsync(n.Id);
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
        await _dbContext.SaveChangesAsync();

        // Act
        await _sut.MarkAsReadAsync(n.Id, TestUser.Id);

        // Assert
        var updated = await _dbContext.Notification.FindAsync(n.Id);
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
        await _dbContext.SaveChangesAsync();

        // Act
        await _sut.MarkAllAsReadAsync(TestUser.Id);

        // Assert
        var userNotifs = await _dbContext.Notification.Where(n => n.UserId == TestUser.Id).ToListAsync();
        userNotifs.Should().AllSatisfy(n => n.IsRead.Should().BeTrue());
        
        var otherNotif = await _dbContext.Notification.FirstAsync(n => n.UserId == "other");
        otherNotif.IsRead.Should().BeFalse();
    }

    // ── SubscribeAsync ─────────────────────────────────────────────────────────

    [TestMethod]
    public async Task SubscribeAsync_NewEndpoint_ShouldSaveSubscription()
    {
        // Arrange
        var dto = new PushSubscriptionDto { Endpoint = "https://push.example.com/1", P256dh = "key1", Auth = "auth1" };

        // Act
        await _sut.SubscribeAsync(TestUser.Id, dto);

        // Assert
        var sub = await _dbContext.PushSubscription
            .FirstOrDefaultAsync(s => s.UserId == TestUser.Id && s.Endpoint == dto.Endpoint);
        sub.Should().NotBeNull();
        sub!.P256dh.Should().Be("key1");
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
        await _dbContext.SaveChangesAsync();

        var dto = new PushSubscriptionDto { Endpoint = "https://push.example.com/1", P256dh = "new-key", Auth = "new-auth" };

        // Act
        await _sut.SubscribeAsync(TestUser.Id, dto);

        // Assert – still one record but with updated keys
        var subs = await _dbContext.PushSubscription
            .Where(s => s.UserId == TestUser.Id && s.Endpoint == dto.Endpoint)
            .ToListAsync();
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
        await _dbContext.SaveChangesAsync();

        // Act
        await _sut.UnsubscribeAsync(TestUser.Id, endpoint);

        // Assert
        var sub = await _dbContext.PushSubscription.FirstOrDefaultAsync(s => s.Endpoint == endpoint);
        sub.Should().BeNull();
    }

    // ── CreateNotificationAsync + Push ─────────────────────────────────────────

    [TestMethod]
    public async Task CreateNotificationAsync_WithSendPush_ShouldCallWebPushService()
    {
        // Act
        await _sut.CreateNotificationAsync(TestUser.Id, "Title", "Body", "/link", sendPush: true);

        // Assert
        await _webPushService.Received(1).SendNotificationAsync(TestUser.Id, "Title", "Body", "/link");
    }

    [TestMethod]
    public async Task CreateCustomNotificationAsync_FanOut_WithSendPush_ShouldCallForEachUser()
    {
        // Arrange – create a second user
        var user2 = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "user2", Name = "User", Surname = "Two", Email = "u2@test.com" };
        _dbContext.Users.Add(user2);
        await _dbContext.SaveChangesAsync();

        var dto = new CreateCustomNotificationDto
        {
            Title = "Broadcast",
            Message = "Hello everyone!",
            FanOut = true,
            SendPush = true
        };

        // Act
        await _sut.CreateCustomNotificationAsync(dto);

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
        await _sut.Invoking(s => s.CreateCustomNotificationAsync(dto))
            .Should().NotThrowAsync();
    }
}
