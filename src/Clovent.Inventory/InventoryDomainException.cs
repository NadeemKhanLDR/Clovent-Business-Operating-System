using Clovent.Domain;
using Clovent.Inventory.Adjustments;
using Clovent.Inventory.Transfers;
using Clovent.Inventory.WarehouseStocks;
using Clovent.MasterData.Warehouses;

namespace Clovent.Inventory;

/// <summary>
/// Raised when an Inventory aggregate operation would violate one of its
/// invariants - mirrors <c>Clovent.Catalog.CatalogDomainException</c> and
/// <c>Clovent.MasterData.MasterDataDomainException</c> exactly: one sealed
/// type, one static factory method per rule.
/// </summary>
public sealed class InventoryDomainException : DomainException
{
    private InventoryDomainException(string message) : base(message)
    {
    }

    /// <summary>An Issue()/Reserve() was attempted that would leave insufficient stock.</summary>
    public static InventoryDomainException InsufficientStock(WarehouseStockId warehouseStockId) =>
        new($"Warehouse stock '{warehouseStockId}' does not have sufficient quantity available for this operation.");

    /// <summary>A Release() was attempted for more than is currently reserved.</summary>
    public static InventoryDomainException InsufficientReservedQuantity(WarehouseStockId warehouseStockId) =>
        new($"Warehouse stock '{warehouseStockId}' does not have that much quantity reserved.");

    /// <summary>A SetStockLevels()/Create() was attempted with a positive maximum below the minimum.</summary>
    public static InventoryDomainException InvalidStockLevelRange(decimal minimumStock, decimal maximumStock) =>
        new($"Maximum stock ({maximumStock}) cannot be less than minimum stock ({minimumStock}).");

    /// <summary>A StockAdjustment Apply()/Cancel() was attempted while not Pending.</summary>
    public static InventoryDomainException StockAdjustmentNotPending(StockAdjustmentId stockAdjustmentId) =>
        new($"Stock adjustment '{stockAdjustmentId}' is not pending.");

    /// <summary>A StockTransfer was created with the same warehouse as source and destination.</summary>
    public static InventoryDomainException TransferSourceEqualsDestination(WarehouseId warehouseId) =>
        new($"A stock transfer's source and destination warehouse cannot both be '{warehouseId}'.");

    /// <summary>A StockTransfer Complete()/Cancel() was attempted while not Pending.</summary>
    public static InventoryDomainException StockTransferNotPending(StockTransferId stockTransferId) =>
        new($"Stock transfer '{stockTransferId}' is not pending.");
}
