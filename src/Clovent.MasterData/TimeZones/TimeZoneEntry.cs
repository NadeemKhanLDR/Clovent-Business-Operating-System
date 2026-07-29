using Clovent.Domain;
using Clovent.MasterData.Shared;
using Clovent.MasterData.TimeZones.Events;

namespace Clovent.MasterData.TimeZones;

/// <summary>
/// A time zone catalog entry - reference data shared across every
/// organization. Named <c>TimeZoneEntry</c> rather than <c>TimeZone</c>
/// deliberately: the BCL has a legacy <see cref="System.TimeZone"/> type
/// still reachable via implicit usings, and this project already learned
/// (<c>AuthenticationIntegration.md</c>'s <c>Sessions</c>/<c>Session</c>
/// collision) that reusing a BCL/sibling-namespace type name invites a
/// silent, hard-to-diagnose shadowing bug rather than a clean compile error.
/// </summary>
public sealed class TimeZoneEntry : AggregateRoot<TimeZoneEntryId>
{
    private const int MaxDisplayNameLength = 100;

    /// <summary>The IANA time zone database identifier.</summary>
    public IanaId IanaId { get; }

    /// <summary>The human-readable display name (e.g. "(UTC-05:00) Eastern Time").</summary>
    public string DisplayName { get; private set; }

    /// <summary>The time zone's standard UTC offset, in minutes (e.g. -300 for Eastern Standard Time).</summary>
    public int UtcOffsetMinutes { get; }

    /// <summary>The time zone's current lifecycle state.</summary>
    public MasterDataStatus Status { get; private set; }

    /// <summary>UTC instant this time zone entry was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private TimeZoneEntry(TimeZoneEntryId id, IanaId ianaId, string displayName, int utcOffsetMinutes, MasterDataStatus status, DateTimeOffset createdAtUtc)
    {
        Id = id;
        IanaId = ianaId;
        DisplayName = displayName;
        UtcOffsetMinutes = utcOffsetMinutes;
        Status = status;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Creates a new, active time zone catalog entry.</summary>
    /// <exception cref="ArgumentException"><paramref name="displayName"/> is empty or too long.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="utcOffsetMinutes"/> is outside +/-14 hours.</exception>
    public static TimeZoneEntry Create(IanaId ianaId, string displayName, int utcOffsetMinutes)
    {
        ArgumentNullException.ThrowIfNull(ianaId);

        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name is required.", nameof(displayName));

        displayName = displayName.Trim();

        if (displayName.Length > MaxDisplayNameLength)
            throw new ArgumentException($"Display name cannot exceed {MaxDisplayNameLength} characters.", nameof(displayName));

        if (utcOffsetMinutes is < -840 or > 840)
            throw new ArgumentOutOfRangeException(nameof(utcOffsetMinutes), utcOffsetMinutes, "UTC offset must be within +/-14 hours.");

        var now = DateTimeOffset.UtcNow;
        var entry = new TimeZoneEntry(TimeZoneEntryId.New(), ianaId, displayName, utcOffsetMinutes, MasterDataStatus.Active, now);
        entry.AddDomainEvent(new TimeZoneEntryCreated(entry.Id, entry.IanaId, now));
        return entry;
    }

    /// <summary>Activates the time zone entry.</summary>
    /// <exception cref="MasterDataDomainException">The entry is already active.</exception>
    public void Activate()
    {
        if (Status == MasterDataStatus.Active)
            throw MasterDataDomainException.TimeZoneEntryAlreadyActive(Id);

        Status = MasterDataStatus.Active;
    }

    /// <summary>Deactivates the time zone entry.</summary>
    /// <exception cref="MasterDataDomainException">The entry is not active.</exception>
    public void Deactivate()
    {
        if (Status != MasterDataStatus.Active)
            throw MasterDataDomainException.TimeZoneEntryNotActive(Id);

        Status = MasterDataStatus.Inactive;
    }
}
