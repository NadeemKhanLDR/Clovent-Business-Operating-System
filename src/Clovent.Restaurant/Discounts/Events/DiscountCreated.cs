using Clovent.Domain;
using Clovent.Restaurant.Orders;

namespace Clovent.Restaurant.Discounts.Events;

/// <summary>Raised when a new <see cref="Discount"/> is created.</summary>
public sealed record DiscountCreated(DiscountId DiscountId, OrderId OrderId, DiscountType DiscountType, decimal Value, DateTimeOffset OccurredOnUtc) : IDomainEvent;
