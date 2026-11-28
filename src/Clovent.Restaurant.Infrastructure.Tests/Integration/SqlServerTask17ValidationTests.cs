using Clovent.Catalog.Variants;
using Clovent.Inventory.Application.WarehouseStocks.Commands;
using Clovent.Inventory.Infrastructure.Persistence;
using Clovent.Inventory.Infrastructure.Repositories;
using Clovent.Inventory.Transactions;
using Clovent.Inventory.WarehouseStocks;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Infrastructure.Repositories;
using Clovent.Restaurant.KitchenTickets;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Outbox;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Tables;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Integration;

/// <summary>
/// TASK-17 Real SQL Server Integration, Multi-Terminal Concurrency &amp; Data Integrity Validation.
/// Executes against a disposable isolated database on <c>(localdb)\MSSQLLocalDB</c>.
/// </summary>
public sealed class SqlServerTask17ValidationTests : IAsyncLifetime
{
    private readonly string _databaseName;
    private readonly string _connectionString;
    private readonly DbContextOptions<RestaurantDbContext> _restaurantOptions;
    private readonly DbContextOptions<InventoryDbContext> _inventoryOptions;

    public SqlServerTask17ValidationTests()
    {
        _databaseName = $"CBOS_Task17_{Guid.NewGuid():N}";
        _connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={_databaseName};Integrated Security=true;TrustServerCertificate=true;Connection Timeout=30;";

        _restaurantOptions = new DbContextOptionsBuilder<RestaurantDbContext>()
            .UseSqlServer(_connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "Restaurant"))
            .Options;

        _inventoryOptions = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlServer(_connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "Inventory"))
            .Options;
    }

    public async Task InitializeAsync()
    {
        await using var restaurantDb = new RestaurantDbContext(_restaurantOptions);
        await restaurantDb.Database.MigrateAsync();

        await using var inventoryDb = new InventoryDbContext(_inventoryOptions);
        await inventoryDb.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        try
        {
            await using var restaurantDb = new RestaurantDbContext(_restaurantOptions);
            await restaurantDb.Database.EnsureDeletedAsync();
        }
        catch
        {
            // Best-effort cleanup of disposable test DB
        }
    }

    [Fact]
    public async Task CleanDatabase_MigrateAsync_AppliesIsolatedSchemaMigrationHistories()
    {
        await using var db = new RestaurantDbContext(_restaurantOptions);
        var restaurantMigrations = await db.Database.GetAppliedMigrationsAsync();
        Assert.NotEmpty(restaurantMigrations);

        await using var invDb = new InventoryDbContext(_inventoryOptions);
        var inventoryMigrations = await invDb.Database.GetAppliedMigrationsAsync();
        Assert.NotEmpty(inventoryMigrations);
    }

    [Fact]
    public async Task PaymentIdempotencyKey_UniqueIndex_EnforcedBySqlServer()
    {
        var orderId = OrderId.New();
        var methodId = PaymentMethodId.New();
        var key = $"idem-{Guid.NewGuid():N}";

        await using (var db1 = new RestaurantDbContext(_restaurantOptions))
        {
            db1.Payments.Add(Payment.Create(orderId, methodId, 25.00m, idempotencyKey: key));
            await db1.SaveChangesAsync();
        }

        await using (var db2 = new RestaurantDbContext(_restaurantOptions))
        {
            db2.Payments.Add(Payment.Create(orderId, methodId, 25.00m, idempotencyKey: key));
            await Assert.ThrowsAsync<DbUpdateException>(() => db2.SaveChangesAsync());
        }

        // Clean-context recovery loads original committed payment
        await using (var db3 = new RestaurantDbContext(_restaurantOptions))
        {
            var repo = new PaymentRepository(db3);
            var existing = await repo.GetByIdempotencyKeyAsync(key);
            Assert.NotNull(existing);
            Assert.Equal(25.00m, existing!.Amount);
        }
    }

    [Fact]
    public async Task ExplicitTransactionRollback_LeavesNoPartialOrderOrOutboxState()
    {
        var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
        await using (var setupDb = new RestaurantDbContext(_restaurantOptions))
        {
            setupDb.Orders.Add(order);
            await setupDb.SaveChangesAsync();
        }

        await using (var txDb = new RestaurantDbContext(_restaurantOptions))
        {
            await using var tx = await txDb.Database.BeginTransactionAsync();
            var trackedOrder = await txDb.Orders.FirstAsync(o => o.Id == order.Id);
            trackedOrder.Complete();
            txDb.OutboxMessages.Add(OutboxMessage.Create(
                OutboxMessageType.InventoryPosting,
                "Order",
                order.Id.Value.ToString(),
                order.Id.Value.ToString(),
                "{}",
                $"inv-{order.Id.Value}"));
            await txDb.SaveChangesAsync();

            await tx.RollbackAsync();
        }

        await using (var verifyDb = new RestaurantDbContext(_restaurantOptions))
        {
            var reloaded = await verifyDb.Orders.FirstAsync(o => o.Id == order.Id);
            Assert.Equal(OrderStatus.Open, reloaded.Status);
            Assert.Empty(await verifyDb.OutboxMessages.Where(m => m.AggregateId == order.Id.Value.ToString()).ToListAsync());
        }
    }

    [Fact]
    public async Task ConcurrentInventoryDeductions_WithSqlAppLock_SerializeWithZeroLostUpdates()
    {
        var warehouseId = WarehouseId.New();
        var variantId = ProductVariantId.New();
        var stock = WarehouseStock.Create(warehouseId, variantId, 0m, 500m, allowNegativeStock: false);
        stock.Receive(100m);

        await using (var setupDb = new InventoryDbContext(_inventoryOptions))
        {
            setupDb.WarehouseStocks.Add(stock);
            await setupDb.SaveChangesAsync();
        }

        const int workerCount = 5;
        const decimal deductPerWorker = 3m;

        var tasks = Enumerable.Range(0, workerCount).Select(async i =>
        {
            await using var db = new InventoryDbContext(_inventoryOptions);
            var uow = new Clovent.Inventory.Infrastructure.Persistence.UnitOfWork(db);
            var stockRepo = new WarehouseStockRepository(db);
            var txRepo = new InventoryTransactionRepository(db);

            await uow.ExecuteStockMovementAtomicallyAsync(
                warehouseId.Value,
                variantId.Value,
                async ct =>
                {
                    var currentStock = await stockRepo.GetByIdAsync(stock.Id, ct)
                        ?? throw new InvalidOperationException("Stock missing");
                    currentStock.Issue(deductPerWorker);
                    await txRepo.AddAsync(
                        InventoryTransaction.Create(
                            warehouseId,
                            variantId,
                            InventoryTransactionType.Issue,
                            deductPerWorker,
                            "Order",
                            Guid.NewGuid(),
                            $"Concurrent-{i}"),
                        ct);
                    return true;
                },
                _ => Task.FromResult(false));
        });

        await Task.WhenAll(tasks);

        await using (var verifyDb = new InventoryDbContext(_inventoryOptions))
        {
            var finalStock = await verifyDb.WarehouseStocks.FirstAsync(s => s.Id == stock.Id);
            var txCount = await verifyDb.InventoryTransactions.CountAsync(t => t.ProductVariantId == variantId);
            Assert.Equal(100m - (workerCount * deductPerWorker), finalStock.QuantityOnHand);
            Assert.Equal(workerCount, txCount);
        }
    }

    [Fact]
    public async Task ConcurrentDuplicateInventoryDeduction_SameReference_DeduplicatedByUniqueIndex()
    {
        var warehouseId = WarehouseId.New();
        var variantId = ProductVariantId.New();
        var orderId = Guid.NewGuid();
        var stock = WarehouseStock.Create(warehouseId, variantId, 0m, 500m, allowNegativeStock: false);
        stock.Receive(100m);

        await using (var setupDb = new InventoryDbContext(_inventoryOptions))
        {
            setupDb.WarehouseStocks.Add(stock);
            await setupDb.SaveChangesAsync();
        }

        var tasks = Enumerable.Range(0, 2).Select(async _ =>
        {
            await using var db = new InventoryDbContext(_inventoryOptions);
            var uow = new Clovent.Inventory.Infrastructure.Persistence.UnitOfWork(db);
            var stockRepo = new WarehouseStockRepository(db);
            var txRepo = new InventoryTransactionRepository(db);

            return await uow.ExecuteStockMovementAtomicallyAsync(
                warehouseId.Value,
                variantId.Value,
                async ct =>
                {
                    var existing = await txRepo.GetByReferenceAsync("Order", orderId, ct);
                    if (existing.Any(t => t.WarehouseId == warehouseId && t.ProductVariantId == variantId && t.TransactionType == InventoryTransactionType.Issue))
                    {
                        return "recovered-precheck";
                    }

                    var currentStock = await stockRepo.GetByIdAsync(stock.Id, ct)!;
                    currentStock!.Issue(5m);
                    await txRepo.AddAsync(
                        InventoryTransaction.Create(warehouseId, variantId, InventoryTransactionType.Issue, 5m, "Order", orderId, "Order"),
                        ct);
                    return "issued";
                },
                _ => Task.FromResult("recovered-unique-index"));
        });

        var results = await Task.WhenAll(tasks);

        await using (var verifyDb = new InventoryDbContext(_inventoryOptions))
        {
            var finalStock = await verifyDb.WarehouseStocks.FirstAsync(s => s.Id == stock.Id);
            var txCount = await verifyDb.InventoryTransactions.CountAsync(t => t.ReferenceId == orderId);
            Assert.Equal(95m, finalStock.QuantityOnHand);
            Assert.Equal(1, txCount);
            Assert.Contains("issued", results);
        }
    }

    [Fact]
    public async Task Task12_OrderKitchenTicketAndTableState_PersistAcrossContextRestart()
    {
        var diningArea = DiningArea.Create(Clovent.Identity.Branches.BranchId.New(), Clovent.Restaurant.DiningAreas.ValueObjects.DiningAreaName.Create("Main Hall"));
        var table = Table.Create(diningArea.Id, EntityCode.Create("T-17"), 4);
        table.Occupy();
        var order = Order.Create(OrderType.DineIn, WarehouseId.New(), table.Id);
        var line = OrderLine.Create(order.Id, ProductVariantId.New(), 2m, 15.00m, 16m, false, "No spice");
        order.AddOrderLine(line.Id);
        var ticket = KitchenTicket.Create(order.Id, [line.Id]);

        await using (var db1 = new RestaurantDbContext(_restaurantOptions))
        {
            db1.DiningAreas.Add(diningArea);
            db1.Tables.Add(table);
            db1.Orders.Add(order);
            db1.OrderLines.Add(line);
            db1.KitchenTickets.Add(ticket);
            await db1.SaveChangesAsync();
        }

        // Context 2 simulates application restart
        await using (var db2 = new RestaurantDbContext(_restaurantOptions))
        {
            var loadedOrder = await db2.Orders.FirstAsync(o => o.Id == order.Id);
            var loadedTicket = await db2.KitchenTickets.FirstAsync(t => t.Id == ticket.Id);
            var loadedTable = await db2.Tables.FirstAsync(t => t.Id == table.Id);

            Assert.Equal(OrderStatus.Open, loadedOrder.Status);
            Assert.Equal(KitchenTicketStatus.New, loadedTicket.Status);
            Assert.Equal(TableOccupancyStatus.Occupied, loadedTable.OccupancyStatus);

            loadedOrder.Cancel("Customer left");
            loadedTicket.Cancel();
            loadedTable.Vacate();
            await db2.SaveChangesAsync();
        }

        await using (var db3 = new RestaurantDbContext(_restaurantOptions))
        {
            var loadedOrder = await db3.Orders.FirstAsync(o => o.Id == order.Id);
            var loadedTicket = await db3.KitchenTickets.FirstAsync(t => t.Id == ticket.Id);
            var loadedTable = await db3.Tables.FirstAsync(t => t.Id == table.Id);

            Assert.Equal(OrderStatus.Cancelled, loadedOrder.Status);
            Assert.Equal(KitchenTicketStatus.Cancelled, loadedTicket.Status);
            Assert.Equal(TableOccupancyStatus.Available, loadedTable.OccupancyStatus);
        }
    }
}
