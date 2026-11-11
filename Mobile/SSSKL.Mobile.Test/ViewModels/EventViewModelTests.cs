using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Configuration;
using SSSKL.Mobile.Core.Localization;
using SSSKL.Mobile.Core.Models;
using SSSKL.Mobile.Core.Services;
using SSSKL.Mobile.Core.ViewModels;

namespace SSSKL.Mobile.Test.ViewModels;

[TestClass]
public class EventViewModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid EventId = Guid.NewGuid();

    private readonly ISssklApi _api = Substitute.For<ISssklApi>();
    private readonly IAppNavigator _navigator = Substitute.For<IAppNavigator>();
    private readonly IUserDialogs _dialogs = Substitute.For<IUserDialogs>();
    private readonly IPhotoPicker _photoPicker = Substitute.For<IPhotoPicker>();
    private readonly IShareService _share = Substitute.For<IShareService>();
    private readonly IExternalLauncher _launcher = Substitute.For<IExternalLauncher>();
    private readonly AppEndpoints _endpoints = new(new Uri("https://api.test"), new Uri("https://web.test"));
    private CurrentUserService _currentUser = null!;

    private void SignIn(string id = "me", params string[] roles)
    {
        _api.GetMeAsync(Arg.Any<CancellationToken>()).Returns(new CurrentUser { Id = id, FullName = "Ik", Roles = roles.Length == 0 ? [RoleNames.User] : roles });
        _api.GetReactionsAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([]);
        _currentUser = new CurrentUserService(_api);
    }

    private static EventItem Event(Action<EventItem>? _ = null) => new()
    {
        Id = EventId, Title = "Kamp", Description = "<p>Leuk</p>", CreatorId = "creator",
        StartDateTime = new DateTime(2026, 11, 18, 19, 0, 0), EndDateTime = new DateTime(2026, 11, 18, 22, 0, 0),
        AcceptedUsers = [new EventMember("a", "anna", null), new EventMember("b", "bert", null), new EventMember("c", "cas", null), new EventMember("d", "dirk", null)],
        DeclinedUsers = [new EventMember("me", "ik", null)],
        UserResponse = EventResponse.Declined, LocationName = "Clubhuis", Latitude = 521234, Longitude = 5.1
    };

    private EventDetailViewModel Detail()
    {
        var comments = new CommentsViewModel(_api, _currentUser, _navigator, _dialogs, _endpoints, NullLogger<CommentsViewModel>.Instance);
        var sut = new EventDetailViewModel(_api, _currentUser, _navigator, _dialogs, _photoPicker, _share, _launcher, _endpoints,
            new FakeTimeProvider(Now), comments, NullLogger<EventDetailViewModel>.Instance);
        sut.Initialize(EventId);
        return sut;
    }

    [TestMethod]
    public void EventRow_SummarisesTheCard()
    {
        // Act
        var row = EventRow.From(Event() with { EndDateTime = new DateTime(2026, 11, 1) }, Now.UtcDateTime, _endpoints);

        // Assert
        row.DateText.Should().Be("18 nov 2026, 20:00");
        row.IsPast.Should().BeTrue();
        row.IsDeclined.Should().BeTrue();
        row.Attendees.Should().HaveCount(EventRow.AvatarCount);
        row.MoreAttendeesText.Should().Be("+1");
        row.AttendeesText.Should().Be(Texts.AttendeeCount(4));
        row.AccessibleText.Should().Contain(Texts.Past).And.Contain(Texts.NotAttending);
    }

    [TestMethod]
    public async Task Events_FutureOnlyByDefault_AndSwitchingReloads()
    {
        // Arrange
        SignIn();
        _api.GetEventsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new Page<EventItem> { Items = [Event()], TotalCount = 1 });
        await _currentUser.RefreshAsync();
        var sut = new EventsViewModel(_api, _currentUser, _navigator, _dialogs, _endpoints, new FakeTimeProvider(Now), NullLogger<EventsViewModel>.Instance);

        // Act
        await sut.LoadCommand.ExecuteAsync(null);
        await sut.SetFutureOnlyCommand.ExecuteAsync(false);

        // Assert
        await _api.Received(1).GetEventsAsync(Arg.Is(0), Arg.Any<int>(), Arg.Is(true), Arg.Any<CancellationToken>());
        await _api.Received(1).GetEventsAsync(Arg.Is(0), Arg.Any<int>(), Arg.Is(false), Arg.Any<CancellationToken>());
        sut.ShowsAll.Should().BeTrue();
        sut.CanCreate.Should().BeTrue();

        await sut.NewEventCommand.ExecuteAsync(null);
        await _navigator.Received(1).OpenWebAsync("events/new");
    }

    [TestMethod]
    public async Task Detail_ShowsTheEvent()
    {
        // Arrange
        SignIn();
        _api.GetEventAsync(EventId, Arg.Any<CancellationToken>()).Returns(Event() with { RequiredRoles = ["Leiding"] });
        var sut = Detail();

        // Act
        await sut.LoadCommand.ExecuteAsync(null);

        // Assert
        sut.Title.Should().Be("Kamp");
        sut.StartText.Should().Be("woensdag 18 november 2026, 20:00");
        sut.IsDeclined.Should().BeTrue();
        sut.Accepted.Select(m => m.Name).Should().Equal("anna", "bert", "cas", "dirk");
        sut.Declined.Should().ContainSingle().Which.IsSelf.Should().BeTrue();
        sut.RequiredRolesText.Should().Be("Leiding");
        sut.CanRsvp.Should().BeFalse("you don't have the Leiding role");
        sut.ShowsRsvpNoRole.Should().BeTrue();
        sut.CanEdit.Should().BeFalse("someone else created it");
        sut.IsFound.Should().BeTrue();
    }

    [TestMethod]
    public void MapsUrl_ScalesMangledCoordinatesBackLikeTheSpa() =>
        EventDetailViewModel.MapsUrl(Event())!.ToString().Should().Be("https://www.google.com/maps/search/?api=1&query=52.1234,5.1");

    [TestMethod]
    public void MapsUrl_WithoutCoordinates_SearchesTheName() =>
        EventDetailViewModel.MapsUrl(Event() with { Latitude = null, LocationName = "Het Clubhuis" })!.AbsoluteUri.Should()
            .Be("https://www.google.com/maps/search/?api=1&query=Het%20Clubhuis");

    [TestMethod]
    public async Task Rsvp_SendsAndShowsTheNewState()
    {
        // Arrange
        SignIn();
        _api.GetEventAsync(EventId, Arg.Any<CancellationToken>()).Returns(Event(), Event() with { UserResponse = EventResponse.Accepted });
        var sut = Detail();
        await sut.LoadCommand.ExecuteAsync(null);
        sut.CanRsvp.Should().BeTrue();

        // Act
        await sut.RsvpCommand.ExecuteAsync(EventResponse.Accepted);

        // Assert
        await _api.Received(1).RespondToEventAsync(EventId, EventResponse.Accepted, Arg.Any<CancellationToken>());
        await _dialogs.Received(1).ToastAsync(Texts.RsvpAccepted);
        sut.IsAccepted.Should().BeTrue();
    }

    [TestMethod]
    public async Task Kiosk_CannotRsvp()
    {
        // Arrange: the API only lets members (User, Admin) respond.
        SignIn("me", RoleNames.Kiosk);
        _api.GetEventAsync(EventId, Arg.Any<CancellationToken>()).Returns(Event());
        var sut = Detail();

        // Act
        await sut.LoadCommand.ExecuteAsync(null);

        // Assert
        sut.CanRsvp.Should().BeFalse();
    }

    [TestMethod]
    public async Task Creator_CanEditAndDelete()
    {
        // Arrange
        SignIn("creator");
        _api.GetEventAsync(EventId, Arg.Any<CancellationToken>()).Returns(Event());
        _dialogs.ConfirmAsync(default!, default!, default!, default!).ReturnsForAnyArgs(true);
        var sut = Detail();
        await sut.LoadCommand.ExecuteAsync(null);
        sut.CanEdit.Should().BeTrue();

        // Act
        await sut.EditCommand.ExecuteAsync(null);
        await sut.DeleteCommand.ExecuteAsync(null);

        // Assert
        await _navigator.Received(1).OpenWebAsync($"events/{EventId}/edit");
        await _api.Received(1).DeleteEventAsync(EventId, Arg.Any<CancellationToken>());
        await _navigator.Received(1).CloseAsync();
    }

    [TestMethod]
    public async Task ChangeImage_UploadsThePickedPhoto()
    {
        // Arrange
        SignIn("creator");
        _api.GetEventAsync(EventId, Arg.Any<CancellationToken>()).Returns(Event() with { ImageUrl = "/img/e.png" });
        _photoPicker.PickAsync().Returns(new PickedPhoto("kamp.jpg", "image/jpeg", [1, 2, 3]));
        var sut = Detail();
        await sut.LoadCommand.ExecuteAsync(null);

        // Act
        await sut.ChangeImageCommand.ExecuteAsync(null);

        // Assert
        await _api.Received(1).UpdateEventImageAsync(EventId, new Base64FileUpload("kamp.jpg", "image/jpeg", "AQID"), Arg.Any<CancellationToken>());
        sut.ImageUrl.Should().StartWith("https://api.test/img/e.png?v=");
    }

    [TestMethod]
    public async Task ShareAndMaps_UseTheEvent()
    {
        // Arrange
        SignIn();
        _api.GetEventAsync(EventId, Arg.Any<CancellationToken>()).Returns(Event());
        var sut = Detail();
        await sut.LoadCommand.ExecuteAsync(null);

        // Act
        await sut.ShareCommand.ExecuteAsync(null);
        await sut.OpenMapsCommand.ExecuteAsync(null);

        // Assert
        await _share.Received(1).ShareAsync("Kamp", Texts.ShareEventText("Kamp"), new Uri($"https://web.test/events/{EventId}"));
        await _launcher.Received(1).OpenAsync(Arg.Is<Uri>(u => u.ToString().Contains("52.1234")));
    }

    [TestMethod]
    public async Task Detail_Gone_IsNotFound()
    {
        // Arrange
        SignIn();
        _api.GetEventAsync(EventId, Arg.Any<CancellationToken>()).ThrowsAsync(new ApiException(HttpStatusCode.NotFound, "Not Found"));
        var sut = Detail();

        // Act
        await sut.LoadCommand.ExecuteAsync(null);

        // Assert
        sut.IsNotFound.Should().BeTrue();
        await _dialogs.DidNotReceiveWithAnyArgs().AlertAsync(default!, default!);
    }
}
