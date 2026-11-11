using SSSKL.Mobile.Core.Navigation;

namespace SSSKL.Mobile.Views;

/// <summary>A pushed native page for an SPA route (with an id or another member's data).</summary>
public interface INativePage
{
    /// <summary>
    /// The route the page shows (set by <see cref="Prepare"/>). When the SPA hands back to the same route,
    /// the navigator just closes the WebView; pages its forms can change reload when they reappear.
    /// </summary>
    NativeRoute? Route { get; }

    /// <summary>Called by <see cref="Services.AppNavigator"/> before the page is pushed.</summary>
    void Prepare(NativeRoute route);
}
