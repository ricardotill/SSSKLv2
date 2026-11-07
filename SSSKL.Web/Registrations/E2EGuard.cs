namespace SSSKL.Web.Registrations;

/// <summary>
/// The E2E environment wipes and reseeds the database on startup. Refuse to do that on a hosted instance.
/// </summary>
internal static class E2EGuard
{
    private static readonly string[] HostedMarkers = ["WEBSITE_INSTANCE_ID", "WEBSITE_SITE_NAME", "AZURE_SQL_CONNECTIONSTRING"];

    public static void EnsureNotHosted()
    {
        var marker = HostedMarkers.FirstOrDefault(m => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(m)));
        if (marker != null)
        {
            throw new InvalidOperationException(
                $"The E2E environment must not run on a hosted instance ({marker} is set). It deletes the database on startup.");
        }
    }
}
