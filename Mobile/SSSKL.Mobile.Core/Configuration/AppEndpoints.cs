namespace SSSKL.Mobile.Core.Configuration;

/// <summary>
/// Where the app finds the API and the Angular SPA (shown in WebViews for everything that isn't native).
/// </summary>
public sealed class AppEndpoints
{
    public AppEndpoints(Uri apiBaseUrl, Uri webBaseUrl)
    {
        ApiBaseUrl = EnsureTrailingSlash(apiBaseUrl);
        WebBaseUrl = EnsureTrailingSlash(webBaseUrl);
    }

    public Uri ApiBaseUrl { get; }
    public Uri WebBaseUrl { get; }

    /// <summary>The origin (scheme://host:port) of the SPA, used to scope token injection.</summary>
    public string WebOrigin => WebBaseUrl.GetLeftPart(UriPartial.Authority);

    /// <summary>Resolves an API-relative URL such as <c>/api/v1/blob/...</c>; absolute URLs pass through.</summary>
    public string? ResolveApiUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }
        return Uri.TryCreate(url, UriKind.Absolute, out var absolute) && absolute.Scheme.StartsWith("http", StringComparison.Ordinal)
            ? absolute.ToString()
            : new Uri(ApiBaseUrl, url.TrimStart('/')).ToString();
    }

    /// <summary>Absolute URL of an SPA route such as <c>/settings</c>.</summary>
    public Uri WebUrl(string path) => new(WebBaseUrl, path.TrimStart('/'));

    public bool IsWebOrigin(Uri uri) =>
        string.Equals(uri.GetLeftPart(UriPartial.Authority), WebOrigin, StringComparison.OrdinalIgnoreCase);

    private static Uri EnsureTrailingSlash(Uri uri) =>
        uri.AbsoluteUri.EndsWith('/') ? uri : new Uri(uri.AbsoluteUri + "/");
}
