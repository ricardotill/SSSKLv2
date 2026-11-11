using System.Net;
using FluentAssertions;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Models;
using SSSKL.Mobile.Test.Util;

namespace SSSKL.Mobile.Test.Api;

[TestClass]
public class SssklApiClientTests
{
    private static SssklApiClient CreateSut(StubHttpHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") });

    [TestMethod]
    [DataRow(HttpStatusCode.NoContent)]
    [DataRow(HttpStatusCode.NotFound)]
    public async Task GetGlobalSettingAsync_UnsetSetting_ReturnsNull(HttpStatusCode status)
    {
        // Arrange
        var handler = new StubHttpHandler((_, _) => StubHttpHandler.Status(status));

        // Act
        var result = await CreateSut(handler).GetGlobalSettingAsync("WhatsNewContent");

        // Assert
        result.Should().BeNull();
        handler.Requests.Single().Request.RequestUri.Should().Be("https://api.test/api/v1/GlobalSettings/WhatsNewContent");
    }

    [TestMethod]
    public async Task GetGlobalSettingAsync_EmptyOkBody_ReturnsNull()
    {
        // Arrange
        var handler = new StubHttpHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([]) });

        // Act
        var result = await CreateSut(handler).GetGlobalSettingAsync("WhatsNewContent");

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetGlobalSettingAsync_Set_ReturnsSetting()
    {
        // Arrange
        var handler = new StubHttpHandler((_, _) =>
            StubHttpHandler.Json("""{"key":"WhatsNewContent","value":"<p>Nieuw</p>","updatedOn":"2026-11-01T10:00:00Z"}"""));

        // Act
        var result = await CreateSut(handler).GetGlobalSettingAsync("WhatsNewContent");

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be("<p>Nieuw</p>");
        result.UpdatedOn.Should().Be("2026-11-01T10:00:00Z");
    }

    [TestMethod]
    public async Task GetGlobalSettingAsync_ServerError_Throws()
    {
        // Arrange
        var handler = new StubHttpHandler((_, _) => StubHttpHandler.Status(HttpStatusCode.InternalServerError));

        // Act
        var act = () => CreateSut(handler).GetGlobalSettingAsync("WhatsNewContent");

        // Assert
        await act.Should().ThrowAsync<ApiException>();
    }

    [TestMethod]
    public async Task UpdateMeAsync_PutsTheChanges()
    {
        // Arrange
        var handler = new StubHttpHandler((_, _) => StubHttpHandler.Status(HttpStatusCode.NoContent));

        // Act
        await CreateSut(handler).UpdateMeAsync(new SelfUpdate("Jan", null, "06"));

        // Assert
        var (request, body) = handler.Requests.Single();
        request.Method.Should().Be(HttpMethod.Put);
        request.RequestUri.Should().Be("https://api.test/api/v1/ApplicationUser/me");
        body.Should().Be("""{"name":"Jan","surname":null,"phoneNumber":"06","description":null}""");
    }

    [TestMethod]
    public async Task UpdateMeAsync_Error_Throws()
    {
        // Arrange
        var handler = new StubHttpHandler((_, _) => StubHttpHandler.Status(HttpStatusCode.BadRequest));

        // Act
        var act = () => CreateSut(handler).UpdateMeAsync(new SelfUpdate("Jan", null, null));

        // Assert
        await act.Should().ThrowAsync<ApiException>();
    }

    [TestMethod]
    public async Task GetTwoFactorStatusAsync_PostsAnEmptyRequest()
    {
        // Arrange
        var handler = new StubHttpHandler((_, _) => StubHttpHandler.Json("""{"sharedKey":"K","isTwoFactorEnabled":true}"""));

        // Act
        var result = await CreateSut(handler).GetTwoFactorStatusAsync();

        // Assert
        result.IsTwoFactorEnabled.Should().BeTrue();
        var (request, body) = handler.Requests.Single();
        request.Method.Should().Be(HttpMethod.Post);
        request.RequestUri.Should().Be("https://api.test/api/v1/identity/manage/2fa");
        body.Should().Be("{}");
    }

    [TestMethod]
    public async Task GetUnreadNotificationCountAsync_ReadsTheNumber()
    {
        // Arrange
        var handler = new StubHttpHandler((_, _) => StubHttpHandler.Json("4"));

        // Act & Assert
        (await CreateSut(handler).GetUnreadNotificationCountAsync()).Should().Be(4);
        handler.Requests.Single().Request.RequestUri.Should().Be("https://api.test/api/v1/Notifications/unread-count");
    }

    [TestMethod]
    [DataRow(HttpStatusCode.OK, true)]
    [DataRow(HttpStatusCode.Forbidden, false)]
    [DataRow(HttpStatusCode.InternalServerError, true)]
    public async Task CanAccessQuotesAsync_OnlyForbiddenHidesQuotes(HttpStatusCode status, bool expected)
    {
        // Arrange
        var handler = new StubHttpHandler((_, _) => StubHttpHandler.Status(status));

        // Act & Assert
        (await CreateSut(handler).CanAccessQuotesAsync()).Should().Be(expected);
        handler.Requests.Single().Request.RequestUri.Should().Be("https://api.test/api/v1/Quote?skip=0&take=1");
    }

    [TestMethod]
    public async Task GetNotificationsAsync_GetsThePage()
    {
        // Arrange
        var id = Guid.NewGuid();
        var handler = new StubHttpHandler((_, _) => StubHttpHandler.Json(
            $$"""[{"id":"{{id}}","userId":"u1","title":"Nieuwe quote","message":"Jan heeft je getagd","isRead":false,"linkUri":"/quotes","createdOn":"2026-11-11T09:30:00"}]"""));

        // Act
        var result = await CreateSut(handler).GetNotificationsAsync(20, 20);

        // Assert
        handler.Requests.Single().Request.RequestUri.Should().Be("https://api.test/api/v1/Notifications?unreadOnly=false&skip=20&take=20");
        result.Should().ContainSingle().Which.Should().BeEquivalentTo(new NotificationItem
        {
            Id = id, Title = "Nieuwe quote", Message = "Jan heeft je getagd", IsRead = false, LinkUri = "/quotes",
            CreatedOn = new DateTime(2026, 11, 11, 9, 30, 0)
        });
    }

    [TestMethod]
    public async Task MarkNotificationReadAsync_PostsToTheNotification()
    {
        // Arrange
        var id = Guid.NewGuid();
        var handler = new StubHttpHandler((_, _) => StubHttpHandler.Status(HttpStatusCode.OK));

        // Act
        await CreateSut(handler).MarkNotificationReadAsync(id);

        // Assert
        var request = handler.Requests.Single().Request;
        request.Method.Should().Be(HttpMethod.Post);
        request.RequestUri.Should().Be($"https://api.test/api/v1/Notifications/{id}/read");
    }

    [TestMethod]
    public async Task MarkAllNotificationsReadAsync_Error_Throws()
    {
        // Arrange
        var handler = new StubHttpHandler((_, _) => StubHttpHandler.Status(HttpStatusCode.InternalServerError));

        // Act
        var act = () => CreateSut(handler).MarkAllNotificationsReadAsync();

        // Assert
        await act.Should().ThrowAsync<ApiException>();
        var request = handler.Requests.Single().Request;
        request.Method.Should().Be(HttpMethod.Post);
        request.RequestUri.Should().Be("https://api.test/api/v1/Notifications/read-all");
    }
}
