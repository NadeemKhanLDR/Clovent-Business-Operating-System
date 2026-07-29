namespace Clovent.Inventory.Transactions;

/// <summary>What kind of stock movement an <see cref="InventoryTransaction"/> records.</summary>
public enum InventoryTransactionType
{
    /// <summary>Stock received into a warehouse.</summary>
    Receipt,

    /// <summary>Stock issued out of a warehouse.</summary>
    Issue,

    /// <summary>Stock adjusted by a <see cref="Adjustments.StockAdjustment"/>.</summary>
    Adjustment,

    /// <summary>Stock arriving at a warehouse as the destination of a <see cref="Transfers.StockTransfer"/>.</summary>
    TransferIn,

    /// <summary>Stock leaving a warehouse as the source of a <see cref="Transfers.StockTransfer"/>.</summary>
    TransferOut
}
