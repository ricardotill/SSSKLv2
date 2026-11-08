using Microsoft.Extensions.Logging;

namespace SSSKL.Mobile.Core.Auth;

/// <summary>
/// Owns the signed-in state: the current tokens, refreshing them (one refresh at a time, like the
/// SPA's auth interceptor) and signing out. Singleton.
/// </summary>
public sealed class AuthSession(ITokenStore store, ITokenRefresher refresher, ILogger<AuthSession> logger)
{
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private TokenSet? _tokens;
    private bool _loaded;

    /// <summary>Raised after sign-in and after every refresh, so WebViews can re-inject the tokens.</summary>
    public event EventHandler<TokenSet>? TokensChanged;

    /// <summary>Raised when the session ended without the user signing out (refresh token rejected).</summary>
    public event EventHandler? SessionExpired;

    public async Task<TokenSet?> GetTokensAsync()
    {
        if (!_loaded)
        {
            _tokens = await store.GetAsync();
            _loaded = true;
        }
        return _tokens;
    }

    public async Task<bool> IsSignedInAsync() => await GetTokensAsync() is not null;

    public async Task SignInAsync(TokenSet tokens)
    {
        await store.SetAsync(tokens);
        _tokens = tokens;
        _loaded = true;
        TokensChanged?.Invoke(this, tokens);
    }

    public async Task SignOutAsync()
    {
        await store.ClearAsync();
        _tokens = null;
        _loaded = true;
    }

    /// <summary>
    /// Called after a 401 with the access token that was rejected. When another caller already
    /// refreshed in the meantime, returns that newer token without refreshing again.
    /// Returns null (and raises <see cref="SessionExpired"/>) when the session can't be renewed.
    /// </summary>
    public async Task<string?> RefreshAsync(string? rejectedAccessToken, CancellationToken cancellationToken)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            var current = await GetTokensAsync();
            if (current is null)
            {
                return null;
            }
            if (current.AccessToken != rejectedAccessToken)
            {
                return current.AccessToken;
            }

            TokenSet? renewed;
            try
            {
                renewed = await refresher.RefreshAsync(current.RefreshToken, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                // Offline or server error: keep the session, the caller just gets its 401.
                logger.LogWarning(ex, "Token refresh failed");
                return null;
            }

            if (renewed is null)
            {
                await SignOutAsync();
                SessionExpired?.Invoke(this, EventArgs.Empty);
                return null;
            }

            await store.SetAsync(renewed);
            _tokens = renewed;
            TokensChanged?.Invoke(this, renewed);
            return renewed.AccessToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}
