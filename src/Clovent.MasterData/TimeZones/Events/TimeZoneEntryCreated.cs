using Clovent.Domain;

namespace Clovent.MasterData.TimeZones.Events;

/// <summary>Raised when a new <see cref="TimeZoneEntry"/> catalog entry is created.</summary>
public sealed record TimeZoneEntryCreated(TimeZoneEntryId TimeZoneEntryId, IanaId IanaId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
