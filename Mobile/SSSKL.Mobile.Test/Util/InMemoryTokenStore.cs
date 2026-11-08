using SSSKL.Mobile.Core.Auth;

namespace SSSKL.Mobile.Test.Util;

internal sealed class InMemoryTokenStore(TokenSet? initial = null) : ITokenStore
{
    public TokenSet? Tokens { get; private set; } = initial;

    public Task<TokenSet?> GetAsync() => Task.FromResult(Tokens);

    public Task SetAsync(TokenSet tokens)
    {
        Tokens = tokens;
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        Tokens = null;
        return Task.CompletedTask;
    }
}

internal sealed class InMemorySettingsStore : SSSKL.Mobile.Core.Services.ISettingsStore
{
    public Dictionary<string, string> Values { get; } = [];
    public string? Get(string key) => Values.GetValueOrDefault(key);
    public void Set(string key, string value) => Values[key] = value;
    public void Remove(string key) => Values.Remove(key);
}
