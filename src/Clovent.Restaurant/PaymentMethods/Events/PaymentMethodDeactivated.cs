using Clovent.Domain;

namespace Clovent.Restaurant.PaymentMethods.Events;

/// <summary>Raised when a <see cref="PaymentMethod"/> is deactivated.</summary>
public sealed record PaymentMethodDeactivated(PaymentMethodId PaymentMethodId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
