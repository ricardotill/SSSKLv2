using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Configuration;
using SSSKL.Mobile.Core.Localization;
using SSSKL.Mobile.Core.Models;
using SSSKL.Mobile.Core.Popups;
using SSSKL.Mobile.Core.Services;

namespace SSSKL.Mobile.Core.ViewModels;

/// <summary>
/// Native point of sale for a member's own phone: pick products and people (yourself preselected),
/// an amount and optionally split the cost. Same rules as Frontend features/pos.
/// </summary>
public sealed partial class PosViewModel(
    ISssklApi api,
    CurrentUserService currentUser,
    AchievementPopupCoordinator achievements,
    IUserDialogs dialogs,
    AppEndpoints endpoints,
    ILogger<PosViewModel> logger) : ObservableObject
{
    public const int MinAmount = 1;
    public const int MaxAmount = 99;
    public const int UserSearchThreshold = 6;

    private static readonly CultureInfo Dutch = CultureInfo.GetCultureInfo("nl-NL");
    private List<UserItemViewModel> _allUsers = [];

    public ObservableCollection<ProductItemViewModel> Products { get; } = [];
    public ObservableCollection<UserItemViewModel> VisibleUsers { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    public partial bool IsSubmitting { get; set; }

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    [ObservableProperty]
    public partial bool ShowUserSearch { get; set; }

    [ObservableProperty]
    public partial string UserQuery { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary), nameof(SummaryText), nameof(CanSubmit))]
    [NotifyCanExecuteChangedFor(nameof(DecreaseAmountCommand), nameof(IncreaseAmountCommand))]
    public partial int Amount { get; set; } = MinAmount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary), nameof(SummaryText))]
    public partial bool Split { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSplit), nameof(Summary), nameof(SummaryText), nameof(CanSubmit))]
    public partial int SelectedUserCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary), nameof(SummaryText), nameof(CanSubmit))]
    public partial int SelectedProductCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SaldoDescription))]
    public partial string Saldo { get; set; } = "";

    [ObservableProperty]
    public partial bool IsSaldoNegative { get; set; }

    public string SaldoDescription => $"{Texts.MySaldo}: {Saldo}";

    public bool IsGuest => currentUser.IsGuest;

    public bool CanSplit => SelectedUserCount > 1;

    public OrderSummary? Summary => OrderPricing.Calculate(
        Products.Where(p => p.IsSelected).Select(p => p.Price), SelectedUserCount, Amount, Split);

    public string SummaryText
    {
        get
        {
            var summary = Summary;
            if (summary is null)
            {
                return Texts.PosSelectHint;
            }
            var total = $"{Texts.Total} {summary.Total.ToString("C", Dutch)}";
            return SelectedUserCount > 1 ? $"{total}, {summary.PerUser.ToString("C", Dutch)} {Texts.PerPerson}" : total;
        }
    }

    public bool CanSubmit => !IsLoading && !IsSubmitting && !IsGuest && Summary is not null;

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            // The saldo can have changed elsewhere, so refresh a cached user too (quietly: it's still usable).
            Task userTask = currentUser.User is null ? currentUser.RefreshAsync() : RefreshUserQuietlyAsync();
            var data = await api.GetOrderInitializeAsync();
            await userTask;
            ApplySaldo();
            var me = currentUser.User?.Id;

            Products.Clear();
            foreach (var product in data.Products)
            {
                Products.Add(new ProductItemViewModel(product));
            }

            // You come first: on a phone you mostly order for yourself.
            _allUsers = data.Users
                .Select(u => new UserItemViewModel(u.Id, u.FullName, endpoints.ResolveApiUrl(u.ProfilePictureUrl), u.Id == me))
                .OrderByDescending(u => u.IsSelf)
                .ToList();
            ShowUserSearch = _allUsers.Count > UserSearchThreshold;
            ResetSelection();
            OnPropertyChanged(nameof(IsGuest));
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Loading POS data failed");
            await dialogs.AlertAsync(Texts.Error, Texts.LoadFailed);
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private void ToggleProduct(ProductItemViewModel product)
    {
        product.IsSelected = !product.IsSelected;
        SelectedProductCount = Products.Count(p => p.IsSelected);
    }

    [RelayCommand]
    private void ToggleUser(UserItemViewModel user)
    {
        user.IsSelected = !user.IsSelected;
        SelectedUserCount = _allUsers.Count(u => u.IsSelected);
        if (SelectedUserCount < 2)
        {
            Split = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanDecreaseAmount))]
    private void DecreaseAmount() => Amount = Math.Max(MinAmount, Amount - 1);

    [RelayCommand(CanExecute = nameof(CanIncreaseAmount))]
    private void IncreaseAmount() => Amount = Math.Min(MaxAmount, Amount + 1);

    private bool CanDecreaseAmount() => Amount > MinAmount;
    private bool CanIncreaseAmount() => Amount < MaxAmount;

    partial void OnAmountChanged(int value)
    {
        var clamped = Math.Clamp(value, MinAmount, MaxAmount);
        if (clamped != value)
        {
            Amount = clamped;
        }
    }

    partial void OnSplitChanged(bool value)
    {
        if (value && !CanSplit)
        {
            Split = false;
        }
    }

    partial void OnUserQueryChanged(string value) => ApplyUserFilter();

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (!CanSubmit)
        {
            if (IsGuest)
            {
                await dialogs.AlertAsync(Texts.Error, Texts.GuestCannotOrder);
            }
            return;
        }

        IsSubmitting = true;
        try
        {
            var order = new OrderSubmit(
                Products.Where(p => p.IsSelected).Select(p => p.Id).ToList(),
                _allUsers.Where(u => u.IsSelected).Select(u => Guid.Parse(u.Id)).ToList(),
                Amount,
                Split && SelectedUserCount > 1);
            await api.SubmitOrderAsync(order);
            await dialogs.ToastAsync(Texts.OrderPlaced);

            foreach (var product in Products)
            {
                product.IsSelected = false;
            }
            SelectedProductCount = 0;
            Amount = MinAmount;
            Split = false;
            UserQuery = "";
            ResetSelection();

            await Task.WhenAll(RefreshUserQuietlyAsync(), achievements.CheckAsync());
            ApplySaldo();
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException or FormatException)
        {
            logger.LogWarning(ex, "Submitting order failed");
            await dialogs.AlertAsync(Texts.OrderFailed, ex is ApiException api ? api.Message : Texts.OrderFailed);
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    /// <summary>Preselect yourself (unless you are a kiosk account).</summary>
    private void ResetSelection()
    {
        var isKiosk = currentUser.User?.IsInRole(RoleNames.Kiosk) ?? false;
        foreach (var user in _allUsers)
        {
            user.IsSelected = user.IsSelf && !isKiosk;
        }
        SelectedUserCount = _allUsers.Count(u => u.IsSelected);
        ApplyUserFilter();
    }

    private void ApplySaldo()
    {
        var saldo = currentUser.User?.Saldo ?? 0m;
        Saldo = saldo.ToString("C", Dutch);
        IsSaldoNegative = saldo < 0;
    }

    private void ApplyUserFilter()
    {
        var query = UserQuery.Trim();
        VisibleUsers.Clear();
        foreach (var user in _allUsers.Where(u => query.Length == 0 || u.DisplayName.Contains(query, StringComparison.CurrentCultureIgnoreCase)))
        {
            VisibleUsers.Add(user);
        }
    }

    private async Task RefreshUserQuietlyAsync()
    {
        try
        {
            await currentUser.RefreshAsync();
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Refreshing the current user failed");
        }
    }
}
