using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SSSKLv2.Controllers.v1;
using SSSKLv2.Dto;
using SSSKLv2.Services.Interfaces;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;

namespace SSSKLv2.Test.Controllers.v1;

[TestClass]
public class NotificationsControllerTests
{
    private INotificationService _notificationService = null!;
    private IConfiguration _configuration = null!;
    private NotificationsController _sut = null!;

    private const string TestUserId = "test-user-id";
    private const string TestVapidPublicKey = "BDummyVapidPublicKey1234567890ABCDEF";

    [TestInitialize]
    public void TestInitialize()
    {
        _notificationService = Substitute.For<INotificationService>();
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["VapidDetails:PublicKey"] = TestVapidPublicKey
            })
            .Build();

        _sut = new NotificationsController(_notificationService, _configuration);

        // Set up an authenticated user context
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, TestUserId) };
        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    // ── GetVapidPublicKey ──────────────────────────────────────────────────────

    [TestMethod]
    public void GetVapidPublicKey_ShouldReturnConfiguredKey()
    {
        // Act
        var result = _sut.GetVapidPublicKey();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)result.Result!;
        ok.Value.Should().Be(TestVapidPublicKey);
    }

    // ── GetNotifications ───────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetNotifications_ShouldCallService()
    {
        // Arrange
        var unreadOnly = true;
        var skip = 10;
        var take = 5;
        _notificationService.GetNotificationsAsync(TestUserId, unreadOnly, skip, take).Returns(new List<NotificationDto>());

        // Act
        var result = await _sut.GetNotifications(unreadOnly, skip, take);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        await _notificationService.Received(1).GetNotificationsAsync(TestUserId, unreadOnly, skip, take);
    }

    // ── GetUnreadCount ─────────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetUnreadCount_ShouldCallService()
    {
        // Arrange
        _notificationService.GetUnreadCountAsync(TestUserId).Returns(42);

        // Act
        var result = await _sut.GetUnreadCount();

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(42);
        await _notificationService.Received(1).GetUnreadCountAsync(TestUserId);
    }

    // ── MarkAsRead ─────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task MarkAsRead_ShouldCallService()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var result = await _sut.MarkAsRead(id);

        // Assert
        result.Should().BeOfType<OkResult>();
        await _notificationService.Received(1).MarkAsReadAsync(id, TestUserId);
    }

    // ── MarkAllAsRead ──────────────────────────────────────────────────────────

    [TestMethod]
    public async Task MarkAllAsRead_ShouldCallService()
    {
        // Act
        var result = await _sut.MarkAllAsRead();

        // Assert
        result.Should().BeOfType<OkResult>();
        await _notificationService.Received(1).MarkAllAsReadAsync(TestUserId);
    }

    // ── SendCustomNotification ─────────────────────────────────────────────────

    [TestMethod]
    public async Task SendCustomNotification_Admin_ShouldCallService()
    {
        // Arrange
        var dto = new CreateCustomNotificationDto { Title = "Title", Message = "Msg" };

        // Act
        var result = await _sut.SendCustomNotification(dto);

        // Assert
        result.Should().BeOfType<OkResult>();
        await _notificationService.Received(1).CreateCustomNotificationAsync(dto);
    }

    // ── Subscribe ──────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Subscribe_AuthenticatedUser_ShouldCallSubscribeAsync()
    {
        // Arrange
        var dto = new PushSubscriptionDto { Endpoint = "https://push.example.com", P256dh = "key", Auth = "auth" };

        // Act
        var result = await _sut.Subscribe(dto);

        // Assert
        result.Should().BeOfType<OkResult>();
        await _notificationService.Received(1).SubscribeAsync(TestUserId, dto);
    }

    // ── Unsubscribe ────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Unsubscribe_AuthenticatedUser_ShouldCallUnsubscribeAsync()
    {
        // Arrange
        var endpoint = "https://push.example.com/endpoint-to-remove";

        // Act
        var result = await _sut.Unsubscribe(endpoint);

        // Assert
        result.Should().BeOfType<OkResult>();
        await _notificationService.Received(1).UnsubscribeAsync(TestUserId, endpoint);
    }
}
