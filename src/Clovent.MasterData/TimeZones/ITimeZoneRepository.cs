namespace Clovent.MasterData.TimeZones;

/// <summary>Persistence contract for <see cref="TimeZoneEntry"/> aggregates.</summary>
public interface ITimeZoneRepository
{
    /// <summary>Retrieves a time zone entry by identity, or <see langword="null"/> if none exists.</summary>
    Task<TimeZoneEntry?> GetByIdAsync(TimeZoneEntryId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a time zone entry by its IANA id, or <see langword="null"/> if none exists.</summary>
    Task<TimeZoneEntry?> GetByIanaIdAsync(IanaId ianaId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every time zone entry in the catalog.</summary>
    Task<IReadOnlyCollection<TimeZoneEntry>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created time zone entry.</summary>
    Task AddAsync(TimeZoneEntry timeZoneEntry, CancellationToken cancellationToken = default);
}
