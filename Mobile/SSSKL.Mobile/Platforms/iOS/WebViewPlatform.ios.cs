using Foundation;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using ObjCRuntime;
using SSSKL.Mobile.Core.Web;
using SSSKL.Mobile.Services;
using WebKit;

namespace SSSKL.Mobile.Controls;

public static partial class WebViewPlatform
{
    public static partial void Configure(IWebViewHandler handler, SssklWebView view)
    {
        var webView = handler.PlatformView;
        var services = handler.MauiContext!.Services;
        var session = services.GetRequiredService<WebSession>();
        var logger = services.GetService<ILogger<SssklWebView>>();

#if DEBUG
        if (OperatingSystem.IsIOSVersionAtLeast(16, 4))
        {
            webView.Inspectable = true;
        }
#endif

        // Fallback for embedded-mode detection; the injected script below is the primary signal.
        var weakWebView = new WeakReference<WKWebView>(webView);
        webView.EvaluateJavaScript("navigator.userAgent", (result, _) =>
        {
            var userAgent = result?.ToString();
            if (userAgent is not null && !userAgent.Contains(WebSessionScript.UserAgentToken, StringComparison.Ordinal) &&
                weakWebView.TryGetTarget(out var target) && target.Handle != NativeHandle.Zero)
            {
                target.CustomUserAgent = userAgent + WebSession.UserAgentSuffix;
            }
        });

        Register(webView, session, logger);
        // ScriptChanged is raised on any thread; WebKit must be touched on the main thread.
        SubscribeToScriptChanges(handler, view, session, webView, target =>
        {
            var weak = new WeakReference<WKWebView>(target);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (weak.TryGetTarget(out var current))
                {
                    Register(current, session, logger);
                }
            });
        });
    }

    private static void Register(WKWebView webView, WebSession session, ILogger? logger)
    {
        try
        {
            if (webView.Handle == NativeHandle.Zero)
            {
                return;
            }
            var controller = webView.Configuration.UserContentController;
            controller.RemoveAllUserScripts();
            controller.AddUserScript(new WKUserScript(new NSString(session.Script), WKUserScriptInjectionTime.AtDocumentStart, isForMainFrameOnly: true));
        }
        catch (Exception ex)
        {
            // Disposed WebView: nothing to update.
            logger?.LogWarning(ex, "Registering the WebView session script failed");
        }
    }
}
