namespace SSSKL.Mobile.Core.Navigation;

/// <summary>The app's native screens that an SPA route can lead to.</summary>
public enum NativePage
{
    Pos,
    Notifications,
    Profile,
    Settings
}

/// <param name="Id">The route's id segment (e.g. an event id), for pages that show one item.</param>
public sealed record NativeRoute(NativePage Page, string? Id = null);

/// <summary>
/// Which SPA routes the app shows natively. Menu entries, notification links (<c>LinkUri</c>, e.g.
/// <c>/achievements</c>) and SPA hand-overs all go through here; everything else opens in the WebView.
/// </summary>
public static class NativeRoutes
{
    /// <summary>Returns null for routes that only exist in the SPA.</summary>
    public static NativeRoute? Resolve(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var (segments, query) = Split(path);
        return segments switch
        {
            ["pos"] => new NativeRoute(NativePage.Pos),
            ["notifications"] => new NativeRoute(NativePage.Notifications),
            ["profile"] => new NativeRoute(NativePage.Profile),
            // Single tabs (?tab=security, ...) are only in the SPA's settings page.
            ["settings"] when !query.Contains("tab=", StringComparison.OrdinalIgnoreCase) => new NativeRoute(NativePage.Settings),
            _ => null
        };
    }

    /// <summary>Lower-cased path segments and the raw query; absolute URLs and fragments are accepted.</summary>
    private static (string[] Segments, string Query) Split(string path)
    {
        var value = path.Trim();
        if (Uri.TryCreate(value, UriKind.Absolute, out var absolute) && absolute.Scheme is "http" or "https")
        {
            value = absolute.PathAndQuery;
        }
        var hash = value.IndexOf('#');
        if (hash >= 0)
        {
            value = value[..hash];
        }
        var question = value.IndexOf('?');
        var query = question >= 0 ? value[(question + 1)..] : "";
        var route = question >= 0 ? value[..question] : value;
        var segments = route.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => s.ToLowerInvariant())
            .ToArray();
        return (segments, query);
    }
}
