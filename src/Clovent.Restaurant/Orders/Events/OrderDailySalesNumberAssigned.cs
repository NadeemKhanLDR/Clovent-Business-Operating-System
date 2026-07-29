using Clovent.Domain;

namespace Clovent.Restaurant.Orders.Events;

/// <summary>Raised when an <see cref="Order"/> is assigned its Daily Sales Number.</summary>
public sealed record OrderDailySalesNumberAssigned(OrderId OrderId, int DailySalesNumber, DateTimeOffset OccurredOnUtc) : IDomainEvent;
