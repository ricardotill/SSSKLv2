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
public class NotificationServicePushTests : RepositoryTest
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

    [TestMethod]
    public async Task SubscribeAsync_DifferentUsers_ShouldNotOverlap()
    {
        // Arrange
        var user2 = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "user2", Name = "User", Surname = "Two", Email = "u2@test.com" };
        _dbContext.Users.Add(user2);
        await _dbContext.SaveChangesAsync();

        var dto = new PushSubscriptionDto { Endpoint = "https://push.example.com/shared", P256dh = "k1", Auth = "a1" };

        // Act
        await _sut.SubscribeAsync(TestUser.Id, dto);
        await _sut.SubscribeAsync(user2.Id, dto);

        // Assert – two separate subscriptions for the same endpoint (different users)
        var subs = await _dbContext.PushSubscription
            .Where(s => s.Endpoint == "https://push.example.com/shared")
            .ToListAsync();
        subs.Should().HaveCount(2);
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

    [TestMethod]
    public async Task UnsubscribeAsync_NonExistentEndpoint_ShouldNotThrow()
    {
        // Act & Assert
        await _sut.Invoking(s => s.UnsubscribeAsync(TestUser.Id, "https://nonexistent.example.com"))
            .Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task UnsubscribeAsync_ShouldOnlyRemoveOwnSubscription()
    {
        // Arrange – two users with same endpoint
        var user2 = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "user2", Name = "User", Surname = "Two", Email = "u2@test.com" };
        _dbContext.Users.Add(user2);
        var endpoint = "https://push.example.com/shared";
        _dbContext.PushSubscription.AddRange(
            new PushSubscription { UserId = TestUser.Id, Endpoint = endpoint, P256dh = "k1", Auth = "a1", CreatedOn = DateTime.UtcNow },
            new PushSubscription { UserId = user2.Id,    Endpoint = endpoint, P256dh = "k2", Auth = "a2", CreatedOn = DateTime.UtcNow }
        );
        await _dbContext.SaveChangesAsync();

        // Act – only unsubscribe TestUser
        await _sut.UnsubscribeAsync(TestUser.Id, endpoint);

        // Assert – user2's subscription should remain
        var remaining = await _dbContext.PushSubscription.Where(s => s.Endpoint == endpoint).ToListAsync();
        remaining.Should().HaveCount(1);
        remaining.Single().UserId.Should().Be(user2.Id);
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
    public async Task CreateNotificationAsync_WithoutSendPush_ShouldNotCallWebPushService()
    {
        // Act
        await _sut.CreateNotificationAsync(TestUser.Id, "Title", "Body", sendPush: false);

        // Assert
        await _webPushService.DidNotReceive().SendNotificationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
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
    public async Task CreateCustomNotificationAsync_TargetedUsers_ShouldOnlySendToDtoUserIds()
    {
        // Arrange
        var user2 = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "user2", Name = "User", Surname = "Two", Email = "u2@test.com" };
        _dbContext.Users.Add(user2);
        await _dbContext.SaveChangesAsync();

        var dto = new CreateCustomNotificationDto
        {
            Title = "Targeted",
            Message = "Just for you",
            FanOut = false,
            SendPush = true,
            UserIds = new List<string> { TestUser.Id }
        };

        // Act
        await _sut.CreateCustomNotificationAsync(dto);

        // Assert – only TestUser receives the push, not user2
        await _webPushService.Received(1).SendNotificationAsync(TestUser.Id, "Targeted", "Just for you", Arg.Any<string>());
        await _webPushService.DidNotReceive().SendNotificationAsync(user2.Id, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }
}
