namespace Clovent.Restaurant.Orders;

/// <summary>An <see cref="Order"/>'s workflow state.</summary>
public enum OrderStatus
{
    /// <summary>Actively being built/served - lines, discounts, service charges, and payments may all still change.</summary>
    Open,

    /// <summary>Temporarily suspended (<see cref="Order.Hold"/>) - resumes back to <see cref="Open"/>.</summary>
    Held,

    /// <summary>Fully paid and closed.</summary>
    Completed,

    /// <summary>Invalidated - can happen from <see cref="Open"/>, <see cref="Held"/>, or even <see cref="Completed"/> (a managerial override).</summary>
    Voided,

    /// <summary>Abandoned before any payment was recorded.</summary>
    Cancelled
}
