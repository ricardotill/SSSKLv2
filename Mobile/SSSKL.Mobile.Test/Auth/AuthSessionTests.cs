using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SSSKL.Mobile.Core.Auth;
using SSSKL.Mobile.Test.Util;

namespace SSSKL.Mobile.Test.Auth;

[TestClass]
public class AuthSessionTests
{
    private static readonly TokenSet Old = new("old-access", "old-refresh");
    private static readonly TokenSet New = new("new-access", "new-refresh");

    [TestMethod]
    public async Task RefreshAsync_ConcurrentCallers_RefreshesOnce()
    {
        // Arrange
        var store = new InMemoryTokenStore(Old);
        var refresher = Substitute.For<ITokenRefresher>();
        var gate = new TaskCompletionSource<TokenSet?>();
        refresher.RefreshAsync("old-refresh", Arg.Any<CancellationToken>()).Returns(gate.Task);
        var sut = new AuthSession(store, refresher, NullLogger<AuthSession>.Instance);

        // Act
        var calls = Enumerable.Range(0, 5).Select(_ => sut.RefreshAsync("old-access", CancellationToken.None)).ToList();
        gate.SetResult(New);
        var results = await Task.WhenAll(calls);

        // Assert
        results.Should().AllBe("new-access");
        await refresher.Received(1).RefreshAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        store.Tokens.Should().Be(New);
    }

    [TestMethod]
    public async Task RefreshAsync_TokenAlreadyRefreshed_ReturnsCurrentTokenWithoutRefreshing()
    {
        // Arrange
        var refresher = Substitute.For<ITokenRefresher>();
        var sut = new AuthSession(new InMemoryTokenStore(New), refresher, NullLogger<AuthSession>.Instance);

        // Act
        var result = await sut.RefreshAsync("old-access", CancellationToken.None);

        // Assert
        result.Should().Be("new-access");
        await refresher.DidNotReceiveWithAnyArgs().RefreshAsync(default!, default);
    }

    [TestMethod]
    public async Task RefreshAsync_RefreshRejected_SignsOutAndRaisesSessionExpired()
    {
        // Arrange
        var store = new InMemoryTokenStore(Old);
        var refresher = Substitute.For<ITokenRefresher>();
        refresher.RefreshAsync(default!, default).ReturnsForAnyArgs((TokenSet?)null);
        var sut = new AuthSession(store, refresher, NullLogger<AuthSession>.Instance);
        var expired = false;
        sut.SessionExpired += (_, _) => expired = true;

        // Act
        var result = await sut.RefreshAsync("old-access", CancellationToken.None);

        // Assert
        result.Should().BeNull();
        expired.Should().BeTrue();
        store.Tokens.Should().BeNull();
        (await sut.IsSignedInAsync()).Should().BeFalse();
    }

    [TestMethod]
    public async Task RefreshAsync_NetworkError_KeepsSession()
    {
        // Arrange
        var store = new InMemoryTokenStore(Old);
        var refresher = Substitute.For<ITokenRefresher>();
        refresher.RefreshAsync(default!, default).ThrowsAsyncForAnyArgs(new HttpRequestException("offline"));
        var sut = new AuthSession(store, refresher, NullLogger<AuthSession>.Instance);
        var expired = false;
        sut.SessionExpired += (_, _) => expired = true;

        // Act
        var result = await sut.RefreshAsync("old-access", CancellationToken.None);

        // Assert
        result.Should().BeNull();
        expired.Should().BeFalse();
        store.Tokens.Should().Be(Old);
    }

    [TestMethod]
    public async Task SignInAsync_StoresTokensAndRaisesTokensChanged()
    {
        // Arrange
        var store = new InMemoryTokenStore();
        var sut = new AuthSession(store, Substitute.For<ITokenRefresher>(), NullLogger<AuthSession>.Instance);
        TokenSet? raised = null;
        sut.TokensChanged += (_, t) => raised = t;

        // Act
        await sut.SignInAsync(New);

        // Assert
        store.Tokens.Should().Be(New);
        raised.Should().Be(New);
        (await sut.GetTokensAsync()).Should().Be(New);
    }
}
