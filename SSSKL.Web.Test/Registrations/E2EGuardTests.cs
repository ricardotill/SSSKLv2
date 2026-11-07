using FluentAssertions;
using SSSKL.Web.Registrations;

namespace SSSKL.Web.Test.Registrations;

[TestClass]
public class E2EGuardTests
{
    [TestMethod]
    public void EnsureNotHosted_NoHostedMarkers_DoesNotThrow()
    {
        var act = () => E2EGuard.EnsureNotHosted(_ => null);

        act.Should().NotThrow();
    }

    [TestMethod]
    public void EnsureNotHosted_EmptyMarker_DoesNotThrow()
    {
        var act = () => E2EGuard.EnsureNotHosted(_ => "");

        act.Should().NotThrow();
    }

    [TestMethod]
    [DataRow("WEBSITE_INSTANCE_ID")]
    [DataRow("WEBSITE_SITE_NAME")]
    [DataRow("AZURE_SQL_CONNECTIONSTRING")]
    public void EnsureNotHosted_HostedMarkerSet_Throws(string marker)
    {
        var act = () => E2EGuard.EnsureNotHosted(name => name == marker ? "value" : null);

        act.Should().Throw<InvalidOperationException>().WithMessage($"*{marker} is set*");
    }

    [TestMethod]
    public void EnsureNotHosted_ProcessEnvironment_DoesNotThrowOnBuildAgent()
    {
        // Test runners (locally and in CI) are never App Service instances.
        Action act = E2EGuard.EnsureNotHosted;

        act.Should().NotThrow();
    }
}
