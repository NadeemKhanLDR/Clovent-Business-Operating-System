using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Catalog.Application.Categories.Dtos;
using Clovent.Catalog.Application.Categories.Queries;
using Clovent.Catalog.Application.Prices.Dtos;
using Clovent.Catalog.Application.Prices.Queries;
using Clovent.Catalog.Application.Products.Dtos;
using Clovent.Catalog.Application.Products.Queries;
using Clovent.Catalog.Application.Variants.Dtos;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Catalog.Prices;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Seed;
using Clovent.Desktop.Theming;
using Clovent.Identity.Branches;
using Clovent.Identity.Organizations;
using Clovent.Identity.Users;
using Clovent.Inventory.Infrastructure.Persistence;
using Clovent.Inventory.Infrastructure.Repositories;
using Clovent.Inventory.Transactions;
using Clovent.Inventory.WarehouseStocks;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Terminals;
using Clovent.MasterData.Warehouses;
using Clovent.MasterData.Warehouses.ValueObjects;
using Clovent.Restaurant.Application.EndOfDay.Queries;
using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.DiningAreas.ValueObjects;
using Clovent.Restaurant.Discounts;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Infrastructure.Repositories;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.ServiceCharges;
using Clovent.Restaurant.Shifts;
using Clovent.Restaurant.Tables;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.EndOfDay;

public sealed class DevelopmentRestaurantReportingSeedExecutionTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly RestaurantDbContext _restaurantDb;
    private readonly InventoryDbContext _inventoryDb;

    public DevelopmentRestaurantReportingSeedExecutionTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _restaurantDb = new RestaurantDbContext(new DbContextOptionsBuilder<RestaurantDbContext>().UseSqlite(_connection).Options);
        _inventoryDb = new InventoryDbContext(new DbContextOptionsBuilder<InventoryDbContext>().UseSqlite(_connection).Options);

        _restaurantDb.Database.EnsureCreated();
        _inventoryDb.Database.ExecuteSqlRaw(_inventoryDb.Database.GenerateCreateScript());
    }

    public void Dispose()
    {
        _restaurantDb.Dispose();
        _inventoryDb.Dispose();
        _connection.Dispose();
    }

    private sealed class FakeWarehouseRepository(Warehouse warehouse) : IWarehouseRepository
    {
        public Task<Warehouse?> GetByIdAsync(WarehouseId id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Warehouse?>(warehouse.Id == id ? warehouse : null);

        public Task<IReadOnlyCollection<Warehouse>> GetByBranchIdAsync(BranchId branchId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Warehouse>>([warehouse]);

        public Task<IReadOnlyCollection<Warehouse>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Warehouse>>([warehouse]);

        public Task AddAsync(Warehouse w, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeOrganizationRepository : IOrganizationRepository
    {
        public Task<Organization?> GetByIdAsync(OrganizationId id, CancellationToken cancellationToken = default) => Task.FromResult<Organization?>(null);
        public Task<IReadOnlyCollection<Organization>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Organization>>([]);
        public Task AddAsync(Organization organization, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeDiscountRepository : IDiscountRepository
    {
        public Task<Discount?> GetByIdAsync(DiscountId id, CancellationToken cancellationToken = default) => Task.FromResult<Discount?>(null);
        public Task<IReadOnlyCollection<Discount>> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Discount>>([]);
        public Task AddAsync(Discount discount, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeServiceChargeRepository : IServiceChargeRepository
    {
        public Task<ServiceCharge?> GetByIdAsync(ServiceChargeId id, CancellationToken cancellationToken = default) => Task.FromResult<ServiceCharge?>(null);
        public Task<IReadOnlyCollection<ServiceCharge>> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<ServiceCharge>>([]);
        public Task AddAsync(ServiceCharge serviceCharge, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeShiftRepository : IShiftRepository
    {
        public Task<Shift?> GetByIdAsync(ShiftId id, CancellationToken cancellationToken = default) => Task.FromResult<Shift?>(null);
        public Task<Shift?> GetActiveShiftForTerminalAsync(TerminalId terminalId, CancellationToken cancellationToken = default) => Task.FromResult<Shift?>(null);
        public Task<Shift?> GetActiveShiftForCashierAsync(UserId cashierId, CancellationToken cancellationToken = default) => Task.FromResult<Shift?>(null);
        public Task<IReadOnlyList<Shift>> SearchShiftsAsync(TerminalId? terminalId = null, UserId? cashierId = null, ShiftStatus? status = null, DateTimeOffset? fromDateUtc = null, DateTimeOffset? toDateUtc = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Shift>>([]);
        public Task<int> GetNextShiftNumberAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task AddAsync(Shift shift, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(Shift shift, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class SeedCatalogMediator(
        Guid biryaniProductId, Guid biryaniVariantId,
        Guid naanProductId, Guid naanVariantId,
        Guid heatingProductId, Guid heatingVariantId) : IMediator
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is ListProductsQuery)
            {
                var prods = new List<ProductDto>
                {
                    new(biryaniProductId, "Chicken Biryani", "CHICKEN-BIRYANI", null, null, null, Guid.NewGuid(), 0m, false, "Active", DateTimeOffset.UtcNow, "Prepared"),
                    new(naanProductId, "Naan", "NAAN", null, null, null, Guid.NewGuid(), 0m, false, "Active", DateTimeOffset.UtcNow, "PurchasedResale"),
                    new(heatingProductId, "Food Heating", "FOOD-HEATING", null, null, null, Guid.NewGuid(), 0m, false, "Active", DateTimeOffset.UtcNow, "Service")
                };
                return Task.FromResult((TResponse)(object)prods);
            }

            if (request is ListProductVariantsQuery)
            {
                var vars = new List<ProductVariantDto>
                {
                    new(biryaniVariantId, biryaniProductId, "Standard", "CHICKEN-BIRYANI-STD", Guid.NewGuid(), "Active", 1, DateTimeOffset.UtcNow, null, "Active", true, "Prepared"),
                    new(naanVariantId, naanProductId, "Standard", "NAAN-STD", Guid.NewGuid(), "Active", 2, DateTimeOffset.UtcNow, null, "Active", true, "PurchasedResale"),
                    new(heatingVariantId, heatingProductId, "Standard", "FOOD-HEATING-STD", Guid.NewGuid(), "Active", 3, DateTimeOffset.UtcNow, null, "Active", true, "Service")
                };
                return Task.FromResult((TResponse)(object)vars);
            }

            if (request is ListProductCategoriesQuery)
            {
                var cats = new List<ProductCategoryDto>();
                return Task.FromResult((TResponse)(object)cats);
            }

            if (request is ListActiveProductPricesByTypeQuery priceQuery)
            {
                var prices = new List<ProductPriceDto>();
                if (priceQuery.PriceType == PriceType.Cost)
                {
                    // Cost price for Naan = 20.00
                    prices.Add(new ProductPriceDto(Guid.NewGuid(), naanVariantId, PriceType.Cost.ToString(), 20.00m, Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-30), "Active", DateTimeOffset.UtcNow.AddDays(-30)));
                }
                return Task.FromResult((TResponse)(object)prices);
            }

            return Task.FromResult(default(TResponse)!);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest => Task.CompletedTask;
        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => Task.FromResult<object?>(null);
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    [Fact]
    public async Task DevelopmentRestaurantReportingSeed_ExecutesSuccessfully_AndReconcilesWithReportingQueries()
    {
        // 1. Arrange
        var branchId = BranchId.New();
        var warehouse = Warehouse.Create(branchId, WarehouseName.Create("Main Warehouse"), EntityCode.Create("WH-MAIN"));

        var diningAreaRepo = new DiningAreaRepository(_restaurantDb);
        var tableRepo = new TableRepository(_restaurantDb);
        var paymentMethodRepo = new PaymentMethodRepository(_restaurantDb);
        var customerRepo = new CustomerRepository(_restaurantDb);
        var ledgerRepo = new CustomerLedgerEntryRepository(_restaurantDb);
        var allocationRepo = new CustomerPaymentAllocationRepository(_restaurantDb);
        var orderRepo = new OrderRepository(_restaurantDb);
        var orderLineRepo = new OrderLineRepository(_restaurantDb);
        var paymentRepo = new PaymentRepository(_restaurantDb);
        var warehouseStockRepo = new WarehouseStockRepository(_inventoryDb);
        var transactionRepo = new InventoryTransactionRepository(_inventoryDb);

        // Pre-create dining area so tables can be added
        var area = DiningArea.Create(branchId, DiningAreaName.Create("Main Dining Hall"));
        await diningAreaRepo.AddAsync(area);
        await _restaurantDb.SaveChangesAsync();

        var biryaniProdId = Guid.NewGuid();
        var biryaniVarId = Guid.NewGuid();
        var naanProdId = Guid.NewGuid();
        var naanVarId = Guid.NewGuid();
        var heatingProdId = Guid.NewGuid();
        var heatingVarId = Guid.NewGuid();

        var mediator = new SeedCatalogMediator(biryaniProdId, biryaniVarId, naanProdId, naanVarId, heatingProdId, heatingVarId);
        var options = Microsoft.Extensions.Options.Options.Create(new DesktopOptions
        {
            SeedDevelopmentRestaurantData = true,
            SeedDevelopmentUser = false,
            SeedDevelopmentMasterData = false,
            SeedDevelopmentCatalogData = false,
            DefaultSkin = "WXI",
            DefaultLanguage = "en-US"
        });

        var seedTask = new DevelopmentRestaurantReportingSeedStartupTask(
            new FakeOrganizationRepository(),
            new FakeWarehouseRepository(warehouse),
            diningAreaRepo,
            tableRepo,
            paymentMethodRepo,
            customerRepo,
            ledgerRepo,
            allocationRepo,
            orderRepo,
            orderLineRepo,
            paymentRepo,
            _restaurantDb,
            warehouseStockRepo,
            transactionRepo,
            _inventoryDb,
            mediator,
            options);

        // 2. Act: Execute seed
        await seedTask.ExecuteAsync();

        // 3. Assert Database Persistence
        var allOrders = await orderRepo.GetAllAsync();
        Assert.Equal(12, allOrders.Count);

        var completedOrders = allOrders.Where(o => o.Status == OrderStatus.Completed).ToList();
        var voidedOrders = allOrders.Where(o => o.Status == OrderStatus.Voided).ToList();
        Assert.Equal(11, completedOrders.Count);
        Assert.Single(voidedOrders);
        Assert.Equal("ORD-RPT-012", voidedOrders[0].OrderNumber.Value);

        // Assert Naan warehouse stock tracking: 100 received - 30 issued = 70 remaining
        var naanStock = await warehouseStockRepo.GetByWarehouseAndVariantAsync(warehouse.Id, new Clovent.Catalog.Variants.ProductVariantId(naanVarId));
        Assert.NotNull(naanStock);
        Assert.Equal(70m, naanStock.QuantityOnHand);

        var inventoryTxs = await transactionRepo.GetByWarehouseIdAsync(warehouse.Id);
        Assert.Contains(inventoryTxs, t => t.TransactionType == InventoryTransactionType.Receipt && t.Quantity == 100m);
        var issueTxs = inventoryTxs.Where(t => t.TransactionType == InventoryTransactionType.Issue).ToList();
        Assert.Equal(30m, issueTxs.Sum(t => t.Quantity));

        // 4. Assert Expanded Sales Summary Query
        var today = BusinessDateTimeService.Instance.Today;
        var queryHandler = new GetExpandedSalesSummaryQueryHandler(
            orderRepo,
            orderLineRepo,
            paymentRepo,
            paymentMethodRepo,
            customerRepo,
            ledgerRepo,
            new FakeDiscountRepository(),
            new FakeServiceChargeRepository(),
            new FakeShiftRepository(),
            tableRepo,
            mediator);

        var summary = await queryHandler.Handle(new GetExpandedSalesSummaryQuery(warehouse.Id.Value, today, today), CancellationToken.None);

        Assert.NotNull(summary);
        Assert.Equal(11, summary.Kpis.TotalOrders);
        Assert.Equal(1, summary.Kpis.VoidedOrdersCount);
        Assert.Equal(4920m, summary.Kpis.GrossSales);
        Assert.Equal(400m, summary.Kpis.DeliveryFees);
        Assert.Equal(5320m, summary.Kpis.NetSales);

        // Verify Items Tab rows
        var items = summary.Items;
        var biryaniItem = items.FirstOrDefault(i => i.ItemName.Contains("Biryani"));
        var naanItem = items.FirstOrDefault(i => i.ItemName.Contains("Naan"));
        var heatingItem = items.FirstOrDefault(i => i.ItemName.Contains("Heating"));

        Assert.NotNull(biryaniItem);
        Assert.Equal(9m, biryaniItem.QuantitySold);
        Assert.Equal(4050m, biryaniItem.TotalSales);
        Assert.Null(biryaniItem.EstimatedCost);
        Assert.Null(biryaniItem.GrossProfit);
        Assert.Null(biryaniItem.MarginPercent);

        Assert.NotNull(naanItem);
        Assert.Equal(30m, naanItem.QuantitySold);
        Assert.Equal(750m, naanItem.TotalSales);
        Assert.Equal(600m, naanItem.EstimatedCost);
        Assert.Equal(150m, naanItem.GrossProfit);
        Assert.Equal(20m, naanItem.MarginPercent);

        Assert.NotNull(heatingItem);
        Assert.Equal(4m, heatingItem.QuantitySold);
        Assert.Equal(120m, heatingItem.TotalSales);
        Assert.Equal(0m, heatingItem.EstimatedCost);
        Assert.Equal(120m, heatingItem.GrossProfit);
        Assert.Equal(100m, heatingItem.MarginPercent);

        // Verify Item Classification / Profitability tab rows
        var classifications = summary.ItemTypes;
        var prepClass = classifications.FirstOrDefault(c => c.ItemType == "Prepared");
        var resaleClass = classifications.FirstOrDefault(c => c.ItemType == "PurchasedResale");
        var serviceClass = classifications.FirstOrDefault(c => c.ItemType == "Service");

        Assert.NotNull(prepClass);
        Assert.Equal(9m, prepClass.QuantitySold);
        Assert.Equal(4050m, prepClass.TotalSales);
        Assert.Equal("N/A", prepClass.CostDisplay);

        Assert.NotNull(resaleClass);
        Assert.Equal(30m, resaleClass.QuantitySold);
        Assert.Equal(750m, resaleClass.TotalSales);
        Assert.Equal(600m, resaleClass.TotalCost);
        Assert.Equal(150m, resaleClass.GrossProfit);
        Assert.Equal(20m, resaleClass.MarginPercent);

        Assert.NotNull(serviceClass);
        Assert.Equal(4m, serviceClass.QuantitySold);
        Assert.Equal(120m, serviceClass.TotalSales);
        Assert.Equal(0m, serviceClass.TotalCost);
        Assert.Equal(120m, serviceClass.GrossProfit);
        Assert.Equal(100m, serviceClass.MarginPercent);
    }
}
