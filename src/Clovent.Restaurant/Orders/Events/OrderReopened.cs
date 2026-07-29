using Clovent.Domain;

namespace Clovent.Restaurant.Orders.Events;

/// <summary>Raised when a voided or cancelled <see cref="Order"/> is reopened.</summary>
public sealed record OrderReopened(OrderId OrderId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
