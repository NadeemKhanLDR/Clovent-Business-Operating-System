using System;
using System.Globalization;

namespace Clovent.Desktop.Forms.Base;

/// <summary>
/// Central business date formatter. Formats DateOnly, DateTime, and DateTimeOffset
/// in the configured business timezone using the company's preferred date format (e.g. dd-MMM-yyyy).
/// </summary>
public static class BusinessDateFormatter
{
    /// <summary>The active date format pattern (e.g. "dd-MMM-yyyy").</summary>
    public static string Pattern => BusinessDateTimeService.Instance.DateFormatPattern;

    /// <summary>Formats a DateOnly value.</summary>
    public static string Format(DateOnly? date) => DateTimeDisplay.FormatDate(date);

    /// <summary>Converts to business timezone and formats the date portion.</summary>
    public static string Format(DateTime? dateTime)
    {
        if (dateTime == null) return "-";
        var local = BusinessDateTimeService.Instance.ConvertUtcToBusinessTime(dateTime.Value);
        return BusinessDateTimeService.Instance.FormatDate(DateOnly.FromDateTime(local));
    }

    /// <summary>Converts to business timezone and formats the date portion.</summary>
    public static string Format(DateTimeOffset? offset) => DateTimeDisplay.FormatDate(offset);

    /// <summary>Configures the business date formatter with timezone and date format.</summary>
    public static void Configure(string timeZone, string dateFormat) =>
        DateTimeDisplay.Configure(ResolveTimeZone(timeZone), dateFormat, BusinessDateTimeService.Instance.TimeFormatPattern);

    /// <summary>Configures the business date formatter with timezone and date format.</summary>
    public static void Configure(TimeZoneInfo timeZone, string dateFormat) =>
        DateTimeDisplay.Configure(timeZone, dateFormat, BusinessDateTimeService.Instance.TimeFormatPattern);

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId) || string.Equals(timeZoneId, "UTC", StringComparison.OrdinalIgnoreCase))
            return TimeZoneInfo.Utc;
        try { return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
        catch { return TimeZoneInfo.Utc; }
    }
}

/// <summary>
/// Central business time formatter. Formats time values in the configured business timezone
/// using the company's preferred time format (e.g. "12 Hour" -> "hh:mm tt", "24 Hour" -> "HH:mm").
/// </summary>
public static class BusinessTimeFormatter
{
    /// <summary>The active time format pattern (e.g. "hh:mm tt" or "HH:mm").</summary>
    public static string Pattern => BusinessDateTimeService.Instance.TimeFormatPattern;

    /// <summary>Configures the business time formatter with timezone and time format.</summary>
    public static void Configure(string timeZone, string timeFormat) =>
        DateTimeDisplay.Configure(ResolveTimeZone(timeZone), BusinessDateTimeService.Instance.DateFormatPattern, timeFormat);

    /// <summary>Configures the business time formatter with timezone and time format.</summary>
    public static void Configure(TimeZoneInfo timeZone, string timeFormat) =>
        DateTimeDisplay.Configure(timeZone, BusinessDateTimeService.Instance.DateFormatPattern, timeFormat);

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId) || string.Equals(timeZoneId, "UTC", StringComparison.OrdinalIgnoreCase))
            return TimeZoneInfo.Utc;
        try { return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
        catch { return TimeZoneInfo.Utc; }
    }

    /// <summary>Formats a TimeOnly value.</summary>
    public static string Format(TimeOnly? time) => time.HasValue ? time.Value.ToString(Pattern, CultureInfo.InvariantCulture) : "-";

    /// <summary>Converts to business timezone and formats the time portion.</summary>
    public static string Format(DateTimeOffset? offset) => DateTimeDisplay.FormatTime(offset);

    /// <summary>Converts to business timezone and formats the time portion.</summary>
    public static string Format(DateTime? dateTime)
    {
        if (dateTime == null) return "-";
        return DateTimeDisplay.FormatTime(new DateTimeOffset(dateTime.Value));
    }
}

/// <summary>
/// Central business combined date and time formatter. Translates UTC timestamps
/// to local business time and formats both Date and Time together (e.g. "01-Oct-2026 06:15 PM").
/// </summary>
public static class BusinessDateTimeFormatter
{
    /// <summary>The active combined date and time format pattern (e.g. "dd-MMM-yyyy hh:mm tt").</summary>
    public static string Pattern => BusinessDateTimeService.Instance.DateTimeFormat;

    /// <summary>Configures the business combined date/time formatter.</summary>
    public static void Configure(string timeZone, string dateFormat, string? timeFormat = null) =>
        DateTimeDisplay.Configure(ResolveTimeZone(timeZone), dateFormat, timeFormat);

    /// <summary>Configures the business combined date/time formatter.</summary>
    public static void Configure(TimeZoneInfo timeZone, string dateFormat, string? timeFormat = null) =>
        DateTimeDisplay.Configure(timeZone, dateFormat, timeFormat);

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId) || string.Equals(timeZoneId, "UTC", StringComparison.OrdinalIgnoreCase))
            return TimeZoneInfo.Utc;
        try { return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
        catch { return TimeZoneInfo.Utc; }
    }

    /// <summary>Converts to business timezone and formats both Date and Time.</summary>
    public static string Format(DateTimeOffset? offset) => DateTimeDisplay.FormatDateTime(offset);

    /// <summary>Converts to business timezone and formats both Date and Time.</summary>
    public static string Format(DateTime? dateTime) => DateTimeDisplay.FormatDateTime(dateTime);
}
