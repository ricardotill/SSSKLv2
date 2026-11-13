using SSSKL.Mobile.Core.Localization;

namespace SSSKL.Mobile.Views.Popups;

/// <summary>The admin-maintained "what's new" HTML. Links open in the browser.</summary>
public sealed class WhatsNewPopupPage : PopupPage
{
    public WhatsNewPopupPage(string html)
    {
        var dark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var document = $$"""
            <!doctype html><html><head><meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <style>
              html, body { overflow-x: hidden; max-width: 100%; }
              body { font-family: -apple-system, system-ui, sans-serif; margin: 0; padding: 0 4px;
                     color: {{(dark ? "#e4e4e7" : "#27272a")}}; background: transparent; line-height: 1.5; }
              /* The editor's HTML can hold long words, URLs, &nbsp; runs and pre-wrap whitespace: wrap them all
                 (like the SPA's what's-new modal) so the popup only scrolls vertically. */
              body * { white-space: normal !important; overflow-wrap: anywhere; word-break: normal;
                       max-width: 100%; box-sizing: border-box; }
              a { color: {{(dark ? "#34d399" : "#047857")}}; }
              img, video, iframe { max-width: 100%; height: auto; }
              ul, ol { padding-left: 1.25rem; }
            </style></head><body>{{html}}</body></html>
            """;

        var webView = new WebView
        {
            Source = new HtmlWebViewSource { Html = document },
            HeightRequest = 360,
            BackgroundColor = Colors.Transparent
        };
        webView.Navigating += async (_, e) =>
        {
            if (e.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase) || e.Url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
            {
                e.Cancel = true;
                await Launcher.Default.TryOpenAsync(e.Url);
            }
        };

        SetCardContent(new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                new Label { Text = Texts.WhatsNewTitle, Style = (Style)Application.Current!.Resources["Headline"] },
                webView,
                CreateCloseButton()
            }
        });
    }
}
