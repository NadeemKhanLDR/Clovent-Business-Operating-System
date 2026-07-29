using Clovent.Domain;
using Clovent.Restaurant.Orders;

namespace Clovent.Restaurant.ServiceCharges.Events;

/// <summary>Raised when a new <see cref="ServiceCharge"/> is created.</summary>
public sealed record ServiceChargeCreated(ServiceChargeId ServiceChargeId, OrderId OrderId, ServiceChargeType ServiceChargeType, decimal Value, DateTimeOffset OccurredOnUtc) : IDomainEvent;
