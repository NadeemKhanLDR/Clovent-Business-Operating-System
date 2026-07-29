using Clovent.Domain;

namespace Clovent.Restaurant.Payments.Events;

/// <summary>Raised when a <see cref="Payment"/> is voided.</summary>
public sealed record PaymentVoided(PaymentId PaymentId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
