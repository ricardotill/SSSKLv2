using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Configuration;
using SSSKL.Mobile.Core.Localization;
using SSSKL.Mobile.Core.Models;
using SSSKL.Mobile.Core.Popups;
using SSSKL.Mobile.Core.Services;
using SSSKL.Mobile.Core.ViewModels;

namespace SSSKL.Mobile.Test.ViewModels;

[TestClass]
public class PosViewModelTests
{
    private static readonly string MeId = Guid.NewGuid().ToString();
    private static readonly string OtherId = Guid.NewGuid().ToString();
    private static readonly OrderProduct Beer = new(Guid.NewGuid(), "Bier", 10, 1.00m);
    private static readonly OrderProduct Cola = new(Guid.NewGuid(), "Cola", 10, 0.80m);

    private readonly ISssklApi _api = Substitute.For<ISssklApi>();
    private readonly IUserDialogs _dialogs = Substitute.For<IUserDialogs>();
    private readonly IPopupPresenter _presenter = Substitute.For<IPopupPresenter>();
    private readonly IHaptics _haptics = Substitute.For<IHaptics>();

    private async Task<PosViewModel> CreateLoadedSutAsync(params string[] roles)
    {
        _api.GetMeAsync().Returns(new CurrentUser { Id = MeId, FullName = "Ik Zelf", Saldo = -2.50m, Roles = roles.Length == 0 ? [RoleNames.User] : roles });
        _api.GetOrderInitializeAsync().Returns(new OrderInitialize
        {
            Products = [Beer, Cola],
            Users = [new OrderUser(OtherId, "Ander Persoon", "/api/v1/blob/profilepicture/image/1"), new OrderUser(MeId, "Ik Zelf", null)]
        });
        _api.GetUnseenAchievementsAsync().Returns([]);

        var sut = new PosViewModel(
            _api,
            new CurrentUserService(_api),
            new AchievementPopupCoordinator(_api, _presenter, new FakeTimeProvider(), NullLogger<AchievementPopupCoordinator>.Instance),
            _dialogs,
            _haptics,
            new AppEndpoints(new Uri("https://api.test"), new Uri("https://web.test")),
            NullLogger<PosViewModel>.Instance);
        await sut.LoadAsync();
        return sut;
    }

    [TestMethod]
    public async Task LoadAsync_PutsSelfFirstAndPreselects()
    {
        // Act
        var sut = await CreateLoadedSutAsync();

        // Assert
        sut.VisibleUsers.Select(u => u.Id).Should().Equal(MeId, OtherId);
        sut.VisibleUsers[0].IsSelected.Should().BeTrue();
        sut.VisibleUsers[1].IsSelected.Should().BeFalse();
        sut.VisibleUsers[1].PictureUrl.Should().Be("https://api.test/api/v1/blob/profilepicture/image/1");
        sut.SelectedUserCount.Should().Be(1);
        sut.CanSubmit.Should().BeFalse("no product is selected yet");
        sut.SummaryText.Should().Be(Texts.PosSelectHint);
    }

    [TestMethod]
    public async Task LoadAsync_YourRowSaysJezelfWithYourSaldo()
    {
        // Act
        var sut = await CreateLoadedSutAsync();

        // Assert
        var self = sut.VisibleUsers[0];
        self.IsSelf.Should().BeTrue();
        self.Title.Should().Be(Texts.Yourself);
        self.SaldoText.Should().Be("€ -2,50");
        self.IsSaldoNegative.Should().BeTrue();
        self.Initials.Should().Be("IZ", "the avatar still shows your own initials");
        sut.VisibleUsers[1].Title.Should().Be("Ander Persoon");
        sut.VisibleUsers[1].HasSaldo.Should().BeFalse();
    }

    [TestMethod]
    public async Task UserQuery_FiltersYourOwnRowToo()
    {
        // Arrange
        var sut = await CreateLoadedSutAsync();

        // Act
        sut.UserQuery = "ander";

        // Assert
        sut.VisibleUsers.Select(u => u.Id).Should().Equal(OtherId);
        sut.UserQuery = "zelf";
        sut.VisibleUsers.Select(u => u.Id).Should().Equal(MeId);
        sut.UserQuery = "niemand";
        sut.VisibleUsers.Should().BeEmpty();
        sut.UserQuery = "";
        sut.VisibleUsers.Select(u => u.Id).Should().Equal(MeId, OtherId);
    }

    [TestMethod]
    public async Task LoadAsync_Kiosk_PreselectsNobody()
    {
        // Act
        var sut = await CreateLoadedSutAsync(RoleNames.Kiosk);

        // Assert
        sut.SelectedUserCount.Should().Be(0);
    }

    [TestMethod]
    public async Task ToggleUser_BackToOne_TurnsSplitOff()
    {
        // Arrange
        var sut = await CreateLoadedSutAsync();
        sut.ToggleUserCommand.Execute(sut.VisibleUsers[1]);
        sut.Split = true;

        // Act
        sut.ToggleUserCommand.Execute(sut.VisibleUsers[1]);

        // Assert
        sut.Split.Should().BeFalse();
        sut.CanSplit.Should().BeFalse();
    }

    [TestMethod]
    public async Task Split_WithOneUser_CannotBeEnabled()
    {
        // Arrange
        var sut = await CreateLoadedSutAsync();

        // Act
        sut.Split = true;

        // Assert
        sut.Split.Should().BeFalse();
    }

    [TestMethod]
    public async Task Amount_IsClampedToRange()
    {
        // Arrange
        var sut = await CreateLoadedSutAsync();

        // Act & Assert
        sut.DecreaseAmountCommand.CanExecute(null).Should().BeFalse();
        sut.Amount = 500;
        sut.Amount.Should().Be(PosViewModel.MaxAmount);
        sut.IncreaseAmountCommand.CanExecute(null).Should().BeFalse();
        sut.Amount = -3;
        sut.Amount.Should().Be(PosViewModel.MinAmount);
    }

    [TestMethod]
    public async Task SummaryText_TwoUsersSplit_ShowsTotalAndPerPerson()
    {
        // Arrange
        var sut = await CreateLoadedSutAsync();
        sut.ToggleProductCommand.Execute(sut.Products[0]);
        sut.ToggleUserCommand.Execute(sut.VisibleUsers[1]);

        // Act
        sut.Split = true;

        // Assert
        sut.Summary.Should().Be(new OrderSummary(0.50m, 1.00m));
        sut.SummaryText.Should().Be($"{Texts.Total} € 1,00, € 0,50 {Texts.PerPerson}");
    }

    [TestMethod]
    public async Task SubmitAsync_SendsOrderAndResets()
    {
        // Arrange
        var sut = await CreateLoadedSutAsync();
        sut.ToggleProductCommand.Execute(sut.Products[1]);
        sut.Amount = 2;

        // Act
        await sut.SubmitCommand.ExecuteAsync(null);

        // Assert
        await _api.Received(1).SubmitOrderAsync(Arg.Is<OrderSubmit>(o =>
            o.Products.SequenceEqual(new[] { Cola.Id }) &&
            o.Users.SequenceEqual(new[] { Guid.Parse(MeId) }) &&
            o.Amount == 2 && !o.Split));
        _haptics.Received(1).Success();
        sut.ShowOrderPlaced.Should().BeTrue();
        sut.OrderPlacedDetail.Should().Be("2× Cola · € 1,60");
        sut.OrderPlacedSaldo.Should().Be(Texts.NewSaldo("€ -2,50"));
        await _api.Received(1).GetUnseenAchievementsAsync();
        sut.Products.Should().OnlyContain(p => !p.IsSelected);
        sut.Amount.Should().Be(1);
        sut.SelectedUserCount.Should().Be(1);
    }

    [TestMethod]
    public async Task SubmitAsync_Guest_DoesNotOrder()
    {
        // Arrange
        var sut = await CreateLoadedSutAsync(RoleNames.Guest);
        sut.ToggleProductCommand.Execute(sut.Products[0]);

        // Act
        await sut.SubmitCommand.ExecuteAsync(null);

        // Assert
        sut.CanSubmit.Should().BeFalse();
        await _api.DidNotReceiveWithAnyArgs().SubmitOrderAsync(default!);
        await _dialogs.Received(1).AlertAsync(Texts.Error, Texts.GuestCannotOrder);
    }

    [TestMethod]
    public async Task SubmitAsync_ApiError_ShowsMessageAndKeepsSelection()
    {
        // Arrange
        var sut = await CreateLoadedSutAsync();
        sut.ToggleProductCommand.Execute(sut.Products[0]);
        _api.SubmitOrderAsync(default!).ReturnsForAnyArgs(Task.FromException(new ApiException(System.Net.HttpStatusCode.BadRequest, "Onvoldoende voorraad")));

        // Act
        await sut.SubmitCommand.ExecuteAsync(null);

        // Assert
        await _dialogs.Received(1).AlertAsync(Texts.OrderFailed, "Onvoldoende voorraad");
        sut.Products[0].IsSelected.Should().BeTrue();
        sut.IsSubmitting.Should().BeFalse();
    }

    [TestMethod]
    public async Task OrderPlaced_IsDismissedByANewSelection()
    {
        // Arrange
        var sut = await CreateLoadedSutAsync();
        sut.ToggleProductCommand.Execute(sut.Products[0]);
        await sut.SubmitCommand.ExecuteAsync(null);

        // Act
        sut.ToggleProductCommand.Execute(sut.Products[0]);

        // Assert
        sut.ShowOrderPlaced.Should().BeFalse();
        _haptics.Received(2).Selection();
    }

    [TestMethod]
    public async Task OrderPlaced_ForSomeoneElse_ShowsNoSaldo()
    {
        // Arrange
        var sut = await CreateLoadedSutAsync();
        sut.ToggleProductCommand.Execute(sut.Products[0]);
        sut.ToggleUserCommand.Execute(sut.VisibleUsers.Single(u => !u.IsSelf));
        sut.ToggleUserCommand.Execute(sut.VisibleUsers.Single(u => u.IsSelf));

        // Act
        await sut.SubmitCommand.ExecuteAsync(null);

        // Assert
        sut.ShowOrderPlaced.Should().BeTrue();
        sut.OrderPlacedSaldo.Should().BeNull();
    }

    [TestMethod]
    public async Task LoadAsync_FirstLoadFails_ShowsTheErrorState()
    {
        // Arrange
        _api.GetMeAsync().Returns(new CurrentUser { Id = MeId, FullName = "Ik Zelf", Roles = [RoleNames.User] });
        _api.GetOrderInitializeAsync().ThrowsAsync(new HttpRequestException("offline"));
        var sut = new PosViewModel(_api, new CurrentUserService(_api),
            new AchievementPopupCoordinator(_api, _presenter, new FakeTimeProvider(), NullLogger<AchievementPopupCoordinator>.Instance),
            _dialogs, _haptics, new AppEndpoints(new Uri("https://api.test"), new Uri("https://web.test")), NullLogger<PosViewModel>.Instance);

        // Act
        await sut.LoadAsync();

        // Assert
        sut.HasLoadError.Should().BeTrue();
        sut.ShowSkeleton.Should().BeFalse();
        await _dialogs.DidNotReceive().AlertAsync(Arg.Any<string>(), Arg.Any<string>());
        _haptics.Received(1).Error();
    }

    [TestMethod]
    public async Task LoadAsync_RefreshFails_KeepsTheDataAndToasts()
    {
        // Arrange
        var sut = await CreateLoadedSutAsync();
        _api.GetOrderInitializeAsync().ThrowsAsync(new HttpRequestException("offline"));

        // Act
        await sut.LoadAsync();

        // Assert
        sut.HasLoadError.Should().BeFalse();
        sut.Products.Should().HaveCount(2);
        await _dialogs.Received(1).ToastAsync(Texts.RefreshFailed);
    }
}
