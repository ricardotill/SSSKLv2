using SSSKL.Mobile.Core.Auth;
using SSSKL.Mobile.Core.Configuration;
using SSSKL.Mobile.Core.Web;

namespace SSSKL.Mobile.Services;

/// <summary>
/// The document-start script for the app's WebViews (see <see cref="WebSessionScript"/>), kept up to
/// date with the native tokens.
/// </summary>
public sealed class WebSession
{
    private readonly AppEndpoints _endpoints;

    public WebSession(AppEndpoints endpoints, AuthSession session)
    {
        _endpoints = endpoints;
        Script = Build(null);
        session.TokensChanged += (_, tokens) => Update(tokens);
    }

    public static string AppVersion => AppInfo.Current.VersionString;

    public static string UserAgentSuffix => $" {WebSessionScript.UserAgentToken}/{AppVersion}";

    public string WebOrigin => _endpoints.WebOrigin;

    public string Script { get; private set; }

    /// <summary>Raised (on any thread) when <see cref="Script"/> changed.</summary>
    public event EventHandler? ScriptChanged;

    public void Update(TokenSet? tokens)
    {
        Script = Build(tokens);
        ScriptChanged?.Invoke(this, EventArgs.Empty);
    }

    private string Build(TokenSet? tokens) => WebSessionScript.Build(_endpoints.WebOrigin, AppVersion, tokens);
}
