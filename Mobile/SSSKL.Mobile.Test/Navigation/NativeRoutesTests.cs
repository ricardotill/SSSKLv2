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
}
