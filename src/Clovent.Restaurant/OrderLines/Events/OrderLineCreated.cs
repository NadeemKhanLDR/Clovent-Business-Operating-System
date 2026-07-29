using Clovent.Catalog.Variants;
using Clovent.Domain;
using Clovent.Restaurant.Orders;

namespace Clovent.Restaurant.OrderLines.Events;

/// <summary>Raised when a new <see cref="OrderLine"/> is created.</summary>
public sealed record OrderLineCreated(OrderLineId OrderLineId, OrderId OrderId, ProductVariantId ProductVariantId, decimal Quantity, decimal UnitPrice, DateTimeOffset OccurredOnUtc) : IDomainEvent;
