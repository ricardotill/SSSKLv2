using FluentAssertions;
using SSSKL.Mobile.Core.Configuration;
using SSSKL.Mobile.Core.Web;

namespace SSSKL.Mobile.Test.Web;

[TestClass]
public class WebNavigationPolicyTests
{
    private readonly WebNavigationPolicy _sut = new(new AppEndpoints(new Uri("https://api.test"), new Uri("https://web.test")));

    [TestMethod]
    [DataRow("ssskl-app://pos", "pos")]
    [DataRow("ssskl-app://Logout", "logout")]
    public void Decide_AppScheme_IsNativeAction(string url, string action) =>
        _sut.Decide(url).Should().Be(new WebNavigationDecision(WebNavigationKind.Native, action));

    [TestMethod]
    [DataRow("https://web.test/events/1")]
    [DataRow("https://api.test/api/v1/blob/x")]
    [DataRow("https://challenges.cloudflare.com/cdn-cgi/challenge-platform/x")]
    [DataRow("https://www.google.com/maps/embed?pb=1")]
    [DataRow("about:blank")]
    public void Decide_SpaApiOrEmbedded_Allows(string url) =>
        _sut.Decide(url).Kind.Should().Be(WebNavigationKind.Allow);

    [TestMethod]
    [DataRow("https://scoutingwilo.nl/")]
    [DataRow("http://web.test/")]
    [DataRow("mailto:info@example.org")]
    public void Decide_OtherOrigins_OpensExternally(string url) =>
        _sut.Decide(url).Kind.Should().Be(WebNavigationKind.External);

    [TestMethod]
    public void Decide_UnknownScheme_Blocks() =>
        _sut.Decide("intent://foo#Intent;end").Kind.Should().Be(WebNavigationKind.Block);

    [TestMethod]
    public void Decide_OpenWithPath_PassesTheRoute() =>
        _sut.Decide("ssskl-app://open?path=%2Fevents%2F1%3Fx%3D1").Should()
            .Be(new WebNavigationDecision(WebNavigationKind.Native, "open", "/events/1?x=1"));

    [TestMethod]
    [DataRow("ssskl-app://open?path=https%3A%2F%2Fevil.test")]
    [DataRow("ssskl-app://open?path=%2F%2Fevil.test")]
    [DataRow("ssskl-app://open")]
    public void Decide_OpenWithoutAnAppRoute_HasNoPath(string url) =>
        _sut.Decide(url).NativePath.Should().BeNull();
}
