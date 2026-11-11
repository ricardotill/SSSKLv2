using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using SSSKL.Mobile.Core.Models;

namespace SSSKL.Mobile.Core.Api;

/// <summary>Typed HttpClient; BaseAddress is the API root and <see cref="Auth.AuthTokenHandler"/> adds the bearer token.</summary>
public sealed class SssklApiClient(HttpClient httpClient) : ISssklApi
{
    public Task<CurrentUser> GetMeAsync(CancellationToken cancellationToken = default) =>
        GetAsync("api/v1/ApplicationUser/me", ApiJsonContext.Default.CurrentUser, cancellationToken);

    public Task<OrderInitialize> GetOrderInitializeAsync(CancellationToken cancellationToken = default) =>
        GetAsync("api/v1/Order/initialize", ApiJsonContext.Default.OrderInitialize, cancellationToken);

    public async Task SubmitOrderAsync(OrderSubmit order, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/v1/Order", order, ApiJsonContext.Default.OrderSubmit, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public Task<UserStats> GetStatsAsync(string userId, CancellationToken cancellationToken = default) =>
        GetAsync($"api/v1/ApplicationUser/{Uri.EscapeDataString(userId)}/stats", ApiJsonContext.Default.UserStats, cancellationToken);

    public async Task<IReadOnlyList<AchievementEntry>> GetAchievementEntriesAsync(string userId, CancellationToken cancellationToken = default) =>
        await GetAsync($"api/v1/Achievement/entries/{Uri.EscapeDataString(userId)}", ApiJsonContext.Default.ListAchievementEntry, cancellationToken);

    public async Task<IReadOnlyList<AchievementEntry>> GetUnseenAchievementsAsync(CancellationToken cancellationToken = default) =>
        await GetAsync("api/v1/Achievement/entries/unseen", ApiJsonContext.Default.ListAchievementEntry, cancellationToken);

    public async Task<GlobalSetting?> GetGlobalSettingAsync(string key, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/v1/GlobalSettings/{Uri.EscapeDataString(key)}", cancellationToken);
        // An unset setting is 204 No Content (GlobalSettingsController.GetSetting); 404 is treated the same.
        if (response.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.NotFound)
        {
            return null;
        }
        await EnsureSuccessAsync(response, cancellationToken);
        if (response.Content.Headers.ContentLength == 0)
        {
            return null;
        }
        return await response.Content.ReadFromJsonAsync(ApiJsonContext.Default.GlobalSetting, cancellationToken);
    }

    public async Task UploadProfilePictureAsync(Base64FileUpload file, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/v1/ApplicationUser/me/profile-picture", file, ApiJsonContext.Default.Base64FileUpload, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task DeleteProfilePictureAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync("api/v1/ApplicationUser/me/profile-picture", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task UpdateMeAsync(SelfUpdate update, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync("api/v1/ApplicationUser/me", update, ApiJsonContext.Default.SelfUpdate, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<TwoFactorStatus> GetTwoFactorStatusAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/v1/identity/manage/2fa", new TwoFactorRequest(), ApiJsonContext.Default.TwoFactorRequest, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync(ApiJsonContext.Default.TwoFactorStatus, cancellationToken)
               ?? throw new ApiException(response.StatusCode, "Empty response");
    }

    public Task<int> GetUnreadNotificationCountAsync(CancellationToken cancellationToken = default) =>
        GetAsync("api/v1/Notifications/unread-count", ApiJsonContext.Default.Int32, cancellationToken);

    public async Task<IReadOnlyList<NotificationItem>> GetNotificationsAsync(int skip, int take, CancellationToken cancellationToken = default) =>
        await GetAsync($"api/v1/Notifications?unreadOnly=false&skip={skip}&take={take}", ApiJsonContext.Default.ListNotificationItem, cancellationToken);

    public Task MarkNotificationReadAsync(Guid id, CancellationToken cancellationToken = default) =>
        PostEmptyAsync($"api/v1/Notifications/{id}/read", cancellationToken);

    public Task MarkAllNotificationsReadAsync(CancellationToken cancellationToken = default) =>
        PostEmptyAsync("api/v1/Notifications/read-all", cancellationToken);

    public async Task<bool> CanAccessQuotesAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync("api/v1/Quote?skip=0&take=1", cancellationToken);
        return response.StatusCode != HttpStatusCode.Forbidden;
    }

    private async Task PostEmptyAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsync(url, content: null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private async Task<T> GetAsync<T>(string url, JsonTypeInfo<T> typeInfo, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(url, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync(typeInfo, cancellationToken)
               ?? throw new ApiException(response.StatusCode, "Empty response");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var message = response.ReasonPhrase ?? response.StatusCode.ToString();
        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(body))
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty("title", out var title) &&
                    title.ValueKind == JsonValueKind.String)
                {
                    message = title.GetString() ?? message;
                }
            }
        }
        catch (JsonException)
        {
            // Not ProblemDetails; keep the reason phrase.
        }

        throw new ApiException(response.StatusCode, message);
    }
}
