using System;
using Clovent.Catalog.Variants;
using Clovent.Inventory.Adjustments;
using Clovent.Inventory.Transactions;
using Clovent.Inventory.Transfers;
using Clovent.Inventory.WarehouseStocks;
using Clovent.MasterData.Warehouses;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Clovent.Inventory.Infrastructure.Persistence;

/// <summary>
/// EF Core <see cref="ValueConverter{TModel,TProvider}"/>s shared across
/// this project's entity type configurations - see
/// <c>Clovent.Catalog.Infrastructure.Persistence.ValueConverters</c> for
/// the identical pattern and reasoning.
/// </summary>
internal static class ValueConverters
{
    /// <summary><see cref="ProductVariantId"/> (from <c>Clovent.Catalog</c>) &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<ProductVariantId, Guid> ProductVariantIdConverter =
        new(id => id.Value, value => new ProductVariantId(value));

    /// <summary><see cref="WarehouseId"/> (from <c>Clovent.MasterData</c>) &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<WarehouseId, Guid> WarehouseIdConverter =
        new(id => id.Value, value => new WarehouseId(value));

    /// <summary><see cref="WarehouseStockId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<WarehouseStockId, Guid> WarehouseStockIdConverter =
        new(id => id.Value, value => new WarehouseStockId(value));

    /// <summary><see cref="InventoryTransactionId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<InventoryTransactionId, Guid> InventoryTransactionIdConverter =
        new(id => id.Value, value => new InventoryTransactionId(value));

    /// <summary><see cref="StockAdjustmentId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<StockAdjustmentId, Guid> StockAdjustmentIdConverter =
        new(id => id.Value, value => new StockAdjustmentId(value));

    /// <summary><see cref="StockTransferId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<StockTransferId, Guid> StockTransferIdConverter =
        new(id => id.Value, value => new StockTransferId(value));

    /// <summary>
    /// <see cref="DateTimeOffset"/> (always UTC in this bounded context) &lt;-&gt; UTC ticks
    /// (<see cref="long"/>). <c>InventoryTransactionRepository.GetRecentAsync</c> orders by
    /// <see cref="Transactions.InventoryTransaction.OccurredAtUtc"/>, and the SQLite provider
    /// refuses to translate <c>ORDER BY</c> over a raw <see cref="DateTimeOffset"/> column
    /// (ambiguous once mixed offsets are possible); ticks are a plain, monotonically
    /// comparable <see cref="long"/> that both SQLite and SQL Server can sort natively.
    /// </summary>
    public static readonly ValueConverter<DateTimeOffset, long> DateTimeOffsetToUtcTicksConverter =
        new(value => value.UtcTicks, value => new DateTimeOffset(value, TimeSpan.Zero));
}
