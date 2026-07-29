using Clovent.Domain;
using Clovent.Restaurant.ServiceCharges;

namespace Clovent.Restaurant.Orders.Events;

/// <summary>Raised when a <see cref="ServiceCharges.ServiceCharge"/> is removed from an <see cref="Order"/>.</summary>
public sealed record OrderServiceChargeRemoved(OrderId OrderId, ServiceChargeId ServiceChargeId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
