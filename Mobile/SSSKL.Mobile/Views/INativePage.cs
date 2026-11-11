using SSSKL.Mobile.Core.Navigation;

namespace SSSKL.Mobile.Views;

/// <summary>A pushed native page that shows one item or another member's data (an id from the route).</summary>
public interface INativePage
{
    /// <summary>Called by <see cref="Services.AppNavigator"/> before the page is pushed.</summary>
    void Prepare(NativeRoute route);
}
