using System.Globalization;

namespace SSSKL.Mobile.Core.ViewModels;

/// <summary>Dutch formatting shared by the native pages (like the SPA's pipes).</summary>
public static class Display
{
    public static readonly CultureInfo Dutch = CultureInfo.GetCultureInfo("nl-NL");

    /// <summary>Where API times (UTC) are shown; tests set a fixed zone.</summary>
    public static TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;

    public static string Money(decimal value) => value.ToString("C", Dutch);

    public static string Number(int value) => value.ToString("N0", Dutch);

    /// <summary>"11 nov 2026"</summary>
    public static string Date(DateTime utc) => ToLocal(utc).ToString("d MMM yyyy", Dutch);

    /// <summary>"11-11-2026 14:03", like the SPA's tables.</summary>
    public static string DateTimeShort(DateTime utc) => ToLocal(utc).ToString("dd-MM-yyyy HH:mm", Dutch);

    /// <summary>"11 nov 2026, 14:03"</summary>
    public static string DateTimeMedium(DateTime utc) => ToLocal(utc).ToString("d MMM yyyy, HH:mm", Dutch);

    /// <summary>The API sends DateTime.MinValue (or nothing) for events that didn't happen yet.</summary>
    public static bool IsUnset(DateTime? value) => value is null || value.Value.Year <= 1;

    /// <summary>API times are UTC, usually sent without an offset.</summary>
    public static DateTime ToLocal(DateTime utc)
    {
        var value = utc.Kind switch
        {
            DateTimeKind.Unspecified => DateTime.SpecifyKind(utc, DateTimeKind.Utc),
            DateTimeKind.Local => utc.ToUniversalTime(),
            _ => utc
        };
        return TimeZoneInfo.ConvertTimeFromUtc(value, TimeZone);
    }
}
