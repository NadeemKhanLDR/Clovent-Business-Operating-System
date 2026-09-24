using System;

namespace Clovent.Desktop.Forms.Base;

/// <summary>
/// Canonical business-timezone and date/time formatting service for CBOS desktop.
/// Converts UTC persisted timestamps to the configured business timezone and formats
/// them using the organization's configured date and time format pattern.
/// </summary>
public interface IBusinessDateTimeService
{
    /// <summary>Current UTC instant.</summary>
    DateTime UtcNow { get; }

    /// <summary>Current UTC instant as DateTimeOffset.</summary>
    DateTimeOffset UtcNowOffset { get; }

    /// <summary>The organization's configured business timezone.</summary>
    TimeZoneInfo BusinessTimeZone { get; }

    /// <summary>The organization's configured date and time format pattern.</summary>
    string DateTimeFormat { get; }

    /// <summary>Converts a UTC DateTime to the configured business timezone.</summary>
    DateTime ConvertUtcToBusinessTime(DateTime utc);

    /// <summary>Converts a UTC DateTimeOffset to the configured business timezone.</summary>
    DateTimeOffset ConvertUtcToBusinessTime(DateTimeOffset utc);

    /// <summary>Gets the current operational business date according to the configured business timezone.</summary>
    DateOnly GetCurrentBusinessDate();

    /// <summary>Formats a DateOnly using the date portion of the configured format pattern.</summary>
    string FormatDate(DateOnly value);

    /// <summary>Formats a DateTimeOffset timestamp using the configured business timezone and format pattern.</summary>
    string FormatDateTime(DateTimeOffset? utc);

    /// <summary>Formats a DateTime timestamp using the configured business timezone and format pattern.</summary>
    string FormatDateTime(DateTime? utc);

    /// <summary>Formats the time portion of a timestamp in the configured business timezone.</summary>
    string FormatTime(DateTimeOffset? utc);

    /// <summary>Gets the configured business timezone info.</summary>
    TimeZoneInfo GetBusinessTimeZone();

    /// <summary>Updates the runtime configuration with a new timezone and date/time format.</summary>
    void Configure(TimeZoneInfo timeZone, string dateTimeFormat);
}
