using AndroidX.WebKit;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using SSSKL.Mobile.Core.Web;
using SSSKL.Mobile.Services;

namespace SSSKL.Mobile.Controls;

public static partial class WebViewPlatform
{
    public static partial void Configure(IWebViewHandler handler, SssklWebView view)
    {
        var webView = handler.PlatformView;
        var services = handler.MauiContext!.Services;
        var session = services.GetRequiredService<WebSession>();

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
            services.GetService<ILogger<SssklWebView>>()?.LogWarning("WebView lacks DOCUMENT_START_SCRIPT; session is not shared");
            return;
        }

        IScriptHandler? registered = null;
        void Register()
        {
            if (webView.Handle == IntPtr.Zero)
            {
                return;
            }
            registered?.Remove();
            registered = WebViewCompat.AddDocumentStartJavaScript(webView, session.Script, new HashSet<string> { session.WebOrigin });
        }

        Register();
        session.ScriptChanged += (_, _) => webView.Post(Register);
    }
}
