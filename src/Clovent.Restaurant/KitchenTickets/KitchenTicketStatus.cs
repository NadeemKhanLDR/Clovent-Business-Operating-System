namespace Clovent.Restaurant.KitchenTickets;

/// <summary>A <see cref="KitchenTicket"/>'s workflow state, tracked on the Kitchen Ticket Viewer.</summary>
public enum KitchenTicketStatus
{
    /// <summary>Sent to the kitchen, not yet started.</summary>
    New,

    /// <summary>Being prepared.</summary>
    InProgress,

    /// <summary>Prepared, waiting to be served.</summary>
    Ready,

    /// <summary>Delivered to the table/customer.</summary>
    Served,

    /// <summary>Withdrawn before completion (e.g. the order itself was voided).</summary>
    Cancelled
}
