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
    // Bumped on every sign-in and sign-out. A refresh that started in an older session discards its result,
    // so a refresh finishing after sign-out can't silently sign the user back in.
    private int _generation;

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
        Interlocked.Increment(ref _generation);
        await store.SetAsync(tokens);
        _tokens = tokens;
        _loaded = true;
        Raise(TokensChanged, tokens, nameof(TokensChanged));
    }

    public async Task SignOutAsync()
    {
        Interlocked.Increment(ref _generation);
        await store.ClearAsync();
        _tokens = null;
        _loaded = true;
    }

    /// <summary>
    /// Called after a 401 with the access token that was rejected. When another caller already
    /// refreshed in the meantime, returns that newer token without refreshing again.
    /// Returns null (and raises <see cref="SessionExpired"/>) when the session can't be renewed, and
    /// null without side effects when the user signed out or in while the refresh was running.
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

            var generation = Volatile.Read(ref _generation);
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

            if (Volatile.Read(ref _generation) != generation)
            {
                // Signed out (or in again) while refreshing: the result belongs to a session that is gone.
                logger.LogInformation("Discarding a token refresh that finished after the session changed");
                return null;
            }

            if (renewed is null)
            {
                await SignOutAsync();
                Raise(SessionExpired, EventArgs.Empty, nameof(SessionExpired));
                return null;
            }

            await store.SetAsync(renewed);
            _tokens = renewed;
            Raise(TokensChanged, renewed, nameof(TokensChanged));
            return renewed.AccessToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <summary>Invokes each subscriber on its own, so a failing one can't break sign-in or a token refresh.</summary>
    private void Raise<T>(EventHandler<T>? handler, T args, string eventName)
    {
        if (handler is null)
        {
            return;
        }
        foreach (var subscriber in handler.GetInvocationList())
        {
            try
            {
                ((EventHandler<T>)subscriber)(this, args);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "A {Event} subscriber failed", eventName);
            }
        }
    }

    private void Raise(EventHandler? handler, EventArgs args, string eventName)
    {
        if (handler is null)
        {
            return;
        }
        foreach (var subscriber in handler.GetInvocationList())
        {
            try
            {
                ((EventHandler)subscriber)(this, args);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "A {Event} subscriber failed", eventName);
            }
        }
    }
}
