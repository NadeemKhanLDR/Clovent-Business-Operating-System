using Clovent.Domain;
using Clovent.Restaurant.Discounts;

namespace Clovent.Restaurant.Orders.Events;

/// <summary>Raised when a <see cref="Discounts.Discount"/> is applied to an <see cref="Order"/>.</summary>
public sealed record OrderDiscountApplied(OrderId OrderId, DiscountId DiscountId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
