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
    private string _dateTimeFormat = "dd/MM/yyyy HH:mm";

    /// <summary>Process-wide default instance.</summary>
    public static BusinessDateTimeService Instance { get; } = new();

    public BusinessDateTimeService(TimeZoneInfo? timeZone = null, string? dateTimeFormat = null)
    {
        _businessTimeZone = timeZone ?? TimeZoneInfo.Utc;
        if (!string.IsNullOrWhiteSpace(dateTimeFormat))
        {
            _dateTimeFormat = dateTimeFormat.Trim();
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
    public void Configure(TimeZoneInfo timeZone, string dateTimeFormat)
    {
        _businessTimeZone = timeZone ?? TimeZoneInfo.Utc;
        if (!string.IsNullOrWhiteSpace(dateTimeFormat))
        {
            _dateTimeFormat = dateTimeFormat.Trim();
        }
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
    public DateOnly GetCurrentBusinessDate()
    {
        var localTime = ConvertUtcToBusinessTime(DateTimeOffset.UtcNow);
        return DateOnly.FromDateTime(localTime.DateTime);
    }

    /// <inheritdoc/>
    public string FormatDate(DateOnly value)
    {
        var dateFormat = ExtractDateFormatPattern(_dateTimeFormat);
        return value.ToString(dateFormat, CultureInfo.InvariantCulture);
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
        return localTime.ToString("hh:mm tt", CultureInfo.InvariantCulture);
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
