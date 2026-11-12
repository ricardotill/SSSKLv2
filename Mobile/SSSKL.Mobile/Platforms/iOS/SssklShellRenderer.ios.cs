using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using SSSKL.Mobile.Controls;
using UIKit;

namespace SSSKL.Mobile;

/// <summary>Shell renderer whose tabs go back to the tab's root page when the open tab is tapped again.</summary>
public sealed class SssklShellRenderer : ShellRenderer
{
    protected override IShellItemRenderer CreateShellItemRenderer(ShellItem item) => new SssklShellItemRenderer(this) { ShellItem = item };

    private sealed class SssklShellItemRenderer(IShellContext context) : ShellItemRenderer(context)
    {
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            // MAUI sets ShouldSelectViewController in ViewDidLoad; wrap it. For the open tab, pop through MAUI's
            // navigation (so Shell's stack stays in sync) and stop UIKit from doing its own pop.
            var mauiShouldSelect = ShouldSelectViewController;
            ShouldSelectViewController = (tabController, viewController) =>
            {
                if (ReferenceEquals(viewController, SelectedViewController) && ShellItem?.CurrentItem is { } section)
                {
                    TabReselect.PopToRoot(section);
                    return false;
                }
                return mauiShouldSelect?.Invoke(tabController, viewController) ?? true;
            };
        }
    }
}
