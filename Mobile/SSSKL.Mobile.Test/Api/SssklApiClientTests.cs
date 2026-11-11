using System.Net;
using FluentAssertions;
using SSSKL.Mobile.Core.Api;
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
}
