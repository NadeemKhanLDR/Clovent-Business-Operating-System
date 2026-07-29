using Clovent.Domain;

namespace Clovent.Restaurant.PaymentMethods.Events;

/// <summary>Raised when a <see cref="PaymentMethod"/> is (re)activated.</summary>
public sealed record PaymentMethodActivated(PaymentMethodId PaymentMethodId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
