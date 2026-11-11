using CommunityToolkit.Mvvm.ComponentModel;

namespace SSSKL.Mobile.Core.ViewModels;

public sealed partial class UserItemViewModel(string id, string displayName, string? pictureUrl, bool isSelf) : ObservableObject
{
    public string Id => id;
    public string DisplayName => displayName;
    public string? PictureUrl => pictureUrl;
    public bool HasPicture => pictureUrl is not null;
    public bool IsSelf => isSelf;
    public string Initials { get; } = InitialsOf(displayName);

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

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
