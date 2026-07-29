using Clovent.Domain;
using Clovent.Restaurant.PaymentMethods.ValueObjects;

namespace Clovent.Restaurant.PaymentMethods.Events;

/// <summary>Raised when a <see cref="PaymentMethod"/>'s name changes.</summary>
public sealed record PaymentMethodRenamed(PaymentMethodId PaymentMethodId, PaymentMethodName Name, DateTimeOffset OccurredOnUtc) : IDomainEvent;
