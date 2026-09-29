namespace Clovent.Restaurant.Orders;

/// <summary>Origin of an <see cref="Order"/>.</summary>
public enum OrderSource
{
    /// <summary>Direct in-person order placed at the counter / register.</summary>
    WalkIn,

    /// <summary>Order received over the phone.</summary>
    Phone,

    /// <summary>Order received via an online channel / aggregator / web ordering.</summary>
    Online
}
