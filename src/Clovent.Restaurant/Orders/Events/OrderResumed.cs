using Clovent.Domain;

namespace Clovent.Restaurant.Orders.Events;

/// <summary>Raised when a held <see cref="Order"/> resumes.</summary>
public sealed record OrderResumed(OrderId OrderId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
