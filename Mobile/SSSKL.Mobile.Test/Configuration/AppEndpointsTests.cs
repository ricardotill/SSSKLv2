using FluentAssertions;
using SSSKL.Mobile.Core.Configuration;

namespace SSSKL.Mobile.Test.Configuration;

[TestClass]
public class AppEndpointsTests
{
    private readonly AppEndpoints _sut = new(new Uri("https://api.test"), new Uri("https://web.test:8443/app"));

    [TestMethod]
    public void ResolveApiUrl_Relative_PrefixesApiBase() =>
        _sut.ResolveApiUrl("/api/v1/blob/profilepicture/image/1").Should().Be("https://api.test/api/v1/blob/profilepicture/image/1");

    [TestMethod]
    public void ResolveApiUrl_Absolute_PassesThrough() =>
        _sut.ResolveApiUrl("https://cdn.test/x.png").Should().Be("https://cdn.test/x.png");

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public void ResolveApiUrl_Empty_ReturnsNull(string? url) => _sut.ResolveApiUrl(url).Should().BeNull();

    [TestMethod]
    public void WebUrl_CombinesWithBasePath() =>
        _sut.WebUrl("/settings").ToString().Should().Be("https://web.test:8443/app/settings");

    [TestMethod]
    public void IsWebOrigin_ComparesSchemeHostAndPort()
    {
        _sut.IsWebOrigin(new Uri("https://web.test:8443/pos")).Should().BeTrue();
        _sut.IsWebOrigin(new Uri("https://web.test/pos")).Should().BeFalse();
        _sut.IsWebOrigin(new Uri("https://evil.test:8443/")).Should().BeFalse();
    }
}
