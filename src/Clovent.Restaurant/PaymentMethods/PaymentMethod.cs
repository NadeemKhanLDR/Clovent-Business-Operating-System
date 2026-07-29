using Clovent.Domain;
using Clovent.Restaurant.PaymentMethods.Events;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using Clovent.Restaurant.Shared;

namespace Clovent.Restaurant.PaymentMethods;

/// <summary>A way a <see cref="Payments.Payment"/> can be tendered (e.g. Cash, Credit Card, Mobile Wallet) - reference data shared across the restaurant.</summary>
public sealed class PaymentMethod : AggregateRoot<PaymentMethodId>
{
    /// <summary>The payment method's display name.</summary>
    public PaymentMethodName Name { get; private set; }

    /// <summary>The payment method's current lifecycle state.</summary>
    public RestaurantStatus Status { get; private set; }

    /// <summary>UTC instant this payment method was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private PaymentMethod(PaymentMethodId id, PaymentMethodName name, RestaurantStatus status, DateTimeOffset createdAtUtc)
    {
        Id = id;
        Name = name;
        Status = status;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Creates a new, active payment method.</summary>
    public static PaymentMethod Create(PaymentMethodName name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var now = DateTimeOffset.UtcNow;
        var method = new PaymentMethod(PaymentMethodId.New(), name, RestaurantStatus.Active, now);
        method.AddDomainEvent(new PaymentMethodCreated(method.Id, method.Name, now));
        return method;
    }

    /// <summary>Renames the payment method. A no-op (no event raised) if unchanged.</summary>
    public void Rename(PaymentMethodName name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (Name == name) return;

        Name = name;
        AddDomainEvent(new PaymentMethodRenamed(Id, name, DateTimeOffset.UtcNow));
    }

    /// <summary>Activates the payment method.</summary>
    /// <exception cref="RestaurantDomainException">The payment method is already active.</exception>
    public void Activate()
    {
        if (Status == RestaurantStatus.Active)
            throw RestaurantDomainException.PaymentMethodAlreadyActive(Id);

        Status = RestaurantStatus.Active;
        AddDomainEvent(new PaymentMethodActivated(Id, DateTimeOffset.UtcNow));
    }

    /// <summary>Deactivates the payment method.</summary>
    /// <exception cref="RestaurantDomainException">The payment method is not active.</exception>
    public void Deactivate()
    {
        if (Status != RestaurantStatus.Active)
            throw RestaurantDomainException.PaymentMethodNotActive(Id);

        Status = RestaurantStatus.Inactive;
        AddDomainEvent(new PaymentMethodDeactivated(Id, DateTimeOffset.UtcNow));
    }
}
