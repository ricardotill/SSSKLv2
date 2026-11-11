using System.Text.Json;
using SSSKL.Mobile.Core.Auth;

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

    public static string Build(string webOrigin, string appVersion, TokenSet? tokens)
    {
        var origin = Js(webOrigin);
        var version = Js(appVersion);
        var setTokens = tokens is null
            ? ""
            : $"localStorage.setItem('access_token',{Js(tokens.AccessToken)});" +
              $"localStorage.setItem('refresh_token',{Js(tokens.RefreshToken)});";

        return "(function(){" +
               $"if(location.origin!=={origin})return;" +
               $"window.SSSKL_APP={{version:{version}}};" +
               $"try{{{setTokens}}}catch(e){{}}" +
               "})();";
    }

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
