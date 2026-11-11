using SSSKL.Mobile.Core.ViewModels;

namespace SSSKL.Mobile.Test;

[TestClass]
public static class AssemblySetup
{
    /// <summary>Dates are shown in Amsterdam time in every test (tests run in parallel: set once, never changed).</summary>
    [AssemblyInitialize]
    public static void Initialize(TestContext _) =>
        Display.TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");
}
