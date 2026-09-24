using System;

namespace Clovent.Desktop.Forms.Base;

/// <summary>
/// An immutable calendar date range between two <see cref="DateOnly"/> values, inclusive.
/// Provides canonical conversion to business-local UTC query bounds.
/// </summary>
public readonly record struct DateRange
{
    public DateOnly From { get; }
    public DateOnly To { get; }

    public DateRange(DateOnly from, DateOnly to)
    {
        From = from;
        To = to;
    }

    /// <summary>True when <see cref="From"/> does not exceed <see cref="To"/>.</summary>
    public bool IsValid => From <= To;

    /// <summary>Start of the From date (00:00:00.000) converted from configured business timezone to UTC.</summary>
    public DateTimeOffset StartOfFromUtc => GetStartUtc(DateTimeDisplay.BusinessTimeZone);

    /// <summary>End of the To date (23:59:59.9999999) converted from configured business timezone to UTC for inclusive bounds.</summary>
    public DateTimeOffset EndOfToUtcInclusive => GetEndUtcInclusive(DateTimeDisplay.BusinessTimeZone);

    /// <summary>Start of the day after the To date (00:00:00.000) converted from configured business timezone to UTC for exclusive upper bounds (&lt;).</summary>
    public DateTimeOffset StartOfDayAfterToUtc => GetEndUtcExclusive(DateTimeDisplay.BusinessTimeZone);

    /// <summary>
    /// Converts the calendar start date (00:00:00) in the specified or configured business timezone to UTC.
    /// </summary>
    public DateTimeOffset GetStartUtc(TimeZoneInfo? timeZone = null)
    {
        var tz = timeZone ?? DateTimeDisplay.BusinessTimeZone;
        var localDateTime = From.ToDateTime(TimeOnly.MinValue);
        var offset = tz.GetUtcOffset(localDateTime);
        return new DateTimeOffset(localDateTime, offset).ToUniversalTime();
    }

    /// <summary>
    /// Converts the calendar day after To date (00:00:00) in the specified or configured business timezone to UTC for half-open upper bounds (&lt;).
    /// </summary>
    public DateTimeOffset GetEndUtcExclusive(TimeZoneInfo? timeZone = null)
    {
        var tz = timeZone ?? DateTimeDisplay.BusinessTimeZone;
        var localDateTime = To.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var offset = tz.GetUtcOffset(localDateTime);
        return new DateTimeOffset(localDateTime, offset).ToUniversalTime();
    }

    /// <summary>
    /// Converts the calendar To date end (23:59:59.9999999) in the specified or configured business timezone to UTC for inclusive upper bounds (&lt;=).
    /// </summary>
    public DateTimeOffset GetEndUtcInclusive(TimeZoneInfo? timeZone = null) =>
        GetEndUtcExclusive(timeZone).AddTicks(-1);

    /// <summary>Deconstructs into from and to.</summary>
    public void Deconstruct(out DateOnly from, out DateOnly to)
    {
        from = From;
        to = To;
    }

    public override string ToString() => $"{From:yyyy-MM-dd} - {To:yyyy-MM-dd}";
}
