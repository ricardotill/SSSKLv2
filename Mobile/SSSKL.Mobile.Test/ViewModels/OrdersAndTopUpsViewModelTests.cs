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
public class OrdersAndTopUpsViewModelTests
{
    private readonly ISssklApi _api = Substitute.For<ISssklApi>();
    private readonly IUserDialogs _dialogs = Substitute.For<IUserDialogs>();
    private CurrentUserService _currentUser = null!;

    [TestInitialize]
    public void Setup()
    {
        _currentUser = new CurrentUserService(_api);
        _api.GetMeAsync(Arg.Any<CancellationToken>()).Returns(new CurrentUser { Id = "me", FullName = "Ik", Saldo = -1.5m });
    }

    private OrdersViewModel Orders() => new(_api, _currentUser, _dialogs, NullLogger<OrdersViewModel>.Instance);

    private static OrderItem Order(string product = "Bier") => new()
    {
        Id = Guid.NewGuid(), ProductName = product, Amount = 2, Paid = 3m, CreatedOn = new DateTime(2026, 11, 11, 9, 30, 0)
    };

    private void ReturnOrders(int skip, IReadOnlyList<OrderItem> items, int total) =>
        _api.GetPersonalOrdersAsync(skip, PagedListViewModel<OrderRow>.PageSize, Arg.Any<CancellationToken>())
            .Returns(new Page<OrderItem> { Items = items, TotalCount = total });

    [TestMethod]
    public async Task Orders_Load_FormatsTheRows()
    {
        // Arrange
        ReturnOrders(0, [Order()], 1);
        var sut = Orders();

        // Act
        await sut.LoadCommand.ExecuteAsync(null);

        // Assert: Amsterdam time, Dutch money.
        sut.Items.Should().ContainSingle().Which.Should().Match<OrderRow>(r =>
            r.ProductName == "Bier" && r.AmountText == "2x" && r.PaidText == "€ 3,00" && r.DateText == "11-11-2026 10:30");
        sut.HasMore.Should().BeFalse();
        sut.IsEmpty.Should().BeFalse();
    }

    [TestMethod]
    public async Task Orders_LoadMore_AppendsUntilTheTotal()
    {
        // Arrange
        var first = Enumerable.Range(0, 20).Select(_ => Order()).ToList();
        ReturnOrders(0, first, 21);
        ReturnOrders(20, [Order("Cola")], 21);
        var sut = Orders();
        await sut.LoadCommand.ExecuteAsync(null);
        sut.HasMore.Should().BeTrue();

        // Act
        await sut.LoadMoreCommand.ExecuteAsync(null);

        // Assert
        sut.Items.Should().HaveCount(21);
        sut.HasMore.Should().BeFalse();
    }

    [TestMethod]
    public async Task Orders_Delete_Confirmed_RemovesItAndRefreshesTheSaldo()
    {
        // Arrange
        var order = Order();
        ReturnOrders(0, [order], 1);
        _dialogs.ConfirmAsync(default!, default!, default!, default!).ReturnsForAnyArgs(true);
        var sut = Orders();
        await sut.LoadCommand.ExecuteAsync(null);

        // Act
        await sut.DeleteCommand.ExecuteAsync(sut.Items[0]);

        // Assert
        await _dialogs.Received(1).ConfirmAsync(Texts.ConfirmDeleteOrderTitle, Texts.ConfirmDeleteOrder("Bier"), Texts.Delete, Texts.Cancel);
        await _api.Received(1).DeleteOrderAsync(order.Id, Arg.Any<CancellationToken>());
        sut.Items.Should().BeEmpty();
        sut.IsEmpty.Should().BeTrue();
        _currentUser.User.Should().NotBeNull("the saldo is reloaded");
    }

    [TestMethod]
    public async Task Orders_Delete_Declined_KeepsIt()
    {
        // Arrange
        ReturnOrders(0, [Order()], 1);
        var sut = Orders();
        await sut.LoadCommand.ExecuteAsync(null);

        // Act
        await sut.DeleteCommand.ExecuteAsync(sut.Items[0]);

        // Assert
        await _api.DidNotReceiveWithAnyArgs().DeleteOrderAsync(default, default);
        sut.Items.Should().HaveCount(1);
    }

    [TestMethod]
    public async Task Orders_Delete_Fails_ShowsTheReasonAndKeepsIt()
    {
        // Arrange
        ReturnOrders(0, [Order()], 1);
        _dialogs.ConfirmAsync(default!, default!, default!, default!).ReturnsForAnyArgs(true);
        _api.DeleteOrderAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiException(System.Net.HttpStatusCode.Forbidden, "Niet toegestaan"));
        var sut = Orders();
        await sut.LoadCommand.ExecuteAsync(null);

        // Act
        await sut.DeleteCommand.ExecuteAsync(sut.Items[0]);

        // Assert
        await _dialogs.Received(1).AlertAsync(Texts.Error, "Niet toegestaan");
        sut.Items.Should().HaveCount(1);
    }

    [TestMethod]
    public async Task TopUps_Load_ShowsTheRowsAndTheSaldo()
    {
        // Arrange
        _api.GetPersonalTopUpsAsync(Arg.Is(0), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new Page<TopUpItem>
        {
            Items = [new TopUpItem { Id = Guid.NewGuid(), Saldo = 20m, CreatedOn = new DateTime(2026, 7, 1, 12, 0, 0) }],
            TotalCount = 1
        });
        var sut = new TopUpsViewModel(_api, _currentUser, _dialogs, NullLogger<TopUpsViewModel>.Instance);

        // Act
        await sut.LoadCommand.ExecuteAsync(null);

        // Assert: 12:00 UTC is 14:00 in Amsterdam in July.
        sut.Items.Should().ContainSingle().Which.Should().Be(new TopUpRow(sut.Items[0].Id, "€ 20,00", "01-07-2026 14:00"));
        sut.Saldo.Should().Be("€ -1,50");
        sut.IsSaldoNegative.Should().BeTrue();
    }

    [TestMethod]
    public async Task TopUps_LoadFails_ShowsAnAlert()
    {
        // Arrange
        _api.GetPersonalTopUpsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("offline"));
        var sut = new TopUpsViewModel(_api, _currentUser, _dialogs, NullLogger<TopUpsViewModel>.Instance);

        // Act
        await sut.LoadCommand.ExecuteAsync(null);

        // Assert
        await _dialogs.Received(1).AlertAsync(Texts.Error, Texts.LoadFailed);
        sut.IsLoading.Should().BeFalse();
    }
}
