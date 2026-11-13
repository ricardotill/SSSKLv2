using FluentAssertions;
using SSSKL.Mobile.Core.Navigation;

namespace SSSKL.Mobile.Test.Navigation;

[TestClass]
public class NativeRoutesTests
{
    [TestMethod]
    [DataRow("pos", NativePage.Pos)]
    [DataRow("/pos", NativePage.Pos)]
    [DataRow("/notifications", NativePage.Notifications)]
    [DataRow("notifications/", NativePage.Notifications)]
    [DataRow("/Notifications?x=1#top", NativePage.Notifications)]
    [DataRow("https://ssskl.scoutingwilo.nl/notifications", NativePage.Notifications)]
    [DataRow("/profile", NativePage.Profile)]
    [DataRow("settings", NativePage.Settings)]
    public void Resolve_NativeRoutes(string path, NativePage expected) =>
        NativeRoutes.Resolve(path).Should().Be(new NativeRoute(expected));

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("  ")]
    [DataRow("/")]
    [DataRow("settings?tab=security")]
    [DataRow("/events/123")]
    [DataRow("admin/notifications")]
    [DataRow("/notifications/extra")]
    public void Resolve_WebOnlyRoutes_ReturnsNull(string? path) =>
        NativeRoutes.Resolve(path).Should().BeNull();

    [TestMethod]
    [DataRow("orders/personal", NativePage.Orders)]
    [DataRow("/personal", NativePage.Orders)]
    [DataRow("orders/saldo", NativePage.TopUps)]
    [DataRow("users", NativePage.SaldoList)]
    [DataRow("leaderboard", NativePage.Leaderboard)]
    [DataRow("stats", NativePage.Stats)]
    [DataRow("/achievements", NativePage.Achievements)]
    [DataRow("about", NativePage.About)]
    [DataRow("/chooser", NativePage.Chooser)]
    public void Resolve_Phase2Pages(string path, NativePage expected) =>
        NativeRoutes.Resolve(path)!.Page.Should().Be(expected);

    [TestMethod]
    public void Resolve_AchievementDetail_KeepsTheId()
    {
        var id = Guid.NewGuid().ToString();

        NativeRoutes.Resolve($"/achievements/{id}").Should().Be(new NativeRoute(NativePage.AchievementDetail, id));
    }

    [TestMethod]
    public void Resolve_AchievementWithoutGuid_IsWebOnly() =>
        NativeRoutes.Resolve("/achievements/not-a-guid").Should().BeNull();

    [TestMethod]
    [DataRow("stats?userId=AbC-1", NativePage.Stats)]
    [DataRow("/achievements?foo=1&userId=AbC-1", NativePage.Achievements)]
    public void Resolve_UserIdQuery_KeepsItsCase(string path, NativePage expected) =>
        NativeRoutes.Resolve(path).Should().Be(new NativeRoute(expected, UserId: "AbC-1"));

    [TestMethod]
    public void UserProfilePath_RoundTripsTheId()
    {
        var path = NativeRoutes.UserProfilePath("Ab c/1");

        NativeRoutes.Resolve(path).Should().Be(new NativeRoute(NativePage.UserProfile, "Ab c/1"));
    }

    [TestMethod]
    public void Resolve_EventsAndQuotes()
    {
        var id = Guid.NewGuid().ToString();

        NativeRoutes.Resolve("/events").Should().Be(new NativeRoute(NativePage.Events));
        NativeRoutes.Resolve($"/events/{id}").Should().Be(new NativeRoute(NativePage.EventDetail, id));
        NativeRoutes.Resolve("/quotes").Should().Be(new NativeRoute(NativePage.Quotes));
        NativeRoutes.Resolve($"/quotes/{id}").Should().Be(new NativeRoute(NativePage.QuoteDetail, id));
    }

    [TestMethod]
    [DataRow("/events/new")]
    [DataRow("/events/6f9619ff-8b86-d011-b42d-00cf4fc964ff/edit")]
    [DataRow("/quotes?new=1")]
    [DataRow("/quotes?new")]
    [DataRow("/quotes?edit=6f9619ff-8b86-d011-b42d-00cf4fc964ff")]
    public void Resolve_Forms_StayInTheSpa(string path) =>
        NativeRoutes.Resolve(path).Should().BeNull();
}
