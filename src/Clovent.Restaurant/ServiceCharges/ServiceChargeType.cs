namespace Clovent.Restaurant.ServiceCharges;

/// <summary>How a <see cref="ServiceCharge"/>'s <see cref="ServiceCharge.Value"/> is interpreted.</summary>
public enum ServiceChargeType
{
    /// <summary>A percentage (0-100) of the order subtotal.</summary>
    Percentage,

    /// <summary>A fixed currency amount.</summary>
    FixedAmount
}
