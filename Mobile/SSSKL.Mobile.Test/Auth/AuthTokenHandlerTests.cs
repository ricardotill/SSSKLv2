using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SSSKL.Mobile.Core.Auth;
using SSSKL.Mobile.Test.Util;

namespace SSSKL.Mobile.Test.Auth;

[TestClass]
public class AuthTokenHandlerTests
{
    private static HttpClient CreateClient(AuthSession session, StubHttpHandler inner) =>
        new(new AuthTokenHandler(session) { InnerHandler = inner }) { BaseAddress = new Uri("https://api.test/") };

    [TestMethod]
    public async Task SendAsync_SignedIn_AddsBearerToken()
    {
        // Arrange
        var session = new AuthSession(new InMemoryTokenStore(new TokenSet("a1", "r1")), Substitute.For<ITokenRefresher>(), NullLogger<AuthSession>.Instance);
        var inner = new StubHttpHandler((_, _) => StubHttpHandler.Status(HttpStatusCode.OK));
        using var client = CreateClient(session, inner);

        // Act
        await client.GetAsync("api/v1/x", TestContext.CancellationToken);

        // Assert
        inner.Requests.Should().ContainSingle().Which.Request.Headers.Authorization!.ToString().Should().Be("Bearer a1");
    }

    [TestMethod]
    public async Task SendAsync_SignedOut_SendsWithoutAuthorization()
    {
        // Arrange
        var session = new AuthSession(new InMemoryTokenStore(), Substitute.For<ITokenRefresher>(), NullLogger<AuthSession>.Instance);
        var inner = new StubHttpHandler((_, _) => StubHttpHandler.Status(HttpStatusCode.OK));
        using var client = CreateClient(session, inner);

        // Act
        await client.GetAsync("api/v1/x", TestContext.CancellationToken);

        // Assert
        inner.Requests.Should().ContainSingle().Which.Request.Headers.Authorization.Should().BeNull();
    }

    [TestMethod]
    public async Task SendAsync_Unauthorized_RefreshesAndRetriesWithBody()
    {
        // Arrange
        var refresher = Substitute.For<ITokenRefresher>();
        refresher.RefreshAsync("r1", Arg.Any<CancellationToken>()).Returns(new TokenSet("a2", "r2"));
        var session = new AuthSession(new InMemoryTokenStore(new TokenSet("a1", "r1")), refresher, NullLogger<AuthSession>.Instance);
        var inner = new StubHttpHandler((req, _) => StubHttpHandler.Status(
            req.Headers.Authorization!.Parameter == "a1" ? HttpStatusCode.Unauthorized : HttpStatusCode.NoContent));
        using var client = CreateClient(session, inner);

        // Act
        var response = await client.PostAsync("api/v1/order", new StringContent("{\"amount\":1}"), TestContext.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        inner.Requests.Should().HaveCount(2);
        inner.Requests[1].Request.Headers.Authorization!.Parameter.Should().Be("a2");
        inner.Requests[1].Body.Should().Be("{\"amount\":1}");
    }

    [TestMethod]
    public async Task SendAsync_UnauthorizedAndRefreshFails_ReturnsOriginal401()
    {
        // Arrange
        var refresher = Substitute.For<ITokenRefresher>();
        refresher.RefreshAsync(default!, default).ReturnsForAnyArgs((TokenSet?)null);
        var session = new AuthSession(new InMemoryTokenStore(new TokenSet("a1", "r1")), refresher, NullLogger<AuthSession>.Instance);
        var inner = new StubHttpHandler((_, _) => StubHttpHandler.Status(HttpStatusCode.Unauthorized));
        using var client = CreateClient(session, inner);

        // Act
        var response = await client.GetAsync("api/v1/x", TestContext.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        inner.Requests.Should().ContainSingle();
    }

    public TestContext TestContext { get; set; } = null!;
}
