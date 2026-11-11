using System.Text.Json;
using SSSKL.Mobile.Core;
using SSSKL.Mobile.Core.Auth;

namespace SSSKL.Mobile.Services;

/// <summary>Keeps the token pair in the Keychain (iOS) / Keystore-backed storage (Android).</summary>
public sealed class SecureTokenStore : ITokenStore
{
    private const string Key = "ssskl_tokens";

    public async Task<TokenSet?> GetAsync()
    {
        try
        {
            var json = await SecureStorage.Default.GetAsync(Key);
            return json is null ? null : JsonSerializer.Deserialize(json, ApiJsonContext.Default.TokenSet);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or System.Security.SecurityException)
        {
            // Unreadable (e.g. restored backup on another device): start signed out.
            SecureStorage.Default.Remove(Key);
            return null;
        }
    }

    public Task SetAsync(TokenSet tokens) => SecureStorage.Default.SetAsync(Key, JsonSerializer.Serialize(tokens, ApiJsonContext.Default.TokenSet));

    public Task ClearAsync()
    {
        SecureStorage.Default.Remove(Key);
        return Task.CompletedTask;
    }
}
