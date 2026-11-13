using SSSKL.Mobile.Core.Localization;
using SSSKL.Mobile.Core.Models;

namespace SSSKL.Mobile.Core.Navigation;

/// <param name="Path">
/// SPA route the entry stands for. <see cref="NativeRoutes"/> decides whether it opens natively or in the WebView.
/// </param>
/// <param name="Badge">Shown as a counter; null hides it.</param>
public sealed record MenuEntry(string Title, string Icon, string Path, string? Badge = null)
{
    /// <summary>The first row of its section (draws no divider above itself).</summary>
    public bool IsFirst { get; init; }
}

public sealed record MenuSection
{
    public MenuSection(string title, IEnumerable<MenuEntry> entries)
    {
        Title = title;
        Entries = entries.Select((entry, index) => entry with { IsFirst = index == 0 }).ToList();
    }

    public string Title { get; }
    public IReadOnlyList<MenuEntry> Entries { get; }
}

/// <summary>
/// The "Meer" tab: every SPA feature without a tab of its own, grouped like the SPA's side menu
/// (Frontend layout/sidebar/sidebar.component.ts) and filtered on the same roles. Keep the two in step.
/// Notificaties has its own tab; "Vingeren" is the only app-only entry.
/// </summary>
public static class MoreMenu
{
    public static IReadOnlyList<MenuSection> Build(CurrentUser user, bool canAccessQuotes)
    {
        var general = new List<MenuEntry>
        {
            new(Texts.MyOrders, Icons.History, "orders/personal"),
            new(Texts.MySaldoPage, Icons.Wallet, "orders/saldo"),
            new(Texts.Events, Icons.Calendar, "events"),
        };
        if (canAccessQuotes)
        {
            general.Add(new(Texts.Quotes, Icons.Message, "quotes"));
        }
        general.AddRange(
        [
            new(Texts.Achievements, Icons.Rosette, "achievements"),
            new(Texts.Leaderboard, Icons.ChartBar, "leaderboard"),
            new(Texts.Stats, Icons.ChartLine, "stats"),
            new(Texts.SaldoList, Icons.Users, "users"),
            // App only: the SPA has no finger chooser.
            new(Texts.Chooser, Icons.HandFinger, NativeRoutes.ChooserPath),
        ]);

        var sections = new List<MenuSection> { new(Texts.SectionGeneral, general) };

        var isAdmin = user.IsInRole(RoleNames.Admin);
        var admin = new List<MenuEntry>();
        if (isAdmin || user.IsInRole(RoleNames.Kiosk))
        {
            admin.Add(new(Texts.AdminOrders, Icons.List, "admin/orders"));
        }
        if (isAdmin)
        {
            admin.AddRange(
            [
                new(Texts.AdminProducts, Icons.Box, "admin/products"),
                new(Texts.AdminTopUps, Icons.Wallet, "admin/topups"),
                new(Texts.AdminUsers, Icons.Users, "admin/users"),
                new(Texts.AdminRoles, Icons.Id, "admin/roles"),
                new(Texts.AdminAchievements, Icons.Rosette, "admin/achievements"),
                new(Texts.AdminAnnouncements, Icons.Speakerphone, "admin/announcements"),
                new(Texts.AdminSendNotification, Icons.Bell, "admin/notifications"),
                new(Texts.AdminGlobalSettings, Icons.AdjustmentsHorizontal, "admin/global-settings"),
            ]);
        }
        if (admin.Count > 0)
        {
            sections.Add(new MenuSection(Texts.SectionAdmin, admin));
        }

        sections.Add(new MenuSection(Texts.SectionApp,
        [
            new MenuEntry(Texts.Settings, Icons.Settings, "settings"),
            new MenuEntry(Texts.About, Icons.InfoCircle, "about"),
        ]));
        return sections;
    }

    /// <summary>A counter badge like the SPA header's: nothing for 0, "9+" above 9.</summary>
    public static string? BadgeText(int count) => count switch
    {
        <= 0 => null,
        > 9 => "9+",
        _ => count.ToString(System.Globalization.CultureInfo.InvariantCulture)
    };
}
