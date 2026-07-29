using Clovent.Domain;
using Clovent.Restaurant.ServiceCharges;

namespace Clovent.Restaurant.Orders.Events;

/// <summary>Raised when a <see cref="ServiceCharges.ServiceCharge"/> is applied to an <see cref="Order"/>.</summary>
public sealed record OrderServiceChargeApplied(OrderId OrderId, ServiceChargeId ServiceChargeId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
