using System;

namespace Clovent.Restaurant.Application.Shifts.Services;

/// <summary>
/// Provides canonical restaurant operating business date calculations,
/// properly mapping UTC timestamps to and from the configured business timezone.
/// Encapsulates future midnight/operating boundary rules so application and reporting logic
/// does not scatter DateTime.Today or raw workstation clock calls.
/// </summary>
public interface IBusinessDateProvider
{
    /// <summary>Gets the current operating business date for the restaurant.</summary>
    DateOnly GetCurrentBusinessDate();

    /// <summary>Gets the configured restaurant business timezone.</summary>
    TimeZoneInfo GetBusinessTimeZone();

    /// <summary>Derives the restaurant operating business date for a given UTC timestamp.</summary>
    DateOnly GetBusinessDateForUtc(DateTimeOffset utcInstant);

    /// <summary>Gets the UTC window covering a given operating business date.</summary>
    (DateTimeOffset StartUtc, DateTimeOffset EndUtc) GetUtcRangeForBusinessDate(DateOnly businessDate);
}
