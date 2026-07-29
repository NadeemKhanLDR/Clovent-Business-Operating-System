namespace Clovent.Inventory.Transactions;

/// <summary>What kind of stock movement an <see cref="InventoryTransaction"/> records.</summary>
public enum InventoryTransactionType
{
    /// <summary>
    /// The first stock ever recorded for a warehouse/variant pairing - the
    /// gap-closing "Opening Stock" feature's marker, distinguishing a
    /// pairing's very first stock-in from an ordinary <see cref="Receipt"/>
    /// restock. Written by the same handler that would otherwise write
    /// <see cref="Receipt"/>, based on whether the <see cref="WarehouseStocks.WarehouseStock"/>
    /// row already existed.
    /// </summary>
    OpeningBalance,

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
