using Clovent.Domain;

namespace Clovent.MasterData.Settings.Events;

/// <summary>Raised when a <see cref="BusinessSettings"/> record's defaults change.</summary>
public sealed record BusinessSettingsUpdated(BusinessSettingsId BusinessSettingsId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
