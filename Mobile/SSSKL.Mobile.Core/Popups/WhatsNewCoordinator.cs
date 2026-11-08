using System.Text.Json;
using Microsoft.Extensions.Logging;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Services;

namespace SSSKL.Mobile.Core.Popups;

/// <summary>
/// Shows the admin-maintained "what's new" text (global setting <c>WhatsNewContent</c>) once per
/// version, the version being the setting's <c>updatedOn</c>. Same rules as the SPA's WhatsNewService,
/// but the seen version is stored in the app's own preferences.
/// </summary>
public sealed class WhatsNewCoordinator(
    ISssklApi api,
    IPopupPresenter presenter,
    ISettingsStore settings,
    ILogger<WhatsNewCoordinator> logger)
{
    public const string SettingKey = "WhatsNewContent";
    public const string SeenVersionKey = "ssskl_whats_new_seen_version";

    private int _running;

    public async Task CheckAsync()
    {
        if (Interlocked.Exchange(ref _running, 1) == 1)
        {
            return;
        }
        try
        {
            var setting = await api.GetGlobalSettingAsync(SettingKey);
            if (setting is null || string.IsNullOrWhiteSpace(setting.Value) || settings.Get(SeenVersionKey) == setting.UpdatedOn)
            {
                return;
            }

            await presenter.ShowWhatsNewAsync(setting.Value);
            settings.Set(SeenVersionKey, setting.UpdatedOn);
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Checking what's new failed");
        }
        finally
        {
            Volatile.Write(ref _running, 0);
        }
    }
}
