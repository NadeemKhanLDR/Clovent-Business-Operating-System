using Clovent.Catalog.Infrastructure.Persistence;
using Clovent.Catalog.Infrastructure.Repositories;
using Clovent.Catalog.Prices;
using Clovent.Catalog.Products;
using Clovent.Catalog.Products.ValueObjects;
using Clovent.Catalog.Shared.ValueObjects;
using Clovent.Catalog.UnitsOfMeasure;
using Clovent.Catalog.Variants;
using Clovent.Catalog.Variants.ValueObjects;
using Clovent.Inventory.Infrastructure.Persistence;
using Clovent.Inventory.Infrastructure.Repositories;
using Clovent.Inventory.Transactions;
using Clovent.Inventory.WarehouseStocks;
using Clovent.MasterData.Currencies;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant;
using Clovent.Restaurant.Application.Orders;
using Clovent.Restaurant.Application.OrderLines.Commands;
using Clovent.Restaurant.Application.Orders.Commands;
using Clovent.Restaurant.Application.Orders.Queries;
using Clovent.Restaurant.Application.Payments.Commands;
using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.DiningAreas.ValueObjects;
using Clovent.Restaurant.Discounts;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Infrastructure.Repositories;
using Clovent.Restaurant.KitchenTickets;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Sales;
using Clovent.Restaurant.ServiceCharges;
using Clovent.Restaurant.Tables;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Integration;

/// <summary>
/// Exercises a full Restaurant POS order lifecycle end to end - real EF Core
/// repositories (SQLite, in-memory) across all three bounded contexts a POS
/// sale actually touches (Restaurant, Catalog, Inventory), wired into one
/// real MediatR pipeline so <c>AddOrderLineCommandHandler</c>'s calls into
/// Catalog and <c>CompleteOrderCommandHandler</c>'s calls into Inventory
/// resolve to genuine handlers, not a test double - the milestone's own
/// "integration tests covering full order lifecycle including inventory
/// deduction" requirement. Every other Restaurant test in this solution
/// verifies one bounded context in isolation (Application tests use
/// <c>FakeMediator</c>, Infrastructure tests exercise one repository at a
/// time); this is the one place the seams between contexts are proven to
/// actually fit together.
/// </summary>
public sealed class OrderLifecycleIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly RestaurantDbContext _restaurantDb;
    private readonly CatalogDbContext _catalogDb;
    private readonly InventoryDbContext _inventoryDb;
    private readonly IMediator _mediator;

    public OrderLifecycleIntegrationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _restaurantDb = new RestaurantDbContext(new DbContextOptionsBuilder<RestaurantDbContext>().UseSqlite(_connection).Options);
        _catalogDb = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(_connection).Options);
        _inventoryDb = new InventoryDbContext(new DbContextOptionsBuilder<InventoryDbContext>().UseSqlite(_connection).Options);

        // EnsureCreated()'s "does the database exist?" check is trivially
        // true for every context after the first, since they all share one
        // already-open SQLite connection - it would silently skip creating
        // the second and third context's tables. Only the first context can
        // use EnsureCreated(); the rest must execute their own generated
        // schema directly.
        _restaurantDb.Database.EnsureCreated();
        _catalogDb.Database.ExecuteSqlRaw(_catalogDb.Database.GenerateCreateScript());
        _inventoryDb.Database.ExecuteSqlRaw(_inventoryDb.Database.GenerateCreateScript());

        var services = new ServiceCollection();

        services.AddScoped<IDiningAreaRepository>(_ => new DiningAreaRepository(_restaurantDb));
        services.AddScoped<ITableRepository>(_ => new TableRepository(_restaurantDb));
        services.AddScoped<IOrderRepository>(_ => new OrderRepository(_restaurantDb));
        services.AddScoped<IOrderLineRepository>(_ => new OrderLineRepository(_restaurantDb));
        services.AddScoped<IPaymentRepository>(_ => new PaymentRepository(_restaurantDb));
        services.AddScoped<IPaymentMethodRepository>(_ => new PaymentMethodRepository(_restaurantDb));
        services.AddScoped<IDiscountRepository>(_ => new DiscountRepository(_restaurantDb));
        services.AddScoped<IServiceChargeRepository>(_ => new ServiceChargeRepository(_restaurantDb));
        services.AddScoped<IKitchenTicketRepository>(_ => new KitchenTicketRepository(_restaurantDb));
        services.AddScoped<IDailySalesSequenceRepository>(_ => new DailySalesSequenceRepository(_restaurantDb));
        services.AddScoped<Clovent.Restaurant.Orders.IOrderNumberSequenceRepository>(_ => new OrderNumberSequenceRepository(_restaurantDb));
        services.AddScoped<Clovent.Restaurant.Customers.ICustomerRepository>(_ => new CustomerRepository(_restaurantDb));
        services.AddScoped<Clovent.Restaurant.Customers.ICustomerLedgerEntryRepository>(_ => new CustomerLedgerEntryRepository(_restaurantDb));

        services.AddScoped<IProductRepository>(_ => new ProductRepository(_catalogDb));
        services.AddScoped<IProductVariantRepository>(_ => new ProductVariantRepository(_catalogDb));
        services.AddScoped<IProductPriceRepository>(_ => new ProductPriceRepository(_catalogDb));

        services.AddScoped<IWarehouseStockRepository>(_ => new WarehouseStockRepository(_inventoryDb));
        services.AddScoped<IInventoryTransactionRepository>(_ => new InventoryTransactionRepository(_inventoryDb));

        // Each bounded context commits its own DbContext via its own
        // IUnitOfWork/UnitOfWorkBehavior<,> pair (three distinct types
        // sharing the same name in different namespaces) - registering all
        // three mirrors exactly how Clovent.Desktop's composition root wires
        // every module's persistence together in the real application.
        services.AddScoped<Clovent.Restaurant.Application.IUnitOfWork>(_ => new Clovent.Restaurant.Infrastructure.Persistence.UnitOfWork(_restaurantDb));
        services.AddScoped<Clovent.Catalog.Application.IUnitOfWork>(_ => new Clovent.Catalog.Infrastructure.Persistence.UnitOfWork(_catalogDb));
        services.AddScoped<Clovent.Inventory.Application.IUnitOfWork>(_ => new Clovent.Inventory.Infrastructure.Persistence.UnitOfWork(_inventoryDb));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Clovent.Restaurant.Infrastructure.Persistence.UnitOfWorkBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Clovent.Catalog.Infrastructure.Persistence.UnitOfWorkBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Clovent.Inventory.Infrastructure.Persistence.UnitOfWorkBehavior<,>));

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
            typeof(Clovent.Restaurant.Application.DependencyInjection.ApplicationServiceCollectionExtensions).Assembly,
            typeof(Clovent.Catalog.Application.DependencyInjection.ApplicationServiceCollectionExtensions).Assembly,
            typeof(Clovent.Inventory.Application.DependencyInjection.ApplicationServiceCollectionExtensions).Assembly));

        var provider = services.BuildServiceProvider();
        _mediator = provider.GetRequiredService<IMediator>();
    }

    /// <inheritdoc/>
    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task FullLifecycle_DineInOrderPaidInFull_CompletesAndReducesWarehouseStock()
    {
        var (tableId, warehouseId, variantId, paymentMethodId) = await SeedReferenceDataAsync(initialStockOnHand: 20);

        var order = await _mediator.Send(new CreateOrderCommand(OrderType.DineIn, warehouseId, tableId));
        var line = await _mediator.Send(new AddOrderLineCommand(order.OrderId, variantId, Quantity: 3));

        Assert.Equal(9.99m, line.UnitPrice);

        var totals = await _mediator.Send(new GetOrderSummaryQuery(order.OrderId));
        await _mediator.Send(new RecordPaymentCommand(order.OrderId, paymentMethodId, totals.GrandTotal));

        var completed = await _mediator.Send(new CompleteOrderCommand(order.OrderId));

        Assert.Equal("Completed", completed.Status);

        var stock = await _inventoryDb.WarehouseStocks.SingleAsync(s => s.ProductVariantId == new ProductVariantId(variantId));
        Assert.Equal(17m, stock.QuantityOnHand);

        var transaction = await _inventoryDb.InventoryTransactions.SingleAsync();
        Assert.Equal(3m, transaction.Quantity);

        var table = await _restaurantDb.Tables.SingleAsync(t => t.Id == new TableId(tableId));
        Assert.Equal(TableOccupancyStatus.Available, table.OccupancyStatus);
    }

    /// <summary>
    /// Phase 2C - partial payments, proven against real EF Core repositories
    /// rather than fakes: 200 then 300 against a 500 bill must both persist,
    /// leaving exactly two payment rows and a zero balance.
    /// </summary>
    [Fact]
    public async Task Payments_TwoPartialPayments_BothPersistAndSettleTheBill()
    {
        var (tableId, warehouseId, variantId, paymentMethodId) = await SeedReferenceDataAsync(initialStockOnHand: 50);

        var order = await _mediator.Send(new CreateOrderCommand(OrderType.DineIn, warehouseId, tableId));
        await _mediator.Send(new AddOrderLineCommand(order.OrderId, variantId, Quantity: 10));

        var totals = await _mediator.Send(new GetOrderSummaryQuery(order.OrderId));
        var first = Math.Round(totals.GrandTotal / 2, 2);
        var second = totals.GrandTotal - first;

        await _mediator.Send(new RecordPaymentCommand(order.OrderId, paymentMethodId, first));
        var afterFirst = await _mediator.Send(new GetOrderSummaryQuery(order.OrderId));
        Assert.Equal(first, afterFirst.PaidTotal);
        Assert.Equal(second, afterFirst.Balance);

        await _mediator.Send(new RecordPaymentCommand(order.OrderId, paymentMethodId, second));
        var afterSecond = await _mediator.Send(new GetOrderSummaryQuery(order.OrderId));

        Assert.Equal(totals.GrandTotal, afterSecond.PaidTotal);
        Assert.Equal(0m, afterSecond.Balance);

        var rows = await _restaurantDb.Payments.Where(p => p.OrderId == new OrderId(order.OrderId)).ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.All(rows, p => Assert.False(p.IsVoided));
    }

    /// <summary>
    /// Phase 2D - an overpayment is refused by the server-side ceiling and
    /// leaves <b>no</b> payment row behind. The check must survive the real
    /// pipeline, where <c>UnitOfWorkBehavior</c> would otherwise commit
    /// whatever the handler staged.
    /// </summary>
    [Fact]
    public async Task Payments_AmountExceedingBalance_IsRejectedAndWritesNoRow()
    {
        var (tableId, warehouseId, variantId, paymentMethodId) = await SeedReferenceDataAsync(initialStockOnHand: 50);

        var order = await _mediator.Send(new CreateOrderCommand(OrderType.DineIn, warehouseId, tableId));
        await _mediator.Send(new AddOrderLineCommand(order.OrderId, variantId, Quantity: 5));

        var totals = await _mediator.Send(new GetOrderSummaryQuery(order.OrderId));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _mediator.Send(new RecordPaymentCommand(order.OrderId, paymentMethodId, totals.GrandTotal + 100m)));

        Assert.Empty(await _restaurantDb.Payments.Where(p => p.OrderId == new OrderId(order.OrderId)).ToListAsync());

        var after = await _mediator.Send(new GetOrderSummaryQuery(order.OrderId));
        Assert.Equal(0m, after.PaidTotal);
        Assert.Equal(totals.GrandTotal, after.Balance);
    }

    /// <summary>
    /// Phase 2B - the server-side ceiling is authoritative regardless of the
    /// UI guard: a second full-balance payment, exactly what a duplicate
    /// Record Payment click submits, is refused once the first has committed.
    /// This is the ORD-35 shape ($660 recorded against a $280 bill).
    /// </summary>
    [Fact]
    public async Task Payments_SecondFullPaymentAfterSettlement_IsRejectedByServer()
    {
        var (tableId, warehouseId, variantId, paymentMethodId) = await SeedReferenceDataAsync(initialStockOnHand: 50);

        var order = await _mediator.Send(new CreateOrderCommand(OrderType.DineIn, warehouseId, tableId));
        await _mediator.Send(new AddOrderLineCommand(order.OrderId, variantId, Quantity: 4));

        var totals = await _mediator.Send(new GetOrderSummaryQuery(order.OrderId));
        await _mediator.Send(new RecordPaymentCommand(order.OrderId, paymentMethodId, totals.GrandTotal));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _mediator.Send(new RecordPaymentCommand(order.OrderId, paymentMethodId, totals.GrandTotal)));

        var rows = await _restaurantDb.Payments.Where(p => p.OrderId == new OrderId(order.OrderId)).ToListAsync();
        Assert.Equal(totals.GrandTotal, Assert.Single(rows).Amount);

        var after = await _mediator.Send(new GetOrderSummaryQuery(order.OrderId));
        Assert.Equal(0m, after.Balance);
    }

    /// <summary>
    /// Phase 5 - a manual Vacate must not free a table that still has a live
    /// order, proven end to end. This is the T-03 / ORD-54 regression.
    /// </summary>
    [Fact]
    public async Task VacateTable_WithLiveOrder_IsRejectedAndTableStaysOccupied()
    {
        var (tableId, warehouseId, variantId, _) = await SeedReferenceDataAsync(initialStockOnHand: 10);

        var order = await _mediator.Send(new CreateOrderCommand(OrderType.DineIn, warehouseId, tableId));
        await _mediator.Send(new AddOrderLineCommand(order.OrderId, variantId, Quantity: 1));

        await Assert.ThrowsAsync<RestaurantDomainException>(() =>
            _mediator.Send(new Clovent.Restaurant.Application.Tables.Commands.VacateTableCommand(tableId)));

        var table = await _restaurantDb.Tables.SingleAsync(t => t.Id == new TableId(tableId));
        Assert.Equal(TableOccupancyStatus.Occupied, table.OccupancyStatus);

        var stillOpen = await _restaurantDb.Orders.SingleAsync(o => o.Id == new OrderId(order.OrderId));
        Assert.Equal(OrderStatus.Open, stillOpen.Status);
    }

    /// <summary>
    /// Phase 5 - completing the order is what legitimately frees the table,
    /// and the manual Vacate guard must not interfere with that path.
    /// </summary>
    [Fact]
    public async Task CompleteOrder_ReleasesTable_AndVacateThenSucceedsAsANoOp()
    {
        var (tableId, warehouseId, variantId, paymentMethodId) = await SeedReferenceDataAsync(initialStockOnHand: 10);

        var order = await _mediator.Send(new CreateOrderCommand(OrderType.DineIn, warehouseId, tableId));
        await _mediator.Send(new AddOrderLineCommand(order.OrderId, variantId, Quantity: 2));

        var totals = await _mediator.Send(new GetOrderSummaryQuery(order.OrderId));
        await _mediator.Send(new RecordPaymentCommand(order.OrderId, paymentMethodId, totals.GrandTotal));
        await _mediator.Send(new CompleteOrderCommand(order.OrderId));

        var table = await _restaurantDb.Tables.SingleAsync(t => t.Id == new TableId(tableId));
        Assert.Equal(TableOccupancyStatus.Available, table.OccupancyStatus);

        // No live order remains, so the manual action is permitted again.
        var vacated = await _mediator.Send(new Clovent.Restaurant.Application.Tables.Commands.VacateTableCommand(tableId));
        Assert.Equal("Available", vacated.OccupancyStatus);
    }

    [Fact]
    public async Task FullLifecycle_CompleteWithoutFullPayment_Throws()
    {
        var (tableId, warehouseId, variantId, _) = await SeedReferenceDataAsync(initialStockOnHand: 10);

        var order = await _mediator.Send(new CreateOrderCommand(OrderType.DineIn, warehouseId, tableId));
        await _mediator.Send(new AddOrderLineCommand(order.OrderId, variantId, Quantity: 1));

        await Assert.ThrowsAsync<RestaurantDomainException>(() => _mediator.Send(new CompleteOrderCommand(order.OrderId)));

        var stock = await _inventoryDb.WarehouseStocks.SingleAsync(s => s.ProductVariantId == new ProductVariantId(variantId));
        Assert.Equal(10m, stock.QuantityOnHand);
    }

    private async Task<(Guid TableId, Guid WarehouseId, Guid VariantId, Guid PaymentMethodId)> SeedReferenceDataAsync(decimal initialStockOnHand)
    {
        var branchId = new Clovent.Identity.Branches.BranchId(Guid.NewGuid());
        var diningArea = DiningArea.Create(branchId, DiningAreaName.Create("Main Hall"));
        var table = Table.Create(diningArea.Id, EntityCode.Create("T-01"), capacity: 4);
        var paymentMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));

        await _restaurantDb.DiningAreas.AddAsync(diningArea);
        await _restaurantDb.Tables.AddAsync(table);
        await _restaurantDb.PaymentMethods.AddAsync(paymentMethod);
        await _restaurantDb.SaveChangesAsync();

        var unitOfMeasureId = UnitOfMeasureId.New();
        var product = Product.Create(ProductName.Create("Burger"), Sku.Create("BURG-001"), unitOfMeasureId, TaxConfiguration.Create(10m, isInclusive: false));
        var variant = ProductVariant.Create(product.Id, VariantName.Create("Regular"), Sku.Create("BURG-001-REG"), unitOfMeasureId);
        var price = ProductPrice.Create(variant.Id, PriceType.Selling, 9.99m, new CurrencyId(Guid.NewGuid()));

        await _catalogDb.Products.AddAsync(product);
        await _catalogDb.ProductVariants.AddAsync(variant);
        await _catalogDb.ProductPrices.AddAsync(price);
        await _catalogDb.SaveChangesAsync();

        var warehouseId = WarehouseId.New();
        var stock = WarehouseStock.Create(warehouseId, variant.Id);
        stock.Receive(initialStockOnHand);

        await _inventoryDb.WarehouseStocks.AddAsync(stock);
        await _inventoryDb.SaveChangesAsync();

        return (table.Id.Value, warehouseId.Value, variant.Id.Value, paymentMethod.Id.Value);
    }
}
