using FluentAssertions;
using SSSKL.Mobile.Core.Auth;
using SSSKL.Mobile.Core.Services;
using SSSKL.Mobile.Core.Web;

namespace SSSKL.Mobile.Test.Web;

[TestClass]
public class WebSessionScriptTests
{
    [TestMethod]
    public void Build_WithTokens_ScopesToOriginAndEscapesValues()
    {
        // Act
        var script = WebSessionScript.Build("https://web.test", "3.12.3", new TokenSet("a'b\"c", "r1"));

        // Assert
        script.Should().Contain("if(location.origin!==\"https://web.test\")return;");
        script.Should().Contain("window.SSSKL_APP={version:\"3.12.3\"}");
        script.Should().Contain("localStorage.setItem('access_token',\"a\\u0027b\\u0022c\")");
        script.Should().Contain("localStorage.setItem('refresh_token',\"r1\")");
    }

    [TestMethod]
    public void Build_WithoutTokens_WritesNoTokens() =>
        WebSessionScript.Build("https://web.test", "1", null).Should().NotContain("_token");

    [TestMethod]
    [DataRow(ThemeMode.Auto, "auto")]
    [DataRow(ThemeMode.Light, "light")]
    [DataRow(ThemeMode.Dark, "dark")]
    public void Build_SharesTheColourModeWithTheSpa(ThemeMode mode, string value) =>
        WebSessionScript.Build("https://web.test", "1", null, mode)
            .Should().Contain($"localStorage.setItem('sssklv2-theme-mode',\"{value}\")");

    [TestMethod]
    public void NavigateScript_PassesAnAppRelativeRoute() =>
        WebSessionScript.NavigateScript("orders/personal")
            .Should().Be("(function(){return !!(window.ssskl&&window.ssskl.navigate(\"/orders/personal\"));})()");

    [TestMethod]
    public void NavigateScript_EscapesTheRoute() =>
        WebSessionScript.NavigateScript("/x\");alert(1)//").Should().Contain("navigate(\"/x\\u0022);alert(1)//\")");

    [TestMethod]
    [DataRow("true", true)]
    [DataRow("\"true\"", true)]
    [DataRow("True", true)]
    [DataRow("false", false)]
    [DataRow(null, false)]
    [DataRow("", false)]
    public void IsTrue_ReadsScriptResultsOfBothPlatforms(string? result, bool expected) =>
        WebSessionScript.IsTrue(result).Should().Be(expected);

    [TestMethod]
    [DataRow("{\"accessToken\":\"a\",\"refreshToken\":\"r\"}")]
    [DataRow("\"{\\\"accessToken\\\":\\\"a\\\",\\\"refreshToken\\\":\\\"r\\\"}\"")]
    [DataRow("{\\\"accessToken\\\":\\\"a\\\",\\\"refreshToken\\\":\\\"r\\\"}")]
    public void ParseTokens_PlainOrQuoted_ReturnsTokens(string result) =>
        WebSessionScript.ParseTokens(result).Should().Be(new TokenSet("a", "r"));

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("null")]
    [DataRow("\"\"")]
    [DataRow("garbage")]
    public void ParseTokens_Empty_ReturnsNull(string? result) =>
        WebSessionScript.ParseTokens(result).Should().BeNull();
}
