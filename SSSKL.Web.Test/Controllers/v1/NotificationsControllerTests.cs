using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using SSSKL.Contracts.Dto;
using MediatR;
using SSSKL.Application.Features.Notifications.Commands.MarkAllNotificationsRead;
using SSSKL.Application.Features.Notifications.Commands.MarkNotificationRead;
using SSSKL.Application.Features.Notifications.Commands.SendCustomNotification;
using SSSKL.Application.Features.Notifications.Commands.SubscribePush;
using SSSKL.Application.Features.Notifications.Commands.UnsubscribePush;
using SSSKL.Application.Features.Notifications.Queries.GetNotifications;
using SSSKL.Application.Features.Notifications.Queries.GetUnreadNotificationCount;
using SSSKL.Web.Controllers.v1;

namespace SSSKL.Web.Test.Controllers.v1;

[TestClass]
public class NotificationsControllerTests
{
    private ISender _sender = null!;
    private IConfiguration _configuration = null!;
    private NotificationsController _sut = null!;

    private const string TestUserId = "test-user-id";
    private const string TestVapidPublicKey = "BDummyVapidPublicKey1234567890ABCDEF";

    [TestInitialize]
    public void TestInitialize()
    {
        _sender = Substitute.For<ISender>();
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["VapidDetails:PublicKey"] = TestVapidPublicKey
            })
            .Build();

        _sut = new NotificationsController(_sender, _configuration);

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
        var contentResult = result.Result.Should().BeOfType<ContentResult>().Subject;
        contentResult.Content.Should().Be(TestVapidPublicKey);
        contentResult.ContentType.Should().Be("text/plain");
    }

    // ── GetNotifications ───────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetNotifications_ShouldCallService()
    {
        // Arrange
        var unreadOnly = true;
        var skip = 10;
        var take = 5;
        _sender.Send(new GetNotificationsQuery(TestUserId, unreadOnly, skip, take), Arg.Any<CancellationToken>()).Returns(new List<NotificationDto>());

        // Act
        var result = await _sut.GetNotifications(unreadOnly, skip, take);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        await _sender.Received(1).Send(new GetNotificationsQuery(TestUserId, unreadOnly, skip, take), Arg.Any<CancellationToken>());
    }

    // ── GetUnreadCount ─────────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetUnreadCount_ShouldCallService()
    {
        // Arrange
        _sender.Send(new GetUnreadNotificationCountQuery(TestUserId), Arg.Any<CancellationToken>()).Returns(42);

        // Act
        var result = await _sut.GetUnreadCount();

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(42);
        await _sender.Received(1).Send(new GetUnreadNotificationCountQuery(TestUserId), Arg.Any<CancellationToken>());
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
        await _sender.Received(1).Send(new MarkNotificationReadCommand(id, TestUserId), Arg.Any<CancellationToken>());
    }

    // ── MarkAllAsRead ──────────────────────────────────────────────────────────

    [TestMethod]
    public async Task MarkAllAsRead_ShouldCallService()
    {
        // Act
        var result = await _sut.MarkAllAsRead();

        // Assert
        result.Should().BeOfType<OkResult>();
        await _sender.Received(1).Send(new MarkAllNotificationsReadCommand(TestUserId), Arg.Any<CancellationToken>());
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
        await _sender.Received(1).Send(new SendCustomNotificationCommand(dto), Arg.Any<CancellationToken>());
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
        await _sender.Received(1).Send(new SubscribePushCommand(TestUserId, dto), Arg.Any<CancellationToken>());
    }

    // ── Unsubscribe ────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Unsubscribe_AuthenticatedUser_ShouldCallUnsubscribeAsync()
    {
        // Arrange
        var endpoint = "https://push.example.com/endpoint-to-remove";
        var dto = new UnsubscribeDto { Endpoint = endpoint };

        // Act
        var result = await _sut.Unsubscribe(dto);

        // Assert
        result.Should().BeOfType<OkResult>();
        await _sender.Received(1).Send(new UnsubscribePushCommand(TestUserId, endpoint), Arg.Any<CancellationToken>());
    }
}
