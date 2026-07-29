namespace Clovent.Restaurant.Orders;

/// <summary>How an <see cref="Order"/> is being served.</summary>
public enum OrderType
{
    /// <summary>Served at a <see cref="Tables.Table"/> - requires a table assignment.</summary>
    DineIn,

    /// <summary>Prepared for the customer to take away - never has a table.</summary>
    TakeAway
}
