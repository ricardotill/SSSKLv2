using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using FluentAssertions;
using SSSKL.Domain.Constants;
using SSSKL.Web.Test.Integration.Infrastructure;

namespace SSSKL.Web.Test.Integration;

/// <summary>
/// Guards the [Authorize] attributes: each row is a request that must be rejected for the given caller.
/// Bodies are minimal because authorization runs before model binding.
/// </summary>
[TestClass]
public class AuthorizationMatrixTests : IntegrationTestBase
{
    private const string Anonymous = "Anonymous";

    private static SssklWebApplicationFactory _factory = null!;
    private static readonly ConcurrentDictionary<string, string> Tokens = new();
    protected override SssklWebApplicationFactory Factory => _factory;

    [ClassInitialize]
    public static void Initialize(TestContext _) => _factory = new SssklWebApplicationFactory();

    [ClassCleanup]
    public static async Task Cleanup()
    {
        Tokens.Clear();
        await _factory.DisposeAsync();
    }

    public static IEnumerable<object[]> Rows()
    {
        var id = Guid.NewGuid();
        // Anonymous callers are challenged on authenticated endpoints.
        yield return ["GET", "/api/v1/order/personal", Anonymous, HttpStatusCode.Unauthorized];
        yield return ["GET", "/api/v1/order/initialize", Anonymous, HttpStatusCode.Unauthorized];
        yield return ["GET", "/api/v1/announcement", Anonymous, HttpStatusCode.Unauthorized];
        yield return ["GET", "/api/v1/events", Anonymous, HttpStatusCode.Unauthorized];
        yield return ["GET", "/api/v1/notifications", Anonymous, HttpStatusCode.Unauthorized];
        yield return ["GET", "/api/v1/applicationuser/me", Anonymous, HttpStatusCode.Unauthorized];
        yield return ["GET", "/api/v1/topup/personal", Anonymous, HttpStatusCode.Unauthorized];
        yield return ["POST", "/api/v1/quote", Anonymous, HttpStatusCode.Unauthorized];
        // Admin-only endpoints reject regular users.
        yield return ["GET", "/api/v1/order", Roles.User, HttpStatusCode.Forbidden];
        yield return ["POST", "/api/v1/order/export/csv", Roles.User, HttpStatusCode.Forbidden];
        yield return ["GET", "/api/v1/topup", Roles.User, HttpStatusCode.Forbidden];
        yield return ["POST", "/api/v1/topup", Roles.User, HttpStatusCode.Forbidden];
        yield return ["DELETE", $"/api/v1/topup/{id}", Roles.User, HttpStatusCode.Forbidden];
        yield return ["POST", "/api/v1/product", Roles.User, HttpStatusCode.Forbidden];
        yield return ["PUT", $"/api/v1/product/{id}", Roles.User, HttpStatusCode.Forbidden];
        yield return ["DELETE", $"/api/v1/product/{id}", Roles.User, HttpStatusCode.Forbidden];
        yield return ["POST", "/api/v1/announcement", Roles.User, HttpStatusCode.Forbidden];
        yield return ["GET", "/api/v1/roles/admin", Roles.User, HttpStatusCode.Forbidden];
        yield return ["POST", "/api/v1/roles", Roles.User, HttpStatusCode.Forbidden];
        yield return ["GET", "/api/v1/applicationuser/admin", Roles.User, HttpStatusCode.Forbidden];
        yield return ["POST", "/api/v1/applicationuser/stats/recalculate-all", Roles.User, HttpStatusCode.Forbidden];
        yield return ["POST", "/api/v1/notifications/custom", Roles.User, HttpStatusCode.Forbidden];
        yield return ["PUT", "/api/v1/globalsettings/QuotesFeatureAllowedRoles", Roles.User, HttpStatusCode.Forbidden];
        // Kiosk accounts can order but not create events or manage stock.
        yield return ["POST", "/api/v1/events", Roles.Kiosk, HttpStatusCode.Forbidden];
        yield return ["POST", "/api/v1/product", Roles.Kiosk, HttpStatusCode.Forbidden];
        yield return ["POST", "/api/v1/topup", Roles.Kiosk, HttpStatusCode.Forbidden];
        // Guests (unconfirmed members) can't order, see roles or create events.
        yield return ["POST", "/api/v1/order", Roles.Guest, HttpStatusCode.Forbidden];
        yield return ["GET", "/api/v1/roles", Roles.Guest, HttpStatusCode.Forbidden];
        yield return ["POST", "/api/v1/events", Roles.Guest, HttpStatusCode.Forbidden];
        yield return ["POST", $"/api/v1/events/{id}/rsvp", Roles.Guest, HttpStatusCode.Forbidden];
    }

    public static string RowName(System.Reflection.MethodInfo _, object?[]? data) =>
        data is null ? "row" : $"{data[2]} {data[0]} {data[1]} -> {(int)(HttpStatusCode)data[3]!}";

    [TestMethod]
    [DynamicData(nameof(Rows), DynamicDataDisplayName = nameof(RowName))]
    public async Task Endpoint_RejectsCaller(string method, string url, string caller, HttpStatusCode expected)
    {
        var client = AnonymousClient();
        if (caller != Anonymous)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await TokenFor(caller));
        }

        using var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (url == "/api/v1/events")
        {
            // [Consumes("multipart/form-data")] answers 415 before authorization for other content types.
            request.Content = new MultipartFormDataContent { { new StringContent("x"), "Title" } };
        }
        else if (method is "POST" or "PUT")
        {
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        }

        var response = await client.SendAsync(request, CT);

        response.StatusCode.Should().Be(expected, await Describe(response));
    }

    private async Task<string> TokenFor(string role)
    {
        if (Tokens.TryGetValue(role, out var token)) return token;
        token = await GetAccessTokenAsync(await CreateUserAsync(role));
        return Tokens.GetOrAdd(role, token);
    }
}
