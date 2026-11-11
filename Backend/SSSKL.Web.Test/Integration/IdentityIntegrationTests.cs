using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SSSKL.Domain.Constants;
using SSSKL.Web.Test.Integration.Infrastructure;
using SSSKL.Web.Test.Integration.Infrastructure.Fakes;

namespace SSSKL.Web.Test.Integration;

[TestClass]
public class IdentityIntegrationTests : IntegrationTestBase
{
    private const string NewPassword = "N3w-P@ssw0rd!";

    private static SssklWebApplicationFactory _factory = null!;
    protected override SssklWebApplicationFactory Factory => _factory;

    [ClassInitialize]
    public static void Initialize(TestContext _) => _factory = new SssklWebApplicationFactory();

    [ClassCleanup]
    public static async Task Cleanup() => await _factory.DisposeAsync();

    private static string UniqueEmail() => $"id-{Guid.NewGuid():N}"[..11] + "@example.com";

    private Task<HttpResponseMessage> LoginRequestAsync(string userName, string password, string query = "") =>
        AnonymousClient().PostAsJsonAsync($"/api/v1/identity/login{query}", new { userName, password }, CT);

    [TestMethod]
    public async Task Login_ValidCredentials_ReturnsAccessAndRefreshToken()
    {
        var user = await CreateUserAsync(Roles.User);

        var response = await LoginRequestAsync(user.UserName!, Password);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync<JsonElement>(response);
        body.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public async Task Login_WrongPassword_Returns401()
    {
        var user = await CreateUserAsync(Roles.User);

        (await LoginRequestAsync(user.UserName!, "Wrong-P@ss1")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task Login_UnconfirmedEmail_Returns401()
    {
        var user = await CreateUserAsync(Roles.User, emailConfirmed: false);

        (await LoginRequestAsync(user.UserName!, Password)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task Register_ThenConfirmEmail_ThenLogin_AsGuest()
    {
        var email = UniqueEmail();
        var client = AnonymousClient();

        var register = await client.PostAsJsonAsync("/api/v1/identity/register",
            new { email, userName = email, password = Password, name = "Nieuw", surname = "Lid" }, CT);
        register.StatusCode.Should().Be(HttpStatusCode.OK, await Describe(register));

        (await LoginRequestAsync(email, Password)).StatusCode.Should().Be(HttpStatusCode.Unauthorized, "the email is not confirmed yet");

        var link = Factory.Emails.LastFor(email, FakeEmailSender.Confirm)!.Content;
        link.Should().Contain("/confirm-email");
        var query = FakeEmailSender.Query(link);
        var confirm = await client.GetAsync($"/api/v1/identity/confirmEmail?userId={query["userId"]}&code={query["code"]}", CT);
        confirm.StatusCode.Should().Be(HttpStatusCode.OK, await Describe(confirm));

        var login = await LoginRequestAsync(email, Password);
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var authed = AnonymousClient();
        authed.DefaultRequestHeaders.Authorization = new("Bearer", (await ReadJsonAsync<JsonElement>(login)).GetProperty("accessToken").GetString());
        var me = await ReadJsonAsync<JsonElement>(await authed.GetAsync("/api/v1/applicationuser/me", CT));
        me.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).Should().Equal(Roles.Guest);
    }

    [TestMethod]
    public async Task ConfirmEmail_BadCodeOrUnknownUser_Returns401()
    {
        var user = await CreateUserAsync(Roles.User, emailConfirmed: false);
        var client = AnonymousClient();

        (await client.GetAsync($"/api/v1/identity/confirmEmail?userId={user.Id}&code=bm90LWEtY29kZQ", CT)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.GetAsync($"/api/v1/identity/confirmEmail?userId={Guid.NewGuid()}&code=bm90LWEtY29kZQ", CT)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task ForgotPassword_ThenResetPassword_ChangesPassword()
    {
        var user = await CreateUserAsync(Roles.User);
        var client = AnonymousClient();

        (await client.PostAsJsonAsync("/api/v1/identity/forgotPassword", new { email = user.Email }, CT)).StatusCode.Should().Be(HttpStatusCode.OK);
        var link = Factory.Emails.LastFor(user.Email!, FakeEmailSender.ResetLink)!.Content;
        link.Should().Contain("/reset-password");
        var resetCode = FakeEmailSender.Query(link)["resetCode"];

        var reset = await client.PostAsJsonAsync("/api/v1/identity/resetPassword", new { email = user.Email, resetCode, newPassword = NewPassword }, CT);
        reset.StatusCode.Should().Be(HttpStatusCode.OK, await Describe(reset));

        (await LoginRequestAsync(user.UserName!, Password)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await LoginRequestAsync(user.UserName!, NewPassword)).StatusCode.Should().Be(HttpStatusCode.OK);

        var reused = await client.PostAsJsonAsync("/api/v1/identity/resetPassword", new { email = user.Email, resetCode, newPassword = "An0ther-P@ss!" }, CT);
        reused.StatusCode.Should().Be(HttpStatusCode.BadRequest, "a reset code can only be used once");
    }

    [TestMethod]
    public async Task ResetPassword_InvalidCode_Returns400()
    {
        var user = await CreateUserAsync(Roles.User);

        var response = await AnonymousClient().PostAsJsonAsync("/api/v1/identity/resetPassword",
            new { email = user.Email, resetCode = "bm90LWEtY29kZQ", newPassword = NewPassword }, CT);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task ForgotPassword_UnknownEmail_Returns200WithoutSendingEmail()
    {
        var email = UniqueEmail();

        var response = await AnonymousClient().PostAsJsonAsync("/api/v1/identity/forgotPassword", new { email }, CT);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Factory.Emails.Sent.Should().NotContain(e => e.To == email);
    }

    [TestMethod]
    public async Task Refresh_ValidToken_ReturnsNewAccessToken_GarbageReturns401()
    {
        var user = await CreateUserAsync(Roles.User);
        var login = await ReadJsonAsync<JsonElement>(await LoginRequestAsync(user.UserName!, Password));
        var client = AnonymousClient();

        var refresh = await client.PostAsJsonAsync("/api/v1/identity/refresh", new { refreshToken = login.GetProperty("refreshToken").GetString() }, CT);
        refresh.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadJsonAsync<JsonElement>(refresh)).GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();

        (await client.PostAsJsonAsync("/api/v1/identity/refresh", new { refreshToken = "garbage" }, CT)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task Logout_WithCookies_ExpiresAuthCookie()
    {
        var user = await CreateUserAsync(Roles.User);
        var client = Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        (await client.PostAsJsonAsync("/api/v1/identity/login?useCookies=true", new { userName = user.UserName, password = Password }, CT))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/api/v1/applicationuser/me", CT)).StatusCode.Should().Be(HttpStatusCode.OK);

        var logout = await client.PostAsync("/api/v1/identity/logout", null, CT);

        logout.StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/api/v1/applicationuser/me", CT)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task ManageInfo_Anonymous_Returns401()
    {
        (await AnonymousClient().GetAsync("/api/v1/identity/manage/info", CT)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
