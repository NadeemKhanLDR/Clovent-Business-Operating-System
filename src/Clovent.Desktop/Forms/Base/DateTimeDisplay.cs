using System;

namespace Clovent.Desktop.Forms.Base;

/// <summary>
/// Centralized display helper for DateTimes. Converts UTC timestamps to the configured
/// business time zone and formats them using the selected Date and Time format string.
/// </summary>
public static class DateTimeDisplay
{
    private static TimeZoneInfo _businessTimeZone = TimeZoneInfo.Utc;
    private static string _dateTimeFormat = "dd/MM/yyyy HH:mm"; // fallback default

    /// <summary>Configures the process-wide timezone and format string.</summary>
    public static void Configure(TimeZoneInfo timeZone, string dateTimeFormat)
    {
        _businessTimeZone = timeZone ?? TimeZoneInfo.Utc;
        if (!string.IsNullOrWhiteSpace(dateTimeFormat))
        {
            _dateTimeFormat = dateTimeFormat;
        }
    }

    /// <summary>Gets the configured business timezone.</summary>
    public static TimeZoneInfo BusinessTimeZone => _businessTimeZone;

    /// <summary>Gets the configured date and time format string.</summary>
    public static string FormatString => _dateTimeFormat;

    /// <summary>
    /// Formats a DateTimeOffset value by first converting it to the configured business timezone,
    /// and then formatting it according to the configured Date and Time Format.
    /// </summary>
    public static string Format(DateTimeOffset? value)
    {
        if (value == null) return "-";
        
        // Convert UTC/any offset to configured business timezone
        var localTime = TimeZoneInfo.ConvertTime(value.Value, _businessTimeZone);
        return localTime.ToString(_dateTimeFormat);
    }

    /// <summary>
    /// Formats a DateTime value (assumed to be in UTC if not specified, or just converted) by first converting it to the configured business timezone,
    /// and then formatting it according to the configured Date and Time Format.
    /// </summary>
    public static string Format(DateTime? value)
    {
        if (value == null) return "-";
        
        DateTime utcDateTime = value.Value;
        if (utcDateTime.Kind != DateTimeKind.Utc)
        {
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        }
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, _businessTimeZone);
        return localTime.ToString(_dateTimeFormat);
    }
}
