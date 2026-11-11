using System.Reflection;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using SSSKL.Mobile.Controls;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Auth;
using SSSKL.Mobile.Core.Configuration;
using SSSKL.Mobile.Core.Popups;
using SSSKL.Mobile.Core.Services;
using SSSKL.Mobile.Core.ViewModels;
using SSSKL.Mobile.Core.Web;
using SSSKL.Mobile.Services;
using SSSKL.Mobile.Views;

namespace SSSKL.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            // Crashes, errors and traces to Sentry (project ssskl-mobile). The DSN is a public client key.
            // Also traces the API calls of IHttpClientFactory clients, continuing the trace into ssskl-api.
            .UseSentry(options =>
            {
                options.Dsn = "https://338189840a26e46f7878b15df3abd3cc@o4512396434341888.ingest.de.sentry.io/4512396459507792";
#if DEBUG
                options.Environment = "development";
                options.TracesSampleRate = 1.0;
#else
                options.Environment = "production";
                options.TracesSampleRate = 0.2;
#endif
                // No IP address or device user name; text and page titles stay out of breadcrumbs (defaults).
                options.SendDefaultPii = false;
            });

        Microsoft.Maui.Handlers.WebViewHandler.Mapper.AppendToMapping(nameof(SssklWebView), (handler, view) =>
        {
            if (view is SssklWebView webView)
            {
                WebViewPlatform.Configure(handler, webView);
            }
        });
        // Entries sit in their own rounded boxes (FieldBox style): drop the platform underline / border.
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("Borderless", (handler, _) =>
        {
#if ANDROID
            handler.PlatformView.Background = null;
#elif IOS
            handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#endif
        });

        var endpoints = CreateEndpoints();
        var services = builder.Services;
        services.AddSingleton(endpoints);
        services.AddSingleton(TimeProvider.System);

        // Auth: the refresher gets its own client without AuthTokenHandler (no recursion on 401).
        services.AddSingleton<ITokenStore, SecureTokenStore>();
        services.AddHttpClient<ITokenRefresher, IdentityTokenRefresher>(c => c.BaseAddress = endpoints.ApiBaseUrl);
        services.AddSingleton<AuthSession>();
        services.AddTransient<AuthTokenHandler>();
        services.AddHttpClient<ISssklApi, SssklApiClient>(c =>
            {
                c.BaseAddress = endpoints.ApiBaseUrl;
                c.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<AuthTokenHandler>();

        services.AddSingleton<CurrentUserService>();
        services.AddSingleton<ISettingsStore, PreferencesSettingsStore>();
        services.AddSingleton<IThemeApplier, ThemeApplier>();
        services.AddSingleton<ThemePreference>();
        services.AddSingleton<IPopupPresenter, PopupPresenter>();
        services.AddSingleton<IUserDialogs, UserDialogs>();
        services.AddSingleton<IPhotoPicker, MediaPhotoPicker>();
        services.AddSingleton<IWebDataCleaner, WebDataCleaner>();
        services.AddSingleton<AppNavigator>();
        services.AddSingleton<IAppNavigator>(sp => sp.GetRequiredService<AppNavigator>());
        services.AddSingleton<AchievementPopupCoordinator>();
        services.AddSingleton<NotificationCenter>();
        services.AddSingleton<WhatsNewCoordinator>();
        services.AddSingleton<WebNavigationPolicy>();
        services.AddSingleton<WebSession>();

        services.AddTransient<PosViewModel>();
        services.AddTransient<ProfileViewModel>();
        services.AddTransient<MoreViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<NotificationsViewModel>();
        services.AddTransient<PosPage>();
        services.AddTransient<ProfilePage>();
        services.AddTransient<MorePage>();
        services.AddTransient<SettingsPage>();
        services.AddTransient<NotificationsPage>();
        // Transient: AppNavigator creates one per AppShell (one per sign-in) and releases it on sign-out, so
        // no SPA state of a previous user survives.
        services.AddTransient<WebPage>();
        services.AddTransient<WelcomePage>();
        services.AddTransient<LoginPage>();
        services.AddTransient<AppShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }

    /// <summary>
    /// Release builds get the production URL from the csproj. Debug builds talk to a local
    /// `npm run start:mobile` dev server, which proxies /api to the API (Android emulators reach the
    /// host machine on 10.0.2.2). The API defaults to the SPA's origin.
    /// </summary>
    private static AppEndpoints CreateEndpoints()
    {
        var metadata = typeof(MauiProgram).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().ToList();
        var api = metadata.FirstOrDefault(m => m.Key == "SssklApiUrl")?.Value;
        var web = metadata.FirstOrDefault(m => m.Key == "SssklWebUrl")?.Value;
        var host = DeviceInfo.Platform == DevicePlatform.Android ? "10.0.2.2" : "localhost";
        var webUrl = new Uri(web ?? $"http://{host}:4300");
        return new AppEndpoints(api is null ? webUrl : new Uri(api), webUrl);
    }
}
