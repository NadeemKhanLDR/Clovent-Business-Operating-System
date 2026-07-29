using Clovent.Domain;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.Restaurant.DiningAreas;

namespace Clovent.Restaurant.Tables.Events;

/// <summary>Raised when a new <see cref="Table"/> is created.</summary>
public sealed record TableCreated(TableId TableId, DiningAreaId DiningAreaId, EntityCode Code, int Capacity, DateTimeOffset OccurredOnUtc) : IDomainEvent;
