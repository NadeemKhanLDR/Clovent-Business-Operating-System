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

        // Catalog Repositories
        services.AddScoped<IProductRepository>(_ => new ProductRepository(catalogDb));
        services.AddScoped<IProductVariantRepository>(_ => new ProductVariantRepository(catalogDb));
        services.AddScoped<IProductPriceRepository>(_ => new ProductPriceRepository(catalogDb));

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
}
