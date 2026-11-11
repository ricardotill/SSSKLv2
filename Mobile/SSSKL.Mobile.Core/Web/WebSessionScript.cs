using System.Text.Json;
using SSSKL.Mobile.Core.Auth;
using SSSKL.Mobile.Core.Services;

namespace SSSKL.Mobile.Core.Web;

/// <summary>
/// JavaScript injected at document start into every SPA page the app shows. It marks the page as
/// embedded (the SPA then hides its own popups, push prompt and service worker) and hands the native
/// session to the SPA by writing the tokens where Frontend auth.service.ts reads them.
/// </summary>
public static class WebSessionScript
{
    /// <summary>Also appended to the WebView user agent, as a fallback for the embedded-mode detection.</summary>
    public const string UserAgentToken = "SSSKLApp";

    public const string ReadTokensScript =
        "(function(){var a=localStorage.getItem('access_token')||sessionStorage.getItem('access_token');" +
        "var r=localStorage.getItem('refresh_token')||sessionStorage.getItem('refresh_token');" +
        "return a&&r?JSON.stringify({accessToken:a,refreshToken:r}):'';})()";

    /// <summary>Where the SPA's ThemeService keeps the colour mode.</summary>
    public const string ThemeStorageKey = "sssklv2-theme-mode";

    public static string Build(string webOrigin, string appVersion, TokenSet? tokens, ThemeMode theme = ThemeMode.Auto)
    {
        var origin = Js(webOrigin);
        var version = Js(appVersion);
        var setTokens = tokens is null
            ? ""
            : $"localStorage.setItem('access_token',{Js(tokens.AccessToken)});" +
              $"localStorage.setItem('refresh_token',{Js(tokens.RefreshToken)});";
        var setTheme = $"localStorage.setItem('{ThemeStorageKey}',{Js(ThemePreference.ToWebValue(theme))});";

        return "(function(){" +
               $"if(location.origin!=={origin})return;" +
               $"window.SSSKL_APP={{version:{version}}};" +
               $"try{{{setTokens}{setTheme}}}catch(e){{}}" +
               "})();";
    }

    /// <summary>Opens an SPA route in a loaded page without reloading it. Evaluates to true when the SPA took it.</summary>
    public static string NavigateScript(string path) =>
        $"(function(){{return !!(window.ssskl&&window.ssskl.navigate({Js("/" + path.TrimStart('/'))}));}})()";

    /// <summary>Goes back inside the SPA. Evaluates to false when the page the app opened is showing.</summary>
    public const string BackScript = "(function(){return !!(window.ssskl&&window.ssskl.back());})()";

    /// <summary>Evaluates to true while the SPA hasn't rendered the requested page yet (or hasn't started).</summary>
    public const string BusyScript = "(function(){return !window.ssskl||window.ssskl.busy();})()";

    /// <summary>WebViews return script results as text; Android quotes it, iOS doesn't.</summary>
    public static bool IsTrue(string? result) =>
        string.Equals(result?.Trim().Trim('"'), "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>A JavaScript string literal (JSON string escaping).</summary>
    private static string Js(string value) => JsonSerializer.Serialize(value, ApiJsonContext.Default.String);

    /// <summary>Parses the result of <see cref="ReadTokensScript"/>; WebViews may return it JSON-quoted.</summary>
    public static TokenSet? ParseTokens(string? result)
    {
        if (string.IsNullOrWhiteSpace(result) || result == "null")
        {
            return null;
        }
        var json = result.Trim();
        try
        {
            if (json.StartsWith('"'))
            {
                json = JsonSerializer.Deserialize(json, ApiJsonContext.Default.String) ?? "";
            }
            json = json.Replace("\\\"", "\"");
            if (json.Length == 0)
            {
                return null;
            }
            using var doc = JsonDocument.Parse(json);
            var access = doc.RootElement.GetProperty("accessToken").GetString();
            var refresh = doc.RootElement.GetProperty("refreshToken").GetString();
            return string.IsNullOrEmpty(access) || string.IsNullOrEmpty(refresh) ? null : new TokenSet(access, refresh);
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return null;
        }
    }
}
