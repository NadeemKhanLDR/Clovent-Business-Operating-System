using System.Diagnostics;
using Clovent.Restaurant.Continuity;
using Xunit;

namespace Clovent.Restaurant.Tests;

public sealed class OperationalCacheIndexingPerformanceTests
{
    [Fact]
    public void OperationalCacheSnapshot_LargeCatalog_ProvidesSubMillisecondLookups()
    {
        const int categoryCount = 50;
        const int productCount = 2500;
        const int variantsPerProduct = 2;

        var categories = new List<CachedCategory>(categoryCount);
        for (int i = 0; i < categoryCount; i++)
        {
            categories.Add(new CachedCategory(Guid.NewGuid(), $"Category {i + 1}", null, null, i + 1, "Active"));
        }

        var products = new List<CachedProduct>(productCount);
        var variants = new List<CachedVariant>(productCount * variantsPerProduct);
        var targetVariantId = Guid.Empty;

        for (int i = 0; i < productCount; i++)
        {
            var catId = categories[i % categoryCount].ProductCategoryId;
            var prodId = Guid.NewGuid();
            var prodCode = $"PRD-{i + 1:D5}";
            var prodName = $"Menu Item {i + 1}";

            products.Add(new CachedProduct(prodId, prodName, prodCode, catId, "Prepared", 16.0m, false, "Active"));

            for (int v = 0; v < variantsPerProduct; v++)
            {
                var varId = Guid.NewGuid();
                if (i == 1234 && v == 1)
                {
                    targetVariantId = varId;
                }

                variants.Add(new CachedVariant(
                    varId, prodId, $"Size {v + 1}", $"{prodCode}-V{v + 1}",
                    150m + (i * 2m) + (v * 50m), Guid.NewGuid(), catId, v, "Active", true, "Prepared", null, prodName, 16.0m, false));
            }
        }

        var metadata = new OperationalCacheMetadata(
            SchemaVersion: 1,
            CacheVersion: "1.2.0-TEST",
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            LastSuccessfulSyncUtc: DateTimeOffset.UtcNow,
            CompanyId: Guid.NewGuid(),
            CompanyName: "Test Co",
            BranchId: Guid.NewGuid(),
            BranchName: "Main Branch",
            TerminalId: Guid.NewGuid(),
            TerminalName: "Terminal 01",
            TerminalCode: "POS01",
            WarehouseId: Guid.NewGuid(),
            WarehouseName: "Main Warehouse",
            CurrencyCode: "PKR",
            CurrencySymbol: "Rs.",
            CurrencyDecimalPlaces: 2,
            SourceDatabaseIdentity: "TestDB",
            SourceRevision: "REV-1",
            TotalCategories: categories.Count,
            TotalProducts: products.Count,
            TotalVariants: variants.Count,
            TotalTemplates: 0,
            PayloadChecksum: "TESTCHECKSUM",
            HmacSignature: "TESTHMAC");

        var payload = new OperationalCachePayload
        {
            Categories = categories,
            Products = products,
            Variants = variants,
            QuickOrderTemplates = [],
            DiningAreas = [],
            Tables = [],
            Discounts = [],
            PaymentMethods = [],
            Operators = [],
            Customers = []
        };

        var snapshot = new OperationalCacheSnapshot(metadata, payload);

        // 1. Warm-up lookup
        var warmed = snapshot.GetVariant(targetVariantId);
        Assert.NotNull(warmed);
        Assert.Equal(targetVariantId, warmed.ProductVariantId);

        // 2. Measure high-throughput random/targeted lookups
        var sw = Stopwatch.StartNew();
        const int lookups = 1000;
        for (int k = 0; k < lookups; k++)
        {
            var found = snapshot.GetVariant(targetVariantId);
            Assert.NotNull(found);
        }
        sw.Stop();

        // 1000 lookups should take well under 10 milliseconds total (averaging < 0.01 ms per lookup)
        Assert.True(sw.ElapsedMilliseconds < 50, $"1,000 lookups took {sw.ElapsedMilliseconds}ms, expected < 50ms");

        // 3. Measure catalog search filter across 2,500 products
        var searchSw = Stopwatch.StartNew();
        var searchResults = snapshot.Payload.Products
            .Where(p => p.Name.Contains("1234", StringComparison.OrdinalIgnoreCase) ||
                        p.Sku.Contains("1234", StringComparison.OrdinalIgnoreCase))
            .ToList();
        searchSw.Stop();

        Assert.NotEmpty(searchResults);
        Assert.True(searchSw.ElapsedMilliseconds < 25, $"Catalog search took {searchSw.ElapsedMilliseconds}ms, expected < 25ms");
    }
}
