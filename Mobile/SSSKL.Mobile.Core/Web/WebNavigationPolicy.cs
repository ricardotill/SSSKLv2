using SSSKL.Mobile.Core.Configuration;

namespace SSSKL.Mobile.Core.Web;

public enum WebNavigationKind
{
    /// <summary>Let the WebView load it.</summary>
    Allow,
    /// <summary>Cancel and hand <see cref="WebNavigationDecision.NativeAction"/> to the app (ssskl-app://...).</summary>
    Native,
    /// <summary>Cancel and open in the system browser.</summary>
    External,
    /// <summary>Cancel silently.</summary>
    Block
}

public sealed record WebNavigationDecision(WebNavigationKind Kind, string? NativeAction = null);

/// <summary>
/// Decides what happens when the embedded SPA navigates. The SPA's embedded mode signals the app by
/// navigating to <c>ssskl-app://&lt;action&gt;</c> (pos, profile, logout, signed-in), because SPA route changes
/// (pushState) don't raise WebView navigation events.
/// </summary>
public sealed class WebNavigationPolicy(AppEndpoints endpoints)
{
    public const string AppScheme = "ssskl-app";

    /// <summary>Third-party frames the SPA embeds (Turnstile, Google Maps); never treated as external links.</summary>
    private static readonly string[] EmbeddedHosts =
    [
        "challenges.cloudflare.com",
        "google.com",
        "googleapis.com",
        "gstatic.com"
    ];

    public WebNavigationDecision Decide(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return new WebNavigationDecision(WebNavigationKind.Allow);
        }

        if (uri.Scheme == AppScheme)
        {
            return new WebNavigationDecision(WebNavigationKind.Native, uri.Host.ToLowerInvariant());
        }

        if (uri.Scheme is "about" or "data" or "blob")
        {
            return new WebNavigationDecision(WebNavigationKind.Allow);
        }

        if (uri.Scheme is "http" or "https")
        {
            if (endpoints.IsWebOrigin(uri) || IsApiOrigin(uri) || IsEmbeddedHost(uri.Host))
            {
                return new WebNavigationDecision(WebNavigationKind.Allow);
            }
            return new WebNavigationDecision(WebNavigationKind.External);
        }

        // mailto:, tel:, whatsapp: and similar.
        return uri.Scheme is "mailto" or "tel" or "sms" or "whatsapp"
            ? new WebNavigationDecision(WebNavigationKind.External)
            : new WebNavigationDecision(WebNavigationKind.Block);
    }

    private bool IsApiOrigin(Uri uri) =>
        string.Equals(uri.GetLeftPart(UriPartial.Authority), endpoints.ApiBaseUrl.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase);

    private static bool IsEmbeddedHost(string host) =>
        EmbeddedHosts.Any(h => host.Equals(h, StringComparison.OrdinalIgnoreCase) || host.EndsWith("." + h, StringComparison.OrdinalIgnoreCase));
}
