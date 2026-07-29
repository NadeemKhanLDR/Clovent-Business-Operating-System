using Clovent.Domain;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.ServiceCharges.Events;

namespace Clovent.Restaurant.ServiceCharges;

/// <summary>
/// A service charge (e.g. an automatic gratuity for large parties) applied
/// to one <see cref="Orders.Order"/> - the additive counterpart to
/// <see cref="Discounts.Discount"/>, same shape, same "applied instance, not
/// a reusable definition" reasoning, immutable once created.
/// </summary>
public sealed class ServiceCharge : AggregateRoot<ServiceChargeId>
{
    /// <summary>The order this service charge applies to, fixed at creation.</summary>
    public OrderId OrderId { get; }

    /// <summary>Whether <see cref="Value"/> is a percentage or a fixed amount, fixed at creation.</summary>
    public ServiceChargeType ServiceChargeType { get; }

    /// <summary>The charge's magnitude - a 0-100 percentage or a currency amount, depending on <see cref="ServiceChargeType"/>.</summary>
    public decimal Value { get; }

    /// <summary>Why this charge was applied.</summary>
    public string Reason { get; }

    /// <summary>UTC instant this charge was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private ServiceCharge(ServiceChargeId id, OrderId orderId, ServiceChargeType serviceChargeType, decimal value, string reason, DateTimeOffset createdAtUtc)
    {
        Id = id;
        OrderId = orderId;
        ServiceChargeType = serviceChargeType;
        Value = value;
        Reason = reason;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Creates a new service charge for the given order.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is negative, or a <see cref="ServiceChargeType.Percentage"/> greater than 100.</exception>
    /// <exception cref="ArgumentException"><paramref name="reason"/> is empty.</exception>
    public static ServiceCharge Create(OrderId orderId, ServiceChargeType serviceChargeType, decimal value, string reason)
    {
        RequireValidValue(serviceChargeType, value);
        reason = RequireReason(reason);

        var now = DateTimeOffset.UtcNow;
        var charge = new ServiceCharge(ServiceChargeId.New(), orderId, serviceChargeType, value, reason, now);
        charge.AddDomainEvent(new ServiceChargeCreated(charge.Id, charge.OrderId, charge.ServiceChargeType, charge.Value, now));
        return charge;
    }

    private static void RequireValidValue(ServiceChargeType serviceChargeType, decimal value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), value, "Service charge value cannot be negative.");

        if (serviceChargeType == ServiceChargeType.Percentage && value > 100)
            throw new ArgumentOutOfRangeException(nameof(value), value, "Percentage service charge cannot exceed 100.");
    }

    private static string RequireReason(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Service charge reason is required.", nameof(value));

        return value.Trim();
    }
}
