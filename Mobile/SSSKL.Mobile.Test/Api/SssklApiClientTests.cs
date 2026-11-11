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

    [TestMethod]
    public async Task GetPersonalOrdersAsync_ReadsThePage()
    {
        // Arrange
        var id = Guid.NewGuid();
        var handler = new StubHttpHandler((_, _) => StubHttpHandler.Json(
            $$"""{"items":[{"id":"{{id}}","createdOn":"2026-11-11T09:30:00","productName":"Bier","amount":2,"paid":3.0,"userFullName":"Jan"}],"totalCount":41}"""));

        // Act
        var page = await CreateSut(handler).GetPersonalOrdersAsync(20, 20);

        // Assert
        handler.Requests.Single().Request.RequestUri.Should().Be("https://api.test/api/v1/Order/personal?skip=20&take=20");
        page.TotalCount.Should().Be(41);
        page.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(new OrderItem
        {
            Id = id, CreatedOn = new DateTime(2026, 11, 11, 9, 30, 0), ProductName = "Bier", Amount = 2, Paid = 3.0m
        });
    }

    [TestMethod]
    public async Task DeleteOrderAsync_DeletesIt()
    {
        // Arrange
        var id = Guid.NewGuid();
        var handler = new StubHttpHandler((_, _) => StubHttpHandler.Status(HttpStatusCode.NoContent));

        // Act
        await CreateSut(handler).DeleteOrderAsync(id);

        // Assert
        var request = handler.Requests.Single().Request;
        request.Method.Should().Be(HttpMethod.Delete);
        request.RequestUri.Should().Be($"https://api.test/api/v1/Order/{id}");
    }

    [TestMethod]
    [DataRow(LeaderboardPeriod.Last12Hours, "12hour/")]
    [DataRow(LeaderboardPeriod.Month, "monthly/")]
    [DataRow(LeaderboardPeriod.AllTime, "")]
    public async Task GetLeaderboardAsync_UsesThePeriodsRoute(LeaderboardPeriod period, string route)
    {
        // Arrange
        var product = Guid.NewGuid();
        var handler = new StubHttpHandler((_, _) => StubHttpHandler.Json(
            """[{"position":1,"fullName":"Jan","productName":"Bier","amount":12,"userId":"u1"}]"""));

        // Act
        var entries = await CreateSut(handler).GetLeaderboardAsync(product, period);

        // Assert
        handler.Requests.Single().Request.RequestUri.Should().Be($"https://api.test/api/v1/Leaderboard/{route}{product}");
        entries.Should().ContainSingle().Which.Amount.Should().Be(12);
    }

    [TestMethod]
    public async Task RecalculateStatsAsync_TooSoon_ThrowsWith429()
    {
        // Arrange
        var handler = new StubHttpHandler((_, _) => StubHttpHandler.Status(HttpStatusCode.TooManyRequests));

        // Act
        var act = () => CreateSut(handler).RecalculateStatsAsync("u1");

        // Assert
        (await act.Should().ThrowAsync<ApiException>()).Which.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        var request = handler.Requests.Single().Request;
        request.Method.Should().Be(HttpMethod.Post);
        request.RequestUri.Should().Be("https://api.test/api/v1/ApplicationUser/u1/stats/recalculate");
    }

    [TestMethod]
    public async Task GetAchievementListingAsync_ReadsTheListing()
    {
        // Arrange
        var handler = new StubHttpHandler((_, _) => StubHttpHandler.Json(
            """[{"id":"6f9619ff-8b86-d011-b42d-00cf4fc964ff","name":"Dorstig","description":"10 bier","dateAdded":null,"imageUrl":null,"completed":false,"tier":"Gold","parentAchievementId":null,"parentAchievementName":null}]"""));

        // Act
        var listing = await CreateSut(handler).GetAchievementListingAsync("u 1");

        // Assert
        handler.Requests.Single().Request.RequestUri!.AbsoluteUri.Should().Be("https://api.test/api/v1/Achievement/all-for-user/u%201");
        listing.Should().ContainSingle().Which.Tier.Should().Be("Gold");
    }

    [TestMethod]
    public async Task GetProductsAsync_GetsAllProducts()
    {
        // Arrange
        var handler = new StubHttpHandler((_, _) => StubHttpHandler.Json("""[{"id":"6f9619ff-8b86-d011-b42d-00cf4fc964ff","name":"Bier","price":1.5,"stock":3,"enableLeaderboard":true}]"""));

        // Act
        var products = await CreateSut(handler).GetProductsAsync();

        // Assert
        handler.Requests.Single().Request.RequestUri.Should().Be("https://api.test/api/v1/Product?all=true");
        products.Should().ContainSingle().Which.EnableLeaderboard.Should().BeTrue();
    }

    [TestMethod]
    public async Task GetEventAsync_ReadsTheEvent()
    {
        // Arrange
        var id = Guid.NewGuid();
        var handler = new StubHttpHandler((_, _) => StubHttpHandler.Json($$"""
            {"id":"{{id}}","title":"Kamp","description":"<p>Hoi</p>","startDateTime":"2026-07-01T10:00:00","endDateTime":"2026-07-01T12:00:00",
             "creatorName":"jan","creatorId":"u1","acceptedUsers":[{"userId":"u2","userName":"anna","profilePictureUrl":null}],"declinedUsers":[],
             "userResponse":"Accepted","locationName":"Clubhuis","latitude":52.1,"longitude":5.1,"requiredRoles":["Leiding"]}
            """));

        // Act
        var item = await CreateSut(handler).GetEventAsync(id);

        // Assert
        handler.Requests.Single().Request.RequestUri.Should().Be($"https://api.test/api/v1/Events/{id}");
        item.UserResponse.Should().Be(EventResponse.Accepted);
        item.AcceptedUsers.Should().ContainSingle().Which.UserName.Should().Be("anna");
        item.RequiredRoles.Should().Equal("Leiding");
    }

    [TestMethod]
    public async Task GetEventsAsync_PassesFutureOnly()
    {
        // Arrange
        var handler = new StubHttpHandler((_, _) => StubHttpHandler.Json("""{"items":[],"totalCount":0}"""));

        // Act
        await CreateSut(handler).GetEventsAsync(20, 20, futureOnly: true);

        // Assert
        handler.Requests.Single().Request.RequestUri.Should().Be("https://api.test/api/v1/Events?skip=20&take=20&futureOnly=true");
    }

    [TestMethod]
    public async Task RespondToEventAsync_PostsTheStatusAsString()
    {
        // Arrange
        var id = Guid.NewGuid();
        var handler = new StubHttpHandler((_, _) => StubHttpHandler.Status(HttpStatusCode.OK));

        // Act
        await CreateSut(handler).RespondToEventAsync(id, EventResponse.Declined);

        // Assert
        var (request, body) = handler.Requests.Single();
        request.RequestUri.Should().Be($"https://api.test/api/v1/Events/{id}/rsvp");
        body.Should().Be("""{"status":"Declined"}""");
    }

    [TestMethod]
    public async Task ToggleQuoteVoteAsync_ReturnsWhetherYouVoted()
    {
        // Arrange
        var id = Guid.NewGuid();
        var handler = new StubHttpHandler((_, _) => StubHttpHandler.Json("true"));

        // Act
        var voted = await CreateSut(handler).ToggleQuoteVoteAsync(id);

        // Assert
        voted.Should().BeTrue();
        handler.Requests.Single().Request.RequestUri.Should().Be($"https://api.test/api/v1/Quote/{id}/vote");
    }

    [TestMethod]
    public async Task Reactions_GetToggleAndDelete()
    {
        // Arrange
        var target = Guid.NewGuid();
        var handler = new StubHttpHandler((request, _) => request.Method == HttpMethod.Get
            ? StubHttpHandler.Json("""[{"id":"6f9619ff-8b86-d011-b42d-00cf4fc964ff","userId":"u1","userName":"jan","content":"Hoi","targetId":"6f9619ff-8b86-d011-b42d-00cf4fc964ff","targetType":"Event","createdOn":"2026-07-01T10:00:00","reactions":[]}]""")
            : StubHttpHandler.Status(HttpStatusCode.OK));
        var sut = CreateSut(handler);

        // Act
        var reactions = await sut.GetReactionsAsync(target, ReactionTarget.Event);
        await sut.ToggleReactionAsync(new ToggleReaction(target, ReactionTarget.Quote, "👍"));
        await sut.DeleteReactionAsync(target);

        // Assert
        reactions.Should().ContainSingle().Which.Content.Should().Be("Hoi");
        handler.Requests[0].Request.RequestUri.Should().Be($"https://api.test/api/v1/Reactions/{target}/Event");
        handler.Requests[1].Request.RequestUri.Should().Be("https://api.test/api/v1/Reactions/toggle");
        handler.Requests[1].Body.Should().Be($$"""{"targetId":"{{target}}","targetType":"Quote","content":"\uD83D\uDC4D"}""");
        handler.Requests[2].Request.Method.Should().Be(HttpMethod.Delete);
    }
}
