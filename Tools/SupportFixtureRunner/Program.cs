using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Clovent.Authentication.Infrastructure.Persistence;
using Clovent.Catalog.Categories;
using Clovent.Catalog.Categories.ValueObjects;
using Clovent.Catalog.Infrastructure.Persistence;
using Clovent.Catalog.Products;
using Clovent.Catalog.Products.ValueObjects;
using Clovent.Catalog.Shared.ValueObjects;
using Clovent.Catalog.TaxProfiles;
using Clovent.Catalog.UnitsOfMeasure;
using Clovent.Catalog.Variants;
using Clovent.Catalog.Variants.ValueObjects;
using Clovent.Desktop.Commissioning.Database;
using Clovent.Desktop.Configuration;
using Clovent.Identity.Branches;
using Clovent.Identity.Branches.ValueObjects;
using Clovent.Identity.Companies;
using Clovent.Identity.Companies.ValueObjects;
using Clovent.Identity.Infrastructure.Persistence;
using Clovent.Identity.Organizations;
using Clovent.Identity.Organizations.ValueObjects;
using Clovent.Inventory.Infrastructure.Persistence;
using Clovent.Inventory.WarehouseStocks;
using Clovent.MasterData.Infrastructure.Persistence;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Terminals;
using Clovent.MasterData.Terminals.ValueObjects;
using Clovent.MasterData.Warehouses;
using Clovent.MasterData.Warehouses.ValueObjects;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.DiningAreas.ValueObjects;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Infrastructure.Repositories;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Orders.ValueObjects;
using Clovent.Restaurant.Outbox;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Refunds;
using Clovent.Restaurant.Sales;
using Clovent.Restaurant.Tables;
using Clovent.Restaurant.Tables.ValueObjects;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Clovent.SupportFixtureRunner;

public sealed record SyntheticFixtureFacts(
    Guid OrgId,
    Guid BranchId,
    Guid WarehouseId,
    Guid CashierId,
    Guid BiryaniVariantId,
    Guid WaterVariantId,
    Guid Order1Id,
    Guid Order2Id,
    Guid Order3Id,
    Guid RefundId,
    decimal TotalCashPaid,
    decimal TotalCreditPaid,
    decimal CustomerOutstandingBalance,
    int CustomerLedgerEntriesCount,
    decimal RefundGrandTotal,
    decimal RefundTaxReversed,
    decimal BiryaniStockOnHand,
    decimal WaterStockOnHand,
    int OutboxPendingCount,
    int OutboxProcessingCount,
    int OutboxProcessedCount);

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  Clovent.SupportFixtureRunner --seed --connection-string \"<connStr>\" [--facts-file \"<path>\"]");
            Console.WriteLine("  Clovent.SupportFixtureRunner --reconcile --source \"<sourceConnStr>\" --restored \"<restoredConnStr>\" [--facts-file \"<path>\"]");
            return 0;
        }

        string mode = "";
        string connStr = "";
        string sourceConnStr = "";
        string restoredConnStr = "";
        string referenceConnStr = "";
        string diagnosticConnStr = "";
        decimal adjustmentAmount = 100.00m;
        string? factsFilePath = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--seed") mode = "seed";
            else if (args[i] == "--reconcile" || args[i] == "--reconcile-equivalence") mode = "reconcile-equivalence";
            else if (args[i] == "--reconcile-post-correction") mode = "reconcile-post-correction";
            else if (args[i] == "--connection-string" && i + 1 < args.Length) connStr = args[++i];
            else if (args[i] == "--source" && i + 1 < args.Length) sourceConnStr = args[++i];
            else if (args[i] == "--restored" && i + 1 < args.Length) restoredConnStr = args[++i];
            else if (args[i] == "--reference" && i + 1 < args.Length) referenceConnStr = args[++i];
            else if (args[i] == "--diagnostic" && i + 1 < args.Length) diagnosticConnStr = args[++i];
            else if (args[i] == "--adjustment" && i + 1 < args.Length && decimal.TryParse(args[++i], out var adj)) adjustmentAmount = adj;
            else if (args[i] == "--facts-file" && i + 1 < args.Length) factsFilePath = args[++i];
        }

        if (string.IsNullOrEmpty(referenceConnStr) && !string.IsNullOrEmpty(restoredConnStr))
        {
            referenceConnStr = restoredConnStr;
        }

        if (mode == "seed")
        {
            if (string.IsNullOrWhiteSpace(connStr))
            {
                Console.Error.WriteLine("Error: --connection-string required for --seed.");
                return 1;
            }

            Console.WriteLine("[FIXTURE-SEEDER] Seeding synthetic business fixture...");
            var facts = await PopulateSyntheticFixtureAsync(connStr);
            var json = JsonSerializer.Serialize(facts, new JsonSerializerOptions { WriteIndented = true });

            if (!string.IsNullOrWhiteSpace(factsFilePath))
            {
                var dir = Path.GetDirectoryName(factsFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                await File.WriteAllTextAsync(factsFilePath, json, Encoding.UTF8);
                Console.WriteLine($"[FIXTURE-SEEDER] Fixture facts saved to '{factsFilePath}'.");
            }

            Console.WriteLine("[FIXTURE-SEEDER] Fixture successfully seeded.");
            return 0;
        }

        if (mode == "reconcile-equivalence")
        {
            if (string.IsNullOrWhiteSpace(sourceConnStr) || string.IsNullOrWhiteSpace(referenceConnStr))
            {
                Console.Error.WriteLine("Error: --source and --reference (or --restored) required for --reconcile-equivalence.");
                return 1;
            }

            SyntheticFixtureFacts? facts = null;
            if (!string.IsNullOrWhiteSpace(factsFilePath) && File.Exists(factsFilePath))
            {
                var json = await File.ReadAllTextAsync(factsFilePath, Encoding.UTF8);
                facts = JsonSerializer.Deserialize<SyntheticFixtureFacts>(json);
            }

            Console.WriteLine("[FIXTURE-RECONCILER] Reconciling equivalence between Source and Restored Reference database...");
            var results = await ReconcileEquivalenceAsync(sourceConnStr, referenceConnStr, facts);
            PrintReconciliationTable(results);

            bool allPass = results.All(r => r.Matches);
            if (!allPass)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[FIXTURE-RECONCILER] RESTORE EQUIVALENCE RECONCILIATION FAILED.");
                Console.ResetColor();
                return 2;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[FIXTURE-RECONCILER] RESTORE EQUIVALENCE PASSED: Reference copy is 100% equivalent to Source.");
            Console.ResetColor();
            return 0;
        }

        if (mode == "reconcile-post-correction")
        {
            if (string.IsNullOrWhiteSpace(referenceConnStr) || string.IsNullOrWhiteSpace(diagnosticConnStr))
            {
                Console.Error.WriteLine("Error: --reference and --diagnostic required for --reconcile-post-correction.");
                return 1;
            }

            Console.WriteLine("[FIXTURE-RECONCILER] Reconciling post-correction domain facts between Reference and Diagnostic database...");
            var results = await ReconcilePostCorrectionAsync(referenceConnStr, diagnosticConnStr, adjustmentAmount);
            PrintReconciliationTable(results);

            bool allPass = results.All(r => r.Matches);
            if (!allPass)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[FIXTURE-RECONCILER] POST-CORRECTION RECONCILIATION FAILED.");
                Console.ResetColor();
                return 2;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[FIXTURE-RECONCILER] POST-CORRECTION RECONCILIATION PASSED: Compensating adjustment verified without historical mutation.");
            Console.ResetColor();
            return 0;
        }

        Console.Error.WriteLine("Error: Unknown mode. Use --seed, --reconcile-equivalence, or --reconcile-post-correction.");
        return 1;
    }

    public static async Task<SyntheticFixtureFacts> PopulateSyntheticFixtureAsync(string connectionString)
    {
        var services = BuildServiceProvider(connectionString);

        Guid orgId = Guid.NewGuid();
        Guid branchId = Guid.NewGuid();
        Guid warehouseId = Guid.NewGuid();
        Guid cashierId = Guid.NewGuid();
        Guid biryaniVariantId = Guid.NewGuid();
        Guid waterVariantId = Guid.NewGuid();

        await using (var scope = services.CreateAsyncScope())
        {
            // 1. Identity & MasterData & Restaurant Setup
            var idContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var org = Organization.Create(OrganizationName.Create("Synthetic Customer Org"));
            typeof(Organization).GetProperty("Id")!.SetValue(org, new OrganizationId(orgId));
            idContext.Organizations.Add(org);

            var company = Company.Create(org.Id, CompanyName.Create("Clovent Support Synthetic Retail"));
            idContext.Companies.Add(company);

            var branch = Branch.Create(company.Id, BranchName.Create("Islamabad Flagship Branch"));
            typeof(Branch).GetProperty("Id")!.SetValue(branch, new BranchId(branchId));
            idContext.Branches.Add(branch);
            await idContext.SaveChangesAsync();

            var mdContext = scope.ServiceProvider.GetRequiredService<MasterDataDbContext>();
            var terminal = Terminal.Create(new BranchId(branchId), TerminalName.Create("POS Terminal 01"), EntityCode.Create("POS-01"));
            mdContext.Terminals.Add(terminal);

            var wh = Warehouse.Create(new BranchId(branchId), WarehouseName.Create("Central Kitchen Warehouse"), EntityCode.Create("WH-01"));
            typeof(Warehouse).GetProperty("Id")!.SetValue(wh, new WarehouseId(warehouseId));
            mdContext.Warehouses.Add(wh);
            await mdContext.SaveChangesAsync();

            var restInitContext = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
            var area = DiningArea.Create(new BranchId(branchId), DiningAreaName.Create("Main Dining Area"));
            restInitContext.DiningAreas.Add(area);

            var table = Table.Create(area.Id, EntityCode.Create("T-01"), 4);
            restInitContext.Tables.Add(table);
            await restInitContext.SaveChangesAsync();

            // 2. Catalog: Synthetic Tax Profiles, Products, Variants
            var catContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            var cat = ProductCategory.Create(ProductCategoryName.Create("Main Entrees"));
            catContext.ProductCategories.Add(cat);

            var uomId = UnitOfMeasureId.New();
            var prod1 = Product.Create(ProductName.Create("Chicken Biryani"), Sku.Create("PROD-BIRYANI"), uomId, categoryId: cat.Id);
            catContext.Products.Add(prod1);

            var var1 = ProductVariant.Create(prod1.Id, VariantName.Create("Single Portion"), Sku.Create("SKU-BIRYANI-01"), uomId);
            typeof(ProductVariant).GetProperty("Id")!.SetValue(var1, new ProductVariantId(biryaniVariantId));
            catContext.ProductVariants.Add(var1);

            var prod2 = Product.Create(ProductName.Create("Mineral Water"), Sku.Create("PROD-WATER"), uomId, categoryId: cat.Id);
            catContext.Products.Add(prod2);

            var var2 = ProductVariant.Create(prod2.Id, VariantName.Create("Bottle 1.5L"), Sku.Create("SKU-WATER-01"), uomId);
            typeof(ProductVariant).GetProperty("Id")!.SetValue(var2, new ProductVariantId(waterVariantId));
            catContext.ProductVariants.Add(var2);

            var taxProfile1 = TaxProfile.Create("SYNTHETIC-TAX-16", "Synthetic Standard 16% Tax Profile", "SYNTH-AUTH", "Region-A", TaxClassification.Taxable, ItemTaxClassification.Goods, 16.00m, TaxPricingMode.Exclusive);
            catContext.TaxProfiles.Add(taxProfile1);

            var taxProfile2 = TaxProfile.Create("SYNTHETIC-TAX-0", "Synthetic Zero-Rated Exempt Profile", "SYNTH-AUTH", "Region-A", TaxClassification.Exempt, ItemTaxClassification.Goods, 0.00m, TaxPricingMode.Exclusive);
            catContext.TaxProfiles.Add(taxProfile2);

            await catContext.SaveChangesAsync();

            // 3. Inventory: WarehouseStock
            var invContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            var stock1 = WarehouseStock.Create(new WarehouseId(warehouseId), new ProductVariantId(biryaniVariantId), allowNegativeStock: true);
            stock1.Receive(50.00m); // Initial 50
            invContext.WarehouseStocks.Add(stock1);

            var stock2 = WarehouseStock.Create(new WarehouseId(warehouseId), new ProductVariantId(waterVariantId), allowNegativeStock: true);
            stock2.Receive(100.00m); // Initial 100
            invContext.WarehouseStocks.Add(stock2);

            await invContext.SaveChangesAsync();
        }

        // 4. Restaurant Transactions
        Guid order1Id = Guid.NewGuid();
        Guid order2Id = Guid.NewGuid();
        Guid order3Id = Guid.NewGuid();
        Guid refundId = Guid.NewGuid();
        Guid order1LineId = Guid.NewGuid();

        decimal totalCash = 0m;
        decimal totalCredit = 0m;
        decimal custBalance = 0m;
        int ledgerCount = 0;
        decimal refundTotal = 522.00m;
        decimal refundTax = 72.00m;
        decimal biryaniFinalStock = 0m;
        decimal waterFinalStock = 0m;

        await using (var scope = services.CreateAsyncScope())
        {
            var restContext = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
            var invContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

            var paymentMethods = await restContext.PaymentMethods.ToListAsync();
            var cashMethod = paymentMethods.First(p => p.Name.Value == "Cash");
            var creditMethod = paymentMethods.First(p => p.Name.Value == "On Account");

            var customers = await restContext.Customers.ToListAsync();
            var creditCustomer = customers.First(c => c.Code.Value == "CREDIT-DEMO");
            var table = await restContext.Tables.FirstAsync();

            // --- ORDER 1: Taxable Dine-In Sale (2x Biryani) ---
            var ord1 = Order.Create(OrderType.DineIn, new WarehouseId(warehouseId), table.Id, OrderNumber.Create("ORD-20261017-0001"));
            typeof(Order).GetProperty("Id")!.SetValue(ord1, new OrderId(order1Id));
            restContext.Orders.Add(ord1);

            var line1 = OrderLine.Create(
                ord1.Id,
                new ProductVariantId(biryaniVariantId),
                quantity: 2.00m,
                unitPrice: 450.00m,
                taxRatePercentage: 16.00m,
                taxIsInclusive: false,
                notes: "Extra spicy",
                taxClassification: "Taxable",
                taxAuthority: "SYNTH-AUTH",
                taxCode: "SYNTHETIC-TAX-16");
            typeof(OrderLine).GetProperty("Id")!.SetValue(line1, new OrderLineId(order1LineId));
            line1.SetTaxSnapshot(taxableBase: 900.00m, taxAmount: 144.00m, allocatedDiscount: 0m, "1.3.0-AwayFromZero-v1");
            ord1.AddOrderLine(line1.Id);
            restContext.OrderLines.Add(line1);

            var pay1 = Payment.Create(ord1.Id, cashMethod.Id, 1044.00m, null, "PAY-ORD1-CASH");
            ord1.RecordPayment(pay1.Id);
            restContext.Payments.Add(pay1);
            totalCash += 1044.00m;

            ord1.AssignDailySalesNumber(1);
            ord1.Complete();
            ord1.SetReceiptSnapshot(JsonSerializer.Serialize(new
            {
                OrderNumber = "ORD-20261017-0001",
                DailyNumber = 1,
                Subtotal = 900.00m,
                TaxTotal = 144.00m,
                GrandTotal = 1044.00m,
                Payment = "Cash"
            }));

            // Stock decrement: 50 - 2 = 48
            var s1 = await invContext.WarehouseStocks.FirstAsync(s => s.ProductVariantId == new ProductVariantId(biryaniVariantId));
            s1.Issue(2.00m);

            // --- ORDER 2: Non-Taxable / Exempt Sale (5x Water) ---
            var ord2 = Order.Create(OrderType.TakeAway, new WarehouseId(warehouseId), null, OrderNumber.Create("ORD-20261017-0002"));
            typeof(Order).GetProperty("Id")!.SetValue(ord2, new OrderId(order2Id));
            restContext.Orders.Add(ord2);

            var line2 = OrderLine.Create(
                ord2.Id,
                new ProductVariantId(waterVariantId),
                quantity: 5.00m,
                unitPrice: 120.00m,
                taxRatePercentage: 0.00m,
                taxIsInclusive: false,
                notes: "Chilled",
                taxClassification: "Exempt",
                taxAuthority: "SYNTH-AUTH",
                taxCode: "SYNTHETIC-TAX-0");
            line2.SetTaxSnapshot(taxableBase: 600.00m, taxAmount: 0.00m, allocatedDiscount: 0m, "1.3.0-AwayFromZero-v1");
            ord2.AddOrderLine(line2.Id);
            restContext.OrderLines.Add(line2);

            var pay2 = Payment.Create(ord2.Id, cashMethod.Id, 600.00m, null, "PAY-ORD2-CASH");
            ord2.RecordPayment(pay2.Id);
            restContext.Payments.Add(pay2);
            totalCash += 600.00m;

            ord2.AssignDailySalesNumber(2);
            ord2.Complete();
            ord2.SetReceiptSnapshot(JsonSerializer.Serialize(new
            {
                OrderNumber = "ORD-20261017-0002",
                DailyNumber = 2,
                Subtotal = 600.00m,
                TaxTotal = 0.00m,
                GrandTotal = 600.00m,
                Payment = "Cash"
            }));

            // Stock decrement: 100 - 5 = 95
            var s2 = await invContext.WarehouseStocks.FirstAsync(s => s.ProductVariantId == new ProductVariantId(waterVariantId));
            s2.Issue(5.00m);

            // --- ORDER 3: Customer Account / Credit Sale ---
            var ord3 = Order.Create(OrderType.TakeAway, new WarehouseId(warehouseId), null, OrderNumber.Create("ORD-20261017-0003"));
            typeof(Order).GetProperty("Id")!.SetValue(ord3, new OrderId(order3Id));
            ord3.SetCustomer(creditCustomer.Id);
            restContext.Orders.Add(ord3);

            var line3 = OrderLine.Create(
                ord3.Id,
                new ProductVariantId(biryaniVariantId),
                quantity: 2.00m,
                unitPrice: 450.00m,
                taxRatePercentage: 16.00m,
                taxIsInclusive: false,
                notes: null,
                taxClassification: "Taxable",
                taxAuthority: "SYNTH-AUTH",
                taxCode: "SYNTHETIC-TAX-16");
            line3.SetTaxSnapshot(taxableBase: 900.00m, taxAmount: 144.00m, allocatedDiscount: 0m, "1.3.0-AwayFromZero-v1");
            ord3.AddOrderLine(line3.Id);
            restContext.OrderLines.Add(line3);

            var pay3 = Payment.Create(ord3.Id, creditMethod.Id, 1044.00m, null, "PAY-ORD3-CREDIT");
            ord3.RecordPayment(pay3.Id);
            restContext.Payments.Add(pay3);
            totalCredit += 1044.00m;

            var ledgerEntry1 = CustomerLedgerEntry.Create(
                creditCustomer.Id,
                "ORD-20261017-0003",
                "Sale Invoice ORD-20261017-0003",
                debit: 1044.00m,
                credit: 0m,
                runningBalance: 1044.00m,
                date: DateTimeOffset.UtcNow,
                paymentMethod: "On Account");
            restContext.CustomerLedgerEntries.Add(ledgerEntry1);

            creditCustomer.AdjustBalance(1044.00m);
            custBalance = creditCustomer.OutstandingBalance;
            ledgerCount = 1;

            ord3.AssignDailySalesNumber(3);
            ord3.Complete();
            ord3.SetReceiptSnapshot(JsonSerializer.Serialize(new
            {
                OrderNumber = "ORD-20261017-0003",
                DailyNumber = 3,
                CustomerCode = creditCustomer.Code.Value,
                GrandTotal = 1044.00m,
                Payment = "On Account"
            }));

            // Stock decrement: 48 - 2 = 46
            s1.Issue(2.00m);

            // --- PARTIAL COMPENSATING REFUND on Order 1 ---
            var refDoc = Refund.Create(
                new RefundNumber("REF-20261017-0001"),
                ord1.Id,
                new BranchId(branchId),
                new WarehouseId(warehouseId),
                cashierId,
                "Support Cashier",
                "Customer portion cancelled before dining",
                "IDEMP-REF-001",
                RefundSettlementMethod.CashPayout);
            typeof(Refund).GetProperty("Id")!.SetValue(refDoc, new RefundId(refundId));

            var refLine = RefundLine.Create(
                refDoc.Id,
                new OrderLineId(order1LineId),
                new ProductVariantId(biryaniVariantId),
                "SKU-BIRYANI-01",
                "Chicken Biryani",
                quantity: 1.00m,
                unitPrice: 450.00m,
                discountReversed: 0m,
                taxReversed: 72.00m,
                lineTotalRefunded: 522.00m,
                taxClassification: "Taxable",
                taxCode: "SYNTHETIC-TAX-16",
                taxRatePercentage: 16.00m,
                taxIsInclusive: false,
                inventoryDisposition: InventoryDisposition.Restock);
            refDoc.AddLine(refLine);
            refDoc.FinalizeFinancials(
                subtotal: 450.00m,
                discountReversed: 0m,
                taxReversed: 72.00m,
                grandTotal: 522.00m,
                creditNoteSnapshotJson: JsonSerializer.Serialize(new
                {
                    RefundNumber = "REF-20261017-0001",
                    OriginalOrder = "ORD-20261017-0001",
                    Amount = 522.00m,
                    TaxReversed = 72.00m,
                    Settlement = "CashPayout"
                }));

            restContext.Refunds.Add(refDoc);

            // Inventory Restock: 46 + 1 = 47
            s1.Receive(1.00m);

            biryaniFinalStock = s1.QuantityOnHand;
            waterFinalStock = s2.QuantityOnHand;

            // --- OUTBOX MESSAGES ---
            // 1. Pending ReceiptPrint
            var msg1 = OutboxMessage.Create(
                OutboxMessageType.ReceiptPrint,
                "Order",
                order1Id.ToString(),
                Guid.NewGuid().ToString(),
                JsonSerializer.Serialize(new { OrderNumber = "ORD-20261017-0001", PrintStatus = "Queued" }),
                "idemp-print-ord1");
            restContext.OutboxMessages.Add(msg1);

            // 2. Stuck / In-Flight Processing QuickBooksSync (for corrective script test)
            var msg2 = OutboxMessage.Create(
                OutboxMessageType.QuickBooksSync,
                "Order",
                order1Id.ToString(),
                Guid.NewGuid().ToString(),
                JsonSerializer.Serialize(new { OrderNumber = "ORD-20261017-0001", Amount = 1044.00m }),
                "idemp-qb-ord1");
            msg2.ClaimForProcessing(DateTimeOffset.UtcNow.AddMinutes(-10));
            // Set attempt count high so our sample corrective script targets it
            typeof(OutboxMessage).GetProperty("AttemptCount")!.SetValue(msg2, 5);
            typeof(OutboxMessage).GetProperty("LastError")!.SetValue(msg2, "Simulated gateway timeout during automated sync");
            restContext.OutboxMessages.Add(msg2);

            // 3. Completed InventoryPosting
            var msg3 = OutboxMessage.Create(
                OutboxMessageType.InventoryPosting,
                "Order",
                order2Id.ToString(),
                Guid.NewGuid().ToString(),
                JsonSerializer.Serialize(new { OrderNumber = "ORD-20261017-0002", Warehouse = "WH-01" }),
                "idemp-inv-ord2");
            msg3.ClaimForProcessing(DateTimeOffset.UtcNow.AddMinutes(-20));
            msg3.MarkCompleted(DateTimeOffset.UtcNow.AddMinutes(-19));
            restContext.OutboxMessages.Add(msg3);

            await restContext.SaveChangesAsync();
            await invContext.SaveChangesAsync();
        }

        return new SyntheticFixtureFacts(
            orgId,
            branchId,
            warehouseId,
            cashierId,
            biryaniVariantId,
            waterVariantId,
            order1Id,
            order2Id,
            order3Id,
            refundId,
            totalCash,
            totalCredit,
            custBalance,
            ledgerCount,
            refundTotal,
            refundTax,
            biryaniFinalStock,
            waterFinalStock,
            OutboxPendingCount: 1,
            OutboxProcessingCount: 1,
            OutboxProcessedCount: 1);
    }

    public sealed record ReconciliationRow(
        string Category,
        string FactDescription,
        string ExpectedValue,
        string RestoredValue,
        bool Matches);

    public static async Task<List<ReconciliationRow>> ReconcileEquivalenceAsync(
        string sourceConnStr,
        string referenceConnStr,
        SyntheticFixtureFacts? facts)
    {
        var rows = new List<ReconciliationRow>();

        var sourceServices = BuildServiceProvider(sourceConnStr);
        var refServices = BuildServiceProvider(referenceConnStr);

        await using var sScope = sourceServices.CreateAsyncScope();
        await using var rScope = refServices.CreateAsyncScope();

        var sRest = sScope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
        var rRest = rScope.ServiceProvider.GetRequiredService<RestaurantDbContext>();

        var sInv = sScope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var rInv = rScope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        // 1. Order Counts
        var sOrdersCount = await sRest.Orders.CountAsync();
        var rOrdersCount = await rRest.Orders.CountAsync();
        rows.Add(new ReconciliationRow("Sales Orders", "Total Completed Orders Count", sOrdersCount.ToString(), rOrdersCount.ToString(), sOrdersCount == rOrdersCount && rOrdersCount >= 3));

        // 2. Monetary Totals: Payments Cash
        var sPaymentMethods = await sRest.PaymentMethods.ToListAsync();
        var rPaymentMethods = await rRest.PaymentMethods.ToListAsync();
        var sCashMethodId = sPaymentMethods.First(pm => pm.Name.Value == "Cash").Id;
        var rCashMethodId = rPaymentMethods.First(pm => pm.Name.Value == "Cash").Id;
        var sCreditMethodId = sPaymentMethods.First(pm => pm.Name.Value == "On Account").Id;
        var rCreditMethodId = rPaymentMethods.First(pm => pm.Name.Value == "On Account").Id;

        var sCashPaid = await sRest.Payments.Where(p => p.PaymentMethodId == sCashMethodId).SumAsync(p => p.Amount);
        var rCashPaid = await rRest.Payments.Where(p => p.PaymentMethodId == rCashMethodId).SumAsync(p => p.Amount);
        rows.Add(new ReconciliationRow("Monetary Totals", "Total Cash Payments (PKR)", $"{sCashPaid:N2}", $"{rCashPaid:N2}", sCashPaid == rCashPaid && rCashPaid > 0));

        // 3. Monetary Totals: Payments Credit / On Account
        var sCreditPaid = await sRest.Payments.Where(p => p.PaymentMethodId == sCreditMethodId).SumAsync(p => p.Amount);
        var rCreditPaid = await rRest.Payments.Where(p => p.PaymentMethodId == rCreditMethodId).SumAsync(p => p.Amount);
        rows.Add(new ReconciliationRow("Monetary Totals", "Total On-Account Payments (PKR)", $"{sCreditPaid:N2}", $"{rCreditPaid:N2}", sCreditPaid == rCreditPaid && rCreditPaid > 0));

        // 4. Tax Snapshots & Integrity on Order 1
        var sLine1 = await sRest.OrderLines.FirstOrDefaultAsync(l => l.TaxCode == "SYNTHETIC-TAX-16");
        var rLine1 = await rRest.OrderLines.FirstOrDefaultAsync(l => l.TaxCode == "SYNTHETIC-TAX-16");
        bool taxSnapMatch = sLine1 != null && rLine1 != null &&
                            sLine1.TaxableBase == rLine1.TaxableBase &&
                            sLine1.TaxAmount == rLine1.TaxAmount &&
                            sLine1.TaxCode == rLine1.TaxCode;
        rows.Add(new ReconciliationRow("Tax Governance", "Synthetic Tax Snapshot (Base 900.00, Tax 144.00, 16%)",
            sLine1 != null ? $"{sLine1.TaxableBase:N2} / {sLine1.TaxAmount:N2} ({sLine1.TaxCode})" : "null",
            rLine1 != null ? $"{rLine1.TaxableBase:N2} / {rLine1.TaxAmount:N2} ({rLine1.TaxCode})" : "null",
            taxSnapMatch));

        // 5. Receipt Snapshots Integrity
        var sOrdersList = await sRest.Orders.ToListAsync();
        var rOrdersList = await rRest.Orders.ToListAsync();
        var sOrd1 = sOrdersList.FirstOrDefault(o => o.OrderNumber.Value == "ORD-20261017-0001");
        var rOrd1 = rOrdersList.FirstOrDefault(o => o.OrderNumber.Value == "ORD-20261017-0001");
        bool receiptMatch = sOrd1 != null && rOrd1 != null &&
                            !string.IsNullOrEmpty(rOrd1.ReceiptSnapshotJson) &&
                            rOrd1.ReceiptSnapshotJson == sOrd1.ReceiptSnapshotJson;
        rows.Add(new ReconciliationRow("Receipt Immutability", "Order #1 Frozen Receipt Snapshot JSON", "Exact Hash Match", receiptMatch ? "Exact Hash Match" : "Mismatch", receiptMatch));

        // 6. Customer Balance & Ledger Reconciliation (Equivalence: identical in Source and Reference!)
        var sCustList = await sRest.Customers.ToListAsync();
        var rCustList = await rRest.Customers.ToListAsync();
        var sCust = sCustList.FirstOrDefault(c => c.Code.Value == "CREDIT-DEMO");
        var rCust = rCustList.FirstOrDefault(c => c.Code.Value == "CREDIT-DEMO");
        var rLedgerEntries = rCust != null ? await rRest.CustomerLedgerEntries.Where(l => l.CustomerId == rCust.Id).ToListAsync() : [];
        var ledgerSum = rLedgerEntries.Sum(l => l.Debit) - rLedgerEntries.Sum(l => l.Credit);
        bool customerReconciled = sCust != null && rCust != null &&
                                  sCust.OutstandingBalance == rCust.OutstandingBalance &&
                                  rCust.OutstandingBalance == ledgerSum &&
                                  rCust.OutstandingBalance == 1044.00m;
        rows.Add(new ReconciliationRow("Receivables Ledger", "Customer CREDIT-DEMO Balance Equivalence",
            sCust != null ? $"{sCust.OutstandingBalance:N2} PKR" : "null",
            rCust != null ? $"{rCust.OutstandingBalance:N2} PKR (Sum: {ledgerSum:N2})" : "null",
            customerReconciled));

        // 7. Compensating Refund Verification
        var sRefund = await sRest.Refunds.Include(r => r.Lines).FirstOrDefaultAsync();
        var rRefund = await rRest.Refunds.Include(r => r.Lines).FirstOrDefaultAsync();
        bool refundMatch = sRefund != null && rRefund != null &&
                           rRefund.GrandTotalRefunded == sRefund.GrandTotalRefunded &&
                           rRefund.TaxReversedTotal == sRefund.TaxReversedTotal;
        rows.Add(new ReconciliationRow("Compensating Refunds", "Refund Total & Tax Reversal Match",
            sRefund != null ? $"{sRefund.GrandTotalRefunded:N2} PKR (Tax: {sRefund.TaxReversedTotal:N2})" : "null",
            rRefund != null ? $"{rRefund.GrandTotalRefunded:N2} PKR (Tax: {rRefund.TaxReversedTotal:N2})" : "null",
            refundMatch));

        // 8. Inventory Positions
        var sStocks = await sInv.WarehouseStocks.ToListAsync();
        var rStocks = await rInv.WarehouseStocks.ToListAsync();
        bool stockMatch = sStocks.Count > 0 && sStocks.Count == rStocks.Count &&
                          sStocks.All(s => rStocks.Any(r => r.ProductVariantId == s.ProductVariantId && r.QuantityOnHand == s.QuantityOnHand));
        rows.Add(new ReconciliationRow("Inventory Positions", "All Warehouse Stock Levels Match",
            $"{sStocks.Count} stock records", $"{rStocks.Count} stock records", stockMatch));

        return rows;
    }

    public static async Task<List<ReconciliationRow>> ReconcilePostCorrectionAsync(
        string referenceConnStr,
        string diagnosticConnStr,
        decimal expectedAdjustment)
    {
        var rows = new List<ReconciliationRow>();
        var refServices = BuildServiceProvider(referenceConnStr);
        var diagServices = BuildServiceProvider(diagnosticConnStr);

        await using var refScope = refServices.CreateAsyncScope();
        await using var diagScope = diagServices.CreateAsyncScope();

        var refRest = refScope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
        var diagRest = diagScope.ServiceProvider.GetRequiredService<RestaurantDbContext>();

        // 1. Customer Balance Adjustment Check (1,044.00 -> 944.00)
        var refCust = (await refRest.Customers.ToListAsync()).FirstOrDefault(c => c.Code.Value == "CREDIT-DEMO");
        var diagCust = (await diagRest.Customers.ToListAsync()).FirstOrDefault(c => c.Code.Value == "CREDIT-DEMO");

        decimal originalBalance = refCust?.OutstandingBalance ?? 0m;
        decimal correctedBalance = diagCust?.OutstandingBalance ?? 0m;
        decimal expectedCorrectedBalance = originalBalance - expectedAdjustment;

        bool balanceAdjusted = refCust != null && diagCust != null &&
                               originalBalance == 1044.00m &&
                               correctedBalance == 944.00m &&
                               correctedBalance == expectedCorrectedBalance;

        rows.Add(new ReconciliationRow("Corrective Adjustment", "Customer Balance (1,044.00 -> 944.00 PKR)",
            $"{originalBalance:N2} PKR", $"{correctedBalance:N2} PKR (Expected: {expectedCorrectedBalance:N2})", balanceAdjusted));

        // 2. Ledger Reconciliation (Sum of debits - credits == Customer.OutstandingBalance)
        var diagLedgerEntries = diagCust != null ? await diagRest.CustomerLedgerEntries.Where(l => l.CustomerId == diagCust.Id).ToListAsync() : [];
        var refLedgerEntries = refCust != null ? await refRest.CustomerLedgerEntries.Where(l => l.CustomerId == refCust.Id).ToListAsync() : [];
        var diagLedgerSum = diagLedgerEntries.Sum(l => l.Debit) - diagLedgerEntries.Sum(l => l.Credit);

        bool ledgerReconciled = diagCust != null &&
                                diagCust.OutstandingBalance == diagLedgerSum &&
                                diagLedgerEntries.Count == refLedgerEntries.Count + 1;

        rows.Add(new ReconciliationRow("Receivables Ledger", "Ledger Sum vs Customer OutstandingBalance",
            $"{diagLedgerSum:N2} PKR (Sum)", $"{correctedBalance:N2} PKR (Cust)", ledgerReconciled));

        // 3. Compensating Credit Entry Validation
        var compEntry = diagLedgerEntries.FirstOrDefault(l => l.Reference.Contains("DISPUTE"));
        bool compEntryValid = compEntry != null && compEntry.Credit == expectedAdjustment && compEntry.Debit == 0.00m && compEntry.RunningBalance == 944.00m;
        rows.Add(new ReconciliationRow("Compensating Entry", "Credit Memo Details (100.00 Credit, Running 944.00)",
            "100.00 Credit / 944.00 Run",
            compEntry != null ? $"{compEntry.Credit:N2} Credit / {compEntry.RunningBalance:N2} Run" : "Not Found",
            compEntryValid));

        // 4. Sales Orders Immutability Check (Must remain IDENTICAL to reference!)
        var refOrdersCount = await refRest.Orders.CountAsync();
        var diagOrdersCount = await diagRest.Orders.CountAsync();
        rows.Add(new ReconciliationRow("Financial Immutability", "Completed Orders Count Unchanged",
            refOrdersCount.ToString(), diagOrdersCount.ToString(), refOrdersCount == diagOrdersCount));

        // 5. Payment Totals Immutability Check (Cash & On-Account payments must remain untouched)
        var refPaymentMethods = await refRest.PaymentMethods.ToListAsync();
        var diagPaymentMethods = await diagRest.PaymentMethods.ToListAsync();
        var refCashId = refPaymentMethods.First(pm => pm.Name.Value == "Cash").Id;
        var diagCashId = diagPaymentMethods.First(pm => pm.Name.Value == "Cash").Id;
        var refCashPaid = await refRest.Payments.Where(p => p.PaymentMethodId == refCashId).SumAsync(p => p.Amount);
        var diagCashPaid = await diagRest.Payments.Where(p => p.PaymentMethodId == diagCashId).SumAsync(p => p.Amount);
        rows.Add(new ReconciliationRow("Financial Immutability", "Total Cash Payments Untouched",
            $"{refCashPaid:N2} PKR", $"{diagCashPaid:N2} PKR", refCashPaid == diagCashPaid));

        // 6. Tax Snapshots Immutability Check
        var refLine1 = await refRest.OrderLines.FirstOrDefaultAsync(l => l.TaxCode == "SYNTHETIC-TAX-16");
        var diagLine1 = await diagRest.OrderLines.FirstOrDefaultAsync(l => l.TaxCode == "SYNTHETIC-TAX-16");
        bool taxSnapUnchanged = refLine1 != null && diagLine1 != null &&
                                refLine1.TaxableBase == diagLine1.TaxableBase &&
                                refLine1.TaxAmount == diagLine1.TaxAmount;
        rows.Add(new ReconciliationRow("Financial Immutability", "Historical Tax Snapshot Untouched",
            refLine1 != null ? $"{refLine1.TaxAmount:N2} PKR" : "null",
            diagLine1 != null ? $"{diagLine1.TaxAmount:N2} PKR" : "null",
            taxSnapUnchanged));

        // 7. Receipt Snapshots Immutability Check
        var refOrd1 = (await refRest.Orders.ToListAsync()).FirstOrDefault(o => o.OrderNumber.Value == "ORD-20261017-0001");
        var diagOrd1 = (await diagRest.Orders.ToListAsync()).FirstOrDefault(o => o.OrderNumber.Value == "ORD-20261017-0001");
        bool receiptUnchanged = refOrd1 != null && diagOrd1 != null &&
                                refOrd1.ReceiptSnapshotJson == diagOrd1.ReceiptSnapshotJson;
        rows.Add(new ReconciliationRow("Financial Immutability", "Frozen Receipt Snapshot JSON Untouched",
            "Exact Match", receiptUnchanged ? "Exact Match" : "Altered", receiptUnchanged));

        return rows;
    }

    private static void PrintReconciliationTable(List<ReconciliationRow> rows)
    {
        Console.WriteLine("\n------------------------------------------------------------------------------------------------------------------");
        Console.WriteLine($"| {"Category",-22} | {"Fact Description",-45} | {"Source",-18} | {"Restored",-18} | {"Result",-6} |");
        Console.WriteLine("------------------------------------------------------------------------------------------------------------------");
        foreach (var r in rows)
        {
            var res = r.Matches ? "PASS" : "FAIL";
            if (!r.Matches) Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"| {r.Category,-22} | {r.FactDescription,-45} | {r.ExpectedValue,-18} | {r.RestoredValue,-18} | {res,-6} |");
            if (!r.Matches) Console.ResetColor();
        }
        Console.WriteLine("------------------------------------------------------------------------------------------------------------------\n");
    }

    private static IServiceProvider BuildServiceProvider(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddDbContext<AuthenticationDbContext>(opts =>
            opts.UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "Authentication"))
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

        services.AddDbContext<IdentityDbContext>(opts =>
            opts.UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "Identity"))
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

        services.AddDbContext<MasterDataDbContext>(opts =>
            opts.UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "MasterData"))
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

        services.AddDbContext<CatalogDbContext>(opts =>
            opts.UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "Catalog"))
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

        services.AddDbContext<InventoryDbContext>(opts =>
            opts.UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "Inventory"))
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

        services.AddDbContext<RestaurantDbContext>(opts =>
            opts.UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "Restaurant"))
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

        services.AddScoped<IOutboxRepository, OutboxRepository>();

        return services.BuildServiceProvider();
    }
}
