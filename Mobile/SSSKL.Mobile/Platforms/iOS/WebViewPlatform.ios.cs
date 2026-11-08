using Foundation;
using Microsoft.Maui.Handlers;
using SSSKL.Mobile.Core.Web;
using SSSKL.Mobile.Services;
using WebKit;

namespace SSSKL.Mobile.Controls;

public static partial class WebViewPlatform
{
    public static partial void Configure(IWebViewHandler handler, SssklWebView view)
    {
        var webView = handler.PlatformView;
        var session = handler.MauiContext!.Services.GetRequiredService<WebSession>();

#if DEBUG
        if (OperatingSystem.IsIOSVersionAtLeast(16, 4))
        {
            webView.Inspectable = true;
        }
#endif

        // Fallback for embedded-mode detection; the injected script below is the primary signal.
        webView.EvaluateJavaScript("navigator.userAgent", (result, _) =>
        {
            var userAgent = result?.ToString();
            if (userAgent is not null && !userAgent.Contains(WebSessionScript.UserAgentToken, StringComparison.Ordinal))
            {
                webView.CustomUserAgent = userAgent + WebSession.UserAgentSuffix;
            }
        });

        var controller = webView.Configuration.UserContentController;
        void Register()
        {
            controller.RemoveAllUserScripts();
            controller.AddUserScript(new WKUserScript(new NSString(session.Script), WKUserScriptInjectionTime.AtDocumentStart, isForMainFrameOnly: true));
        }

        Register();
        session.ScriptChanged += (_, _) => MainThread.BeginInvokeOnMainThread(Register);
    }
}
