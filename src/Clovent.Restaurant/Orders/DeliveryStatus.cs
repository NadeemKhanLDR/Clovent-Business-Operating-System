namespace Clovent.Restaurant.Orders;

/// <summary>Lifecycle fulfillment stage of a delivery order.</summary>
public enum DeliveryStatus
{
    /// <summary>Not a delivery order or unassigned.</summary>
    None,

    /// <summary>Delivery order received and confirmed.</summary>
    Received,

    /// <summary>Kitchen is currently preparing the order.</summary>
    Preparing,

    /// <summary>Order is packaged and ready for pickup / dispatch.</summary>
    Ready,

    /// <summary>Rider has dispatched with the order.</summary>
    OutForDelivery,

    /// <summary>Delivered to customer successfully.</summary>
    Delivered,

    /// <summary>Delivery cancelled.</summary>
    Cancelled
}
