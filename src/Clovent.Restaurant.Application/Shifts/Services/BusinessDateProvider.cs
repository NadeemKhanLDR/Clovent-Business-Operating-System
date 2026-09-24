using System;

namespace Clovent.Restaurant.Application.Shifts.Services;

/// <summary>
/// Default implementation of <see cref="IBusinessDateProvider"/> translating timestamps
/// against the configured restaurant business timezone.
/// </summary>
public sealed class BusinessDateProvider : IBusinessDateProvider
{
    private TimeZoneInfo _businessTimeZone;

    /// <summary>Initializes a new instance using the system local timezone or a specified timezone.</summary>
    public BusinessDateProvider(TimeZoneInfo? timeZone = null)
    {
        _businessTimeZone = timeZone ?? TimeZoneInfo.Local;
    }

    /// <summary>Updates the business timezone used for business date calculations.</summary>
    public void SetBusinessTimeZone(TimeZoneInfo timeZone)
    {
        _businessTimeZone = timeZone ?? throw new ArgumentNullException(nameof(timeZone));
    }

    /// <inheritdoc/>
    public TimeZoneInfo GetBusinessTimeZone() => _businessTimeZone;

    /// <inheritdoc/>
    public DateOnly GetCurrentBusinessDate() => GetBusinessDateForUtc(DateTimeOffset.UtcNow);

    /// <inheritdoc/>
    public DateOnly GetBusinessDateForUtc(DateTimeOffset utcInstant)
    {
        var localTime = TimeZoneInfo.ConvertTime(utcInstant, _businessTimeZone);
        return DateOnly.FromDateTime(localTime.DateTime);
    }

    /// <inheritdoc/>
    public (DateTimeOffset StartUtc, DateTimeOffset EndUtc) GetUtcRangeForBusinessDate(DateOnly businessDate)
    {
        var startLocal = DateTime.SpecifyKind(businessDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var endLocal = DateTime.SpecifyKind(businessDate.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Unspecified);

        var startOffset = _businessTimeZone.GetUtcOffset(startLocal);
        var endOffset = _businessTimeZone.GetUtcOffset(endLocal);

        var startDto = new DateTimeOffset(startLocal, startOffset).ToUniversalTime();
        var endDto = new DateTimeOffset(endLocal, endOffset).ToUniversalTime();

        return (startDto, endDto);
    }
}
