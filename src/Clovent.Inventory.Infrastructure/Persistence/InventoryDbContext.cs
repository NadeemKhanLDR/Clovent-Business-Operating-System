using Clovent.Inventory.Adjustments;
using Clovent.Inventory.Transactions;
using Clovent.Inventory.Transfers;
using Clovent.Inventory.WarehouseStocks;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Inventory.Infrastructure.Persistence;

/// <summary>
/// EF Core persistence context for the Inventory bounded context (Milestone
/// 14, "Product Catalog &amp; Inventory Foundation"). Tables live under the
/// <c>Inventory</c> schema, mirroring how <c>CatalogDbContext</c> uses the
/// <c>Catalog</c> schema.
/// </summary>
public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    /// <summary>WarehouseStock aggregates.</summary>
    public DbSet<WarehouseStock> WarehouseStocks => Set<WarehouseStock>();

    /// <summary>InventoryTransaction ledger entries.</summary>
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();

    /// <summary>StockAdjustment aggregates.</summary>
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();

    /// <summary>StockTransfer aggregates.</summary>
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
    }
}
