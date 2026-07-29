using Clovent.Domain;
using Clovent.Restaurant.PaymentMethods.ValueObjects;

namespace Clovent.Restaurant.PaymentMethods.Events;

/// <summary>Raised when a new <see cref="PaymentMethod"/> is created.</summary>
public sealed record PaymentMethodCreated(PaymentMethodId PaymentMethodId, PaymentMethodName Name, DateTimeOffset OccurredOnUtc) : IDomainEvent;
