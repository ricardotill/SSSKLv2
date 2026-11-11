using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using SSSKL.Mobile.Services;

namespace SSSKL.Mobile.Controls;

public static partial class WebViewPlatform
{
    private static readonly ConditionalWeakTable<SssklWebView, IDisposable> Subscriptions = new();

    public static partial void Configure(IWebViewHandler handler, SssklWebView view);

    /// <summary>
    /// Re-runs <paramref name="onScriptChanged"/> for the native view whenever <see cref="WebSession.Script"/>
    /// changes, until the view's handler disconnects or the native view is collected. The singleton
    /// <see cref="WebSession"/> only holds the native view weakly, so (transient) pages with a WebView don't
    /// leak; <paramref name="onScriptChanged"/> must therefore not capture the native view itself.
    /// </summary>
    private static void SubscribeToScriptChanges<TNative>(
        IWebViewHandler handler,
        SssklWebView view,
        WebSession session,
        TNative nativeView,
        Action<TNative> onScriptChanged) where TNative : class
    {
        var logger = handler.MauiContext?.Services.GetService<ILogger<SssklWebView>>();
        var subscription = new ScriptSubscription<TNative>(session, nativeView, onScriptChanged, logger);

        if (Subscriptions.TryGetValue(view, out var previous))
        {
            previous.Dispose();
        }
        Subscriptions.AddOrUpdate(view, subscription);

        void OnHandlerChanging(object? sender, HandlerChangingEventArgs e)
        {
            if (ReferenceEquals(e.OldHandler, handler))
            {
                view.HandlerChanging -= OnHandlerChanging;
                subscription.Dispose();
            }
        }
        view.HandlerChanging += OnHandlerChanging;
    }

    private sealed class ScriptSubscription<TNative> : IDisposable where TNative : class
    {
        private readonly WebSession _session;
        private readonly WeakReference<TNative> _nativeView;
        private readonly Action<TNative> _onScriptChanged;
        private readonly ILogger? _logger;

        public ScriptSubscription(WebSession session, TNative nativeView, Action<TNative> onScriptChanged, ILogger? logger)
        {
            _session = session;
            _nativeView = new WeakReference<TNative>(nativeView);
            _onScriptChanged = onScriptChanged;
            _logger = logger;
            session.ScriptChanged += OnScriptChanged;
        }

        private void OnScriptChanged(object? sender, EventArgs e)
        {
            if (!_nativeView.TryGetTarget(out var nativeView))
            {
                Dispose();
                return;
            }
            try
            {
                _onScriptChanged(nativeView);
            }
            catch (Exception ex)
            {
                // E.g. the native view was disposed: stop updating it.
                _logger?.LogWarning(ex, "Updating the WebView session script failed");
                Dispose();
            }
        }

        public void Dispose() => _session.ScriptChanged -= OnScriptChanged;
    }
}
