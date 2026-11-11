using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace SSSKL.Mobile.Core.ViewModels;

/// <summary>
/// The profile description is HTML from the SPA's rich-text editor (Quill: one <c>&lt;p&gt;</c> per line,
/// <c>&lt;p&gt;&lt;br&gt;&lt;/p&gt;</c> for an empty one). The app edits it as plain text and converts both ways.
/// </summary>
public static partial class DescriptionText
{
    /// <summary>The HTML as text to edit: one line per paragraph, list item or <c>&lt;br&gt;</c>, entities decoded.</summary>
    public static string ToPlainText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return "";
        }
        var text = LineBreak().Replace(html, "\n");
        text = BlockEnd().Replace(text, "\n");
        text = Tag().Replace(text, "");
        text = WebUtility.HtmlDecode(text).Replace(' ', ' ');
        // A Quill empty line is <p><br></p>: both the <br> and the </p> became a newline, keep one.
        text = text.Replace("\r\n", "\n");
        var lines = text.Split('\n').Select(l => l.TrimEnd()).ToList();
        return CollapseBlankLines().Replace(string.Join('\n', lines), "\n\n").Trim('\n', ' ');
    }

    /// <summary>Text from the editor as HTML in the SPA editor's own shape. Empty text clears the description.</summary>
    public static string ToHtml(string? text)
    {
        var normalized = (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
        if (normalized.Length == 0)
        {
            return "";
        }
        var html = new StringBuilder();
        foreach (var line in normalized.Split('\n'))
        {
            var trimmed = line.TrimEnd();
            html.Append(trimmed.Length == 0 ? "<p><br></p>" : $"<p>{WebUtility.HtmlEncode(trimmed)}</p>");
        }
        return html.ToString();
    }

    /// <summary>
    /// True when the HTML uses formatting the plain-text editor can't keep (bold, lists, links, headings, ...),
    /// so saving from the app would flatten it.
    /// </summary>
    public static bool HasRichFormatting(string? html) =>
        !string.IsNullOrEmpty(html) && (RichTag().IsMatch(html) || StyledTag().IsMatch(html));

    [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex LineBreak();

    [GeneratedRegex(@"</(p|div|li|h[1-6]|blockquote|pre)\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex BlockEnd();

    [GeneratedRegex(@"<[^>]*>")]
    private static partial Regex Tag();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex CollapseBlankLines();

    [GeneratedRegex(@"<(strong|b|em|i|u|s|strike|a|ol|ul|li|h[1-6]|blockquote|pre|code|img|iframe|video)\b", RegexOptions.IgnoreCase)]
    private static partial Regex RichTag();

    /// <summary>Quill alignment/indent/colour classes and inline styles.</summary>
    [GeneratedRegex(@"<[a-z][^>]*\s(class|style)\s*=", RegexOptions.IgnoreCase)]
    private static partial Regex StyledTag();
}
