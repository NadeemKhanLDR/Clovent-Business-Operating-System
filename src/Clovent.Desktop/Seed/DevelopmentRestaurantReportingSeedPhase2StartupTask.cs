using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Catalog.Application.Products.Queries;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Desktop.Theming;
using Clovent.Identity.Organizations;
using Clovent.Identity.Users;
using Clovent.Inventory.Infrastructure.Persistence;
using Clovent.Inventory.Transactions;
using Clovent.Inventory.WarehouseStocks;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Warehouses;
using Clovent.Platform.Bootstrap;
using Clovent.Restaurant.Application.Customers.Commands;
using Clovent.Restaurant.Application.Shifts.Commands;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.KitchenTickets;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Orders.ValueObjects;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Shifts;
using Clovent.Restaurant.SmartRecommendations;
using Clovent.Restaurant.Tables;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Clovent.Desktop.Seed;

/// <summary>
/// Development-only Phase 2 seed: provisions reporting transactions covering all remaining
/// operational reporting scenarios using pure domain entities and application workflows.
/// <para>
/// 100% legitimate workflows:
/// <list type="bullet">
///   <item>Service item: Food Heating (100% margin, zero stock impact)</item>
///   <item>Resale item: Naan received via Goods Receipt and issued via POS orders</item>
///   <item>Prepared item: Chicken Biryani and Chicken Karahi</item>
///   <item>Dine-In, Take Away, Delivery (with rider details and delivery fee)</item>
///   <item>Tenders: Cash, Credit Card, Mobile Wallet, On Account, Customer Advance, Split</item>
///   <item>Customer overpayment creating advance credit, advance applied to subsequent order</item>
///   <item>Customer partial payments and bulk collection batch across 3 accounts</item>
///   <item>Held order, Running order, and Kitchen Ticket</item>
///   <item>Upsell recommendation events (Offered, Accepted, Dismissed)</item>
///   <item>Shift balancing: Starting cash, Cash In, Cash Out, Cash Sales, Collections, Expected = Counted, 0 variance</item>
/// </list>
/// </para>
/// Fully idempotent: skips execution once verified clean dataset is in place.
/// </summary>
public sealed class DevelopmentRestaurantReportingSeedPhase2StartupTask(
    IOrganizationRepository organizationRepository,
    IWarehouseRepository warehouseRepository,
    IDiningAreaRepository diningAreaRepository,
    ITableRepository tableRepository,
    IPaymentMethodRepository paymentMethodRepository,
    ICustomerRepository customerRepository,
    ICustomerLedgerEntryRepository ledgerRepository,
    ICustomerPaymentAllocationRepository allocationRepository,
    IOrderRepository orderRepository,
    IOrderLineRepository orderLineRepository,
    IPaymentRepository paymentRepository,
    IShiftRepository shiftRepository,
    IKitchenTicketRepository kitchenTicketRepository,
    ISuggestionEventRepository suggestionEventRepository,
    RestaurantDbContext restaurantDbContext,
    IWarehouseStockRepository warehouseStockRepository,
    IInventoryTransactionRepository transactionRepository,
    InventoryDbContext inventoryDbContext,
    IMediator mediator,
    IOptions<DesktopOptions> options) : IStartupTask
{
    private const string CheckOrderNumber = "ORD-RPT-S2-001";
    private const string CleanSeedTag = "[V2-CLEAN-SEED]";

    /// <inheritdoc/>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!options.Value.SeedDevelopmentRestaurantData)
            return;

        // ── 1. Resolve Warehouse ──────────────────────────────────────────────
        var warehouses = await warehouseRepository.GetAllAsync(cancellationToken);
        if (warehouses.Count == 0) return;
        var warehouse = warehouses.First();
        var warehouseId = warehouse.Id;

        // ── 2. Resolve Catalog Variants ───────────────────────────────────────
        var products = await mediator.Send(new ListProductsQuery(), cancellationToken);
        var variants = await mediator.Send(new ListProductVariantsQuery(), cancellationToken);

        var biryaniProduct = products.FirstOrDefault(p =>
            string.Equals(p.Name, "Chicken Biryani", StringComparison.OrdinalIgnoreCase) ||
            p.Sku.Contains("BIRYANI", StringComparison.OrdinalIgnoreCase));
        var naanProduct = products.FirstOrDefault(p =>
            string.Equals(p.Name, "Naan", StringComparison.OrdinalIgnoreCase) ||
            p.Sku.Contains("NAAN", StringComparison.OrdinalIgnoreCase));
        var heatingProduct = products.FirstOrDefault(p =>
            string.Equals(p.Name, "Food Heating", StringComparison.OrdinalIgnoreCase) ||
            p.Sku.Contains("HEAT", StringComparison.OrdinalIgnoreCase));
        var karahiProduct = products.FirstOrDefault(p =>
            string.Equals(p.Name, "Chicken Karahi", StringComparison.OrdinalIgnoreCase) ||
            p.Sku.Contains("KARAHI", StringComparison.OrdinalIgnoreCase));

        var biryaniVariant = variants.FirstOrDefault(v =>
            (biryaniProduct != null && v.ProductId == biryaniProduct.ProductId) ||
            v.Sku.Contains("BIRYANI", StringComparison.OrdinalIgnoreCase) ||
            v.Name.Contains("Biryani", StringComparison.OrdinalIgnoreCase));
        var naanVariant = variants.FirstOrDefault(v =>
            (naanProduct != null && v.ProductId == naanProduct.ProductId) ||
            v.Sku.Contains("NAAN", StringComparison.OrdinalIgnoreCase) ||
            v.Name.Contains("Naan", StringComparison.OrdinalIgnoreCase));
        var heatingVariant = variants.FirstOrDefault(v =>
            (heatingProduct != null && v.ProductId == heatingProduct.ProductId) ||
            v.Sku.Contains("HEAT", StringComparison.OrdinalIgnoreCase) ||
            v.Name.Contains("Heating", StringComparison.OrdinalIgnoreCase));
        var karahiVariant = variants.FirstOrDefault(v =>
            (karahiProduct != null && v.ProductId == karahiProduct.ProductId) ||
            v.Sku.Contains("KARAHI", StringComparison.OrdinalIgnoreCase) ||
            v.Name.Contains("Karahi", StringComparison.OrdinalIgnoreCase));

        if (biryaniVariant is null || naanVariant is null || heatingVariant is null)
            return;

        var vBiryaniId = new Clovent.Catalog.Variants.ProductVariantId(biryaniVariant.ProductVariantId);
        var vNaanId = new Clovent.Catalog.Variants.ProductVariantId(naanVariant.ProductVariantId);
        var vHeatingId = new Clovent.Catalog.Variants.ProductVariantId(heatingVariant.ProductVariantId);
        Clovent.Catalog.Variants.ProductVariantId? vKarahiId = karahiVariant != null
            ? new Clovent.Catalog.Variants.ProductVariantId(karahiVariant.ProductVariantId)
            : null;

        // ── 3. Resolve Tables ─────────────────────────────────────────────────
        var tables = (await tableRepository.GetAllAsync(cancellationToken)).ToList();
        Table? t01 = tables.FirstOrDefault(t => t.Code.Value == "T-01") ?? tables.FirstOrDefault();
        Table? t02 = tables.FirstOrDefault(t => t.Code.Value == "T-02") ?? t01;
        Table? t03 = tables.FirstOrDefault(t => t.Code.Value == "T-03") ?? t01;

        if (t01 is null)
        {
            var diningAreas = await diningAreaRepository.GetAllAsync(cancellationToken);
            if (diningAreas.Count == 0) return;
            var area = diningAreas.First();
            t01 = Table.Create(area.Id, EntityCode.Create("T-01"), 2);
            t02 = Table.Create(area.Id, EntityCode.Create("T-02"), 4);
            t03 = Table.Create(area.Id, EntityCode.Create("T-03"), 6);
            await tableRepository.AddAsync(t01!, cancellationToken);
            await tableRepository.AddAsync(t02!, cancellationToken);
            await tableRepository.AddAsync(t03!, cancellationToken);
            await restaurantDbContext.SaveChangesAsync(cancellationToken);
        }

        // ── 4. Idempotency & Clean Dataset Check ───────────────────────────────
        var existingOrders = await orderRepository.GetAllAsync(cancellationToken);
        var s2001 = existingOrders.FirstOrDefault(o => o.OrderNumber.Value == CheckOrderNumber);
        var s2005 = existingOrders.FirstOrDefault(o => o.OrderNumber.Value == "ORD-RPT-S2-005");
        var srvOrder = existingOrders.FirstOrDefault(o => o.OrderNumber.Value == "ORD-RPT-S2-SRV");

        bool isCleanV2Present = s2001 != null &&
                                s2005 != null &&
                                srvOrder != null &&
                                await restaurantDbContext.CustomerLedgerEntries.AnyAsync(
                                    l => l.Description != null && l.Description.Contains(CleanSeedTag), cancellationToken);

        if (isCleanV2Present)
        {
            // Already cleanly seeded through domain workflows. Ensure open shift and active held/running orders then return.
            await EnsureOpenShiftForTestingAsync(cancellationToken);
            await EnsureRunningAndHeldOrdersAsync(warehouseId, vBiryaniId, vNaanId, t01, t03, cancellationToken);
            return;
        }

        // Clean up any existing Phase 2 entities using domain DbContexts/repositories
        if (existingOrders.Any(o => o.OrderNumber.Value.StartsWith("ORD-RPT-S2-")))
        {
            await CleanPhase2DataAsync(warehouseId, vNaanId, cancellationToken);
        }

        // ── 5. Resolve Payment Methods ────────────────────────────────────────
        var allMethods = (await paymentMethodRepository.GetAllAsync(cancellationToken)).ToList();

        PaymentMethod GetOrCreateMethod(string name)
        {
            var match = allMethods.FirstOrDefault(m =>
                string.Equals(m.Name.Value, name, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;
            var newMethod = PaymentMethod.Create(PaymentMethodName.Create(name));
            allMethods.Add(newMethod);
            paymentMethodRepository.AddAsync(newMethod, cancellationToken).GetAwaiter().GetResult();
            return newMethod;
        }

        var cashMethod = GetOrCreateMethod("Cash");
        var cardMethod = GetOrCreateMethod("Credit Card");
        var onAccountMethod = GetOrCreateMethod("On Account");
        var advanceMethod = GetOrCreateMethod("Customer Advance");
        var mobileWallet = GetOrCreateMethod("Mobile Wallet");
        await restaurantDbContext.SaveChangesAsync(cancellationToken);

        // ── 6. Resolve / Create Scenario Customers ─────────────────────────────
        var existingCusts = (await customerRepository.GetAllAsync(cancellationToken)).ToList();

        Customer GetOrCreateCustomer(
            string code, string name, string mobile, string address,
            decimal creditLimit, bool isCreditAllowed = true, bool isDefault = false)
        {
            var found = existingCusts.FirstOrDefault(c =>
                c.Code.Value == code ||
                string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
            if (found != null) return found;
            var created = Customer.Create(
                EntityCode.Create(code), name, mobile, address,
                email: null, openingBalance: 0m, creditLimit: creditLimit,
                notes: "Reporting Coverage Run - 29-Sep-2026",
                shopNo: null, mobile2: null, phone: mobile,
                isDefault: isDefault, isCreditAllowed: isCreditAllowed);
            customerRepository.AddAsync(created, cancellationToken).GetAwaiter().GetResult();
            existingCusts.Add(created);
            return created;
        }

        var custB = GetOrCreateCustomer("CUST-RPT-B", "Malik Faisal Enterprises", "0300-5551111", "Jinnah Super Market, G-8, Islamabad", 25_000m);
        var custC = GetOrCreateCustomer("CUST-RPT-C", "Hassan Brothers Trading", "0321-5552222", "Blue Area, Islamabad", 15_000m);
        var custD = GetOrCreateCustomer("CUST-RPT-D", "Sana Textile & General", "0311-5553333", "F-8 Markaz, Islamabad", 20_000m);
        var custE = GetOrCreateCustomer("CUST-RPT-E", "Rizwan Home Delivery", "0333-5554444", "House 7, Street 9, F-10/3, Islamabad", 5_000m);
        var walkInCustomer = existingCusts.FirstOrDefault(c => c.IsDefault || c.Code.Value == "C000") ??
                             GetOrCreateCustomer("C000", "Walk-in Guest", "0000-0000000", "Counter", 0m, isCreditAllowed: false, isDefault: true);
        var corpCustomer = existingCusts.FirstOrDefault(c => c.Code.Value == "CUST-CORP-01" || string.Equals(c.Name, "Corporate Lunch Account", StringComparison.OrdinalIgnoreCase));

        // Ensure clean customer balances before Phase 2 seeding
        custB.AdjustBalance(-custB.OutstandingBalance);
        custC.AdjustBalance(-custC.OutstandingBalance);
        custD.AdjustBalance(-custD.OutstandingBalance);
        custE.AdjustBalance(-custE.OutstandingBalance);
        if (corpCustomer != null)
            corpCustomer.AdjustBalance(130m - corpCustomer.OutstandingBalance);

        await customerRepository.UpdateAsync(custB, cancellationToken);
        await customerRepository.UpdateAsync(custC, cancellationToken);
        await customerRepository.UpdateAsync(custD, cancellationToken);
        await customerRepository.UpdateAsync(custE, cancellationToken);
        if (corpCustomer != null)
            await customerRepository.UpdateAsync(corpCustomer, cancellationToken);
        await restaurantDbContext.SaveChangesAsync(cancellationToken);

        // ── 7. Resolve Shift #1003 (The Reporting Test Shift) ──────────────────
        var allShifts = await shiftRepository.SearchShiftsAsync(cancellationToken: cancellationToken);
        var shift1003 = allShifts.FirstOrDefault(s => s.ShiftNumber == 1003);
        ShiftId? targetShiftId = shift1003?.Id;

        if (shift1003 != null && shift1003.Status == ShiftStatus.Open)
        {
            var cashierId = shift1003.CashierId;
            if (!shift1003.CashMovements.Any(m => m.Reason == "Petty cash replenishment" && m.Amount == 1000m))
            {
                shift1003.AddCashMovement(CashMovementType.CashIn, 1000m, "Petty cash replenishment", cashierId, "Reporting Coverage Run - 29-Sep-2026");
            }
            if (!shift1003.CashMovements.Any(m => m.Reason == "Kitchen supplies purchase" && m.Amount == 500m))
            {
                shift1003.AddCashMovement(CashMovementType.CashOut, 500m, "Kitchen supplies purchase", cashierId, "Reporting Coverage Run - 29-Sep-2026");
            }
            await shiftRepository.UpdateAsync(shift1003, cancellationToken);
            await restaurantDbContext.SaveChangesAsync(cancellationToken);
        }

        // ── 8. Inventory Setup: GRN-2026-09-001 Goods Receipt ────────────────
        var naanStock = await warehouseStockRepository.GetByWarehouseAndVariantAsync(warehouseId, vNaanId, cancellationToken);
        if (naanStock is null)
        {
            naanStock = WarehouseStock.Create(warehouseId, vNaanId, minimumStock: 10, maximumStock: 500);
            naanStock.Receive(70m); // Baseline from Phase 1 (100 received - 30 issued)
            await warehouseStockRepository.AddAsync(naanStock, cancellationToken);
            await inventoryDbContext.SaveChangesAsync(cancellationToken);
        }

        // Execute legitimate Goods Receipt: GRN-2026-09-001 (100 Naan @ Rs.20 cost)
        naanStock.Receive(100m);
        var grnTx = InventoryTransaction.Create(
            warehouseId,
            vNaanId,
            InventoryTransactionType.Receipt,
            100m,
            referenceType: "GoodsReceipt",
            referenceId: null,
            notes: "GRN-2026-09-001: Direct Vendor Delivery (100 Naan @ Rs.20 cost)",
            occurredAtUtc: DateTimeOffset.UtcNow);
        await transactionRepository.AddAsync(grnTx, cancellationToken);
        await inventoryDbContext.SaveChangesAsync(cancellationToken);

        // ── 9. Order Factory Helper ───────────────────────────────────────────
        var now = DateTimeOffset.UtcNow;

        async Task<Order> CreateAndCompleteOrderAsync(
            string orderNum,
            OrderType type,
            Table? tbl,
            OrderSource src,
            Customer? cust,
            (Clovent.Catalog.Variants.ProductVariantId vId, decimal qty, decimal price)[] lines,
            (PaymentMethod pm, decimal amt)[] tenders,
            decimal deliveryFee = 0m,
            string? delivCust = null,
            string? delivPhone = null,
            string? delivAddr = null,
            string? rider = null)
        {
            var order = Order.Create(type, warehouseId, tbl?.Id, OrderNumber.Create(orderNum), src);
            if (cust != null && !cust.IsDefault)
                order.SetCustomer(cust.Id);

            if (type == OrderType.Delivery)
            {
                order.SetDeliveryDetails(src, delivCust, delivPhone, delivAddr,
                    "Reporting Coverage Run - 29-Sep-2026", deliveryFee, rider);
                order.UpdateDeliveryStatus(DeliveryStatus.Delivered);
            }

            await orderRepository.AddAsync(order, cancellationToken);

            foreach (var (vId, qty, price) in lines)
            {
                var line = OrderLine.Create(order.Id, vId, qty, price, 0m, false);
                await orderLineRepository.AddAsync(line, cancellationToken);
                order.AddOrderLine(line.Id);
            }

            foreach (var (pm, amt) in tenders)
            {
                var pay = Payment.Create(order.Id, pm.Id, amt, targetShiftId);
                await paymentRepository.AddAsync(pay, cancellationToken);
                order.RecordPayment(pay.Id);
            }

            order.Complete();

            // Issue physical stock through aggregate Issue() and record InventoryTransaction
            foreach (var (vId, qty, _) in lines)
            {
                if (vId == vNaanId && naanStock != null)
                {
                    naanStock.Issue(qty);
                    var issueTx = InventoryTransaction.Create(
                        warehouseId, vId,
                        InventoryTransactionType.Issue, qty,
                        order.OrderNumber.Value, order.Id.Value,
                        $"Sale for {order.OrderNumber.Value} (Phase 2 seed)",
                        now);
                    await transactionRepository.AddAsync(issueTx, cancellationToken);
                }
            }

            return order;
        }

        // ══════════════════════════════════════════════════════════════════════
        // S2-001: Dine-In, Mobile Wallet
        // Biryani 450 + 2x Naan 50 = 500, Mobile Wallet
        // ══════════════════════════════════════════════════════════════════════
        var ordS2001 = await CreateAndCompleteOrderAsync(
            "ORD-RPT-S2-001", OrderType.DineIn, t01, OrderSource.WalkIn, walkInCustomer,
            [(vBiryaniId, 1m, 450m), (vNaanId, 2m, 25m)],
            [(mobileWallet, 500m)]);

        // ══════════════════════════════════════════════════════════════════════
        // S2-002: TakeAway, Credit Card (today)
        // Biryani 450 + 4x Naan 100 = 550, Credit Card
        // ══════════════════════════════════════════════════════════════════════
        await CreateAndCompleteOrderAsync(
            "ORD-RPT-S2-002", OrderType.TakeAway, null, OrderSource.WalkIn, walkInCustomer,
            [(vBiryaniId, 1m, 450m), (vNaanId, 4m, 25m)],
            [(cardMethod, 550m)]);

        // ══════════════════════════════════════════════════════════════════════
        // S2-003: Split Tender — Cash + Mobile Wallet
        // Biryani 450 + 2x Naan 50 = 500 (250 Cash + 250 Mobile Wallet)
        // ══════════════════════════════════════════════════════════════════════
        await CreateAndCompleteOrderAsync(
            "ORD-RPT-S2-003", OrderType.DineIn, t02, OrderSource.WalkIn, walkInCustomer,
            [(vBiryaniId, 1m, 450m), (vNaanId, 2m, 25m)],
            [(cashMethod, 250m), (mobileWallet, 250m)]);

        // ══════════════════════════════════════════════════════════════════════
        // S2-004: Customer B — On Account Dine-In
        // Biryani x2 = 900 + 4x Naan = 100 → 1,000 On Account
        // ══════════════════════════════════════════════════════════════════════
        var ordS2004 = await CreateAndCompleteOrderAsync(
            "ORD-RPT-S2-004", OrderType.DineIn, t03, OrderSource.WalkIn, custB,
            [(vBiryaniId, 2m, 450m), (vNaanId, 4m, 25m)],
            [(onAccountMethod, 1000m)]);

        custB.AdjustBalance(1000m);
        var ledgerS2004 = CustomerLedgerEntry.Create(
            custB.Id, "ORD-RPT-S2-004",
            $"On Account Sale (ORD-RPT-S2-004) - Reporting Coverage Run - 29-Sep-2026 {CleanSeedTag}",
            1000m, 0m, 1000m, targetShiftId, "On Account");
        await ledgerRepository.AddAsync(ledgerS2004, cancellationToken);

        // ══════════════════════════════════════════════════════════════════════
        // S2-005: Customer C — On Account then Partial Payments
        // Sale: 4x Biryani = 1,800 + 8x Naan = 200, total 2,000 On Account
        // Payment 1: Rs.800 Cash (A/R → 1,200)
        // Payment 2: Rs.500 Cash (A/R → 700, still outstanding)
        // ══════════════════════════════════════════════════════════════════════
        var ordS2005 = await CreateAndCompleteOrderAsync(
            "ORD-RPT-S2-005", OrderType.DineIn, t01, OrderSource.WalkIn, custC,
            [(vBiryaniId, 4m, 450m), (vNaanId, 8m, 25m)],
            [(onAccountMethod, 2000m)]);

        custC.AdjustBalance(2000m);
        var ledgerS2005 = CustomerLedgerEntry.Create(
            custC.Id, "ORD-RPT-S2-005",
            $"On Account Sale (ORD-RPT-S2-005) - Reporting Coverage Run - 29-Sep-2026 {CleanSeedTag}",
            2000m, 0m, 2000m, targetShiftId, "On Account");
        await ledgerRepository.AddAsync(ledgerS2005, cancellationToken);
        await customerRepository.UpdateAsync(custC, cancellationToken);
        await restaurantDbContext.SaveChangesAsync(cancellationToken);

        // Partial payment 1 via Application Command: Rs.800 Cash
        await mediator.Send(new RecordCustomerPaymentCommand(
            custC.Id.Value,
            800m,
            "Cash",
            "PAY-RPT-S2-C01",
            $"Reporting Coverage Run - 29-Sep-2026 {CleanSeedTag}",
            targetShiftId?.Value), cancellationToken);

        // Partial payment 2 via Application Command: Rs.500 Cash
        await mediator.Send(new RecordCustomerPaymentCommand(
            custC.Id.Value,
            500m,
            "Cash",
            "PAY-RPT-S2-C02",
            $"Reporting Coverage Run - 29-Sep-2026 {CleanSeedTag}",
            targetShiftId?.Value), cancellationToken);

        // ══════════════════════════════════════════════════════════════════════
        // S2-006: Customer D — Overpayment → Advance → New Order using Advance
        // Sale: Biryani 450 + 1 Naan 25 = 475, On Account
        // Collection: Rs.700 Cash (475 applied + 225 advance)
        // New order: Biryani 450 (225 advance + 225 cash)
        // ══════════════════════════════════════════════════════════════════════
        var ordS2006 = await CreateAndCompleteOrderAsync(
            "ORD-RPT-S2-006", OrderType.TakeAway, null, OrderSource.WalkIn, custD,
            [(vBiryaniId, 1m, 450m), (vNaanId, 1m, 25m)],
            [(onAccountMethod, 475m)]);

        custD.AdjustBalance(475m);
        var ledgerS2006 = CustomerLedgerEntry.Create(
            custD.Id, "ORD-RPT-S2-006",
            $"On Account Sale (ORD-RPT-S2-006) - Reporting Coverage Run - 29-Sep-2026 {CleanSeedTag}",
            475m, 0m, 475m, targetShiftId, "On Account");
        await ledgerRepository.AddAsync(ledgerS2006, cancellationToken);
        await customerRepository.UpdateAsync(custD, cancellationToken);
        await restaurantDbContext.SaveChangesAsync(cancellationToken);

        // Customer D overpays Rs.700 Cash via Application Command (475 applied, 225 advance created)
        await mediator.Send(new RecordCustomerPaymentCommand(
            custD.Id.Value,
            700m,
            "Cash",
            "PAY-RPT-S2-D01",
            $"Reporting Coverage Run - 29-Sep-2026 {CleanSeedTag}",
            targetShiftId?.Value), cancellationToken);

        // New order using Customer D's advance: Biryani 450 (225 advance + 225 cash)
        var ordS2006B = await CreateAndCompleteOrderAsync(
            "ORD-RPT-S2-006B", OrderType.DineIn, t02, OrderSource.WalkIn, custD,
            [(vBiryaniId, 1m, 450m)],
            [(advanceMethod, 225m), (cashMethod, 225m)]);

        // Consume the advance
        custD.AdjustBalance(225m);
        var ledgerD_Adv = CustomerLedgerEntry.Create(
            custD.Id, "ORD-RPT-S2-006B",
            $"Customer Advance Settlement (ORD-RPT-S2-006B) - Reporting Coverage Run - 29-Sep-2026 {CleanSeedTag}",
            225m, 0m, custD.OutstandingBalance, targetShiftId, "Customer Advance");
        await ledgerRepository.AddAsync(ledgerD_Adv, cancellationToken);
        await allocationRepository.AddAsync(
            CustomerPaymentAllocation.Create(custD.Id, ordS2006B.Id, 225m, ledgerD_Adv.Id,
                "Customer advance applied to ORD-RPT-S2-006B"),
            cancellationToken);

        // ══════════════════════════════════════════════════════════════════════
        // S2-007: Customer E — Registered Customer Delivery, Mobile Wallet
        // Biryani 450 + 2x Naan 50 + Delivery Fee 150 = 650
        // ══════════════════════════════════════════════════════════════════════
        await CreateAndCompleteOrderAsync(
            "ORD-RPT-S2-007", OrderType.Delivery, null, OrderSource.Phone, custE,
            [(vBiryaniId, 1m, 450m), (vNaanId, 2m, 25m)],
            [(mobileWallet, 650m)],
            deliveryFee: 150m,
            delivCust: custE.Name,
            delivPhone: custE.MobileNumber,
            delivAddr: custE.Address,
            rider: "Kamran Rider");

        // ══════════════════════════════════════════════════════════════════════
        // S2-008: Bulk Collection Batch RCV-BATCH-S2-001 via Application Command
        // 3 customers receive collections simultaneously
        // ══════════════════════════════════════════════════════════════════════
        var bulkItems = new List<BulkCustomerPaymentItem>();
        if (corpCustomer != null)
        {
            bulkItems.Add(new(corpCustomer.Id.Value, 300m, "Cash", Notes: $"Bulk collection batch {CleanSeedTag}"));
        }
        bulkItems.Add(new(custB.Id.Value, 500m, "Cash", Notes: $"Bulk collection batch {CleanSeedTag}"));
        bulkItems.Add(new(custC.Id.Value, 200m, "Mobile Wallet", Notes: $"Bulk collection batch {CleanSeedTag}"));

        await mediator.Send(new RecordBulkCustomerPaymentsCommand(
            bulkItems,
            ShiftId: targetShiftId?.Value,
            BatchReference: "RCV-BATCH-S2-001"), cancellationToken);

        // ══════════════════════════════════════════════════════════════════════
        // S2-SRV: Real Service Order — Food Heating x2 @ Rs. 30 = Rs. 60 Cash
        // ══════════════════════════════════════════════════════════════════════
        await CreateAndCompleteOrderAsync(
            "ORD-RPT-S2-SRV", OrderType.TakeAway, null, OrderSource.WalkIn, walkInCustomer,
            [(vHeatingId, 2m, 30m)],
            [(cashMethod, 60m)]);

        // ══════════════════════════════════════════════════════════════════════
        // S2-009: HELD ORDER — Chicken Biryani 450, remains Held
        // ══════════════════════════════════════════════════════════════════════
        var heldOrder = Order.Create(
            OrderType.DineIn, warehouseId, t03?.Id,
            OrderNumber.Create("ORD-RPT-S2-HELD"), OrderSource.WalkIn);
        await orderRepository.AddAsync(heldOrder, cancellationToken);
        var heldLine = OrderLine.Create(heldOrder.Id, vBiryaniId, 1m, 450m, 0m, false);
        await orderLineRepository.AddAsync(heldLine, cancellationToken);
        heldOrder.AddOrderLine(heldLine.Id);
        heldOrder.Hold();

        // ══════════════════════════════════════════════════════════════════════
        // S2-010: RUNNING ORDER — Biryani x2 + 4x Naan = 1,000, remains Open
        // ══════════════════════════════════════════════════════════════════════
        var runningOrder = Order.Create(
            OrderType.DineIn, warehouseId, t01?.Id,
            OrderNumber.Create("ORD-RPT-S2-RUN"), OrderSource.WalkIn);
        await orderRepository.AddAsync(runningOrder, cancellationToken);
        var runLine1 = OrderLine.Create(runningOrder.Id, vBiryaniId, 2m, 450m, 0m, false);
        var runLine2 = OrderLine.Create(runningOrder.Id, vNaanId, 4m, 25m, 0m, false);
        await orderLineRepository.AddAsync(runLine1, cancellationToken);
        await orderLineRepository.AddAsync(runLine2, cancellationToken);
        runningOrder.AddOrderLine(runLine1.Id);
        runningOrder.AddOrderLine(runLine2.Id);

        // Kitchen Ticket for Running Order
        var kitchenTicket = KitchenTicket.Create(runningOrder.Id, [runLine1.Id, runLine2.Id]);
        await kitchenTicketRepository.AddAsync(kitchenTicket, cancellationToken);

        // ══════════════════════════════════════════════════════════════════════
        // S2-011: UPSELL SUGGESTION EVENTS
        // ══════════════════════════════════════════════════════════════════════
        var lines001 = await orderLineRepository.GetByOrderIdAsync(ordS2001.Id, cancellationToken);
        var naanLine001 = lines001.FirstOrDefault(l => l.ProductVariantId == vNaanId);

        var offeredNaan = SuggestionEvent.Offered(
            ordS2001.Id.Value,
            vNaanId,
            biryaniVariant.ProductVariantId);
        await suggestionEventRepository.AddAsync(offeredNaan, cancellationToken);

        var acceptedNaan = SuggestionEvent.Accepted(
            ordS2001.Id.Value,
            vNaanId,
            biryaniVariant.ProductVariantId,
            naanLine001?.Id.Value,
            2m,
            25m);
        await suggestionEventRepository.AddAsync(acceptedNaan, cancellationToken);

        if (vKarahiId.HasValue)
        {
            var offeredKarahi = SuggestionEvent.Offered(
                ordS2001.Id.Value,
                vKarahiId.Value,
                biryaniVariant.ProductVariantId);
            await suggestionEventRepository.AddAsync(offeredKarahi, cancellationToken);

            var dismissedKarahi = SuggestionEvent.Dismissed(
                ordS2001.Id.Value,
                vKarahiId.Value,
                biryaniVariant.ProductVariantId);
            await suggestionEventRepository.AddAsync(dismissedKarahi, cancellationToken);
        }

        await customerRepository.UpdateAsync(custB, cancellationToken);
        await customerRepository.UpdateAsync(custC, cancellationToken);
        await customerRepository.UpdateAsync(custD, cancellationToken);

        await restaurantDbContext.SaveChangesAsync(cancellationToken);
        await inventoryDbContext.SaveChangesAsync(cancellationToken);

        // ── 10. Close Shift #1003 (with 0 variance) & Open Shift #1004 ────────
        await EnsureShiftBalanceAndOpenShiftAsync(cashMethod, cancellationToken);
    }

    private async Task CleanPhase2DataAsync(WarehouseId warehouseId, Clovent.Catalog.Variants.ProductVariantId vNaanId, CancellationToken cancellationToken)
    {
        var s2Orders = await orderRepository.GetAllAsync(cancellationToken);
        var phase2Orders = s2Orders
            .Where(o => o.OrderNumber.Value.StartsWith("ORD-RPT-S2-"))
            .ToList();
        var phase2OrderIds = phase2Orders.Select(o => o.Id).ToList();
        var orderIdGuids = phase2OrderIds.Select(id => id.Value).ToHashSet();

        // 1. Kitchen tickets
        var tickets = restaurantDbContext.KitchenTickets
            .Where(k => phase2OrderIds.Contains(k.OrderId))
            .ToList();
        if (tickets.Count > 0)
        {
            restaurantDbContext.KitchenTickets.RemoveRange(tickets);
        }

        // 2. Suggestion events
        var suggestions = restaurantDbContext.SuggestionEvents
            .Where(s => orderIdGuids.Contains(s.OrderId))
            .ToList();
        if (suggestions.Count > 0)
        {
            restaurantDbContext.SuggestionEvents.RemoveRange(suggestions);
        }

        // 3. Allocations
        var allocations = restaurantDbContext.CustomerPaymentAllocations
            .Where(a => phase2OrderIds.Contains(a.OrderId) ||
                        (a.Notes != null && (a.Notes.Contains("ORD-RPT-S2") || a.Notes.Contains("RCV-BATCH-S2") || a.Notes.Contains("PAY-RPT-S2"))))
            .ToList();
        if (allocations.Count > 0)
        {
            restaurantDbContext.CustomerPaymentAllocations.RemoveRange(allocations);
        }

        // 4. Ledgers
        var ledgers = restaurantDbContext.CustomerLedgerEntries
            .Where(e => e.Reference.StartsWith("ORD-RPT-S2-") ||
                        e.Reference.StartsWith("PAY-RPT-S2-") ||
                        e.Reference.StartsWith("RCV-BATCH-S2-"))
            .ToList();
        if (ledgers.Count > 0)
        {
            restaurantDbContext.CustomerLedgerEntries.RemoveRange(ledgers);
        }

        // 5. Payments
        var payments = restaurantDbContext.Payments
            .Where(p => phase2OrderIds.Contains(p.OrderId))
            .ToList();
        if (payments.Count > 0)
        {
            restaurantDbContext.Payments.RemoveRange(payments);
        }

        // 6. Order lines
        var lines = restaurantDbContext.OrderLines
            .Where(l => phase2OrderIds.Contains(l.OrderId))
            .ToList();
        if (lines.Count > 0)
        {
            restaurantDbContext.OrderLines.RemoveRange(lines);
        }

        // 7. Orders
        if (phase2Orders.Count > 0)
        {
            restaurantDbContext.Orders.RemoveRange(phase2Orders);
        }

        await restaurantDbContext.SaveChangesAsync(cancellationToken);

        // 8. Remove Phase 2 inventory transactions
        var s2Txs = inventoryDbContext.InventoryTransactions
            .Where(t => (t.ReferenceType != null && t.ReferenceType.StartsWith("ORD-RPT-S2-")) ||
                        (t.Notes != null && (t.Notes.Contains("Phase 2 seed") || t.Notes.Contains("GRN-2026-09-001"))))
            .ToList();
        if (s2Txs.Count > 0)
        {
            inventoryDbContext.InventoryTransactions.RemoveRange(s2Txs);
        }

        // 9. Reset Naan warehouse stock to exactly 70m (Phase 1 baseline: 100 receipt - 30 issues)
        var naanStock = await warehouseStockRepository.GetByWarehouseAndVariantAsync(warehouseId, vNaanId, cancellationToken);
        if (naanStock != null)
        {
            const decimal targetBaseline = 70m;
            if (naanStock.QuantityOnHand > targetBaseline)
            {
                naanStock.Issue(naanStock.QuantityOnHand - targetBaseline);
            }
            else if (naanStock.QuantityOnHand < targetBaseline)
            {
                naanStock.Receive(targetBaseline - naanStock.QuantityOnHand);
            }
        }

        await inventoryDbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureShiftBalanceAndOpenShiftAsync(PaymentMethod cashMethod, CancellationToken cancellationToken)
    {
        var allShifts = await shiftRepository.SearchShiftsAsync(cancellationToken: cancellationToken);
        var shift1003 = allShifts.FirstOrDefault(s => s.ShiftNumber == 1003);

        if (shift1003 != null && shift1003.Status == ShiftStatus.Open)
        {
            var payments = await paymentRepository.GetByShiftIdAsync(shift1003.Id, cancellationToken);
            decimal cashSales = payments
                .Where(p => !p.IsVoided && p.PaymentMethodId == cashMethod.Id)
                .Sum(p => p.Amount);

            var ledgerEntries = await ledgerRepository.GetByShiftIdAsync(shift1003.Id, cancellationToken);
            decimal cashCollections = ledgerEntries
                .Where(e => e.Credit > 0 && string.Equals(e.PaymentMethod, "Cash", StringComparison.OrdinalIgnoreCase))
                .Sum(e => e.Credit);

            decimal cashIn = shift1003.CashMovements.Where(m => m.Type == CashMovementType.CashIn).Sum(m => m.Amount);
            decimal cashOut = shift1003.CashMovements.Where(m => m.Type == CashMovementType.CashOut).Sum(m => m.Amount);

            decimal expectedCash = shift1003.StartingCash + cashIn + cashSales + cashCollections - cashOut;

            // Close Shift 1003 with counted cash = expected cash (0 variance)
            await mediator.Send(new CloseShiftCommand(shift1003.Id.Value, expectedCash, null, "Reporting reconciliation close"), cancellationToken);
        }

        await EnsureOpenShiftForTestingAsync(cancellationToken);
    }

    private async Task EnsureRunningAndHeldOrdersAsync(
        WarehouseId warehouseId,
        Clovent.Catalog.Variants.ProductVariantId vBiryaniId,
        Clovent.Catalog.Variants.ProductVariantId vNaanId,
        Table? t01,
        Table? t03,
        CancellationToken cancellationToken)
    {
        var allOrders = await orderRepository.GetAllAsync(cancellationToken);
        var runOrder = allOrders.FirstOrDefault(o => o.OrderNumber.Value == "ORD-RPT-S2-RUN");
        if (runOrder == null || runOrder.Status != OrderStatus.Open)
        {
            if (runOrder != null)
            {
                var oldTickets = restaurantDbContext.KitchenTickets.Where(k => k.OrderId == runOrder.Id).ToList();
                if (oldTickets.Count > 0) restaurantDbContext.KitchenTickets.RemoveRange(oldTickets);
                var oldLines = restaurantDbContext.OrderLines.Where(l => l.OrderId == runOrder.Id).ToList();
                if (oldLines.Count > 0) restaurantDbContext.OrderLines.RemoveRange(oldLines);
                restaurantDbContext.Orders.Remove(runOrder);
                await restaurantDbContext.SaveChangesAsync(cancellationToken);
            }

            var newRunOrder = Order.Create(
                OrderType.DineIn, warehouseId, t01?.Id,
                OrderNumber.Create("ORD-RPT-S2-RUN"), OrderSource.WalkIn);
            await orderRepository.AddAsync(newRunOrder, cancellationToken);
            var runLine1 = OrderLine.Create(newRunOrder.Id, vBiryaniId, 2m, 450m, 0m, false);
            var runLine2 = OrderLine.Create(newRunOrder.Id, vNaanId, 4m, 25m, 0m, false);
            await orderLineRepository.AddAsync(runLine1, cancellationToken);
            await orderLineRepository.AddAsync(runLine2, cancellationToken);
            newRunOrder.AddOrderLine(runLine1.Id);
            newRunOrder.AddOrderLine(runLine2.Id);

            var ticket = KitchenTicket.Create(newRunOrder.Id, [runLine1.Id, runLine2.Id]);
            await kitchenTicketRepository.AddAsync(ticket, cancellationToken);
            await restaurantDbContext.SaveChangesAsync(cancellationToken);
        }

        var heldOrder = allOrders.FirstOrDefault(o => o.OrderNumber.Value == "ORD-RPT-S2-HELD");
        if (heldOrder == null || heldOrder.Status != OrderStatus.Held)
        {
            if (heldOrder != null)
            {
                var oldLines = restaurantDbContext.OrderLines.Where(l => l.OrderId == heldOrder.Id).ToList();
                if (oldLines.Count > 0) restaurantDbContext.OrderLines.RemoveRange(oldLines);
                restaurantDbContext.Orders.Remove(heldOrder);
                await restaurantDbContext.SaveChangesAsync(cancellationToken);
            }

            var newHeldOrder = Order.Create(
                OrderType.DineIn, warehouseId, t03?.Id,
                OrderNumber.Create("ORD-RPT-S2-HELD"), OrderSource.WalkIn);
            await orderRepository.AddAsync(newHeldOrder, cancellationToken);
            var heldLine = OrderLine.Create(newHeldOrder.Id, vBiryaniId, 1m, 450m, 0m, false);
            await orderLineRepository.AddAsync(heldLine, cancellationToken);
            newHeldOrder.AddOrderLine(heldLine.Id);
            newHeldOrder.Hold();
            await restaurantDbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsureOpenShiftForTestingAsync(CancellationToken cancellationToken)
    {
        var openShifts = await shiftRepository.SearchShiftsAsync(status: ShiftStatus.Open, cancellationToken: cancellationToken);
        if (!openShifts.Any())
        {
            var allShifts = await shiftRepository.SearchShiftsAsync(cancellationToken: cancellationToken);
            var lastShift = allShifts.OrderByDescending(s => s.ShiftNumber).FirstOrDefault();
            if (lastShift != null)
            {
                await mediator.Send(new OpenShiftCommand(
                    lastShift.BranchId.Value,
                    lastShift.WarehouseId.Value,
                    lastShift.TerminalId.Value,
                    lastShift.CashierId.Value,
                    lastShift.CashierName,
                    4000m,
                    "Active register shift for manual testing"), cancellationToken);
            }
        }
    }
}
