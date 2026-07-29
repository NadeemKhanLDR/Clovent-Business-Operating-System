using Clovent.Catalog.Variants;
using Clovent.Domain;
using Clovent.Inventory.WarehouseStocks.Events;
using Clovent.MasterData.Warehouses;

namespace Clovent.Inventory.WarehouseStocks;

/// <summary>
/// The live stock balance for one <see cref="Catalog.Variants.ProductVariant"/>
/// at one <see cref="Warehouse"/> - quantity on hand, quantity reserved,
/// min/max stock levels, and the negative-stock policy. Exactly one record
/// per (warehouse, variant) pair - enforced by a unique index at the
/// Infrastructure layer, the same pattern <c>BusinessSettings</c> uses for
/// "one per organization." Mutating on-hand quantity here (<see cref="Receive"/>/<see cref="Issue"/>)
/// is always paired, by the Application-layer handler that calls it, with
/// creating an <see cref="Transactions.InventoryTransaction"/> ledger entry -
/// this aggregate itself has no visibility into the transaction ledger,
/// the same "cross-aggregate consistency is the handler's job" pattern
/// already established for <c>Organization.AddCompany</c>.
/// </summary>
public sealed class WarehouseStock : AggregateRoot<WarehouseStockId>
{
    /// <summary>The warehouse this balance belongs to, fixed at creation.</summary>
    public WarehouseId WarehouseId { get; }

    /// <summary>The variant this balance tracks, fixed at creation.</summary>
    public ProductVariantId ProductVariantId { get; }

    /// <summary>Physical quantity currently held.</summary>
    public decimal QuantityOnHand { get; private set; }

    /// <summary>Quantity currently reserved (e.g. against open sales orders) - not available for a new reservation or issue.</summary>
    public decimal QuantityReserved { get; private set; }

    /// <summary>The reorder floor - not enforced here, a read signal for purchasing/reporting.</summary>
    public decimal MinimumStock { get; private set; }

    /// <summary>The stocking ceiling - not enforced here, a read signal for purchasing/reporting.</summary>
    public decimal MaximumStock { get; private set; }

    /// <summary>Whether <see cref="Issue"/> may drive <see cref="QuantityOnHand"/> below zero.</summary>
    public bool AllowNegativeStock { get; private set; }

    /// <summary>UTC instant this balance record was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>UTC instant this balance was last changed.</summary>
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>Quantity on hand not already committed to a reservation.</summary>
    public decimal QuantityAvailable => QuantityOnHand - QuantityReserved;

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private WarehouseStock(
        WarehouseStockId id,
        WarehouseId warehouseId,
        ProductVariantId productVariantId,
        decimal quantityOnHand,
        decimal quantityReserved,
        decimal minimumStock,
        decimal maximumStock,
        bool allowNegativeStock,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        WarehouseId = warehouseId;
        ProductVariantId = productVariantId;
        QuantityOnHand = quantityOnHand;
        QuantityReserved = quantityReserved;
        MinimumStock = minimumStock;
        MaximumStock = maximumStock;
        AllowNegativeStock = allowNegativeStock;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>Creates a new, zero-balance stock record for a warehouse/variant pair.</summary>
    /// <exception cref="InventoryDomainException"><paramref name="maximumStock"/> is positive and less than <paramref name="minimumStock"/>.</exception>
    public static WarehouseStock Create(WarehouseId warehouseId, ProductVariantId productVariantId, decimal minimumStock = 0, decimal maximumStock = 0, bool allowNegativeStock = false)
    {
        RequireValidStockLevels(minimumStock, maximumStock);

        var now = DateTimeOffset.UtcNow;
        var stock = new WarehouseStock(WarehouseStockId.New(), warehouseId, productVariantId, 0, 0, minimumStock, maximumStock, allowNegativeStock, now, now);
        stock.AddDomainEvent(new WarehouseStockCreated(stock.Id, stock.WarehouseId, stock.ProductVariantId, now));
        return stock;
    }

    /// <summary>Receives stock, increasing quantity on hand.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="quantity"/> is not positive.</exception>
    public void Receive(decimal quantity)
    {
        RequirePositive(quantity);

        QuantityOnHand += quantity;
        Touch();
        AddDomainEvent(new StockReceived(Id, quantity, DateTimeOffset.UtcNow));
    }

    /// <summary>Issues stock, decreasing quantity on hand.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="quantity"/> is not positive.</exception>
    /// <exception cref="InventoryDomainException">The result would be negative and <see cref="AllowNegativeStock"/> is <see langword="false"/>.</exception>
    public void Issue(decimal quantity)
    {
        RequirePositive(quantity);

        if (!AllowNegativeStock && QuantityOnHand - quantity < 0)
            throw InventoryDomainException.InsufficientStock(Id);

        QuantityOnHand -= quantity;
        Touch();
        AddDomainEvent(new StockIssued(Id, quantity, DateTimeOffset.UtcNow));
    }

    /// <summary>Reserves quantity against this balance (e.g. for an open sales order).</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="quantity"/> is not positive.</exception>
    /// <exception cref="InventoryDomainException"><paramref name="quantity"/> exceeds <see cref="QuantityAvailable"/>.</exception>
    public void Reserve(decimal quantity)
    {
        RequirePositive(quantity);

        if (quantity > QuantityAvailable)
            throw InventoryDomainException.InsufficientStock(Id);

        QuantityReserved += quantity;
        Touch();
        AddDomainEvent(new StockReserved(Id, quantity, DateTimeOffset.UtcNow));
    }

    /// <summary>Releases a previously-made reservation.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="quantity"/> is not positive.</exception>
    /// <exception cref="InventoryDomainException"><paramref name="quantity"/> exceeds <see cref="QuantityReserved"/>.</exception>
    public void Release(decimal quantity)
    {
        RequirePositive(quantity);

        if (quantity > QuantityReserved)
            throw InventoryDomainException.InsufficientReservedQuantity(Id);

        QuantityReserved -= quantity;
        Touch();
        AddDomainEvent(new StockReservationReleased(Id, quantity, DateTimeOffset.UtcNow));
    }

    /// <summary>Sets the minimum/maximum stock levels.</summary>
    /// <exception cref="InventoryDomainException"><paramref name="maximumStock"/> is positive and less than <paramref name="minimumStock"/>.</exception>
    public void SetStockLevels(decimal minimumStock, decimal maximumStock)
    {
        RequireValidStockLevels(minimumStock, maximumStock);

        MinimumStock = minimumStock;
        MaximumStock = maximumStock;
        Touch();
        AddDomainEvent(new StockLevelsChanged(Id, minimumStock, maximumStock, DateTimeOffset.UtcNow));
    }

    /// <summary>Sets whether <see cref="Issue"/> may drive the balance negative.</summary>
    public void SetNegativeStockPolicy(bool allowNegativeStock)
    {
        if (AllowNegativeStock == allowNegativeStock) return;

        AllowNegativeStock = allowNegativeStock;
        Touch();
        AddDomainEvent(new NegativeStockPolicyChanged(Id, allowNegativeStock, DateTimeOffset.UtcNow));
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;

    private static void RequirePositive(decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity must be positive.");
    }

    private static void RequireValidStockLevels(decimal minimumStock, decimal maximumStock)
    {
        if (maximumStock > 0 && maximumStock < minimumStock)
            throw InventoryDomainException.InvalidStockLevelRange(minimumStock, maximumStock);
    }
}
