namespace SSSKL.Mobile.Core.Navigation;

/// <summary>The app's native screens that an SPA route can lead to.</summary>
public enum NativePage
{
    // Tabs
    Pos,
    Notifications,
    Profile,
    // Pushed on the current tab
    Settings,
    Orders,
    TopUps,
    SaldoList,
    UserProfile,
    Leaderboard,
    Stats,
    Achievements,
    AchievementDetail,
    About,
    Events,
    EventDetail,
    Quotes,
    QuoteDetail,
    Chooser
}

/// <param name="Id">The route's id segment (a user or achievement id), for pages that show one item.</param>
/// <param name="UserId">The <c>?userId=</c> of stats and achievements: another user's (null: your own).</param>
public sealed record NativeRoute(NativePage Page, string? Id = null, string? UserId = null);

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
        var userId = QueryValue(query, "userId");
        return segments switch
        {
            ["pos"] => new NativeRoute(NativePage.Pos),
            ["notifications"] => new NativeRoute(NativePage.Notifications),
            ["profile"] => new NativeRoute(NativePage.Profile),
            // Single tabs (?tab=security, ...) are only in the SPA's settings page.
            ["settings"] when QueryValue(query, "tab") is null => new NativeRoute(NativePage.Settings),
            ["orders", "personal"] or ["personal"] => new NativeRoute(NativePage.Orders),
            ["orders", "saldo"] => new NativeRoute(NativePage.TopUps),
            ["users"] => new NativeRoute(NativePage.SaldoList),
            // Not an SPA route (the SPA shows profiles in a drawer); the app's own link to a member's profile.
            ["users", var id] => new NativeRoute(NativePage.UserProfile, id),
            ["leaderboard"] => new NativeRoute(NativePage.Leaderboard),
            ["stats"] => new NativeRoute(NativePage.Stats, UserId: userId),
            ["achievements"] => new NativeRoute(NativePage.Achievements, UserId: userId),
            ["achievements", var id] when Guid.TryParse(id, out _) => new NativeRoute(NativePage.AchievementDetail, id),
            ["about"] => new NativeRoute(NativePage.About),
            // The create/edit forms (events/new, events/{id}/edit, quotes?new / ?edit=) stay in the SPA.
            ["events"] => new NativeRoute(NativePage.Events),
            ["events", var id] when Guid.TryParse(id, out _) => new NativeRoute(NativePage.EventDetail, id),
            ["quotes"] when QueryValue(query, "new") is null && QueryValue(query, "edit") is null => new NativeRoute(NativePage.Quotes),
            ["quotes", var id] when Guid.TryParse(id, out _) => new NativeRoute(NativePage.QuoteDetail, id),
            // Not an SPA route: the app's own finger chooser ("Vingeren").
            [ChooserPath] => new NativeRoute(NativePage.Chooser),
            _ => null
        };
    }

    /// <summary>The app's path for "Vingeren" (see <see cref="NativePage.Chooser"/>).</summary>
    public const string ChooserPath = "chooser";

    /// <summary>The app's path for a member's profile (see <see cref="NativePage.UserProfile"/>).</summary>
    public static string UserProfilePath(string userId) => $"users/{Uri.EscapeDataString(userId)}";

    private static string? QueryValue(string query, string name)
    {
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var equals = pair.IndexOf('=');
            var key = equals >= 0 ? pair[..equals] : pair;
            if (string.Equals(Uri.UnescapeDataString(key), name, StringComparison.OrdinalIgnoreCase))
            {
                // A bare flag (?new) counts as set.
                return equals >= 0 ? NullIfEmpty(Uri.UnescapeDataString(pair[(equals + 1)..])) : "";
            }
        }
        return null;
    }

    private static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;

    /// <summary>The path segments (the first lower-cased) and the raw query; absolute URLs and fragments are accepted.</summary>
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
        // Route names are case-insensitive; ids keep their case (user ids are case-sensitive strings).
        var segments = route.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select((s, i) => i == 0 ? s.ToLowerInvariant() : Uri.UnescapeDataString(s))
            .ToArray();
        return (segments, query);
    }
}
