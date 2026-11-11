using Microsoft.Extensions.Hosting;

namespace SSSKL.Infrastructure.Hosting;

/// <summary>
/// Names of the non-production environments used for automated tests.
/// <c>IntegrationTest</c> is used by WebApplicationFactory tests, <c>E2E</c> by the Playwright suite.
/// Both run on SQLite and behave like Development for cookies and SignalR.
/// </summary>
public static class TestEnvironments
{
    public const string IntegrationTest = "IntegrationTest";
    public const string E2E = "E2E";

    public static bool IsIntegrationTest(this IHostEnvironment environment) =>
        environment.IsEnvironment(IntegrationTest);

    public static bool IsE2E(this IHostEnvironment environment) =>
        environment.IsEnvironment(E2E);

    public static bool IsTestLike(this IHostEnvironment environment) =>
        environment.IsIntegrationTest() || environment.IsE2E();
}
