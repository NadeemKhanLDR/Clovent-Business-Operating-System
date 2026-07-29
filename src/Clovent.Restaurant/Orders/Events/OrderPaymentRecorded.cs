using Clovent.Domain;
using Clovent.Restaurant.Payments;

namespace Clovent.Restaurant.Orders.Events;

/// <summary>Raised when a <see cref="Payments.Payment"/> is recorded against an <see cref="Order"/>.</summary>
public sealed record OrderPaymentRecorded(OrderId OrderId, PaymentId PaymentId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
