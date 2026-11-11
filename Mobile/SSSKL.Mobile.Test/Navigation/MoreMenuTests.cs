using FluentAssertions;
using SSSKL.Mobile.Core.Localization;
using SSSKL.Mobile.Core.Models;
using SSSKL.Mobile.Core.Navigation;

namespace SSSKL.Mobile.Test.Navigation;

[TestClass]
public class MoreMenuTests
{
    private static CurrentUser User(params string[] roles) => new() { Id = "u1", UserName = "jan", Roles = roles };

    private static IEnumerable<string> Paths(IEnumerable<MenuSection> sections) =>
        sections.SelectMany(s => s.Entries).Select(e => e.Path);

    [TestMethod]
    public void Build_User_ListsEveryGeneralFeatureWithoutAdmin()
    {
        // Act
        var sections = MoreMenu.Build(User(RoleNames.User), canAccessQuotes: true);

        // Assert
        sections.Select(s => s.Title).Should().Equal(Texts.SectionGeneral, Texts.SectionApp);
        Paths(sections).Should().Equal(
            "orders/personal", "orders/saldo", "events", "quotes", "achievements",
            "leaderboard", "stats", "users", "settings", "about");
    }

    [TestMethod]
    public void Build_NoQuotesAccess_HidesQuotes() =>
        Paths(MoreMenu.Build(User(RoleNames.User), canAccessQuotes: false))
            .Should().NotContain("quotes");

    [TestMethod]
    public void Build_Kiosk_OnlyGetsAdminOrders()
    {
        // Act
        var sections = MoreMenu.Build(User(RoleNames.Kiosk), canAccessQuotes: true);

        // Assert
        sections.Single(s => s.Title == Texts.SectionAdmin).Entries.Select(e => e.Path).Should().Equal("admin/orders");
    }

    [TestMethod]
    public void Build_Admin_GetsEveryAdminPage()
    {
        // Act
        var sections = MoreMenu.Build(User(RoleNames.Admin), canAccessQuotes: true);

        // Assert
        sections.Single(s => s.Title == Texts.SectionAdmin).Entries.Select(e => e.Path).Should().Equal(
            "admin/orders", "admin/products", "admin/topups", "admin/users", "admin/roles",
            "admin/achievements", "admin/announcements", "admin/notifications", "admin/global-settings");
    }

    [TestMethod]
    public void Build_MarksTheFirstEntryOfEachSection()
    {
        // Act
        var sections = MoreMenu.Build(User(RoleNames.Admin), canAccessQuotes: true);

        // Assert
        foreach (var section in sections)
        {
            section.Entries.Select(e => e.IsFirst).Should().Equal(section.Entries.Select((_, i) => i == 0));
        }
    }

    [TestMethod]
    [DataRow(0, null)]
    [DataRow(-1, null)]
    [DataRow(1, "1")]
    [DataRow(9, "9")]
    [DataRow(10, "9+")]
    public void BadgeText_LikeTheSpaHeader(int count, string? expected) =>
        MoreMenu.BadgeText(count).Should().Be(expected);
}
