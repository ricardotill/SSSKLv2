using SSSKL.Mobile.Core.Services;

namespace SSSKL.Mobile.Services;

public sealed class PreferencesSettingsStore : ISettingsStore
{
    public string? Get(string key) => Preferences.Default.Get<string?>(key, null);
    public void Set(string key, string value) => Preferences.Default.Set(key, value);
    public void Remove(string key) => Preferences.Default.Remove(key);
}
