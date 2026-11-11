using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using SSSKL.Mobile.Core.Localization;
using SSSKL.Mobile.Core.Models;

namespace SSSKL.Mobile.Core.ViewModels;

/// <summary>One row of the Notificaties tab.</summary>
public sealed partial class NotificationItemViewModel : ObservableObject
{
    private static readonly CultureInfo Dutch = CultureInfo.GetCultureInfo("nl-NL");

    public NotificationItemViewModel(NotificationItem item, TimeZoneInfo? timeZone = null)
    {
        Id = item.Id;
        Title = item.Title;
        Message = item.Message;
        LinkUri = string.IsNullOrWhiteSpace(item.LinkUri) ? null : item.LinkUri;
        IsRead = item.IsRead;
        // The API stores UTC and sends it without an offset.
        var utc = item.CreatedOn.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(item.CreatedOn, DateTimeKind.Utc)
            : item.CreatedOn.ToUniversalTime();
        DateText = TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone ?? TimeZoneInfo.Local).ToString("d MMM yyyy, HH:mm", Dutch);
    }

    public Guid Id { get; }
    public string Title { get; }
    public string Message { get; }
    public string? LinkUri { get; }
    public string DateText { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Icon), nameof(Description))]
    public partial bool IsRead { get; set; }

    /// <summary>A bell while unread, an envelope once read (like the SPA).</summary>
    public string Icon => IsRead ? Icons.Mail : Icons.Bell;

    /// <summary>What a screen reader says for the row.</summary>
    public string Description => IsRead ? $"{Title}. {Message}. {DateText}" : $"{Texts.Unread}: {Title}. {Message}. {DateText}";
}
