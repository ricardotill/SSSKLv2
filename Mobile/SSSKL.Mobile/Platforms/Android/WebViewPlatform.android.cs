using System.Runtime.CompilerServices;
using AndroidX.WebKit;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using SSSKL.Mobile.Core.Web;
using SSSKL.Mobile.Services;

namespace SSSKL.Mobile.Controls;

public static partial class WebViewPlatform
{
    /// <summary>The registered document-start script per WebView, so a new script replaces the old one.</summary>
    private static readonly ConditionalWeakTable<Android.Webkit.WebView, IScriptHandler> ScriptHandlers = new();

    public static partial void Configure(IWebViewHandler handler, SssklWebView view)
    {
        var webView = handler.PlatformView;
        var services = handler.MauiContext!.Services;
        var session = services.GetRequiredService<WebSession>();
        var logger = services.GetService<ILogger<SssklWebView>>();

        var settings = webView.Settings;
        settings.JavaScriptEnabled = true;
        settings.DomStorageEnabled = true;
        if (settings.UserAgentString?.Contains(WebSessionScript.UserAgentToken, StringComparison.Ordinal) != true)
        {
            settings.UserAgentString += WebSession.UserAgentSuffix;
        }
#if DEBUG
        Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);
#endif

        if (!WebViewFeature.IsFeatureSupported(WebViewFeature.DocumentStartScript))
        {
            // Very old WebView: the SPA still detects the app by user agent, but shows its own login.
            logger?.LogWarning("WebView lacks DOCUMENT_START_SCRIPT; session is not shared");
            return;
        }

        Register(webView, session, logger);
        // ScriptChanged is raised on any thread; the WebView must be touched on its own (UI) thread.
        SubscribeToScriptChanges(handler, view, session, webView, target =>
        {
            var weak = new WeakReference<Android.Webkit.WebView>(target);
            target.Post(() =>
            {
                if (weak.TryGetTarget(out var current))
                {
                    Register(current, session, logger);
                }
            });
        });
    }

    private static void Register(Android.Webkit.WebView webView, WebSession session, ILogger? logger)
    {
        try
        {
            if (webView.Handle == IntPtr.Zero)
            {
                return;
            }
            if (ScriptHandlers.TryGetValue(webView, out var previous))
            {
                previous.Remove();
            }
            var registered = WebViewCompat.AddDocumentStartJavaScript(webView, session.Script, new HashSet<string> { session.WebOrigin });
            if (registered is null)
            {
                ScriptHandlers.Remove(webView);
            }
            else
            {
                ScriptHandlers.AddOrUpdate(webView, registered);
            }
        }
        catch (Exception ex)
        {
            // Disposed or destroyed WebView: nothing to update.
            logger?.LogWarning(ex, "Registering the WebView session script failed");
        }
    }
}
