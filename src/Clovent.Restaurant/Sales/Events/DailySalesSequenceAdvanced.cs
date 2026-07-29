using Clovent.Domain;
using Clovent.MasterData.Warehouses;

namespace Clovent.Restaurant.Sales.Events;

/// <summary>Raised when a <see cref="DailySalesSequence"/> is advanced to issue the next number.</summary>
public sealed record DailySalesSequenceAdvanced(DailySalesSequenceId DailySalesSequenceId, WarehouseId WarehouseId, DateOnly Date, int Number, DateTimeOffset OccurredOnUtc) : IDomainEvent;
