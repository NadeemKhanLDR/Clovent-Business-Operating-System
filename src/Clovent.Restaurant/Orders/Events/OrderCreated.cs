using Clovent.Domain;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Orders.ValueObjects;
using Clovent.Restaurant.Tables;

namespace Clovent.Restaurant.Orders.Events;

/// <summary>Raised when a new <see cref="Order"/> is created.</summary>
public sealed record OrderCreated(OrderId OrderId, OrderNumber OrderNumber, OrderType OrderType, WarehouseId WarehouseId, TableId? TableId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
