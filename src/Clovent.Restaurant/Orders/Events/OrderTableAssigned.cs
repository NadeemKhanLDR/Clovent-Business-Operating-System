using Clovent.Domain;
using Clovent.Restaurant.Tables;

namespace Clovent.Restaurant.Orders.Events;

/// <summary>Raised when an <see cref="Order"/> is assigned to a (possibly different) <see cref="Tables.Table"/>.</summary>
public sealed record OrderTableAssigned(OrderId OrderId, TableId TableId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
