using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Localization;
using SSSKL.Mobile.Core.Models;
using SSSKL.Mobile.Core.Services;

namespace SSSKL.Mobile.Core.ViewModels;

/// <summary>One row of "Mijn bestellingen".</summary>
public sealed record OrderRow(Guid Id, string ProductName, string AmountText, string PaidText, string DateText)
{
    public static OrderRow From(OrderItem order) =>
        new(order.Id, order.ProductName, $"{order.Amount}x", Display.Money(order.Paid), Display.DateTimeShort(order.CreatedOn));

    public string Description => $"{AmountText} {ProductName}, {PaidText}, {DateText}";
}

/// <summary>
/// "Mijn bestellingen" (SPA orders/personal): your orders, newest first. You can delete one; its price
/// goes back to your saldo.
/// </summary>
public sealed partial class OrdersViewModel(
    ISssklApi api,
    CurrentUserService currentUser,
    IUserDialogs dialogs,
    ILogger<OrdersViewModel> logger) : PagedListViewModel<OrderRow>(dialogs, logger)
{
    protected override async Task<(IReadOnlyList<OrderRow> Items, int Total)> FetchAsync(int skip, int take)
    {
        var page = await api.GetPersonalOrdersAsync(skip, take);
        return (page.Items.Select(OrderRow.From).ToList(), page.TotalCount);
    }

    protected override object KeyOf(OrderRow item) => item.Id;

    [RelayCommand]
    private async Task DeleteAsync(OrderRow order)
    {
        if (!await Dialogs.ConfirmAsync(Texts.ConfirmDeleteOrderTitle, Texts.ConfirmDeleteOrder(order.ProductName), Texts.Delete, Texts.Cancel))
        {
            return;
        }
        try
        {
            await api.DeleteOrderAsync(order.Id);
            Items.Remove(order);
            NotifyEmptyChanged();
            await Dialogs.ToastAsync(Texts.OrderDeleted);
        }
        catch (Exception ex) when (IsExpected(ex))
        {
            Logger.LogWarning(ex, "Deleting order {Id} failed", order.Id);
            await Dialogs.AlertAsync(Texts.Error, ex is ApiException api ? api.Message : Texts.OrderDeleteFailed);
            return;
        }

        // The saldo changed (Kassa, Meer and Profiel show it).
        try
        {
            await currentUser.RefreshAsync();
        }
        catch (Exception ex) when (IsExpected(ex))
        {
            Logger.LogWarning(ex, "Refreshing the current user failed");
        }
    }
}
