using Microsoft.Extensions.Logging;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Services;

namespace SSSKL.Mobile.Core.Popups;

/// <summary>
/// The only caller of <c>GET achievement/entries/unseen</c>. That call marks the entries seen on the
/// server, so whatever it returns is shown immediately. Checks never overlap. Singleton.
/// </summary>
public sealed class AchievementPopupCoordinator(
    ISssklApi api,
    IPopupPresenter presenter,
    TimeProvider timeProvider,
    ILogger<AchievementPopupCoordinator> logger)
{
    /// <summary>Minimum time between checks triggered by the app resuming.</summary>
    public static readonly TimeSpan ResumeInterval = TimeSpan.FromMinutes(1);

    private int _running;
    private DateTimeOffset _lastCheck = DateTimeOffset.MinValue;

    public Task CheckOnResumeAsync() =>
        timeProvider.GetUtcNow() - _lastCheck < ResumeInterval ? Task.CompletedTask : CheckAsync();

    public async Task CheckAsync()
    {
        if (Interlocked.Exchange(ref _running, 1) == 1)
        {
            return;
        }
        try
        {
            _lastCheck = timeProvider.GetUtcNow();
            var entries = await api.GetUnseenAchievementsAsync();
            if (entries.Count > 0)
            {
                await presenter.ShowAchievementsAsync(entries);
            }
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Checking unseen achievements failed");
        }
        finally
        {
            Volatile.Write(ref _running, 0);
        }
    }
}
