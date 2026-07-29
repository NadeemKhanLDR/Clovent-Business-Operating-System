using Clovent.Domain;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.Payments.Events;

namespace Clovent.Restaurant.Payments;

/// <summary>
/// One amount tendered against one <see cref="Orders.Order"/> via one
/// <see cref="PaymentMethods.PaymentMethod"/> - "Partial Payment"/"Multiple
/// Payments"/"Split Bill" are all a natural consequence of an order
/// accumulating several of these (the same "let the data model imply the
/// feature" reasoning <c>CatalogArchitecture.md</c>'s "multiple prices"
/// section already applies) rather than a special mechanism. A payment
/// recorded in error is <see cref="Void"/>'d, never edited or deleted - a
/// correction is a new payment, not a rewritten one.
/// </summary>
public sealed class Payment : AggregateRoot<PaymentId>
{
    /// <summary>The order this payment is recorded against, fixed at creation.</summary>
    public OrderId OrderId { get; }

    /// <summary>The method this payment was tendered through, fixed at creation.</summary>
    public PaymentMethodId PaymentMethodId { get; }

    /// <summary>The amount tendered, fixed at creation.</summary>
    public decimal Amount { get; }

    /// <summary>Whether this payment has been voided - excluded from the order's paid total, but never physically removed once recorded.</summary>
    public bool IsVoided { get; private set; }

    /// <summary>UTC instant this payment was recorded.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private Payment(PaymentId id, OrderId orderId, PaymentMethodId paymentMethodId, decimal amount, bool isVoided, DateTimeOffset createdAtUtc)
    {
        Id = id;
        OrderId = orderId;
        PaymentMethodId = paymentMethodId;
        Amount = amount;
        IsVoided = isVoided;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Records a new payment against the given order.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="amount"/> is not positive.</exception>
    public static Payment Create(OrderId orderId, PaymentMethodId paymentMethodId, decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Payment amount must be positive.");

        var now = DateTimeOffset.UtcNow;
        var payment = new Payment(PaymentId.New(), orderId, paymentMethodId, amount, false, now);
        payment.AddDomainEvent(new PaymentCreated(payment.Id, payment.OrderId, payment.PaymentMethodId, payment.Amount, now));
        return payment;
    }

    /// <summary>Voids this payment (recorded in error). One-way - a correction is a new payment, not an unvoid.</summary>
    /// <exception cref="RestaurantDomainException">The payment is already voided.</exception>
    public void Void()
    {
        if (IsVoided)
            throw RestaurantDomainException.PaymentAlreadyVoided(Id);

        IsVoided = true;
        AddDomainEvent(new PaymentVoided(Id, DateTimeOffset.UtcNow));
    }
}
