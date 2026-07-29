using Clovent.Domain;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;

namespace Clovent.Restaurant.Payments.Events;

/// <summary>Raised when a new <see cref="Payment"/> is recorded.</summary>
public sealed record PaymentCreated(PaymentId PaymentId, OrderId OrderId, PaymentMethodId PaymentMethodId, decimal Amount, DateTimeOffset OccurredOnUtc) : IDomainEvent;
