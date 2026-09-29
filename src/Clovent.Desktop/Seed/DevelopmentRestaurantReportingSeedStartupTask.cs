using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Catalog.Application.Products.Queries;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Theming;
using Clovent.Identity.Organizations;
using Clovent.Inventory.Infrastructure.Persistence;
using Clovent.Inventory.Transactions;
using Clovent.Inventory.WarehouseStocks;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Warehouses;
using Clovent.Platform.Bootstrap;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Orders.ValueObjects;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Tables;
using MediatR;
using Microsoft.Extensions.Options;

namespace Clovent.Desktop.Seed;

/// <summary>
/// Development-only startup task that provisions a realistic, fully-reconciled
/// restaurant reporting scenario inside the CURRENT BUSINESS MONTH.
/// Covers Dine-In, Take Away, Delivery, Resale, Service, On Account, Partial Collection,
/// Overpayment/Advance, Advance Application, Pure Advance, Split Payment, and Voided order.
/// 100% idempotent and non-destructive.
/// </summary>
public sealed class DevelopmentRestaurantReportingSeedStartupTask(
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
    RestaurantDbContext restaurantDbContext,
    IWarehouseStockRepository warehouseStockRepository,
    IInventoryTransactionRepository transactionRepository,
    InventoryDbContext inventoryDbContext,
    IMediator mediator,
    IOptions<DesktopOptions> options) : IStartupTask
{
    private const string CheckOrderNumber = "ORD-RPT-001";

    /// <inheritdoc/>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!options.Value.SeedDevelopmentRestaurantData)
        {
            return;
        }

        // Idempotency check: if reporting dataset already exists, do not recreate or duplicate
        var existingOrders = await orderRepository.GetAllAsync(cancellationToken);
        if (existingOrders.Any(o => o.OrderNumber.Value == CheckOrderNumber))
        {
            return;
        }

        // 1. Resolve Warehouse
        var warehouses = await warehouseRepository.GetAllAsync(cancellationToken);
        if (warehouses.Count == 0) return;
        var warehouse = warehouses.First();
        var warehouseId = warehouse.Id;

        // 2. Resolve or create Tables
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
            await tableRepository.AddAsync(t01, cancellationToken);
            await tableRepository.AddAsync(t02, cancellationToken);
            await tableRepository.AddAsync(t03, cancellationToken);
            await restaurantDbContext.SaveChangesAsync(cancellationToken);
        }

        // 3. Resolve or create Payment Methods
        var allMethods = (await paymentMethodRepository.GetAllAsync(cancellationToken)).ToList();
        PaymentMethod GetOrCreateMethod(string name)
        {
            var match = allMethods.FirstOrDefault(m => string.Equals(m.Name.Value, name, StringComparison.OrdinalIgnoreCase));
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
        await restaurantDbContext.SaveChangesAsync(cancellationToken);

        // 4. Resolve Catalog Variants (Chicken Biryani, Naan, Food Heating)
        var products = await mediator.Send(new ListProductsQuery(), cancellationToken);
        var biryaniProduct = products.FirstOrDefault(p => string.Equals(p.Name, "Chicken Biryani", StringComparison.OrdinalIgnoreCase) || p.Sku.Contains("BIRYANI", StringComparison.OrdinalIgnoreCase));
        var naanProduct = products.FirstOrDefault(p => string.Equals(p.Name, "Naan", StringComparison.OrdinalIgnoreCase) || p.Sku.Contains("NAAN", StringComparison.OrdinalIgnoreCase));
        var heatingProduct = products.FirstOrDefault(p => string.Equals(p.Name, "Food Heating", StringComparison.OrdinalIgnoreCase) || p.Sku.Contains("HEAT", StringComparison.OrdinalIgnoreCase));

        var variants = await mediator.Send(new ListProductVariantsQuery(), cancellationToken);
        var biryaniVariant = variants.FirstOrDefault(v => (biryaniProduct != null && v.ProductId == biryaniProduct.ProductId) || v.Sku.Contains("BIRYANI", StringComparison.OrdinalIgnoreCase) || v.Name.Contains("Biryani", StringComparison.OrdinalIgnoreCase));
        var naanVariant = variants.FirstOrDefault(v => (naanProduct != null && v.ProductId == naanProduct.ProductId) || v.Sku.Contains("NAAN", StringComparison.OrdinalIgnoreCase) || v.Name.Contains("Naan", StringComparison.OrdinalIgnoreCase));
        var heatingVariant = variants.FirstOrDefault(v => (heatingProduct != null && v.ProductId == heatingProduct.ProductId) || v.Sku.Contains("HEAT", StringComparison.OrdinalIgnoreCase) || v.Name.Contains("Heating", StringComparison.OrdinalIgnoreCase));

        if (biryaniVariant is null || naanVariant is null || heatingVariant is null)
        {
            // Catalog seed should have run, but if not yet available, fallback gracefully
            return;
        }

        var vBiryaniId = new Clovent.Catalog.Variants.ProductVariantId(biryaniVariant.ProductVariantId);
        var vNaanId = new Clovent.Catalog.Variants.ProductVariantId(naanVariant.ProductVariantId);
        var vHeatingId = new Clovent.Catalog.Variants.ProductVariantId(heatingVariant.ProductVariantId);

        // Ensure warehouse stock exists for Naan (Purchased / Resale)
        var naanStock = await warehouseStockRepository.GetByWarehouseAndVariantAsync(warehouseId, vNaanId, cancellationToken);
        if (naanStock is null)
        {
            naanStock = WarehouseStock.Create(warehouseId, vNaanId, minimumStock: 10, maximumStock: 500);
            naanStock.Receive(100);
            var initialReceiptTx = InventoryTransaction.Create(
                warehouseId,
                vNaanId,
                InventoryTransactionType.Receipt,
                100,
                "Seed",
                null,
                "Initial Naan stock for restaurant operations",
                DateTimeOffset.UtcNow.AddMinutes(-360));
            await warehouseStockRepository.AddAsync(naanStock, cancellationToken);
            await transactionRepository.AddAsync(initialReceiptTx, cancellationToken);
            await inventoryDbContext.SaveChangesAsync(cancellationToken);
        }

        // 5. Resolve or create Realistic Credit Customers
        var existingCusts = (await customerRepository.GetAllAsync(cancellationToken)).ToList();

        Customer GetOrCreateCustomer(string code, string name, string mobile, string address, decimal limit, bool isDefault = false)
        {
            var found = existingCusts.FirstOrDefault(c => c.Code.Value == code || string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
            if (found != null) return found;
            var created = Customer.Create(
                EntityCode.Create(code),
                name,
                mobile,
                address,
                email: null,
                openingBalance: 0m,
                creditLimit: limit,
                notes: "Development Reporting Seed Account",
                shopNo: null,
                mobile2: null,
                phone: mobile,
                isDefault: isDefault,
                isCreditAllowed: !isDefault);
            customerRepository.AddAsync(created, cancellationToken).GetAwaiter().GetResult();
            existingCusts.Add(created);
            return created;
        }

        var walkInCustomer = GetOrCreateCustomer("C000", "Walk-in Guest", "0000-0000000", "Counter", 0m, isDefault: true);
        var corpCustomer = GetOrCreateCustomer("CUST-CORP-01", "Corporate Lunch Account", "0300-1234567", "Executive Tower, 5th Floor", 10000m);
        var officeCustomer = GetOrCreateCustomer("CUST-OFFICE-01", "Office Lunch Account", "0321-7654321", "Blue Area, 2nd Floor", 10000m);
        await restaurantDbContext.SaveChangesAsync(cancellationToken);

        // Timestamps in current business month
        var nowOffset = DateTimeOffset.UtcNow;
        var t1 = nowOffset.AddMinutes(-300);
        var t2 = nowOffset.AddMinutes(-260);
        var t3 = nowOffset.AddMinutes(-220);
        var t4 = nowOffset.AddMinutes(-180);
        var t5 = nowOffset.AddMinutes(-140);
        var t6 = nowOffset.AddMinutes(-100);
        var t7 = nowOffset.AddMinutes(-80);
        var t8 = nowOffset.AddMinutes(-60);
        var t9 = nowOffset.AddMinutes(-40);
        var t10 = nowOffset.AddMinutes(-25);
        var t11 = nowOffset.AddMinutes(-10);

        // Helper to complete an order
        async Task<Order> CreateAndCompleteOrderAsync(
            string orderNum,
            OrderType type,
            Table? tbl,
            OrderSource src,
            Customer? cust,
            (Clovent.Catalog.Variants.ProductVariantId vId, decimal qty, decimal price)[] lines,
            (PaymentMethod pm, decimal amt)[] tenders,
            DateTimeOffset stamp,
            decimal deliveryFee = 0m,
            string? delivCust = null,
            string? delivPhone = null,
            string? delivAddr = null,
            string? rider = null,
            bool voidOrder = false)
        {
            var order = Order.Create(type, warehouseId, tbl?.Id, OrderNumber.Create(orderNum), src);
            if (cust != null && !cust.IsDefault)
            {
                order.SetCustomer(cust.Id);
            }

            if (type == OrderType.Delivery)
            {
                order.SetDeliveryDetails(src, delivCust, delivPhone, delivAddr, "Seed delivery instructions", deliveryFee, rider);
                order.UpdateDeliveryStatus(DeliveryStatus.Delivered);
            }

            await orderRepository.AddAsync(order, cancellationToken);

            foreach (var (vId, qty, price) in lines)
            {
                var line = OrderLine.Create(order.Id, vId, qty, price, 0m, false);
                await orderLineRepository.AddAsync(line, cancellationToken);
                order.AddOrderLine(line.Id);
            }

            if (!voidOrder)
            {
                foreach (var (pm, amt) in tenders)
                {
                    var pay = Payment.Create(order.Id, pm.Id, amt, null);
                    await paymentRepository.AddAsync(pay, cancellationToken);
                    order.RecordPayment(pay.Id);
                }
                order.Complete();

                foreach (var (vId, qty, _) in lines)
                {
                    if (vId == vNaanId && naanStock != null)
                    {
                        naanStock.Issue(qty);
                        var issueTx = InventoryTransaction.Create(
                            warehouseId,
                            vId,
                            InventoryTransactionType.Issue,
                            qty,
                            order.OrderNumber.Value,
                            order.Id.Value,
                            $"Restaurant sale for order {order.OrderNumber.Value}",
                            stamp);
                        await transactionRepository.AddAsync(issueTx, cancellationToken);
                    }
                }
            }
            else
            {
                order.Void("Customer cancelled order before preparation");
            }

            return order;
        }

        // ==========================================
        // A. DINE-IN CASH SALE: Chicken Biryani (450)
        // ==========================================
        await CreateAndCompleteOrderAsync(
            "ORD-RPT-001", OrderType.DineIn, t01, OrderSource.WalkIn, walkInCustomer,
            [(vBiryaniId, 1m, 450m)],
            [(cashMethod, 450m)],
            t1);

        // ==========================================
        // B. TAKE AWAY CARD SALE: Biryani (450) + 2x Naan (50) = 500
        // ==========================================
        await CreateAndCompleteOrderAsync(
            "ORD-RPT-002", OrderType.TakeAway, null, OrderSource.WalkIn, walkInCustomer,
            [(vBiryaniId, 1m, 450m), (vNaanId, 2m, 25m)],
            [(cardMethod, 500m)],
            t2);

        // ==========================================
        // C. DELIVERY ORDER (Cash/COD): Biryani (450) + 2x Naan (50) + Fee (150) = 650
        // ==========================================
        await CreateAndCompleteOrderAsync(
            "ORD-RPT-003", OrderType.Delivery, null, OrderSource.Phone, walkInCustomer,
            [(vBiryaniId, 1m, 450m), (vNaanId, 2m, 25m)],
            [(cashMethod, 650m)],
            t3,
            deliveryFee: 150m,
            delivCust: "Kamran Akmal",
            delivPhone: "0301-9876543",
            delivAddr: "House 12, Street 4, Sector G-9",
            rider: "Ali Rider");

        // ==========================================
        // D. SERVICE SALE: 3x Food Heating (90)
        // ==========================================
        await CreateAndCompleteOrderAsync(
            "ORD-RPT-004", OrderType.TakeAway, null, OrderSource.WalkIn, walkInCustomer,
            [(vHeatingId, 3m, 30m)],
            [(cashMethod, 90m)],
            t4);

        // ==========================================
        // E. PURCHASED/RESALE SALE: 4x Naan (100)
        // ==========================================
        await CreateAndCompleteOrderAsync(
            "ORD-RPT-005", OrderType.TakeAway, null, OrderSource.WalkIn, walkInCustomer,
            [(vNaanId, 4m, 25m)],
            [(cashMethod, 100m)],
            t5);

        // ==========================================
        // F. MIXED ORDER: Biryani (450) + 2x Naan (50) + Heating (30) = 530
        // ==========================================
        await CreateAndCompleteOrderAsync(
            "ORD-RPT-006", OrderType.DineIn, t02, OrderSource.WalkIn, walkInCustomer,
            [(vBiryaniId, 1m, 450m), (vNaanId, 2m, 25m), (vHeatingId, 1m, 30m)],
            [(cashMethod, 530m)],
            t6);

        // ==========================================
        // G. ON ACCOUNT SALE: Corporate Lunch Account (1,000)
        // ==========================================
        var ord007 = await CreateAndCompleteOrderAsync(
            "ORD-RPT-007", OrderType.DineIn, t03, OrderSource.WalkIn, corpCustomer,
            [(vBiryaniId, 2m, 450m), (vNaanId, 4m, 25m)],
            [(onAccountMethod, 1000m)],
            t7);

        // Customer Ledger: On Account debit 1,000
        corpCustomer.AdjustBalance(1000m);
        var entryOrd007 = CustomerLedgerEntry.Create(
            corpCustomer.Id,
            "ORD-RPT-007",
            "On Account Sale (ORD-RPT-007)",
            1000m,
            0m,
            1000m,
            null,
            "On Account");
        await ledgerRepository.AddAsync(entryOrd007, cancellationToken);

        // ==========================================
        // H. PARTIAL CUSTOMER PAYMENT: Pays 400 Cash towards 1,000 debt
        // ==========================================
        corpCustomer.AdjustBalance(-400m);
        var entryPay001 = CustomerLedgerEntry.Create(
            corpCustomer.Id,
            "PAY-RPT-001",
            "Customer Payment (Cash)",
            0m,
            400m,
            600m,
            null,
            "Cash");
        await ledgerRepository.AddAsync(entryPay001, cancellationToken);

        var alloc001 = CustomerPaymentAllocation.Create(
            corpCustomer.Id,
            ord007.Id,
            400m,
            entryPay001.Id,
            "Partial payment applied to ORD-RPT-007");
        await allocationRepository.AddAsync(alloc001, cancellationToken);

        // ==========================================
        // I. OVERPAYMENT / ADVANCE: Pays 700 Cash (600 A/R applied + 100 new advance)
        // ==========================================
        corpCustomer.AdjustBalance(-700m); // Balance is now -100 (Advance 100)
        var entryPay002 = CustomerLedgerEntry.Create(
            corpCustomer.Id,
            "PAY-RPT-002",
            "Customer Payment (Cash) [600.00 applied, 100.00 advance]",
            0m,
            700m,
            -100m,
            null,
            "Cash");
        await ledgerRepository.AddAsync(entryPay002, cancellationToken);

        var alloc002 = CustomerPaymentAllocation.Create(
            corpCustomer.Id,
            ord007.Id,
            600m,
            entryPay002.Id,
            "Settlement payment applied to ORD-RPT-007");
        await allocationRepository.AddAsync(alloc002, cancellationToken);

        // ==========================================
        // J. ADVANCE APPLICATION: Corporate Lunch buys 300 (12x Naan). 100 advance applied + 200 cash!
        // ==========================================
        var ord008 = await CreateAndCompleteOrderAsync(
            "ORD-RPT-008", OrderType.DineIn, t01, OrderSource.WalkIn, corpCustomer,
            [(vNaanId, 12m, 25m)],
            [(advanceMethod, 100m), (cashMethod, 200m)],
            t8);

        corpCustomer.AdjustBalance(100m); // Consumes the 100 advance, net balance returns to 0
        var entryAdvSettlement = CustomerLedgerEntry.Create(
            corpCustomer.Id,
            "ORD-RPT-008",
            "Customer Advance Settlement (ORD-RPT-008)",
            100m,
            0m,
            0m,
            null,
            "Customer Advance");
        await ledgerRepository.AddAsync(entryAdvSettlement, cancellationToken);

        var alloc003 = CustomerPaymentAllocation.Create(
            corpCustomer.Id,
            ord008.Id,
            100m,
            entryAdvSettlement.Id,
            "Customer advance applied to ORD-RPT-008");
        await allocationRepository.AddAsync(alloc003, cancellationToken);

        // ==========================================
        // K. PURE ADVANCE: Office Lunch Account with 0 debt pays 500 advance
        // ==========================================
        officeCustomer.AdjustBalance(-500m);
        var entryPureAdv = CustomerLedgerEntry.Create(
            officeCustomer.Id,
            "PAY-RPT-ADV01",
            "Customer Advance Payment (Cash)",
            0m,
            500m,
            -500m,
            null,
            "Cash");
        await ledgerRepository.AddAsync(entryPureAdv, cancellationToken);

        // ==========================================
        // L. SPLIT PAYMENT: Biryani (450) + 2x Naan (50) = 500 (300 Cash + 200 Card)
        // ==========================================
        await CreateAndCompleteOrderAsync(
            "ORD-RPT-009", OrderType.DineIn, t02, OrderSource.WalkIn, walkInCustomer,
            [(vBiryaniId, 1m, 450m), (vNaanId, 2m, 25m)],
            [(cashMethod, 300m), (cardMethod, 200m)],
            t9);

        // ==========================================
        // M. DELIVERY CASH/COD: Biryani (450) + Fee (100) = 550
        // ==========================================
        await CreateAndCompleteOrderAsync(
            "ORD-RPT-010", OrderType.Delivery, null, OrderSource.Online, walkInCustomer,
            [(vBiryaniId, 1m, 450m)],
            [(cashMethod, 550m)],
            t10,
            deliveryFee: 100m,
            delivCust: "Tariq Mahmood",
            delivPhone: "0322-5551234",
            delivAddr: "Office 402, Blue Area",
            rider: "Zubair Rider");

        // ==========================================
        // N. ON ACCOUNT DELIVERY: Biryani (450) + 2x Naan (50) + Fee (150) = 650
        // ==========================================
        var ord011 = await CreateAndCompleteOrderAsync(
            "ORD-RPT-011", OrderType.Delivery, null, OrderSource.Phone, corpCustomer,
            [(vBiryaniId, 1m, 450m), (vNaanId, 2m, 25m)],
            [(onAccountMethod, 650m)],
            t11,
            deliveryFee: 150m,
            delivCust: "Corporate Office Reception",
            delivPhone: "0300-1234567",
            delivAddr: "Executive Tower, 5th Floor",
            rider: "Ali Rider");

        corpCustomer.AdjustBalance(650m);
        var entryOrd011 = CustomerLedgerEntry.Create(
            corpCustomer.Id,
            "ORD-RPT-011",
            "On Account Sale (ORD-RPT-011)",
            650m,
            0m,
            650m,
            null,
            "On Account");
        await ledgerRepository.AddAsync(entryOrd011, cancellationToken);

        // ==========================================
        // O. VOID/CANCEL: Biryani (450) Voided
        // ==========================================
        await CreateAndCompleteOrderAsync(
            "ORD-RPT-012", OrderType.DineIn, t03, OrderSource.WalkIn, walkInCustomer,
            [(vBiryaniId, 1m, 450m)],
            [],
            t11,
            voidOrder: true);

        // Save all customer balance updates & domain records
        await customerRepository.UpdateAsync(corpCustomer, cancellationToken);
        await customerRepository.UpdateAsync(officeCustomer, cancellationToken);
        await restaurantDbContext.SaveChangesAsync(cancellationToken);
        await inventoryDbContext.SaveChangesAsync(cancellationToken);
    }
}
