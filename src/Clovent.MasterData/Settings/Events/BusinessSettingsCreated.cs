using Clovent.Domain;
using Clovent.Identity.Organizations;

namespace Clovent.MasterData.Settings.Events;

/// <summary>Raised when a new <see cref="BusinessSettings"/> record is created for an organization.</summary>
public sealed record BusinessSettingsCreated(BusinessSettingsId BusinessSettingsId, OrganizationId OrganizationId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
