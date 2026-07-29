using Clovent.Domain;
using Clovent.Identity.Branches;
using Clovent.Restaurant.DiningAreas.ValueObjects;

namespace Clovent.Restaurant.DiningAreas.Events;

/// <summary>Raised when a new <see cref="DiningArea"/> is created.</summary>
public sealed record DiningAreaCreated(DiningAreaId DiningAreaId, BranchId BranchId, DiningAreaName Name, DateTimeOffset OccurredOnUtc) : IDomainEvent;
