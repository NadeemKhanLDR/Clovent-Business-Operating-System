using System;
using System.Globalization;

namespace Clovent.Desktop.Forms.Base;

/// <summary>
/// Canonical implementation of <see cref="IBusinessDateTimeService"/>.
/// Translates UTC timestamps to the configured business timezone and produces
/// culture-invariant date/time strings matching the configured Business Settings pattern.
/// </summary>
public sealed class BusinessDateTimeService : IBusinessDateTimeService
{
    private TimeZoneInfo _businessTimeZone = TimeZoneInfo.Utc;
    private string _dateFormatPattern = "dd/MM/yyyy";
    private string _timeFormatPattern = "HH:mm";
    private string _dateTimeFormat = "dd/MM/yyyy HH:mm";

    /// <summary>Process-wide default instance.</summary>
    public static BusinessDateTimeService Instance { get; } = new();

    public BusinessDateTimeService(TimeZoneInfo? timeZone = null, string? dateTimeFormat = null)
    {
        _businessTimeZone = timeZone ?? TimeZoneInfo.Utc;
        if (!string.IsNullOrWhiteSpace(dateTimeFormat))
        {
            ApplyFormat(dateTimeFormat.Trim(), null);
        }
    }

    /// <inheritdoc/>
    public DateTime UtcNow => DateTime.UtcNow;

    /// <inheritdoc/>
    public DateTimeOffset UtcNowOffset => DateTimeOffset.UtcNow;

    /// <inheritdoc/>
    public TimeZoneInfo BusinessTimeZone => _businessTimeZone;

    /// <inheritdoc/>
    public string DateTimeFormat => _dateTimeFormat;

    /// <inheritdoc/>
    public string DateFormatPattern => _dateFormatPattern;

    /// <inheritdoc/>
    public string TimeFormatPattern => _timeFormatPattern;

    /// <inheritdoc/>
    public void Configure(TimeZoneInfo timeZone, string dateFormat, string? timeFormat = null)
    {
        _businessTimeZone = timeZone ?? TimeZoneInfo.Utc;
        ApplyFormat(dateFormat, timeFormat);
    }

    private void ApplyFormat(string dateFormat, string? timeFormat)
    {
        if (string.IsNullOrWhiteSpace(dateFormat))
        {
            dateFormat = "dd/MM/yyyy";
        }

        var trimmedDate = dateFormat.Trim();

        // Resolve time pattern
        if (!string.IsNullOrWhiteSpace(timeFormat))
        {
            var tf = timeFormat.Trim();
            if (tf.Equals("24 Hour", StringComparison.OrdinalIgnoreCase) ||
                tf.Equals("24-Hour", StringComparison.OrdinalIgnoreCase) ||
                tf.Equals("24Hour", StringComparison.OrdinalIgnoreCase) ||
                tf.Equals("HH:mm", StringComparison.OrdinalIgnoreCase))
            {
                _timeFormatPattern = "HH:mm";
            }
            else
            {
                _timeFormatPattern = "hh:mm tt";
            }
        }
        else if (trimmedDate.Contains("HH:mm", StringComparison.OrdinalIgnoreCase))
        {
            _timeFormatPattern = "HH:mm";
        }
        else if (trimmedDate.Contains("hh:mm", StringComparison.OrdinalIgnoreCase) || trimmedDate.Contains("tt", StringComparison.OrdinalIgnoreCase))
        {
            _timeFormatPattern = "hh:mm tt";
        }

        _dateFormatPattern = ExtractDateFormatPattern(trimmedDate);
        _dateTimeFormat = $"{_dateFormatPattern} {_timeFormatPattern}";
    }

    /// <inheritdoc/>
    public DateTime ConvertUtcToBusinessTime(DateTime utc)
    {
        if (utc.Kind != DateTimeKind.Utc)
        {
            utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        }

        return TimeZoneInfo.ConvertTimeFromUtc(utc, _businessTimeZone);
    }

    /// <inheritdoc/>
    public DateTimeOffset ConvertUtcToBusinessTime(DateTimeOffset utc)
    {
        return TimeZoneInfo.ConvertTime(utc, _businessTimeZone);
    }

    /// <inheritdoc/>
    public DateOnly Today => GetCurrentBusinessDate();

    /// <inheritdoc/>
    public DateOnly GetCurrentBusinessDate()
    {
        var localTime = ConvertUtcToBusinessTime(DateTimeOffset.UtcNow);
        return DateOnly.FromDateTime(localTime.DateTime);
    }

    /// <inheritdoc/>
    public string FormatDate(DateOnly value)
    {
        return value.ToString(_dateFormatPattern, CultureInfo.InvariantCulture);
    }

    /// <summary>Formats the date portion of a DateTimeOffset in the configured business timezone.</summary>
    public string FormatDate(DateTimeOffset? utc)
    {
        if (utc == null) return "-";
        var localTime = ConvertUtcToBusinessTime(utc.Value);
        return localTime.ToString(_dateFormatPattern, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public string FormatDateTime(DateTimeOffset? utc)
    {
        if (utc == null) return "-";
        var localTime = ConvertUtcToBusinessTime(utc.Value);
        return localTime.ToString(_dateTimeFormat, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public string FormatDateTime(DateTime? utc)
    {
        if (utc == null) return "-";
        var localTime = ConvertUtcToBusinessTime(utc.Value);
        return localTime.ToString(_dateTimeFormat, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public string FormatTime(DateTimeOffset? utc)
    {
        if (utc == null) return "-";
        var localTime = ConvertUtcToBusinessTime(utc.Value);
        return localTime.ToString(_timeFormatPattern, CultureInfo.InvariantCulture);
    }

    /// <summary>Formats the time portion of a DateTime in the configured business timezone.</summary>
    public string FormatTime(DateTime? utc)
    {
        if (utc == null) return "-";
        var localTime = ConvertUtcToBusinessTime(utc.Value);
        return localTime.ToString(_timeFormatPattern, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public TimeZoneInfo GetBusinessTimeZone() => _businessTimeZone;

    internal static string ExtractDateFormatPattern(string fullFormat)
    {
        if (string.IsNullOrWhiteSpace(fullFormat)) return "dd/MM/yyyy";
        var parts = fullFormat.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0] : "dd/MM/yyyy";
    }
}
