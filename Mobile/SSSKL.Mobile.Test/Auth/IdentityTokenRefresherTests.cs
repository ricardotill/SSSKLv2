using System.Net;
using FluentAssertions;
using SSSKL.Mobile.Core.Auth;
using SSSKL.Mobile.Test.Util;

namespace SSSKL.Mobile.Test.Auth;

[TestClass]
public class IdentityTokenRefresherTests
{
    [TestMethod]
    public async Task RefreshAsync_Success_ReturnsNewTokens()
    {
        // Arrange
        var inner = new StubHttpHandler((_, _) => StubHttpHandler.Json(
            """{"tokenType":"Bearer","accessToken":"a2","expiresIn":3600,"refreshToken":"r2"}"""));
        var sut = new IdentityTokenRefresher(new HttpClient(inner) { BaseAddress = new Uri("https://api.test/") });

        // Act
        var result = await sut.RefreshAsync("r1", TestContext.CancellationToken);

        // Assert
        result.Should().Be(new TokenSet("a2", "r2"));
        inner.Requests.Single().Request.RequestUri!.AbsolutePath.Should().Be("/api/v1/identity/refresh");
        inner.Requests.Single().Body.Should().Be("""{"refreshToken":"r1"}""");
    }

    [TestMethod]
    [DataRow(HttpStatusCode.Unauthorized)]
    [DataRow(HttpStatusCode.BadRequest)]
    public async Task RefreshAsync_Rejected_ReturnsNull(HttpStatusCode status)
    {
        // Arrange
        var inner = new StubHttpHandler((_, _) => StubHttpHandler.Status(status));
        var sut = new IdentityTokenRefresher(new HttpClient(inner) { BaseAddress = new Uri("https://api.test/") });

        // Act
        var result = await sut.RefreshAsync("r1", TestContext.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task RefreshAsync_ServerError_Throws()
    {
        // Arrange
        var inner = new StubHttpHandler((_, _) => StubHttpHandler.Status(HttpStatusCode.InternalServerError));
        var sut = new IdentityTokenRefresher(new HttpClient(inner) { BaseAddress = new Uri("https://api.test/") });

        // Act
        var act = () => sut.RefreshAsync("r1", TestContext.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    public TestContext TestContext { get; set; } = null!;
}
