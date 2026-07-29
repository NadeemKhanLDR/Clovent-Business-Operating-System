using Clovent.Domain;
using Clovent.Restaurant.Discounts;

namespace Clovent.Restaurant.Orders.Events;

/// <summary>Raised when a <see cref="Discounts.Discount"/> is removed from an <see cref="Order"/>.</summary>
public sealed record OrderDiscountRemoved(OrderId OrderId, DiscountId DiscountId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
