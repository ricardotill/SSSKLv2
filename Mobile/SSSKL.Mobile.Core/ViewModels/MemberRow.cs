namespace SSSKL.Mobile.Core.ViewModels;

/// <summary>
/// A member in a list (saldo list, leaderboard, achievement earners): avatar, name, a value on the right
/// (<see cref="Detail"/>), and tapping opens their profile.
/// </summary>
/// <param name="Rank">Leaderboard position; null elsewhere.</param>
public sealed record MemberRow(
    string? UserId,
    string Name,
    string? PictureUrl,
    string Detail,
    bool IsSelf = false,
    bool IsDetailNegative = false,
    int? Rank = null,
    string? Subtitle = null)
{
    public string Initials { get; } = UserItemViewModel.InitialsOf(Name);
    public bool HasPicture => PictureUrl is not null;
    public bool HasSubtitle => !string.IsNullOrEmpty(Subtitle);
    public bool CanOpen => !string.IsNullOrEmpty(UserId);

    /// <summary>The top three get a medal colour.</summary>
    public bool IsGold => Rank == 1;
    public bool IsSilver => Rank == 2;
    public bool IsBronze => Rank == 3;
    public string RankText => Rank is { } rank ? rank.ToString(Display.Dutch) : "";

    public string Description => string.Join(", ", new[]
    {
        Rank is { } r ? Localization.Texts.Rank(r) : null, Name, Subtitle, Detail
    }.Where(p => !string.IsNullOrEmpty(p)));
}
