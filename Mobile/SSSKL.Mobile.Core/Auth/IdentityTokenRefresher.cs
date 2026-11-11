using System.Net;
using System.Net.Http.Json;
using SSSKL.Mobile.Core.Models;

namespace SSSKL.Mobile.Core.Auth;

/// <summary>Calls <c>POST /api/v1/identity/refresh</c> on an HttpClient without <see cref="AuthTokenHandler"/>.</summary>
public sealed class IdentityTokenRefresher(HttpClient httpClient) : ITokenRefresher
{
    public async Task<TokenSet?> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/v1/identity/refresh", new RefreshRequest(refreshToken), ApiJsonContext.Default.RefreshRequest, cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest or HttpStatusCode.Forbidden)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync(ApiJsonContext.Default.AccessTokenResponse, cancellationToken);
        return body is null ? null : new TokenSet(body.AccessToken, body.RefreshToken);
    }
}
