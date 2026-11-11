using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Models;
using SSSKL.Mobile.Core.Services;

namespace SSSKL.Mobile.Core.ViewModels;

public sealed record TopUpRow(Guid Id, string AmountText, string DateText)
{
    public static TopUpRow From(TopUpItem topUp) => new(topUp.Id, Display.Money(topUp.Saldo), Display.DateTimeShort(topUp.CreatedOn));

    public string Description => $"{AmountText}, {DateText}";
}

/// <summary>"Mijn saldo" (SPA orders/saldo): your current saldo and your top-ups, newest first.</summary>
public sealed partial class TopUpsViewModel(
    ISssklApi api,
    CurrentUserService currentUser,
    IUserDialogs dialogs,
    ILogger<TopUpsViewModel> logger) : PagedListViewModel<TopUpRow>(dialogs, logger)
{
    [ObservableProperty]
    public partial string Saldo { get; set; } = "";

    [ObservableProperty]
    public partial bool IsSaldoNegative { get; set; }

    protected override async Task<(IReadOnlyList<TopUpRow> Items, int Total)> FetchAsync(int skip, int take)
    {
        var page = await api.GetPersonalTopUpsAsync(skip, take);
        return (page.Items.Select(TopUpRow.From).ToList(), page.TotalCount);
    }

    protected override object KeyOf(TopUpRow item) => item.Id;

    protected override async Task OnLoadedAsync()
    {
        ApplySaldo(currentUser.User);
        try
        {
            ApplySaldo(await currentUser.RefreshAsync());
        }
        catch (Exception ex) when (IsExpected(ex))
        {
            Logger.LogWarning(ex, "Refreshing the current user failed");
        }
    }

    private void ApplySaldo(CurrentUser? user)
    {
        if (user is null)
        {
            return;
        }
        Saldo = Display.Money(user.Saldo);
        IsSaldoNegative = user.Saldo < 0;
    }
}
