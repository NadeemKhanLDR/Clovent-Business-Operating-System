using System;
using System.Globalization;

namespace Clovent.Desktop.Forms.Base;

/// <summary>
/// Centralized display helper for DateTimes. Converts UTC timestamps to the configured
/// business time zone and formats them using the selected Date and Time format string.
/// Backed by <see cref="BusinessDateTimeService.Instance"/>.
/// </summary>
public static class DateTimeDisplay
{
    /// <summary>Configures the process-wide timezone and format string.</summary>
    public static void Configure(TimeZoneInfo timeZone, string dateTimeFormat)
    {
        BusinessDateTimeService.Instance.Configure(timeZone, dateTimeFormat);
    }

    /// <summary>Gets the configured business timezone.</summary>
    public static TimeZoneInfo BusinessTimeZone => BusinessDateTimeService.Instance.BusinessTimeZone;

    /// <summary>Gets the configured date and time format string.</summary>
    public static string FormatString => BusinessDateTimeService.Instance.DateTimeFormat;

    /// <summary>
    /// Formats a DateTimeOffset value by first converting it to the configured business timezone,
    /// and then formatting it according to the configured Date and Time Format.
    /// </summary>
    public static string Format(DateTimeOffset? value)
    {
        return BusinessDateTimeService.Instance.FormatDateTime(value);
    }

    /// <summary>
    /// Formats a DateTime value by first converting it to the configured business timezone,
    /// and then formatting it according to the configured Date and Time Format.
    /// </summary>
    public static string Format(DateTime? value)
    {
        return BusinessDateTimeService.Instance.FormatDateTime(value);
    }

    /// <summary>Formats a DateOnly value using the date portion of the configured format.</summary>
    public static string FormatDate(DateOnly? value)
    {
        if (value == null) return "-";
        return BusinessDateTimeService.Instance.FormatDate(value.Value);
    }

    /// <summary>Formats the date portion of a DateTimeOffset in the configured business timezone.</summary>
    public static string FormatDate(DateTimeOffset? value)
    {
        if (value == null) return "-";
        var localTime = BusinessDateTimeService.Instance.ConvertUtcToBusinessTime(value.Value);
        return BusinessDateTimeService.Instance.FormatDate(DateOnly.FromDateTime(localTime.DateTime));
    }

    /// <summary>Formats the time portion of a timestamp in the configured business timezone ("hh:mm tt").</summary>
    public static string FormatTime(DateTimeOffset? value)
    {
        return BusinessDateTimeService.Instance.FormatTime(value);
    }

    /// <summary>Gets the current operational business date derived from the configured business timezone.</summary>
    public static DateOnly GetCurrentBusinessDate()
    {
        return BusinessDateTimeService.Instance.GetCurrentBusinessDate();
    }
}
