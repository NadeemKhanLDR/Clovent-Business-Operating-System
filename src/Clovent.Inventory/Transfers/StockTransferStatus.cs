namespace Clovent.Inventory.Transfers;

/// <summary>A <see cref="StockTransfer"/>'s workflow state.</summary>
public enum StockTransferStatus
{
    /// <summary>Proposed but not yet moved between warehouses.</summary>
    Pending,

    /// <summary>Completed - stock moved from source to destination. A one-way transition, no undo.</summary>
    Completed,

    /// <summary>Cancelled before being completed.</summary>
    Cancelled
}
