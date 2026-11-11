using CommunityToolkit.Mvvm.ComponentModel;
using SSSKL.Mobile.Core.Localization;

namespace SSSKL.Mobile.Core.ViewModels;

public sealed partial class UserItemViewModel(string id, string displayName, string? pictureUrl, bool isSelf) : ObservableObject
{
    public string Id => id;
    public string DisplayName => displayName;
    public string? PictureUrl => pictureUrl;
    public bool HasPicture => pictureUrl is not null;
    public bool IsSelf => isSelf;
    public string Initials { get; } = InitialsOf(displayName);

    /// <summary>What the row shows: "Jezelf" for your own row, the name for everyone else.</summary>
    public string Title => isSelf ? Texts.Yourself : displayName;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>Your saldo, shown in your own row only; null elsewhere.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSaldo), nameof(Description))]
    public partial string? SaldoText { get; set; }

    [ObservableProperty]
    public partial bool IsSaldoNegative { get; set; }

    public bool HasSaldo => SaldoText is not null;

    /// <summary>What a screen reader says for the row.</summary>
    public string Description => SaldoText is null ? Title : $"{Title}, {Texts.MySaldo} {SaldoText}";

    internal static string InitialsOf(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "?",
            1 => parts[0][..1].ToUpperInvariant(),
            _ => (parts[0][..1] + parts[^1][..1]).ToUpperInvariant()
        };
    }
}
