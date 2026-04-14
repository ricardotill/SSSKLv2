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

    [TestMethod]
    public void GetVapidPublicKey_EnvironmentVariableOverride_ShouldReturnEnvKey()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["VAPID_PUBLIC_KEY"]        = "env-public-key",
                ["VapidDetails:PublicKey"]  = "appsettings-public-key"
            })
            .Build();

        var controller = new NotificationsController(_notificationService, config);
        controller.ControllerContext = _sut.ControllerContext;

        // Act
        var result = controller.GetVapidPublicKey();

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be("env-public-key");
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

    [TestMethod]
    public async Task Subscribe_UnauthenticatedUser_ShouldReturnUnauthorized()
    {
        // Arrange – no identity
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };
        var dto = new PushSubscriptionDto { Endpoint = "https://push.example.com", P256dh = "key", Auth = "auth" };

        // Act
        var result = await _sut.Subscribe(dto);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
        await _notificationService.DidNotReceive().SubscribeAsync(Arg.Any<string>(), Arg.Any<PushSubscriptionDto>());
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

    [TestMethod]
    public async Task Unsubscribe_UnauthenticatedUser_ShouldReturnUnauthorized()
    {
        // Arrange – no identity
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        // Act
        var result = await _sut.Unsubscribe("https://some.endpoint.com");

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
        await _notificationService.DidNotReceive().UnsubscribeAsync(Arg.Any<string>(), Arg.Any<string>());
    }
}
