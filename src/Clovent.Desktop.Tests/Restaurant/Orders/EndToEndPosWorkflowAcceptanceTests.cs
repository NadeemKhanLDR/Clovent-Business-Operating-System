using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Catalog.Application.Prices.Queries;
using Clovent.Catalog.Application.Products.Queries;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Catalog.Infrastructure.Persistence;
using Clovent.Catalog.Infrastructure.Repositories;
using Clovent.Catalog.Prices;
using Clovent.Catalog.Products;
using Clovent.Catalog.Products.ValueObjects;
using Clovent.Catalog.Shared.ValueObjects;
using Clovent.Catalog.TaxProfiles;
using Clovent.Catalog.UnitsOfMeasure;
using Clovent.Catalog.Variants;
using Clovent.Catalog.Variants.ValueObjects;
using Clovent.Identity.Branches;
using Clovent.Identity.Users;
using Clovent.Inventory.Application.WarehouseStocks.Commands;
using Clovent.Inventory.Infrastructure.Persistence;
using Clovent.Inventory.Infrastructure.Repositories;
using Clovent.Inventory.Transactions;
using Clovent.Inventory.WarehouseStocks;
using Clovent.MasterData.Currencies;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Terminals;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant;
using Clovent.Restaurant.ActivityLogs;
using Clovent.Restaurant.Application.DayClose.Commands;
using Clovent.Restaurant.Application.DayClose.Queries;
using Clovent.Restaurant.Application.Discounts.Commands;
using Clovent.Restaurant.Application.OrderLines.Commands;
using Clovent.Restaurant.Application.Orders;
using Clovent.Restaurant.Application.Orders.Commands;
using Clovent.Restaurant.Application.Orders.Queries;
using Clovent.Restaurant.Application.Payments.Commands;
using Clovent.Restaurant.Application.Refunds.Commands;
using Clovent.Restaurant.Application.Shifts.Commands;
using Clovent.Restaurant.Application.Shifts.Queries;
using Clovent.Restaurant.Application.Shifts.Services;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.DayClose;
using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.DiningAreas.ValueObjects;
using Clovent.Restaurant.Discounts;
using Clovent.Restaurant.DomainServices;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Infrastructure.Repositories;
using Clovent.Restaurant.KitchenTickets;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Refunds;
using Clovent.Restaurant.Sales;
using Clovent.Restaurant.ServiceCharges;
using Clovent.Restaurant.Shifts;
using Clovent.Restaurant.Tables;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Orders;

/// <summary>
/// End-to-End Acceptance &amp; Smoke Testing Suite for Clovent Business Operating System (CBOS).
/// Includes both SQLite in-memory tests (labeled as simulated persistence; does not establish physical hardware
/// or SQL Server acceptance) and an explicitly isolated disposable SQL Server database acceptance test.
/// Drawer and printer actions in this suite are simulated/headless and do not establish physical hardware acceptance.
/// </summary>
public sealed class EndToEndPosWorkflowAcceptanceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly RestaurantDbContext _restaurantDb;
    private readonly CatalogDbContext _catalogDb;
    private readonly InventoryDbContext _inventoryDb;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMediator _mediator;

    /// <summary>
    /// Test-only model customizer converting DateTimeOffset to UTC ticks for SQLite in-memory tests so
    /// production repositories (ShiftRepository, RefundRepository) remain 100% pure SQL Server LINQ queries.
    /// </summary>
    private sealed class SqliteDateTimeOffsetCustomizer(ModelCustomizerDependencies dependencies) : ModelCustomizer(dependencies)
    {
        public override void Customize(ModelBuilder modelBuilder, DbContext context)
        {
            base.Customize(modelBuilder, context);
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entity.GetProperties())
                {
                    if (property.ClrType == typeof(DateTimeOffset))
                    {
                        property.SetValueConverter(new ValueConverter<DateTimeOffset, long>(
                            v => v.UtcTicks,
                            v => new DateTimeOffset(v, TimeSpan.Zero)));
                    }
                    else if (property.ClrType == typeof(DateTimeOffset?))
                    {
                        property.SetValueConverter(new ValueConverter<DateTimeOffset?, long?>(
                            v => v.HasValue ? v.Value.UtcTicks : null,
                            v => v.HasValue ? new DateTimeOffset(v.Value, TimeSpan.Zero) : null));
                    }
                }
            }
        }
    }

    public EndToEndPosWorkflowAcceptanceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _restaurantDb = new RestaurantDbContext(
            new DbContextOptionsBuilder<RestaurantDbContext>()
                .UseSqlite(_connection)
                .ReplaceService<IModelCustomizer, SqliteDateTimeOffsetCustomizer>()
                .Options);
        _catalogDb = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(_connection).Options);
        _inventoryDb = new InventoryDbContext(new DbContextOptionsBuilder<InventoryDbContext>().UseSqlite(_connection).Options);

        _restaurantDb.Database.EnsureCreated();
        _catalogDb.Database.ExecuteSqlRaw(_catalogDb.Database.GenerateCreateScript());
        _inventoryDb.Database.ExecuteSqlRaw(_inventoryDb.Database.GenerateCreateScript());

        _serviceProvider = BuildWorkflowServiceProvider(_restaurantDb, _catalogDb, _inventoryDb);
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
    }

    private static IServiceProvider BuildWorkflowServiceProvider(
        RestaurantDbContext restaurantDb,
        CatalogDbContext catalogDb,
        InventoryDbContext inventoryDb)
    {
        var services = new ServiceCollection();

        // Restaurant Repositories
        services.AddScoped<IDiningAreaRepository>(_ => new DiningAreaRepository(restaurantDb));
        services.AddScoped<ITableRepository>(_ => new TableRepository(restaurantDb));
        services.AddScoped<IOrderRepository>(_ => new OrderRepository(restaurantDb));
        services.AddScoped<IOrderLineRepository>(_ => new OrderLineRepository(restaurantDb));
        services.AddScoped<IPaymentRepository>(_ => new PaymentRepository(restaurantDb));
        services.AddScoped<IPaymentMethodRepository>(_ => new PaymentMethodRepository(restaurantDb));
        services.AddScoped<IDiscountRepository>(_ => new DiscountRepository(restaurantDb));
        services.AddScoped<IServiceChargeRepository>(_ => new ServiceChargeRepository(restaurantDb));
        services.AddScoped<IKitchenTicketRepository>(_ => new KitchenTicketRepository(restaurantDb));
        services.AddScoped<IDailySalesSequenceRepository>(_ => new DailySalesSequenceRepository(restaurantDb));
        services.AddScoped<Clovent.Restaurant.Orders.IOrderNumberSequenceRepository>(_ => new OrderNumberSequenceRepository(restaurantDb));
        services.AddScoped<ICustomerRepository>(_ => new CustomerRepository(restaurantDb));
        services.AddScoped<ICustomerLedgerEntryRepository>(_ => new CustomerLedgerEntryRepository(restaurantDb));
        services.AddScoped<ICustomerPaymentAllocationRepository>(_ => new CustomerPaymentAllocationRepository(restaurantDb));
        services.AddScoped<IShiftRepository>(_ => new ShiftRepository(restaurantDb));
        services.AddScoped<IRefundRepository>(_ => new RefundRepository(restaurantDb));
        services.AddScoped<IBusinessDayCloseRepository>(_ => new BusinessDayCloseRepository(restaurantDb));
        services.AddScoped<IActivityLogEntryRepository>(_ => new ActivityLogEntryRepository(restaurantDb));

        // Catalog Repositories & Domain Services
        services.AddScoped<IProductRepository>(_ => new ProductRepository(catalogDb));
        services.AddScoped<IProductVariantRepository>(_ => new ProductVariantRepository(catalogDb));
        services.AddScoped<IProductPriceRepository>(_ => new ProductPriceRepository(catalogDb));
        services.AddScoped<Clovent.Catalog.Categories.IProductCategoryRepository>(_ => new ProductCategoryRepository(catalogDb));
        services.AddScoped<ITaxProfileRepository>(_ => new TaxProfileRepository(catalogDb));
        services.AddScoped<Clovent.Catalog.Application.TaxProfiles.Services.ITaxProfileResolver, Clovent.Catalog.Application.TaxProfiles.Services.TaxProfileResolver>();

        // Inventory Repositories
        services.AddScoped<IWarehouseStockRepository>(_ => new WarehouseStockRepository(inventoryDb));
        services.AddScoped<IInventoryTransactionRepository>(_ => new InventoryTransactionRepository(inventoryDb));

        // Services & Providers
        services.AddSingleton<IBusinessDateProvider, BusinessDateProvider>();

        // Units of Work
        services.AddScoped<Clovent.Restaurant.Application.IUnitOfWork>(_ => new Clovent.Restaurant.Infrastructure.Persistence.UnitOfWork(restaurantDb));
        services.AddScoped<Clovent.Catalog.Application.IUnitOfWork>(_ => new Clovent.Catalog.Infrastructure.Persistence.UnitOfWork(catalogDb));
        services.AddScoped<Clovent.Inventory.Application.IUnitOfWork>(_ => new Clovent.Inventory.Infrastructure.Persistence.UnitOfWork(inventoryDb));

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Clovent.Restaurant.Infrastructure.Persistence.UnitOfWorkBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Clovent.Catalog.Infrastructure.Persistence.UnitOfWorkBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Clovent.Inventory.Infrastructure.Persistence.UnitOfWorkBehavior<,>));

        // MediatR assembly scanning across Application layers
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
            typeof(Clovent.Restaurant.Application.DependencyInjection.ApplicationServiceCollectionExtensions).Assembly,
            typeof(Clovent.Catalog.Application.DependencyInjection.ApplicationServiceCollectionExtensions).Assembly,
            typeof(Clovent.Inventory.Application.DependencyInjection.ApplicationServiceCollectionExtensions).Assembly));

        return services.BuildServiceProvider();
    }

    public void Dispose()
    {
        _restaurantDb.Dispose();
        _catalogDb.Dispose();
        _inventoryDb.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task SimulateFullShiftAndCashierLifecycle_CompleteWorkflow_SucceedsWithExactReconciliation()
    {
        await ExecuteCompleteCashierWorkflowAsync(_restaurantDb, _catalogDb, _inventoryDb, _mediator);
    }

    [Fact]
    public async Task DisposableSqlServer_EndToEndWorkflowAndRepositories_ValidatesAgainstIsolatedSqlServerDatabase()
    {
        var dbName = $"Clovent_DisposableAcceptance_{Guid.NewGuid():N}";
        var connectionString = $"Server=.;Database={dbName};Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";

        await using var restaurantDb = new RestaurantDbContext(
            new DbContextOptionsBuilder<RestaurantDbContext>().UseSqlServer(connectionString).Options);
        await using var catalogDb = new CatalogDbContext(
            new DbContextOptionsBuilder<CatalogDbContext>().UseSqlServer(connectionString).Options);
        await using var inventoryDb = new InventoryDbContext(
            new DbContextOptionsBuilder<InventoryDbContext>().UseSqlServer(connectionString).Options);

        try
        {
            await restaurantDb.Database.EnsureCreatedAsync();
            await catalogDb.Database.GetService<IRelationalDatabaseCreator>().CreateTablesAsync();
            await inventoryDb.Database.GetService<IRelationalDatabaseCreator>().CreateTablesAsync();

            var provider = BuildWorkflowServiceProvider(restaurantDb, catalogDb, inventoryDb);
            var mediator = provider.GetRequiredService<IMediator>();

            await ExecuteCompleteCashierWorkflowAsync(restaurantDb, catalogDb, inventoryDb, mediator);

            // Explicitly validate ShiftRepository and RefundRepository native SQL Server DATETIMEOFFSET range queries
            var shiftRepo = provider.GetRequiredService<IShiftRepository>();
            var refundRepo = provider.GetRequiredService<IRefundRepository>();
            var nowUtc = DateTimeOffset.UtcNow;

            var shiftsInRange = await shiftRepo.SearchShiftsAsync(
                fromDateUtc: nowUtc.AddHours(-2),
                toDateUtc: nowUtc.AddHours(2));
            Assert.Equal(2, shiftsInRange.Count);

            var shiftsOutOfRange = await shiftRepo.SearchShiftsAsync(
                fromDateUtc: nowUtc.AddDays(1),
                toDateUtc: nowUtc.AddDays(2));
            Assert.Empty(shiftsOutOfRange);

            var refundsInRange = await refundRepo.GetByDateRangeAsync(
                nowUtc.AddHours(-2),
                nowUtc.AddHours(2));
            Assert.Equal(3, refundsInRange.Count);

            var refundsOutOfRange = await refundRepo.GetByDateRangeAsync(
                nowUtc.AddDays(1),
                nowUtc.AddDays(2));
            Assert.Empty(refundsOutOfRange);
        }
        finally
        {
            await restaurantDb.Database.EnsureDeletedAsync();
        }
    }

    private static async Task ExecuteCompleteCashierWorkflowAsync(
        RestaurantDbContext restaurantDb,
        CatalogDbContext catalogDb,
        InventoryDbContext inventoryDb,
        IMediator mediator)
    {
        // -------------------------------------------------------------------------------------------------
        // Setup: Reference Master Data (Branch, Warehouse, Terminal, Cashier, Customer, Catalog, Stock)
        // -------------------------------------------------------------------------------------------------
        var branchId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var terminalId = Guid.NewGuid();
        var cashierId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        const string cashierName = "Muhammad Usman";
        const string managerName = "Shift Manager";
        var today = DateOnly.FromDateTime(DateTime.Today);

        // Payment Methods: Cash, On Account, Card
        var cashMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        var onAccountMethod = PaymentMethod.Create(PaymentMethodName.Create("On Account"));
        var cardMethod = PaymentMethod.Create(PaymentMethodName.Create("Card"));
        await restaurantDb.PaymentMethods.AddRangeAsync(cashMethod, onAccountMethod, cardMethod);

        // Dining Area & Table T-01
        var diningArea = DiningArea.Create(new BranchId(branchId), DiningAreaName.Create("Main Dining Hall"));
        var table = Table.Create(diningArea.Id, EntityCode.Create("T-01"), capacity: 6);
        await restaurantDb.DiningAreas.AddAsync(diningArea);
        await restaurantDb.Tables.AddAsync(table);

        // Corporate Account Customer with Credit Privileges
        var corporateCustomer = Customer.Create(
            EntityCode.Create("CUST-CORP-01"),
            "NexGen Corporate Systems",
            "+923001234567",
            "Gulberg III, Lahore, Pakistan",
            "accounts@nexgen.example.pk",
            openingBalance: 0.00m,
            creditLimit: 50_000.00m,
            notes: "Approved corporate client with 30-day settlement terms",
            isDefault: false,
            isCreditAllowed: true);
        await restaurantDb.Customers.AddAsync(corporateCustomer);
        await restaurantDb.SaveChangesAsync();

        // Catalog Products with Synthetic Pakistan Tax Policies (100 initial stock each):
        // 1. Chicken Biryani: PKR 600.00, Taxable Exclusive PRA 16% (PK-PRA-16)
        // 2. Mutton Karahi: PKR 2,000.00, Taxable Exclusive PRA 16% (PK-PRA-16)
        // 3. Gourmet Drink: PKR 100.00, Taxable Inclusive PRA 16% (PK-PRA-16-INC)
        // 4. Roghani Naan: PKR 40.00, Exempt 0% tax (PK-EXEMPT)
        var (biryaniVarId, karahiVarId, drinkVarId, naanVarId) =
            await SeedPakistaniCatalogIntoAsync(catalogDb, inventoryDb, warehouseId);

        // =================================================================================================
        // PHASE 1: Open Cash Drawer / Shift Session with Opening Float & Cash-In / Cash-Out Movements
        // =================================================================================================
        const decimal startingFloat = 5_000.00m;
        var openShiftCmd = new OpenShiftCommand(
            branchId,
            warehouseId,
            terminalId,
            cashierId,
            cashierName,
            startingFloat,
            "Shift 1 Opening Cash Float");

        var shiftDto = await mediator.Send(openShiftCmd);
        Assert.NotNull(shiftDto);
        Assert.Equal("Open", shiftDto.Status);
        Assert.Equal(startingFloat, shiftDto.StartingCash);
        Assert.Equal(cashierName, shiftDto.CashierName);

        // Guard Invariant: Terminal already in use cannot open a concurrent shift
        await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(openShiftCmd));

        // Record explicit Cash-In (PKR 200.00) and Cash-Out (PKR 100.00) drawer movements
        const decimal cashInAmount = 200.00m;
        const decimal cashOutAmount = 100.00m;
        var cashInDto = await mediator.Send(new RecordCashMovementCommand(
            shiftDto.ShiftId,
            CashMovementType.CashIn,
            cashInAmount,
            "Change coins top-up",
            cashierId));
        Assert.Equal(cashInAmount, cashInDto.Amount);

        var cashOutDto = await mediator.Send(new RecordCashMovementCommand(
            shiftDto.ShiftId,
            CashMovementType.CashOut,
            cashOutAmount,
            "Petty cash thermal paper purchase",
            cashierId));
        Assert.Equal(cashOutAmount, cashOutDto.Amount);

        // =================================================================================================
        // PHASE 2: Ring Up Multi-Item Sale with Synthetic Tax Policy & Trade Discount
        // =================================================================================================
        var createOrderCmd = new CreateOrderCommand(
            OrderType.DineIn,
            warehouseId,
            table.Id.Value,
            CustomerId: corporateCustomer.Id.Value);

        var order = await mediator.Send(createOrderCmd);
        Assert.Equal("Open", order.Status);

        // Add Multi-Item Lines:
        // - 2x Chicken Biryani @ 600.00 = 1,200.00 Gross (Exclusive 16% PRA)
        // - 1x Mutton Karahi @ 2,000.00 = 2,000.00 Gross (Exclusive 16% PRA)
        // - 4x Gourmet Drink @ 100.00 = 400.00 Gross (Inclusive 16% PRA)
        // - 5x Roghani Naan @ 40.00 = 200.00 Gross (Exempt 0%)
        // Independently calculated Gross Subtotal = 1,200 + 2,000 + 400 + 200 = 3,800.00
        var biryaniLine = await mediator.Send(new AddOrderLineCommand(order.OrderId, biryaniVarId, Quantity: 2));
        var karahiLine = await mediator.Send(new AddOrderLineCommand(order.OrderId, karahiVarId, Quantity: 1));
        var drinkLine = await mediator.Send(new AddOrderLineCommand(order.OrderId, drinkVarId, Quantity: 4));
        var naanLine = await mediator.Send(new AddOrderLineCommand(order.OrderId, naanVarId, Quantity: 5));

        // Apply a 10% Trade Discount on the order (PKR 380.00)
        var discountDto = await mediator.Send(new ApplyDiscountToOrderCommand(
            order.OrderId,
            DiscountType.Percentage,
            10.00m,
            "Corporate Trade Discount 10%"));

        Assert.Equal(10.00m, discountDto.Value);

        // Query Order Summary and verify against independently calculated expected figures:
        // Line 1 (Biryani): Gross 1200, Disc 120, Net 1080. Exclusive 16% PRA Tax = 172.80
        // Line 2 (Karahi): Gross 2000, Disc 200, Net 1800. Exclusive 16% PRA Tax = 288.00
        // Line 3 (Drink): Gross 400, Disc 40, Net 360. Inclusive 16% PRA Tax = 360 - 310.34 = 49.66
        // Line 4 (Naan): Gross 200, Disc 20, Net 180. Exempt = 0.00
        // Exclusive Tax = 172.80 + 288.00 = 460.80
        // Inclusive Tax = 49.66
        // Total Tax = 510.46
        // Grand Total = 3,800.00 - 380.00 + 460.80 = 3,880.80
        var summary = await mediator.Send(new GetOrderSummaryQuery(order.OrderId));
        Assert.Equal(3_800.00m, summary.Subtotal);
        Assert.Equal(380.00m, summary.DiscountTotal);
        Assert.Equal(460.80m, summary.ExclusiveTaxTotal);
        Assert.Equal(49.66m, summary.InclusiveTaxTotal);
        Assert.Equal(510.46m, summary.TaxTotal);
        Assert.Equal(3_880.80m, summary.GrandTotal);
        Assert.Equal(3_880.80m, summary.Balance);

        // =================================================================================================
        // PHASE 3: Process Split Tenders (Cash + On-Account Credit) & Complete Sale
        // =================================================================================================
        // Split Tender 1: Customer pays PKR 1,500.00 in Cash (tagged with active shift)
        const decimal cashTenderAmount = 1_500.00m;
        var cashPaymentDto = await mediator.Send(new RecordPaymentCommand(
            order.OrderId,
            cashMethod.Id.Value,
            cashTenderAmount,
            ExceedCreditLimitApproved: false,
            ShiftId: shiftDto.ShiftId));
        Assert.Equal(cashTenderAmount, cashPaymentDto.Amount);

        var afterCashSummary = await mediator.Send(new GetOrderSummaryQuery(order.OrderId));
        Assert.Equal(1_500.00m, afterCashSummary.PaidTotal);
        Assert.Equal(2_380.80m, afterCashSummary.Balance);

        // Split Tender 2: Customer pays remaining PKR 2,380.80 On Account (Corporate credit)
        const decimal creditTenderAmount = 2_380.80m;
        var creditPaymentDto = await mediator.Send(new RecordPaymentCommand(
            order.OrderId,
            onAccountMethod.Id.Value,
            creditTenderAmount,
            ExceedCreditLimitApproved: false,
            ShiftId: shiftDto.ShiftId));
        Assert.Equal(creditTenderAmount, creditPaymentDto.Amount);

        var fullyPaidSummary = await mediator.Send(new GetOrderSummaryQuery(order.OrderId));
        Assert.Equal(3_880.80m, fullyPaidSummary.PaidTotal);
        Assert.Equal(0.00m, fullyPaidSummary.Balance);

        // Complete Order: table vacated, daily sales number assigned, receipt & tax snapshots frozen, stock issued
        var completedOrder = await mediator.Send(new CompleteOrderCommand(order.OrderId));
        Assert.Equal("Completed", completedOrder.Status);
        Assert.False(string.IsNullOrWhiteSpace(completedOrder.ReceiptSnapshotJson));
        Assert.Contains("3880.8", completedOrder.ReceiptSnapshotJson, StringComparison.OrdinalIgnoreCase);

        // Verify Inventory Movements after Order Completion (initial 100 each -> 98, 99, 96, 95)
        var biryaniStockAfterSale = await inventoryDb.WarehouseStocks.SingleAsync(s => s.ProductVariantId == new ProductVariantId(biryaniVarId));
        var karahiStockAfterSale = await inventoryDb.WarehouseStocks.SingleAsync(s => s.ProductVariantId == new ProductVariantId(karahiVarId));
        var drinkStockAfterSale = await inventoryDb.WarehouseStocks.SingleAsync(s => s.ProductVariantId == new ProductVariantId(drinkVarId));
        var naanStockAfterSale = await inventoryDb.WarehouseStocks.SingleAsync(s => s.ProductVariantId == new ProductVariantId(naanVarId));
        Assert.Equal(98m, biryaniStockAfterSale.QuantityOnHand);
        Assert.Equal(99m, karahiStockAfterSale.QuantityOnHand);
        Assert.Equal(96m, drinkStockAfterSale.QuantityOnHand);
        Assert.Equal(95m, naanStockAfterSale.QuantityOnHand);

        var issueTxns = await inventoryDb.InventoryTransactions
            .Where(t => t.ReferenceType == "Order" && t.ReferenceId == order.OrderId)
            .ToListAsync();
        Assert.Equal(4, issueTxns.Count);

        // Verify Customer Accounts Receivable Ledger after credit sale
        var updatedCustomer = await restaurantDb.Customers.SingleAsync(c => c.Id == corporateCustomer.Id);
        Assert.Equal(2_380.80m, updatedCustomer.OutstandingBalance);
        Assert.Equal(2_380.80m, updatedCustomer.ReceivableBalance);

        var customerLedger = (await restaurantDb.CustomerLedgerEntries
            .Where(e => e.CustomerId == corporateCustomer.Id)
            .ToListAsync())
            .OrderByDescending(e => e.Date)
            .ToList();

        var creditSaleLedgerEntry = Assert.Single(customerLedger);
        Assert.Equal(2_380.80m, creditSaleLedgerEntry.Debit);
        Assert.Equal(0.00m, creditSaleLedgerEntry.Credit);
        Assert.Equal(2_380.80m, creditSaleLedgerEntry.RunningBalance);
        Assert.Equal("On Account", creditSaleLedgerEntry.PaymentMethod);

        // =================================================================================================
        // PHASE 4: Authorized Partial Refunds (On-Account Credit & Cash Payout) + Duplicate Retry
        // =================================================================================================
        // Partial Refund 1: Return 1x Mutton Karahi settled to CustomerAccountCredit
        // Independently calculated: Gross 2,000.00 - Disc 200.00 + Tax 288.00 = PKR 2,088.00
        var creditRefundCmd = new ProcessRefundCommand(
            order.OrderId,
            Items: [new RefundItemRequest(karahiLine.Id, Quantity: 1m, InventoryDisposition.Restock)],
            Reason: "Customer requested dish exchange / return",
            SettlementMethod: RefundSettlementMethod.CustomerAccountCredit,
            IdempotencyKey: $"REF-IDEMP-CREDIT-{Guid.NewGuid():N}",
            CashierId: cashierId,
            CashierName: cashierName,
            BranchId: branchId,
            ApprovingUserId: managerId,
            ApprovingUserName: managerName);

        var creditRefundDto = await mediator.Send(creditRefundCmd);
        Assert.NotNull(creditRefundDto);
        Assert.Equal(2_000.00m, creditRefundDto.SubtotalRefunded);
        Assert.Equal(200.00m, creditRefundDto.DiscountReversedTotal);
        Assert.Equal(288.00m, creditRefundDto.TaxReversedTotal);
        Assert.Equal(2_088.00m, creditRefundDto.GrandTotalRefunded);
        Assert.Equal(managerId, creditRefundDto.ApprovedByUserId);
        Assert.Equal(managerName, creditRefundDto.ApprovedByUserName);
        Assert.False(string.IsNullOrWhiteSpace(creditRefundDto.ReceiptSnapshotJson));

        // Duplicate-Request Retry Check: repeating the exact command returns the original refund record without double-posting
        var duplicateRefund = await mediator.Send(creditRefundCmd);
        Assert.Equal(creditRefundDto.RefundId, duplicateRefund.RefundId);
        Assert.Equal(2_088.00m, duplicateRefund.GrandTotalRefunded);

        // Partial Refund 2: Return 1 of 2 Chicken Biryani settled via CashPayout from drawer
        // Independently calculated: 1x Biryani Gross 600.00 - Disc 60.00 (half of 120.00) + Tax 86.40 (half of 172.80) = PKR 626.40
        const decimal cashRefundAmount = 626.40m;
        var cashRefundCmd = new ProcessRefundCommand(
            order.OrderId,
            Items: [new RefundItemRequest(biryaniLine.Id, Quantity: 1m, InventoryDisposition.Restock)],
            Reason: "Partial portion return paid out in cash",
            SettlementMethod: RefundSettlementMethod.CashPayout,
            IdempotencyKey: $"REF-IDEMP-CASH-{Guid.NewGuid():N}",
            CashierId: cashierId,
            CashierName: cashierName,
            BranchId: branchId,
            ApprovingUserId: managerId,
            ApprovingUserName: managerName);

        var cashRefundDto = await mediator.Send(cashRefundCmd);
        Assert.NotNull(cashRefundDto);
        Assert.Equal(600.00m, cashRefundDto.SubtotalRefunded);
        Assert.Equal(60.00m, cashRefundDto.DiscountReversedTotal);
        Assert.Equal(86.40m, cashRefundDto.TaxReversedTotal);
        Assert.Equal(cashRefundAmount, cashRefundDto.GrandTotalRefunded);
        Assert.False(string.IsNullOrWhiteSpace(cashRefundDto.ReceiptSnapshotJson));

        // Audit Immutability Invariant: original completed order remains Completed and unmutated
        var dbOrderAfterRefund = await restaurantDb.Orders.SingleAsync(o => o.Id == new OrderId(order.OrderId));
        Assert.Equal(OrderStatus.Completed, dbOrderAfterRefund.Status);
        Assert.False(string.IsNullOrWhiteSpace(dbOrderAfterRefund.ReceiptSnapshotJson));

        // Verify Inventory Restock Movements after Refunds (Karahi 99 -> 100, Biryani 98 -> 99)
        var biryaniStockAfterRefund = await inventoryDb.WarehouseStocks.SingleAsync(s => s.ProductVariantId == new ProductVariantId(biryaniVarId));
        var karahiStockAfterRefund = await inventoryDb.WarehouseStocks.SingleAsync(s => s.ProductVariantId == new ProductVariantId(karahiVarId));
        Assert.Equal(99m, biryaniStockAfterRefund.QuantityOnHand);
        Assert.Equal(100m, karahiStockAfterRefund.QuantityOnHand);

        // Verify Customer Account Ledger updated ONLY by the CustomerAccountCredit refund (2,380.80 - 2,088.00 = 292.80)
        var customerAfterRefund = await restaurantDb.Customers.SingleAsync(c => c.Id == corporateCustomer.Id);
        Assert.Equal(292.80m, customerAfterRefund.OutstandingBalance);

        var ledgerAfterRefund = (await restaurantDb.CustomerLedgerEntries
            .Where(e => e.CustomerId == corporateCustomer.Id)
            .ToListAsync())
            .OrderBy(e => e.Date)
            .ToList();

        Assert.Equal(2, ledgerAfterRefund.Count);
        var refundCreditEntry = ledgerAfterRefund[1];
        Assert.Equal(0.00m, refundCreditEntry.Debit);
        Assert.Equal(2_088.00m, refundCreditEntry.Credit);
        Assert.Equal(292.80m, refundCreditEntry.RunningBalance);

        // =================================================================================================
        // PHASE 5: Cash Drawer Balancing & Shift Closure (Distinct from Business-Day Close)
        // =================================================================================================
        // Mathematical Shift Drawer Invariant (independently calculated):
        // ExpectedDrawerCash = OpeningFloat (5,000.00) + NetCashReceipts (1,500.00)
        //                    - CashRefunds (626.40) + CashIns (200.00) - CashOuts (100.00)
        //                    = 5,973.60.
        // On-account amounts (2,380.80 credit sale and 2,088.00 credit refund) NEVER count as drawer cash.
        const decimal expectedDrawerCash = 5_973.60m;
        const decimal actualCountedCash = 5_973.60m;

        var openShift1Summary = await mediator.Send(new GetShiftSummaryQuery(shiftDto.ShiftId));
        Assert.Equal("Open", openShift1Summary.Shift.Status);
        Assert.Equal(5_000.00m, openShift1Summary.StartingCash);
        Assert.Equal(1_500.00m, openShift1Summary.CashSales);
        Assert.Equal(2_380.80m, openShift1Summary.OtherSales);
        Assert.Equal(200.00m, openShift1Summary.CashIn);
        Assert.Equal(100.00m, openShift1Summary.CashOut);
        Assert.Equal(expectedDrawerCash, openShift1Summary.ExpectedCash);

        var closeShiftCmd = new CloseShiftCommand(
            shiftDto.ShiftId,
            CountedCash: actualCountedCash,
            VarianceReason: null,
            Notes: "Exact cash drawer reconciliation without variance");

        var closedShiftSummary = await mediator.Send(closeShiftCmd);
        Assert.NotNull(closedShiftSummary);
        Assert.Equal("Closed", closedShiftSummary.Shift.Status);
        Assert.Equal(5_000.00m, closedShiftSummary.StartingCash);
        Assert.Equal(1_500.00m, closedShiftSummary.CashSales);
        Assert.Equal(2_380.80m, closedShiftSummary.OtherSales);
        Assert.Equal(200.00m, closedShiftSummary.CashIn);
        Assert.Equal(100.00m, closedShiftSummary.CashOut);
        Assert.Equal(expectedDrawerCash, closedShiftSummary.ExpectedCash);
        Assert.Equal(actualCountedCash, closedShiftSummary.CountedCash);
        Assert.Equal(0.00m, closedShiftSummary.Variance);

        // =================================================================================================
        // PHASE 5B: Cross-Shift Refund for Earlier Sale — Affects Current Shift, Not Closed Historical Shift
        // =================================================================================================
        const decimal shift2StartingFloat = 3_000.00m;
        const decimal shift2CashIn = 150.00m;
        const decimal shift2CashOut = 50.00m;

        var shift2Dto = await mediator.Send(new OpenShiftCommand(
            branchId,
            warehouseId,
            terminalId,
            cashierId,
            cashierName,
            shift2StartingFloat,
            "Shift 2 Opening Cash Float"));
        Assert.Equal("Open", shift2Dto.Status);

        await mediator.Send(new RecordCashMovementCommand(
            shift2Dto.ShiftId,
            CashMovementType.CashIn,
            shift2CashIn,
            "Shift 2 float supplement",
            cashierId));
        await mediator.Send(new RecordCashMovementCommand(
            shift2Dto.ShiftId,
            CashMovementType.CashOut,
            shift2CashOut,
            "Shift 2 courier fee",
            cashierId));

        // Refund 5x Roghani Naan from Shift 1's completed order during Shift 2 in Cash:
        // Independently calculated: 5 * 40.00 = 200.00 Gross - 20.00 Trade Discount Reversed + 0.00 Exempt Tax = PKR 180.00
        const decimal shift2CashRefundForShift1Order = 180.00m;
        var shift2RefundDto = await mediator.Send(new ProcessRefundCommand(
            order.OrderId,
            Items: [new RefundItemRequest(naanLine.Id, Quantity: 5m, InventoryDisposition.Restock)],
            Reason: "Returned unopened naan pack in subsequent shift",
            SettlementMethod: RefundSettlementMethod.CashPayout,
            IdempotencyKey: $"REF-IDEMP-SHIFT2-{Guid.NewGuid():N}",
            CashierId: cashierId,
            CashierName: cashierName,
            BranchId: branchId,
            ApprovingUserId: managerId,
            ApprovingUserName: managerName));
        Assert.Equal(200.00m, shift2RefundDto.SubtotalRefunded);
        Assert.Equal(20.00m, shift2RefundDto.DiscountReversedTotal);
        Assert.Equal(0.00m, shift2RefundDto.TaxReversedTotal);
        Assert.Equal(shift2CashRefundForShift1Order, shift2RefundDto.GrandTotalRefunded);

        // Closed historical Shift 1 must remain completely unchanged
        var historicalShift1AfterShift2Refund = await mediator.Send(new GetShiftSummaryQuery(shiftDto.ShiftId));
        Assert.Equal("Closed", historicalShift1AfterShift2Refund.Shift.Status);
        Assert.Equal(expectedDrawerCash, historicalShift1AfterShift2Refund.ExpectedCash);
        Assert.Equal(actualCountedCash, historicalShift1AfterShift2Refund.CountedCash);
        Assert.Equal(0.00m, historicalShift1AfterShift2Refund.Variance);

        var persistedShift1 = await restaurantDb.Shifts.SingleAsync(s => s.Id == new ShiftId(shiftDto.ShiftId));
        Assert.Equal(expectedDrawerCash, persistedShift1.ExpectedCash);
        Assert.Equal(actualCountedCash, persistedShift1.CountedCash);
        Assert.Equal(0.00m, persistedShift1.CashVariance);

        // Open Shift 2 must deduct ONLY Shift 2's cash refund (180.00) exactly once:
        // ExpectedDrawerCash2 = 3,000.00 (Float) + 0.00 (Cash Sales) - 180.00 (Refund) + 150.00 (CashIn) - 50.00 (CashOut) = 2,920.00
        const decimal expectedShift2DrawerCash = 2_920.00m;
        var openShift2Summary = await mediator.Send(new GetShiftSummaryQuery(shift2Dto.ShiftId));
        Assert.Equal("Open", openShift2Summary.Shift.Status);
        Assert.Equal(shift2StartingFloat, openShift2Summary.StartingCash);
        Assert.Equal(0.00m, openShift2Summary.CashSales);
        Assert.Equal(shift2CashIn, openShift2Summary.CashIn);
        Assert.Equal(shift2CashOut, openShift2Summary.CashOut);
        Assert.Equal(expectedShift2DrawerCash, openShift2Summary.ExpectedCash);

        var closedShift2Summary = await mediator.Send(new CloseShiftCommand(
            shift2Dto.ShiftId,
            CountedCash: expectedShift2DrawerCash,
            VarianceReason: null,
            Notes: "Shift 2 reconciled after cross-shift refund"));
        Assert.Equal("Closed", closedShift2Summary.Shift.Status);
        Assert.Equal(expectedShift2DrawerCash, closedShift2Summary.ExpectedCash);
        Assert.Equal(expectedShift2DrawerCash, closedShift2Summary.CountedCash);
        Assert.Equal(0.00m, closedShift2Summary.Variance);

        var postCloseShift2Summary = await mediator.Send(new GetShiftSummaryQuery(shift2Dto.ShiftId));
        Assert.Equal(expectedShift2DrawerCash, postCloseShift2Summary.ExpectedCash);
        Assert.Equal(expectedShift2DrawerCash, postCloseShift2Summary.CountedCash);
        Assert.Equal(0.00m, postCloseShift2Summary.Variance);

        // =================================================================================================
        // PHASE 6: Business Day-Close Shift Reconciliation (Distinct Operation)
        // =================================================================================================
        // Verify Day Close Preconditions and Aggregates
        // Total Refunds = 2,088.00 (Account Credit) + 626.40 (Shift 1 Cash Payout) + 180.00 (Shift 2 Cash Payout) = 2,894.40
        var daySummaryQuery = new GetBusinessDaySummaryQuery(branchId, today);
        var daySummary = await mediator.Send(daySummaryQuery);

        Assert.False(daySummary.IsAlreadyClosed);
        Assert.True(daySummary.CanClose);
        Assert.Empty(daySummary.OpenShifts);
        Assert.Equal(2, daySummary.ClosedShifts.Count);
        Assert.Equal(3_880.80m, daySummary.TotalSales);
        Assert.Equal(1_500.00m, daySummary.CashSales);
        Assert.Equal(2_380.80m, daySummary.OtherSales);
        Assert.Equal(380.00m, daySummary.Discounts);
        Assert.Equal(510.46m, daySummary.Tax);
        Assert.Equal(2_894.40m, daySummary.Refunds);
        Assert.Equal(350.00m, daySummary.CashIn);
        Assert.Equal(150.00m, daySummary.CashOut);
        Assert.Equal(0.00m, daySummary.TotalShiftVariance);

        // Execute Permanent Business Day Close
        var closeDayCmd = new CloseBusinessDayCommand(
            branchId,
            today,
            cashierId,
            cashierName,
            "Official Business Day Close Reconciled");

        var dayCloseResult = await mediator.Send(closeDayCmd);
        Assert.NotNull(dayCloseResult);
        Assert.Equal(branchId, dayCloseResult.BranchId);
        Assert.Equal(today, dayCloseResult.BusinessDate);
        Assert.Equal(3_880.80m, dayCloseResult.TotalSales);
        Assert.Equal(1_500.00m, dayCloseResult.CashSales);
        Assert.Equal(2_380.80m, dayCloseResult.OtherPayments);
        Assert.Equal(2_894.40m, dayCloseResult.Refunds);

        // Day Close Idempotency Invariant: duplicate close attempts fail closed
        await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(closeDayCmd));
    }

    [Fact]
    public void PakistanTaxAndTradeDiscount_CalculationInvariant_MatchesAwayFromZeroAndHamiltonHare()
    {
        // Tests pure Pakistan sales tax engine invariants across exclusive, inclusive, and exempt items
        var line1 = new TaxCalculationLineInput(
            Guid.NewGuid(),
            Quantity: 2m,
            UnitPrice: 600.00m,
            LineDiscountAmount: 0m,
            TaxClassification: "Taxable",
            Authority: "PRA",
            TaxCode: "PK-PRA-16",
            TaxRatePercentage: 16.00m,
            TaxIsInclusive: false);

        var line2 = new TaxCalculationLineInput(
            Guid.NewGuid(),
            Quantity: 1m,
            UnitPrice: 2000.00m,
            LineDiscountAmount: 0m,
            TaxClassification: "Taxable",
            Authority: "PRA",
            TaxCode: "PK-PRA-16",
            TaxRatePercentage: 16.00m,
            TaxIsInclusive: false);

        var line3 = new TaxCalculationLineInput(
            Guid.NewGuid(),
            Quantity: 4m,
            UnitPrice: 100.00m,
            LineDiscountAmount: 0m,
            TaxClassification: "Taxable",
            Authority: "PRA",
            TaxCode: "PK-PRA-16-INC",
            TaxRatePercentage: 16.00m,
            TaxIsInclusive: true);

        var line4 = new TaxCalculationLineInput(
            Guid.NewGuid(),
            Quantity: 5m,
            UnitPrice: 40.00m,
            LineDiscountAmount: 0m,
            TaxClassification: "Exempt",
            Authority: "FBR",
            TaxCode: "PK-EXEMPT",
            TaxRatePercentage: 0.00m,
            TaxIsInclusive: false);

        const decimal orderDiscount = 380.00m; // 10% on 3,800.00 gross
        var result = TaxCalculator.Calculate([line1, line2, line3, line4], orderDiscount);

        // Subtotal gross = 1200 + 2000 + 400 + 200 = 3800.00
        Assert.Equal(3_800.00m, result.SubtotalGross);
        Assert.Equal(orderDiscount, result.TotalOrderDiscounts);

        // Proportional Hamilton-Hare discount apportionment:
        // Line 1: 120.00 discount, net 1080.00 -> PRA 16% = 172.80
        // Line 2: 200.00 discount, net 1800.00 -> PRA 16% = 288.00
        // Line 3: 40.00 discount, net 360.00 -> Inclusive 16% = 360 - 310.34 = 49.66
        // Line 4: 20.00 discount, net 180.00 -> Exempt = 0.00
        Assert.Equal(460.80m, result.TotalExclusiveTax);
        Assert.Equal(49.66m, result.TotalInclusiveTax);
        Assert.Equal(510.46m, result.TotalTax);

        // Invariant: Total lines payable includes inclusive line net + exclusive line gross + exclusive tax - discount
        Assert.Equal(3_880.80m, result.TotalLinesPayable);
    }

    [Fact]
    public async Task SplitTender_CashPlusOnAccount_StrictSeparationOfDrawerCashAndCustomerReceivables()
    {
        var warehouseId = Guid.NewGuid();
        var cashierId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var terminalId = Guid.NewGuid();

        var cashMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        var onAccountMethod = PaymentMethod.Create(PaymentMethodName.Create("On Account"));
        await _restaurantDb.PaymentMethods.AddRangeAsync(cashMethod, onAccountMethod);

        var customer = Customer.Create(
            EntityCode.Create("CUST-SPLIT-01"),
            "Karachi Traders",
            "+923211234567",
            "I.I. Chundrigar Road, Karachi",
            "finance@ktraders.example.pk",
            openingBalance: 0.00m,
            creditLimit: 10_000.00m,
            notes: null,
            isDefault: false,
            isCreditAllowed: true);
        await _restaurantDb.Customers.AddAsync(customer);
        await _restaurantDb.SaveChangesAsync();

        var (biryaniVarId, _, _, _) = await SeedPakistaniCatalogAsync(warehouseId);

        // Open Shift with PKR 2,000 float
        var shift = await _mediator.Send(new OpenShiftCommand(branchId, warehouseId, terminalId, cashierId, "Tariq", 2_000.00m));

        // Create Order: 5x Biryani @ 600 = 3,000 gross + 16% tax (480) = 3,480.00
        var order = await _mediator.Send(new CreateOrderCommand(OrderType.TakeAway, warehouseId, CustomerId: customer.Id.Value));
        await _mediator.Send(new AddOrderLineCommand(order.OrderId, biryaniVarId, Quantity: 5));

        // Tender 1: 1,000 Cash
        await _mediator.Send(new RecordPaymentCommand(order.OrderId, cashMethod.Id.Value, 1_000.00m, false, shift.ShiftId));
        // Tender 2: 2,480 On Account
        await _mediator.Send(new RecordPaymentCommand(order.OrderId, onAccountMethod.Id.Value, 2_480.00m, false, shift.ShiftId));

        await _mediator.Send(new CompleteOrderCommand(order.OrderId));

        // Close Shift: Expected Cash must strictly equal Starting Float (2,000) + Cash Tender (1,000) = 3,000.00
        // It must NOT include the 2,480.00 On-Account credit!
        var shiftSummary = await _mediator.Send(new CloseShiftCommand(shift.ShiftId, CountedCash: 3_000.00m));
        Assert.Equal(3_000.00m, shiftSummary.ExpectedCash);
        Assert.Equal(0.00m, shiftSummary.Variance);

        // Customer outstanding balance must reflect the 2,480.00 receivable
        var customerAfter = await _restaurantDb.Customers.SingleAsync(c => c.Id == customer.Id);
        Assert.Equal(2_480.00m, customerAfter.OutstandingBalance);
    }

    [Fact]
    public async Task CompensatingRefund_OnCompletedOrder_IsImmutableAndReversesTaxAndDiscount()
    {
        var warehouseId = Guid.NewGuid();
        var cashierId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var terminalId = Guid.NewGuid();

        var cashMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        await _restaurantDb.PaymentMethods.AddAsync(cashMethod);
        await _restaurantDb.SaveChangesAsync();

        var (_, karahiVarId, _, _) = await SeedPakistaniCatalogAsync(warehouseId);

        var shift = await _mediator.Send(new OpenShiftCommand(branchId, warehouseId, terminalId, cashierId, "Usman", 1_000.00m));

        // Order: 2x Karahi @ 2,000 = 4,000. Trade discount: 400. Tax: 16% on 3600 = 576. Grand Total = 4,176.00
        var order = await _mediator.Send(new CreateOrderCommand(OrderType.TakeAway, warehouseId));
        var line = await _mediator.Send(new AddOrderLineCommand(order.OrderId, karahiVarId, Quantity: 2));
        await _mediator.Send(new ApplyDiscountToOrderCommand(order.OrderId, DiscountType.FixedAmount, 400.00m, "Fixed Trade Promo"));

        var summary = await _mediator.Send(new GetOrderSummaryQuery(order.OrderId));
        Assert.Equal(4_176.00m, summary.GrandTotal);

        await _mediator.Send(new RecordPaymentCommand(order.OrderId, cashMethod.Id.Value, summary.GrandTotal, false, shift.ShiftId));
        await _mediator.Send(new CompleteOrderCommand(order.OrderId));

        // Partial Refund: 1 of 2 Karahi refunded in Cash Payout
        var refund = await _mediator.Send(new ProcessRefundCommand(
            order.OrderId,
            Items: [new RefundItemRequest(line.Id, Quantity: 1m, InventoryDisposition.Restock)],
            Reason: "Partial order return",
            SettlementMethod: RefundSettlementMethod.CashPayout,
            IdempotencyKey: $"REF-PARTIAL-{Guid.NewGuid():N}",
            CashierId: cashierId,
            CashierName: "Usman",
            BranchId: branchId));

        // Proportional reversal: 200 discount reversed, 288 tax reversed, 2,088 payable refund
        Assert.Equal(2_000.00m, refund.SubtotalRefunded);
        Assert.Equal(200.00m, refund.DiscountReversedTotal);
        Assert.Equal(288.00m, refund.TaxReversedTotal);
        Assert.Equal(2_088.00m, refund.GrandTotalRefunded);

        // Verification: Attempting to refund more quantity than remains eligible throws RestaurantDomainException
        await Assert.ThrowsAsync<RestaurantDomainException>(() => _mediator.Send(new ProcessRefundCommand(
            order.OrderId,
            Items: [new RefundItemRequest(line.Id, Quantity: 2m, InventoryDisposition.Restock)],
            Reason: "Excessive quantity return",
            SettlementMethod: RefundSettlementMethod.CashPayout,
            IdempotencyKey: $"REF-EXCESS-{Guid.NewGuid():N}",
            CashierId: cashierId,
            CashierName: "Usman",
            BranchId: branchId)));
    }

    [Fact]
    public async Task DayClose_FailsClosedWhenOpenShiftsExist_AndSucceedsWhenAllReconciled()
    {
        var branchId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var terminalId = Guid.NewGuid();
        var cashierId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.Today);

        // Open a shift without closing it
        var shift = await _mediator.Send(new OpenShiftCommand(branchId, warehouseId, terminalId, cashierId, "Cashier 1", 3_000.00m));

        // Attempting to close business day must fail closed
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _mediator.Send(
            new CloseBusinessDayCommand(branchId, today, cashierId, "Manager", "Early close attempt")));

        Assert.Contains("open", ex.Message, StringComparison.OrdinalIgnoreCase);

        // Close the open shift
        await _mediator.Send(new CloseShiftCommand(shift.ShiftId, CountedCash: 3_000.00m));

        // Now day close must succeed
        var dayClose = await _mediator.Send(new CloseBusinessDayCommand(branchId, today, cashierId, "Manager", "Proper day close"));
        Assert.NotNull(dayClose);
        Assert.Equal(today, dayClose.BusinessDate);
    }

    private Task<(Guid BiryaniVariantId, Guid KarahiVariantId, Guid DrinkVariantId, Guid NaanVariantId)> SeedPakistaniCatalogAsync(Guid warehouseId) =>
        SeedPakistaniCatalogIntoAsync(_catalogDb, _inventoryDb, warehouseId);

    private static async Task<(Guid BiryaniVariantId, Guid KarahiVariantId, Guid DrinkVariantId, Guid NaanVariantId)> SeedPakistaniCatalogIntoAsync(
        CatalogDbContext catalogDb,
        InventoryDbContext inventoryDb,
        Guid warehouseId)
    {
        var uom = UnitOfMeasureId.New();
        var pkrCurrencyId = new CurrencyId(Guid.NewGuid());

        // 1. Chicken Biryani: Exclusive PRA 16% (PK-PRA-16)
        var p1 = Product.Create(
            ProductName.Create("Chicken Biryani"),
            Sku.Create("PAK-BIRYANI"),
            uom,
            TaxConfiguration.Create(16.00m, isInclusive: false, TaxClassification.Taxable, "PK-PRA-16", "PRA"));
        var v1 = ProductVariant.Create(p1.Id, VariantName.Create("Single Portion"), Sku.Create("PAK-BIRYANI-SGL"), uom);
        var price1 = ProductPrice.Create(v1.Id, PriceType.Selling, 600.00m, pkrCurrencyId);

        // 2. Mutton Karahi: Exclusive PRA 16% (PK-PRA-16)
        var p2 = Product.Create(
            ProductName.Create("Mutton Karahi"),
            Sku.Create("PAK-KARAHI"),
            uom,
            TaxConfiguration.Create(16.00m, isInclusive: false, TaxClassification.Taxable, "PK-PRA-16", "PRA"));
        var v2 = ProductVariant.Create(p2.Id, VariantName.Create("Full Pot"), Sku.Create("PAK-KARAHI-FULL"), uom);
        var price2 = ProductPrice.Create(v2.Id, PriceType.Selling, 2_000.00m, pkrCurrencyId);

        // 3. Gourmet Soft Drink: Inclusive PRA 16% (PK-PRA-16-INC)
        var p3 = Product.Create(
            ProductName.Create("Gourmet Soft Drink"),
            Sku.Create("PAK-DRINK"),
            uom,
            TaxConfiguration.Create(16.00m, isInclusive: true, TaxClassification.Taxable, "PK-PRA-16-INC", "PRA"));
        var v3 = ProductVariant.Create(p3.Id, VariantName.Create("Can 250ml"), Sku.Create("PAK-DRINK-CAN"), uom);
        var price3 = ProductPrice.Create(v3.Id, PriceType.Selling, 100.00m, pkrCurrencyId);

        // 4. Roghani Naan: Exempt 0% tax (PK-EXEMPT)
        var p4 = Product.Create(
            ProductName.Create("Roghani Naan"),
            Sku.Create("PAK-NAAN"),
            uom,
            TaxConfiguration.Create(0.00m, isInclusive: false, TaxClassification.Exempt, "PK-EXEMPT", "FBR"));
        var v4 = ProductVariant.Create(p4.Id, VariantName.Create("Single"), Sku.Create("PAK-NAAN-SGL"), uom);
        var price4 = ProductPrice.Create(v4.Id, PriceType.Selling, 40.00m, pkrCurrencyId);

        await catalogDb.Products.AddRangeAsync(p1, p2, p3, p4);
        await catalogDb.ProductVariants.AddRangeAsync(v1, v2, v3, v4);
        await catalogDb.ProductPrices.AddRangeAsync(price1, price2, price3, price4);
        await catalogDb.SaveChangesAsync();

        // Warehouse Stock reception (100 units each)
        var s1 = WarehouseStock.Create(new WarehouseId(warehouseId), v1.Id);
        var s2 = WarehouseStock.Create(new WarehouseId(warehouseId), v2.Id);
        var s3 = WarehouseStock.Create(new WarehouseId(warehouseId), v3.Id);
        var s4 = WarehouseStock.Create(new WarehouseId(warehouseId), v4.Id);
        s1.Receive(100m);
        s2.Receive(100m);
        s3.Receive(100m);
        s4.Receive(100m);

        await inventoryDb.WarehouseStocks.AddRangeAsync(s1, s2, s3, s4);
        await inventoryDb.SaveChangesAsync();

        return (v1.Id.Value, v2.Id.Value, v3.Id.Value, v4.Id.Value);
    }

    [Fact]
    public async Task CategoryInheritedTaxProfile_OrderLineAndExpandedSalesReport_PreservesInheritedAndExemptSnapshots()
    {
        var warehouseId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var terminalId = Guid.NewGuid();
        var cashierId = Guid.NewGuid();
        var uom = UnitOfMeasureId.New();
        var pkrCurrencyId = new CurrencyId(Guid.NewGuid());

        var cashMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        await _restaurantDb.PaymentMethods.AddAsync(cashMethod);
        await _restaurantDb.SaveChangesAsync();

        // 1. Seed a PRA 16% exclusive tax profile and assign it as the default profile on a Category
        var praProfile = TaxProfile.Create(
            "PK-PRA-CAT-16",
            "Punjab Restaurant Sales Tax 16%",
            "PRA",
            "Punjab",
            TaxClassification.Taxable,
            ItemTaxClassification.Services,
            16.00m,
            TaxPricingMode.Exclusive,
            DateTimeOffset.UtcNow.AddDays(-10));
        await _catalogDb.TaxProfiles.AddAsync(praProfile);

        var category = Clovent.Catalog.Categories.ProductCategory.Create(
            Clovent.Catalog.Categories.ValueObjects.ProductCategoryName.Create("BBQ Platters"),
            defaultTaxProfileId: praProfile.Id);
        await _catalogDb.ProductCategories.AddAsync(category);

        // 2. Product A: no explicit product tax profile (TaxConfiguration.None) -> must inherit 16% from Category
        var inheritedProduct = Product.Create(
            ProductName.Create("Seekh Kabab Platter"),
            Sku.Create("PAK-KABAB"),
            uom,
            TaxConfiguration.None,
            categoryId: category.Id);
        var inheritedVariant = ProductVariant.Create(inheritedProduct.Id, VariantName.Create("Full"), Sku.Create("PAK-KABAB-FULL"), uom);
        var inheritedPrice = ProductPrice.Create(inheritedVariant.Id, PriceType.Selling, 1_000.00m, pkrCurrencyId);

        // 3. Product B: in the same Category, but explicitly marked Exempt -> must preserve 0% Exempt and NOT inherit 16%
        var exemptProduct = Product.Create(
            ProductName.Create("Plain Roti"),
            Sku.Create("PAK-ROTI"),
            uom,
            TaxConfiguration.Create(0.00m, isInclusive: false, TaxClassification.Exempt, "PK-EXEMPT", "FBR"),
            categoryId: category.Id);
        var exemptVariant = ProductVariant.Create(exemptProduct.Id, VariantName.Create("Single"), Sku.Create("PAK-ROTI-SGL"), uom);
        var exemptPrice = ProductPrice.Create(exemptVariant.Id, PriceType.Selling, 50.00m, pkrCurrencyId);

        await _catalogDb.Products.AddRangeAsync(inheritedProduct, exemptProduct);
        await _catalogDb.ProductVariants.AddRangeAsync(inheritedVariant, exemptVariant);
        await _catalogDb.ProductPrices.AddRangeAsync(inheritedPrice, exemptPrice);
        await _catalogDb.SaveChangesAsync();

        var stockA = WarehouseStock.Create(new WarehouseId(warehouseId), inheritedVariant.Id);
        var stockB = WarehouseStock.Create(new WarehouseId(warehouseId), exemptVariant.Id);
        stockA.Receive(50m);
        stockB.Receive(50m);
        await _inventoryDb.WarehouseStocks.AddRangeAsync(stockA, stockB);
        await _inventoryDb.SaveChangesAsync();

        var shift = await _mediator.Send(new OpenShiftCommand(branchId, warehouseId, terminalId, cashierId, "Bilal", 1_000.00m));
        var order = await _mediator.Send(new CreateOrderCommand(OrderType.TakeAway, warehouseId));

        var lineInherited = await _mediator.Send(new AddOrderLineCommand(order.OrderId, inheritedVariant.Id.Value, 1m));
        var lineExempt = await _mediator.Send(new AddOrderLineCommand(order.OrderId, exemptVariant.Id.Value, 2m));

        // Verify Category inheritance reached lineInherited (16% PK-PRA-CAT-16) and explicit exemption preserved on lineExempt (0% Exempt)
        Assert.Equal(16.00m, lineInherited.TaxRatePercentage);
        Assert.Equal("PK-PRA-CAT-16", lineInherited.TaxCode);
        Assert.Equal("PRA", lineInherited.TaxAuthority);
        Assert.Equal(0.00m, lineExempt.TaxRatePercentage);
        Assert.Equal("Exempt", lineExempt.TaxClassification);

        // Apply 10% order discount: Gross = 1,000 + 100 = 1,100. Discount = 110 (100 on Kabab, 10 on Roti).
        // NetOfDiscount tax on Kabab: (1,000 - 100) * 16% = 144.00. Total Payable = 900 + 144 + 90 = 1,134.00.
        await _mediator.Send(new ApplyDiscountToOrderCommand(order.OrderId, DiscountType.Percentage, 10m, "Promo 10%"));
        var summary = await _mediator.Send(new GetOrderSummaryQuery(order.OrderId));
        Assert.Equal(144.00m, summary.TaxTotal);
        Assert.Equal(1_134.00m, summary.GrandTotal);

        await _mediator.Send(new RecordPaymentCommand(order.OrderId, cashMethod.Id.Value, summary.GrandTotal, false, shift.ShiftId));
        await _mediator.Send(new CompleteOrderCommand(order.OrderId));

        // Verify GetExpandedSalesSummaryQuery uses the frozen snapshot totals (144.00 tax, 110.00 discount, 1,134.00 sales)
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        var report = await _mediator.Send(new Clovent.Restaurant.Application.EndOfDay.Queries.GetExpandedSalesSummaryQuery(
            warehouseId,
            today.AddDays(-1),
            today.AddDays(1)));

        Assert.Equal(1_100.00m, report.Kpis.GrossSales);
        Assert.Equal(110.00m, report.Kpis.Discounts);
        Assert.Equal(990.00m, report.Kpis.NetSales);
        Assert.Equal(144.00m, report.Kpis.Tax);
        Assert.Equal(1_134.00m, report.Kpis.TotalCollected);
    }

    [Fact]
    public async Task SplitPayment_PartialFailureRetryAndCustomerCredit_PreservesCommittedLegAndBlocksCompletedOrderVoid()
    {
        var warehouseId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var terminalId = Guid.NewGuid();
        var cashierId = Guid.NewGuid();

        var cashMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        var customerCreditMethod = PaymentMethod.Create(PaymentMethodName.Create("Customer Credit"));
        await _restaurantDb.PaymentMethods.AddRangeAsync(cashMethod, customerCreditMethod);

        var customer = Customer.Create(
            EntityCode.Create("CUST-RETRY-01"),
            "Lahore Logistics",
            "+923001112233",
            "Gulberg III, Lahore",
            "accounts@lahorelogistics.example.pk",
            openingBalance: 0.00m,
            creditLimit: 3_000.00m,
            notes: null,
            isDefault: false,
            isCreditAllowed: true);
        await _restaurantDb.Customers.AddAsync(customer);
        await _restaurantDb.SaveChangesAsync();

        var (biryaniVarId, _, _, _) = await SeedPakistaniCatalogAsync(warehouseId);
        var shift = await _mediator.Send(new OpenShiftCommand(branchId, warehouseId, terminalId, cashierId, "Hamza", 2_000.00m));

        // Order: 5x Biryani @ 600 = 3,000 + 16% tax (480) = 3,480.00
        var order = await _mediator.Send(new CreateOrderCommand(OrderType.TakeAway, warehouseId, CustomerId: customer.Id.Value));
        await _mediator.Send(new AddOrderLineCommand(order.OrderId, biryaniVarId, Quantity: 5m));

        // 1. Leg 1 succeeds: 1,000.00 PKR Cash with idempotency key
        var leg1Key = $"SPLIT-{order.OrderId:N}-LEG1";
        var leg1Payment = await _mediator.Send(new RecordPaymentCommand(order.OrderId, cashMethod.Id.Value, 1_000.00m, false, shift.ShiftId, IdempotencyKey: leg1Key));
        Assert.Equal(1_000.00m, leg1Payment.Amount);
        Assert.Equal(shift.ShiftId, leg1Payment.ShiftId);

        // 2. Leg 2 fails (attempts 3,200.00 PKR Customer Credit which exceeds customer's 3,000.00 credit limit / remaining balance)
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _mediator.Send(new RecordPaymentCommand(order.OrderId, customerCreditMethod.Id.Value, 3_200.00m, false, shift.ShiftId, IdempotencyKey: $"SPLIT-{order.OrderId:N}-LEG2-FAIL")));

        // 3. Verify Leg 1 remains durably committed and visible, and order remains Open
        var orderAfterFailure = await _mediator.Send(new GetOrderByIdQuery(order.OrderId));
        var summaryAfterFailure = await _mediator.Send(new GetOrderSummaryQuery(order.OrderId));
        Assert.Equal(OrderStatus.Open.ToString(), orderAfterFailure!.Status);
        Assert.Equal(1_000.00m, summaryAfterFailure.PaidTotal);
        Assert.Equal(2_480.00m, summaryAfterFailure.Balance);

        // 4. Retrying Leg 1 with the same idempotency key does NOT double-post or charge twice
        var leg1Retry = await _mediator.Send(new RecordPaymentCommand(order.OrderId, cashMethod.Id.Value, 1_000.00m, false, shift.ShiftId, IdempotencyKey: leg1Key));
        Assert.Equal(leg1Payment.PaymentId, leg1Retry.PaymentId);
        var summaryAfterRetry = await _mediator.Send(new GetOrderSummaryQuery(order.OrderId));
        Assert.Equal(1_000.00m, summaryAfterRetry.PaidTotal);

        // 5. Cashier resumes and records valid Leg 2 (2,480.00 PKR via "Customer Credit") and completes order
        var leg2Key = $"SPLIT-{order.OrderId:N}-LEG2";
        var leg2Payment = await _mediator.Send(new RecordPaymentCommand(order.OrderId, customerCreditMethod.Id.Value, 2_480.00m, false, shift.ShiftId, IdempotencyKey: leg2Key));
        Assert.Equal(2_480.00m, leg2Payment.Amount);
        Assert.Equal(shift.ShiftId, leg2Payment.ShiftId);

        await _mediator.Send(new CompleteOrderCommand(order.OrderId));

        // Verify "Customer Credit" updated customer receivable balance and created ledger entry with ShiftId
        var customerAfter = await _restaurantDb.Customers.SingleAsync(c => c.Id == customer.Id);
        Assert.Equal(2_480.00m, customerAfter.OutstandingBalance);
        var ledgerEntry = await _restaurantDb.CustomerLedgerEntries.SingleAsync(e => e.CustomerId == customer.Id);
        Assert.Equal(2_480.00m, ledgerEntry.Debit);
        Assert.Equal(new ShiftId(shift.ShiftId), ledgerEntry.ShiftId);

        // 6. Verify completed order cannot be voided directly (must use compensating refund workflow)
        await Assert.ThrowsAsync<RestaurantDomainException>(() =>
            _mediator.Send(new VoidOrderCommand(order.OrderId, "Attempt to void completed order")));
    }

    [Fact]
    public async Task DisposableSqlServer_MigrationUpgradeFrom56aed8dSchema_PreservesRefundsAndSnapshotsAndAppliesAdditiveColumns()
    {
        var dbName = $"Clovent_DisposableUpgrade_{Guid.NewGuid():N}";
        var masterCs = "Server=.;Database=master;Integrated Security=True;TrustServerCertificate=True;";
        var connectionString = $"Server=.;Database={dbName};Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";

        try
        {
            await using (var masterConn = new Microsoft.Data.SqlClient.SqlConnection(masterCs))
            {
                await masterConn.OpenAsync();
                await using var createCmd = masterConn.CreateCommand();
                createCmd.CommandText = $"CREATE DATABASE [{dbName}];";
                await createCmd.ExecuteNonQueryAsync();
            }

            // 1. Provision full current schema first, then simulate an earlier 56aed8d database state by:
            //    - Removing migration '20261011050000_AddRefundShiftAndTerminalAttribution' from [Restaurant].[__EFMigrationsHistory]
            //    - Dropping IX_Refunds_ShiftId, IX_Refunds_TerminalId, ShiftId, and TerminalId from [Restaurant].[Refunds]
            //    - Inserting a pre-upgrade Refund row and verifying pre-upgrade data exists
            var provisioningService = new Clovent.Desktop.Commissioning.Database.DatabaseProvisioningService();
            await provisioningService.ApplyMigrationsAsync(connectionString);

            var preUpgradeRefundId = Guid.NewGuid();
            var preUpgradeOrderId = Guid.NewGuid();
            var preUpgradeBranchId = Guid.NewGuid();
            var preUpgradeWarehouseId = Guid.NewGuid();

            await using (var sqlConn = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
            {
                await sqlConn.OpenAsync();
                await using var downgradeCmd = sqlConn.CreateCommand();
                downgradeCmd.CommandText = @"
                    DELETE FROM [Restaurant].[__EFMigrationsHistory]
                    WHERE [MigrationId] = N'20261011050000_AddRefundShiftAndTerminalAttribution';

                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Refunds_ShiftId' AND object_id = OBJECT_ID('[Restaurant].[Refunds]'))
                        DROP INDEX [IX_Refunds_ShiftId] ON [Restaurant].[Refunds];

                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Refunds_TerminalId' AND object_id = OBJECT_ID('[Restaurant].[Refunds]'))
                        DROP INDEX [IX_Refunds_TerminalId] ON [Restaurant].[Refunds];

                    IF COL_LENGTH('[Restaurant].[Refunds]', 'ShiftId') IS NOT NULL
                        ALTER TABLE [Restaurant].[Refunds] DROP COLUMN [ShiftId];

                    IF COL_LENGTH('[Restaurant].[Refunds]', 'TerminalId') IS NOT NULL
                        ALTER TABLE [Restaurant].[Refunds] DROP COLUMN [TerminalId];

                    INSERT INTO [Restaurant].[Refunds] (
                        [Id], [RefundNumber], [OrderId], [BranchId], [WarehouseId],
                        [RefundedAtUtc], [CashierId], [CashierName], [Reason],
                        [ApprovedByUserId], [ApprovedByUserName], [IdempotencyKey],
                        [SubtotalRefunded], [DiscountReversedTotal], [TaxReversedTotal], [GrandTotalRefunded],
                        [SettlementMethod], [SettlementReference], [CustomerId],
                        [ReceiptSnapshotJson], [CreatedAtUtc]
                    ) VALUES (
                        @refundId, N'RF-PRE-UPGRADE-001', @orderId, @branchId, @warehouseId,
                        SYSDATETIMEOFFSET(), @userId, N'Cashier PreUpgrade', N'Historical refund before upgrade',
                        NULL, NULL, N'IDEMP-PRE-UPGRADE-001',
                        1000.00, 100.00, 144.00, 1044.00,
                        N'CashPayout', NULL, NULL,
                        N'{""Policy"":""CBOS-TaxPolicy-NetOfDiscount-v1""}', SYSDATETIMEOFFSET()
                    );";
                downgradeCmd.Parameters.AddWithValue("@refundId", preUpgradeRefundId);
                downgradeCmd.Parameters.AddWithValue("@orderId", preUpgradeOrderId);
                downgradeCmd.Parameters.AddWithValue("@branchId", preUpgradeBranchId);
                downgradeCmd.Parameters.AddWithValue("@warehouseId", preUpgradeWarehouseId);
                downgradeCmd.Parameters.AddWithValue("@userId", Guid.NewGuid());
                await downgradeCmd.ExecuteNonQueryAsync();
            }

            // 2. Run ApplyMigrationsAsync on the 56aed8d-shaped database to exercise the additive upgrade path
            await provisioningService.ApplyMigrationsAsync(connectionString);

            // 3. Verify migration history, restored columns/indexes, and preservation of the historical refund row
            await using (var verifyConn = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
            {
                await verifyConn.OpenAsync();

                await using var checkHistoryCmd = verifyConn.CreateCommand();
                checkHistoryCmd.CommandText = @"
                    SELECT COUNT(*) FROM [Restaurant].[__EFMigrationsHistory]
                    WHERE [MigrationId] IN (
                        N'20261008152015_AddRefundDomainAndTaxSnapshots',
                        N'20261011050000_AddRefundShiftAndTerminalAttribution'
                    );";
                var historyCount = (int)(await checkHistoryCmd.ExecuteScalarAsync())!;
                Assert.Equal(2, historyCount);

                await using var checkColsCmd = verifyConn.CreateCommand();
                checkColsCmd.CommandText = @"
                    SELECT CASE WHEN COL_LENGTH('[Restaurant].[Refunds]', 'ShiftId') IS NOT NULL
                                 AND COL_LENGTH('[Restaurant].[Refunds]', 'TerminalId') IS NOT NULL
                           THEN 1 ELSE 0 END;";
                var colsExist = (int)(await checkColsCmd.ExecuteScalarAsync())!;
                Assert.Equal(1, colsExist);
            }

            await using var upgradedRestaurantDb = new RestaurantDbContext(
                new DbContextOptionsBuilder<RestaurantDbContext>().UseSqlServer(connectionString).Options);
            var refundRepo = new RefundRepository(upgradedRestaurantDb);

            var preservedRefund = await refundRepo.GetByIdempotencyKeyAsync("IDEMP-PRE-UPGRADE-001");
            Assert.NotNull(preservedRefund);
            Assert.Equal(preUpgradeRefundId, preservedRefund!.Id.Value);
            Assert.Equal(1_044.00m, preservedRefund.GrandTotalRefunded);
            Assert.Null(preservedRefund.ShiftId);
            Assert.Null(preservedRefund.TerminalId);
            Assert.Contains("CBOS-TaxPolicy-NetOfDiscount-v1", preservedRefund.ReceiptSnapshotJson);

            // 4. Verify atomic Refund + durable Outbox restock intent and idempotent retry on the upgraded SQL Server database
            await using var upgradedCatalogDb = new CatalogDbContext(
                new DbContextOptionsBuilder<CatalogDbContext>().UseSqlServer(connectionString).Options);
            await using var upgradedInventoryDb = new InventoryDbContext(
                new DbContextOptionsBuilder<InventoryDbContext>().UseSqlServer(connectionString).Options);

            var sqlSp = BuildWorkflowServiceProvider(upgradedRestaurantDb, upgradedCatalogDb, upgradedInventoryDb);
            var sqlMediator = sqlSp.GetRequiredService<IMediator>();
            var outboxRepo = new OutboxRepository(upgradedRestaurantDb);

            var sqlWarehouseId = Guid.NewGuid();
            var sqlBranchId = Guid.NewGuid();
            var sqlTerminalId = Guid.NewGuid();
            var sqlCashierId = Guid.NewGuid();
            var (sqlBiryaniVarId, _, _, _) = await SeedPakistaniCatalogIntoAsync(upgradedCatalogDb, upgradedInventoryDb, sqlWarehouseId);

            var sqlCashMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
            await upgradedRestaurantDb.PaymentMethods.AddAsync(sqlCashMethod);
            await upgradedRestaurantDb.SaveChangesAsync();

            var sqlShift = await sqlMediator.Send(new OpenShiftCommand(sqlBranchId, sqlWarehouseId, sqlTerminalId, sqlCashierId, "SQL Cashier", 2_000.00m));
            var sqlOrder = await sqlMediator.Send(new CreateOrderCommand(OrderType.TakeAway, sqlWarehouseId));
            var sqlLine = await sqlMediator.Send(new AddOrderLineCommand(sqlOrder.OrderId, sqlBiryaniVarId, 2m));
            var sqlSummary = await sqlMediator.Send(new GetOrderSummaryQuery(sqlOrder.OrderId));
            await sqlMediator.Send(new RecordPaymentCommand(sqlOrder.OrderId, sqlCashMethod.Id.Value, sqlSummary.GrandTotal, false, sqlShift.ShiftId));
            await sqlMediator.Send(new CompleteOrderCommand(sqlOrder.OrderId));

            var stockAfterSale = await upgradedInventoryDb.WarehouseStocks.AsNoTracking()
                .SingleAsync(s => s.ProductVariantId == new ProductVariantId(sqlBiryaniVarId));
            Assert.Equal(98m, stockAfterSale.QuantityOnHand);

            var refundHandlerWithOutbox = new ProcessRefundCommandHandler(
                new RefundRepository(upgradedRestaurantDb),
                new OrderRepository(upgradedRestaurantDb),
                new OrderLineRepository(upgradedRestaurantDb),
                new PaymentRepository(upgradedRestaurantDb),
                new CustomerRepository(upgradedRestaurantDb),
                new CustomerLedgerEntryRepository(upgradedRestaurantDb),
                new ActivityLogEntryRepository(upgradedRestaurantDb),
                sqlMediator,
                new Clovent.Restaurant.Infrastructure.Persistence.UnitOfWork(upgradedRestaurantDb),
                new ShiftRepository(upgradedRestaurantDb),
                outboxRepository: outboxRepo,
                paymentMethodRepository: new PaymentMethodRepository(upgradedRestaurantDb));

            var sqlRefundCmd = new ProcessRefundCommand(
                sqlOrder.OrderId,
                [new RefundItemRequest(sqlLine.OrderLineId, 1m, InventoryDisposition.Restock)],
                "SQL Server atomic outbox restock test",
                RefundSettlementMethod.CashPayout,
                "IDEMP-SQL-UPGRADED-RESTOCK-001",
                sqlCashierId,
                "SQL Cashier",
                BranchId: sqlBranchId,
                ShiftId: sqlShift.ShiftId,
                TerminalId: sqlTerminalId);

            var sqlRefundDto = await refundHandlerWithOutbox.Handle(sqlRefundCmd, CancellationToken.None);
            Assert.Equal(sqlShift.ShiftId, sqlRefundDto.ShiftId);
            Assert.Equal(sqlTerminalId, sqlRefundDto.TerminalId);

            var outboxMsg = await outboxRepo.GetByIdempotencyKeyAsync($"RESTOCK-{sqlRefundDto.RefundId:N}");
            Assert.NotNull(outboxMsg);
            Assert.Equal(Clovent.Restaurant.Outbox.OutboxMessageStatus.Completed, outboxMsg!.Status);

            // Re-delivering the outbox message and retrying ProcessRefundCommand with the same idempotency key must not double-restock
            var outboxHandler = new Clovent.Restaurant.Application.Outbox.Handlers.InventoryPostingOutboxHandler(
                sqlMediator,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<Clovent.Restaurant.Application.Outbox.Handlers.InventoryPostingOutboxHandler>.Instance);
            await outboxHandler.HandleAsync(outboxMsg, CancellationToken.None);
            var sqlRefundRetry = await refundHandlerWithOutbox.Handle(sqlRefundCmd, CancellationToken.None);
            Assert.Equal(sqlRefundDto.RefundId, sqlRefundRetry.RefundId);

            var stockAfterRestockAndRetries = await upgradedInventoryDb.WarehouseStocks.AsNoTracking()
                .SingleAsync(s => s.ProductVariantId == new ProductVariantId(sqlBiryaniVarId));
            Assert.Equal(99m, stockAfterRestockAndRetries.QuantityOnHand);
        }
        finally
        {
            await using var masterConn = new Microsoft.Data.SqlClient.SqlConnection(masterCs);
            await masterConn.OpenAsync();
            await using var dropCmd = masterConn.CreateCommand();
            dropCmd.CommandText = $@"
                IF DB_ID(N'{dbName}') IS NOT NULL
                BEGIN
                    ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{dbName}];
                END";
            await dropCmd.ExecuteNonQueryAsync();
        }
    }

    [Fact]
    public async Task DisposableSqlServer_ConcurrentRefundRestockAndPartialMultiLineRetry_AtomicDeduplicationAcrossSeparateScopesAndConnections()
    {
        var dbName = $"Clovent_DisposableConcurrentRestock_{Guid.NewGuid():N}";
        var masterCs = "Server=.;Database=master;Integrated Security=True;TrustServerCertificate=True;";
        var connectionString = $"Server=.;Database={dbName};Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";

        try
        {
            await using (var masterConn = new Microsoft.Data.SqlClient.SqlConnection(masterCs))
            {
                await masterConn.OpenAsync();
                await using var createCmd = masterConn.CreateCommand();
                createCmd.CommandText = $"CREATE DATABASE [{dbName}];";
                await createCmd.ExecuteNonQueryAsync();
            }

            var provisioningService = new Clovent.Desktop.Commissioning.Database.DatabaseProvisioningService();
            var (migrated, migrateMsg) = await provisioningService.ApplyMigrationsAsync(connectionString);
            Assert.True(migrated, migrateMsg);

            // 1. Verify database-enforced unique filtered index IX_InventoryTransactions_Reference_Deduplication exists on SQL Server
            await using (var verifyConn = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
            {
                await verifyConn.OpenAsync();
                await using var checkIndexCmd = verifyConn.CreateCommand();
                checkIndexCmd.CommandText = @"
                    SELECT COUNT(*) FROM sys.indexes
                    WHERE name = N'IX_InventoryTransactions_Reference_Deduplication'
                      AND object_id = OBJECT_ID(N'[Inventory].[InventoryTransactions]')
                      AND is_unique = 1
                      AND has_filter = 1;";
                var indexExists = (int)(await checkIndexCmd.ExecuteScalarAsync())!;
                Assert.Equal(1, indexExists);
            }

            // 2. Seed catalog, initial stock (100 Biryani, 100 Naan), shift, and a 2-line completed order on primary setup connection
            var warehouseId = Guid.NewGuid();
            var branchId = Guid.NewGuid();
            var terminalId = Guid.NewGuid();
            var cashierId = Guid.NewGuid();
            Guid biryaniVarId;
            Guid naanVarId;
            Guid orderId;
            Guid biryaniLineId;
            Guid naanLineId;
            Guid shiftId;

            await using (var setupRestaurantDb = new RestaurantDbContext(new DbContextOptionsBuilder<RestaurantDbContext>().UseSqlServer(connectionString).Options))
            await using (var setupCatalogDb = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlServer(connectionString).Options))
            await using (var setupInventoryDb = new InventoryDbContext(new DbContextOptionsBuilder<InventoryDbContext>().UseSqlServer(connectionString).Options))
            {
                (biryaniVarId, _, _, naanVarId) = await SeedPakistaniCatalogIntoAsync(setupCatalogDb, setupInventoryDb, warehouseId);

                var cashMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
                await setupRestaurantDb.PaymentMethods.AddAsync(cashMethod);
                await setupRestaurantDb.SaveChangesAsync();

                var setupSp = BuildWorkflowServiceProvider(setupRestaurantDb, setupCatalogDb, setupInventoryDb);
                var setupMediator = setupSp.GetRequiredService<IMediator>();

                var shiftDto = await setupMediator.Send(new OpenShiftCommand(branchId, warehouseId, terminalId, cashierId, "Concurrent SQL Cashier", 5_000.00m));
                shiftId = shiftDto.ShiftId;

                var orderDto = await setupMediator.Send(new CreateOrderCommand(OrderType.TakeAway, warehouseId));
                orderId = orderDto.OrderId;

                var line1Dto = await setupMediator.Send(new AddOrderLineCommand(orderId, biryaniVarId, 2m));
                var line2Dto = await setupMediator.Send(new AddOrderLineCommand(orderId, naanVarId, 5m));
                biryaniLineId = line1Dto.OrderLineId;
                naanLineId = line2Dto.OrderLineId;

                var summary = await setupMediator.Send(new GetOrderSummaryQuery(orderId));
                await setupMediator.Send(new RecordPaymentCommand(orderId, cashMethod.Id.Value, summary.GrandTotal, false, shiftId));
                await setupMediator.Send(new CompleteOrderCommand(orderId));

                // Verify post-sale stock: Biryani = 98 (100 - 2), Naan = 95 (100 - 5)
                var biryaniAfterSale = await setupInventoryDb.WarehouseStocks.AsNoTracking()
                    .SingleAsync(s => s.ProductVariantId == new ProductVariantId(biryaniVarId));
                var naanAfterSale = await setupInventoryDb.WarehouseStocks.AsNoTracking()
                    .SingleAsync(s => s.ProductVariantId == new ProductVariantId(naanVarId));
                Assert.Equal(98m, biryaniAfterSale.QuantityOnHand);
                Assert.Equal(95m, naanAfterSale.QuantityOnHand);

                // Verify fail-closed validation on SQL Server when paymentMethodRepository is omitted
                var handlerMissingPaymentMethodRepo = new ProcessRefundCommandHandler(
                    new RefundRepository(setupRestaurantDb),
                    new OrderRepository(setupRestaurantDb),
                    new OrderLineRepository(setupRestaurantDb),
                    new PaymentRepository(setupRestaurantDb),
                    new CustomerRepository(setupRestaurantDb),
                    new CustomerLedgerEntryRepository(setupRestaurantDb),
                    new ActivityLogEntryRepository(setupRestaurantDb),
                    setupMediator,
                    new Clovent.Restaurant.Infrastructure.Persistence.UnitOfWork(setupRestaurantDb),
                    new ShiftRepository(setupRestaurantDb),
                    paymentMethodRepository: null);

                await Assert.ThrowsAsync<RestaurantDomainException>(() =>
                    handlerMissingPaymentMethodRepo.Handle(new ProcessRefundCommand(
                        orderId,
                        [new RefundItemRequest(biryaniLineId, 1m, InventoryDisposition.Restock)],
                        "Fail-closed payment method check on SQL Server",
                        RefundSettlementMethod.CashPayout,
                        "IDEMP-SQL-FAIL-CLOSED-PM",
                        cashierId,
                        "Concurrent SQL Cashier",
                        BranchId: branchId,
                        ShiftId: shiftId,
                        TerminalId: terminalId), CancellationToken.None));
            }

            // 3. Commit a 2-line Refund + Pending OutboxMessage where Line 1 (1x Biryani) succeeded and Line 2 (2x Naan) failed mid-loop
            var refundCmd = new ProcessRefundCommand(
                orderId,
                [
                    new RefundItemRequest(biryaniLineId, 1m, InventoryDisposition.Restock),
                    new RefundItemRequest(naanLineId, 2m, InventoryDisposition.Restock)
                ],
                "Multi-line refund with partial restock interruption and concurrent retry",
                RefundSettlementMethod.CashPayout,
                "IDEMP-SQL-CONCURRENT-MULTILINE-001",
                cashierId,
                "Concurrent SQL Cashier",
                BranchId: branchId,
                ShiftId: shiftId,
                TerminalId: terminalId);

            Guid refundId;
            string refundNumber;

            await using (var partialRestDb = new RestaurantDbContext(new DbContextOptionsBuilder<RestaurantDbContext>().UseSqlServer(connectionString).Options))
            await using (var partialCatDb = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlServer(connectionString).Options))
            await using (var partialInvDb = new InventoryDbContext(new DbContextOptionsBuilder<InventoryDbContext>().UseSqlServer(connectionString).Options))
            {
                var realInnerSp = BuildWorkflowServiceProvider(partialRestDb, partialCatDb, partialInvDb);
                var realInnerMediator = realInnerSp.GetRequiredService<IMediator>();

                // Mediator wrapper that lets Line 1 (Biryani) OpenOrReceiveStockCommand commit to SQL Server,
                // then throws a simulated transient failure on Line 2 (Naan) OpenOrReceiveStockCommand
                var partialFailureMediator = new DelegatingInterceptionMediator(realInnerMediator, req =>
                {
                    if (req is OpenOrReceiveStockCommand cmd && cmd.ProductVariantId == naanVarId)
                    {
                        throw new InvalidOperationException("Simulated mid-batch failure after Line 1 restock committed");
                    }
                });

                var partialRefundHandler = new ProcessRefundCommandHandler(
                    new RefundRepository(partialRestDb),
                    new OrderRepository(partialRestDb),
                    new OrderLineRepository(partialRestDb),
                    new PaymentRepository(partialRestDb),
                    new CustomerRepository(partialRestDb),
                    new CustomerLedgerEntryRepository(partialRestDb),
                    new ActivityLogEntryRepository(partialRestDb),
                    partialFailureMediator,
                    new Clovent.Restaurant.Infrastructure.Persistence.UnitOfWork(partialRestDb),
                    new ShiftRepository(partialRestDb),
                    outboxRepository: new OutboxRepository(partialRestDb),
                    paymentMethodRepository: new PaymentMethodRepository(partialRestDb));

                var initialRefundDto = await partialRefundHandler.Handle(refundCmd, CancellationToken.None);
                refundId = initialRefundDto.RefundId;
                refundNumber = initialRefundDto.RefundNumber;

                // Verify partial multi-line state in SQL Server:
                // - Biryani was restocked (+1 -> 99) and its InventoryTransaction is committed
                // - Naan was NOT restocked yet (still 95)
                // - OutboxMessage is still Pending in [Restaurant].[OutboxMessages]
                var biryaniAfterPartial = await partialInvDb.WarehouseStocks.AsNoTracking()
                    .SingleAsync(s => s.ProductVariantId == new ProductVariantId(biryaniVarId));
                var naanAfterPartial = await partialInvDb.WarehouseStocks.AsNoTracking()
                    .SingleAsync(s => s.ProductVariantId == new ProductVariantId(naanVarId));
                Assert.Equal(99m, biryaniAfterPartial.QuantityOnHand);
                Assert.Equal(95m, naanAfterPartial.QuantityOnHand);

                var pendingOutbox = await new OutboxRepository(partialRestDb).GetByIdempotencyKeyAsync($"RESTOCK-{refundId:N}");
                Assert.NotNull(pendingOutbox);
                Assert.Equal(Clovent.Restaurant.Outbox.OutboxMessageStatus.Pending, pendingOutbox!.Status);
            }

            // 4. Launch 6 simultaneous attempts across 6 completely separate DI scopes & separate SQL Server connections:
            //    - 2 concurrent inline ProcessRefundCommand retries (EnsureRestockAppliedIdempotentlyAsync)
            //    - 2 concurrent background InventoryPostingOutboxHandler.HandleAsync executions
            //    - 2 concurrent direct OpenOrReceiveStockCommand dispatches (bypassing any pre-query check!)
            var startGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            async Task RunConcurrentScopeAttemptAsync(int workerIndex)
            {
                await using var scopeRestDb = new RestaurantDbContext(new DbContextOptionsBuilder<RestaurantDbContext>().UseSqlServer(connectionString).Options);
                await using var scopeCatDb = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlServer(connectionString).Options);
                await using var scopeInvDb = new InventoryDbContext(new DbContextOptionsBuilder<InventoryDbContext>().UseSqlServer(connectionString).Options);
                var scopeSp = BuildWorkflowServiceProvider(scopeRestDb, scopeCatDb, scopeInvDb);
                var scopeMediator = scopeSp.GetRequiredService<IMediator>();

                await startGate.Task;

                if (workerIndex is 0 or 1)
                {
                    // Inline post-commit retry path on its own scope/connection
                    var handler = new ProcessRefundCommandHandler(
                        new RefundRepository(scopeRestDb),
                        new OrderRepository(scopeRestDb),
                        new OrderLineRepository(scopeRestDb),
                        new PaymentRepository(scopeRestDb),
                        new CustomerRepository(scopeRestDb),
                        new CustomerLedgerEntryRepository(scopeRestDb),
                        new ActivityLogEntryRepository(scopeRestDb),
                        scopeMediator,
                        new Clovent.Restaurant.Infrastructure.Persistence.UnitOfWork(scopeRestDb),
                        new ShiftRepository(scopeRestDb),
                        outboxRepository: new OutboxRepository(scopeRestDb),
                        paymentMethodRepository: new PaymentMethodRepository(scopeRestDb));

                    await handler.Handle(refundCmd, CancellationToken.None);
                }
                else if (workerIndex is 2 or 3)
                {
                    // Background outbox handler path on its own scope/connection
                    var outboxMsg = await new OutboxRepository(scopeRestDb).GetByIdempotencyKeyAsync($"RESTOCK-{refundId:N}");
                    var outboxHandler = new Clovent.Restaurant.Application.Outbox.Handlers.InventoryPostingOutboxHandler(
                        scopeMediator,
                        Microsoft.Extensions.Logging.Abstractions.NullLogger<Clovent.Restaurant.Application.Outbox.Handlers.InventoryPostingOutboxHandler>.Instance);
                    await outboxHandler.HandleAsync(outboxMsg!, CancellationToken.None);
                }
                else
                {
                    // Direct OpenOrReceiveStockCommand dispatch (no pre-check) on its own scope/connection
                    await scopeMediator.Send(new OpenOrReceiveStockCommand(
                        warehouseId,
                        biryaniVarId,
                        1m,
                        $"Direct concurrent restock {refundNumber}",
                        "Refund",
                        refundId));
                    await scopeMediator.Send(new OpenOrReceiveStockCommand(
                        warehouseId,
                        naanVarId,
                        2m,
                        $"Direct concurrent restock {refundNumber}",
                        "Refund",
                        refundId));
                }
            }

            var tasks = Enumerable.Range(0, 6).Select(i => Task.Run(() => RunConcurrentScopeAttemptAsync(i))).ToArray();
            startGate.SetResult();
            await Task.WhenAll(tasks);

            // 5. Verify final stock and ledger rows on a fresh verification connection:
            //    - Biryani: 98 + 1 = 99 (strictly 1 restock movement)
            //    - Naan: 95 + 2 = 97 (strictly 1 restock movement)
            //    - Total Refund InventoryTransactions for refundId = strictly 2 rows (1 per variant)
            await using (var assertInvDb = new InventoryDbContext(new DbContextOptionsBuilder<InventoryDbContext>().UseSqlServer(connectionString).Options))
            {
                var finalBiryani = await assertInvDb.WarehouseStocks.AsNoTracking()
                    .SingleAsync(s => s.ProductVariantId == new ProductVariantId(biryaniVarId));
                var finalNaan = await assertInvDb.WarehouseStocks.AsNoTracking()
                    .SingleAsync(s => s.ProductVariantId == new ProductVariantId(naanVarId));

                Assert.Equal(99m, finalBiryani.QuantityOnHand);
                Assert.Equal(97m, finalNaan.QuantityOnHand);

                var refundMovements = await assertInvDb.InventoryTransactions.AsNoTracking()
                    .Where(t => t.ReferenceType == "Refund" && t.ReferenceId == refundId)
                    .ToListAsync();

                Assert.Equal(2, refundMovements.Count);
                Assert.Single(refundMovements, t => t.ProductVariantId == new ProductVariantId(biryaniVarId) && t.Quantity == 1m);
                Assert.Single(refundMovements, t => t.ProductVariantId == new ProductVariantId(naanVarId) && t.Quantity == 2m);
            }

            // 6. Prove database-enforced unique index rejects a raw duplicate INSERT with SQL Error 2601
            await using (var dupConn = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
            {
                await dupConn.OpenAsync();
                await using var dupCmd = dupConn.CreateCommand();
                dupCmd.CommandText = @"
                    INSERT INTO [Inventory].[InventoryTransactions] (
                        [Id], [WarehouseId], [ProductVariantId], [TransactionType],
                        [Quantity], [ReferenceType], [ReferenceId], [Notes], [OccurredAtUtc]
                    ) VALUES (
                        NEWID(), @warehouseId, @variantId, N'Receipt',
                        1.0000, N'Refund', @refundId, N'Raw duplicate attempt', 638000000000000000
                    );";
                dupCmd.Parameters.AddWithValue("@warehouseId", warehouseId);
                dupCmd.Parameters.AddWithValue("@variantId", biryaniVarId);
                dupCmd.Parameters.AddWithValue("@refundId", refundId);

                var sqlEx = await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => dupCmd.ExecuteNonQueryAsync());
                Assert.Equal(2601, sqlEx.Number);
                Assert.Contains("IX_InventoryTransactions_Reference_Deduplication", sqlEx.Message, StringComparison.OrdinalIgnoreCase);
            }

            // 7. Prove UnitOfWork.ExecuteStockMovementAtomicallyAsync atomically rolls back WarehouseStock update
            //    if SaveChangesAsync hits IX_InventoryTransactions_Reference_Deduplication (SQL Error 2601)
            await using (var rollbackInvDb = new InventoryDbContext(new DbContextOptionsBuilder<InventoryDbContext>().UseSqlServer(connectionString).Options))
            {
                var invUow = new Clovent.Inventory.Infrastructure.Persistence.UnitOfWork(rollbackInvDb);
                var stockRepo = new WarehouseStockRepository(rollbackInvDb);
                var txRepo = new InventoryTransactionRepository(rollbackInvDb);

                var recoveredDto = await invUow.ExecuteStockMovementAtomicallyAsync(
                    warehouseId,
                    biryaniVarId,
                    async ct =>
                    {
                        var stock = await stockRepo.GetByWarehouseAndVariantAsync(new WarehouseId(warehouseId), new ProductVariantId(biryaniVarId), ct);
                        stock!.Receive(50m); // Mutates QuantityOnHand from 99 -> 149 inside the transaction
                        await txRepo.AddAsync(InventoryTransaction.Create(
                            new WarehouseId(warehouseId),
                            new ProductVariantId(biryaniVarId),
                            InventoryTransactionType.Receipt,
                            50m,
                            referenceType: "Refund",
                            referenceId: refundId,
                            notes: "Forced duplicate transaction to test atomic stock rollback"), ct);
                        return Clovent.Inventory.Application.WarehouseStocks.Dtos.WarehouseStockDto.FromDomain(stock);
                    },
                    async ct =>
                    {
                        var committed = await stockRepo.GetByWarehouseAndVariantAsync(new WarehouseId(warehouseId), new ProductVariantId(biryaniVarId), ct);
                        return Clovent.Inventory.Application.WarehouseStocks.Dtos.WarehouseStockDto.FromDomain(committed!);
                    });

                // The +50m stock mutation was rolled back when the duplicate InventoryTransaction insert hit SQL Error 2601
                Assert.Equal(99m, recoveredDto.QuantityOnHand);

                var verifiedStock = await rollbackInvDb.WarehouseStocks.AsNoTracking()
                    .SingleAsync(s => s.ProductVariantId == new ProductVariantId(biryaniVarId));
                Assert.Equal(99m, verifiedStock.QuantityOnHand);
            }
        }
        finally
        {
            await using var masterConn = new Microsoft.Data.SqlClient.SqlConnection(masterCs);
            await masterConn.OpenAsync();
            await using var dropCmd = masterConn.CreateCommand();
            dropCmd.CommandText = $@"
                IF DB_ID(N'{dbName}') IS NOT NULL
                BEGIN
                    ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{dbName}];
                END";
            await dropCmd.ExecuteNonQueryAsync();
        }
    }

    private sealed class DelegatingInterceptionMediator(IMediator inner, Action<object> beforeSend) : IMediator
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            beforeSend(request);
            return inner.Send(request, cancellationToken);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            beforeSend(request);
            return inner.Send(request, cancellationToken);
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            beforeSend(request);
            return inner.Send(request, cancellationToken);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            inner.CreateStream(request, cancellationToken);

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            inner.CreateStream(request, cancellationToken);

        public Task Publish(object notification, CancellationToken cancellationToken = default) =>
            inner.Publish(notification, cancellationToken);

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification =>
            inner.Publish(notification, cancellationToken);
    }
}
