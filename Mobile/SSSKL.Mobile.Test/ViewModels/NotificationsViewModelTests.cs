using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Localization;
using SSSKL.Mobile.Core.Models;
using SSSKL.Mobile.Core.Services;
using SSSKL.Mobile.Core.ViewModels;

namespace SSSKL.Mobile.Test.ViewModels;

[TestClass]
public class NotificationsViewModelTests
{
    private readonly ISssklApi _api = Substitute.For<ISssklApi>();
    private readonly IAppNavigator _navigator = Substitute.For<IAppNavigator>();
    private readonly IUserDialogs _dialogs = Substitute.For<IUserDialogs>();
    private NotificationInbox _center = null!;

    [TestInitialize]
    public void Setup()
    {
        _center = new NotificationInbox(_api, NullLogger<NotificationInbox>.Instance);
        _api.GetUnreadNotificationCountAsync(Arg.Any<CancellationToken>()).Returns(1);
    }

    private NotificationsViewModel CreateSut() =>
        new(_api, _center, _navigator, _dialogs, NullLogger<NotificationsViewModel>.Instance);

    private static NotificationItem Item(bool isRead = false, string? link = null, Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(), Title = "Titel", Message = "Bericht", IsRead = isRead, LinkUri = link,
        CreatedOn = new DateTime(2026, 11, 11, 9, 30, 0)
    };

    private static List<NotificationItem> Page(int count) => Enumerable.Range(0, count).Select(_ => Item(isRead: true)).ToList();

    [TestMethod]
    public async Task LoadAsync_ShowsTheFirstPageAndTheCount()
    {
        // Arrange
        _api.GetNotificationsAsync(0, NotificationsViewModel.PageSize, Arg.Any<CancellationToken>()).Returns([Item(), Item(isRead: true)]);
        var sut = CreateSut();

        // Act
        await sut.LoadCommand.ExecuteAsync(null);

        // Assert
        sut.Items.Should().HaveCount(2);
        sut.HasMore.Should().BeFalse();
        sut.IsEmpty.Should().BeFalse();
        sut.CanMarkAll.Should().BeTrue();
        _center.UnreadCount.Should().Be(1);
    }

    [TestMethod]
    public async Task LoadAsync_Nothing_ShowsTheEmptyState()
    {
        // Arrange
        _api.GetNotificationsAsync(Arg.Is(0), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        _api.GetUnreadNotificationCountAsync(Arg.Any<CancellationToken>()).Returns(0);
        var sut = CreateSut();
        sut.IsEmpty.Should().BeFalse("nothing is shown before the first load");

        // Act
        await sut.LoadCommand.ExecuteAsync(null);

        // Assert
        sut.IsEmpty.Should().BeTrue();
        sut.CanMarkAll.Should().BeFalse();
    }

    [TestMethod]
    public async Task LoadAsync_FirstLoadFails_ShowsTheErrorState()
    {
        // Arrange
        _api.GetNotificationsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("offline"));
        var sut = CreateSut();
        sut.IsRefreshing = true;

        // Act
        await sut.LoadCommand.ExecuteAsync(null);

        // Assert
        sut.HasLoadError.Should().BeTrue();
        await _dialogs.DidNotReceive().AlertAsync(Arg.Any<string>(), Arg.Any<string>());
        sut.IsRefreshing.Should().BeFalse();
        sut.IsLoading.Should().BeFalse();
    }

    [TestMethod]
    public async Task LoadMoreCommand_AppendsTheNextPageWithoutDuplicates()
    {
        // Arrange
        var first = Page(NotificationsViewModel.PageSize);
        _api.GetNotificationsAsync(0, NotificationsViewModel.PageSize, Arg.Any<CancellationToken>()).Returns(first);
        // A new notification arrived meanwhile, so the last one of page 1 comes again.
        _api.GetNotificationsAsync(NotificationsViewModel.PageSize, NotificationsViewModel.PageSize, Arg.Any<CancellationToken>())
            .Returns([first[^1], Item(isRead: true)]);
        var sut = CreateSut();
        await sut.LoadCommand.ExecuteAsync(null);
        sut.HasMore.Should().BeTrue();

        // Act
        await sut.LoadMoreCommand.ExecuteAsync(null);

        // Assert
        sut.Items.Should().HaveCount(NotificationsViewModel.PageSize + 1);
        sut.Items.Select(i => i.Id).Should().OnlyHaveUniqueItems();
        sut.HasMore.Should().BeFalse();
    }

    [TestMethod]
    public async Task LoadMoreCommand_NoMorePages_DoesNotCall()
    {
        // Arrange
        _api.GetNotificationsAsync(Arg.Is(0), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([Item()]);
        var sut = CreateSut();
        await sut.LoadCommand.ExecuteAsync(null);

        // Act
        await sut.LoadMoreCommand.ExecuteAsync(null);

        // Assert
        await _api.Received(1).GetNotificationsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task OpenCommand_Unread_MarksItReadAndOpensTheLink()
    {
        // Arrange
        var item = Item(link: "/achievements");
        _api.GetNotificationsAsync(Arg.Is(0), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([item]);
        var sut = CreateSut();
        await sut.LoadCommand.ExecuteAsync(null);

        // Act
        await sut.OpenCommand.ExecuteAsync(sut.Items[0]);

        // Assert
        sut.Items[0].IsRead.Should().BeTrue();
        _center.UnreadCount.Should().Be(0);
        await _api.Received(1).MarkNotificationReadAsync(item.Id, Arg.Any<CancellationToken>());
        await _navigator.Received(1).OpenAsync("/achievements");
    }

    [TestMethod]
    public async Task OpenCommand_ReadWithoutLink_DoesNothing()
    {
        // Arrange
        _api.GetNotificationsAsync(Arg.Is(0), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([Item(isRead: true)]);
        var sut = CreateSut();
        await sut.LoadCommand.ExecuteAsync(null);

        // Act
        await sut.OpenCommand.ExecuteAsync(sut.Items[0]);

        // Assert
        await _api.DidNotReceiveWithAnyArgs().MarkNotificationReadAsync(default, default);
        await _navigator.DidNotReceiveWithAnyArgs().OpenAsync(default!);
    }

    [TestMethod]
    public async Task OpenCommand_MarkReadFails_UndoesItAndStillOpensTheLink()
    {
        // Arrange
        _api.GetNotificationsAsync(Arg.Is(0), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([Item(link: "/quotes")]);
        _api.MarkNotificationReadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("offline"));
        var sut = CreateSut();
        await sut.LoadCommand.ExecuteAsync(null);

        // Act
        await sut.OpenCommand.ExecuteAsync(sut.Items[0]);

        // Assert
        sut.Items[0].IsRead.Should().BeFalse();
        _center.UnreadCount.Should().Be(1, "the count is reloaded from the server");
        await _navigator.Received(1).OpenAsync("/quotes");
    }

    [TestMethod]
    public async Task MarkAllReadCommand_MarksEverythingAndClearsTheBadge()
    {
        // Arrange
        _api.GetNotificationsAsync(Arg.Is(0), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([Item(), Item()]);
        var sut = CreateSut();
        await sut.LoadCommand.ExecuteAsync(null);

        // Act
        await sut.MarkAllReadCommand.ExecuteAsync(null);

        // Assert
        await _api.Received(1).MarkAllNotificationsReadAsync(Arg.Any<CancellationToken>());
        sut.Items.Should().OnlyContain(i => i.IsRead);
        _center.UnreadCount.Should().Be(0);
        sut.CanMarkAll.Should().BeFalse();
    }

    [TestMethod]
    public async Task MarkAllReadCommand_Fails_ShowsAnAlertAndKeepsThemUnread()
    {
        // Arrange
        _api.GetNotificationsAsync(Arg.Is(0), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([Item()]);
        _api.MarkAllNotificationsReadAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("offline"));
        var sut = CreateSut();
        await sut.LoadCommand.ExecuteAsync(null);

        // Act
        await sut.MarkAllReadCommand.ExecuteAsync(null);

        // Assert
        await _dialogs.Received(1).AlertAsync(Texts.Error, Texts.MarkAllReadFailed);
        sut.Items[0].IsRead.Should().BeFalse();
        _center.UnreadCount.Should().Be(1);
    }

    [TestMethod]
    public void NotificationItem_ShowsUtcAsLocalTimeInDutch()
    {
        // Arrange
        var amsterdam = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");

        // Act
        var sut = new NotificationItemViewModel(Item(), amsterdam);

        // Assert: 09:30 UTC is 10:30 in Amsterdam in November.
        sut.DateText.Should().Be("11 nov 2026, 10:30");
        sut.Icon.Should().Be(Icons.Bell);
        sut.Description.Should().StartWith(Texts.Unread);
    }
}
