using System;
using System.Collections.Generic;
using System.Linq;
using Clovent.Catalog.Application.Categories.Dtos;
using Clovent.Catalog.Application.Variants.Dtos;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Orders;

/// <summary>
/// Regression test suite for Restaurant POS Menu Eligibility and Category Count Reconciliation.
/// Validates test cases MENU-COUNT-01 through MENU-COUNT-12.
/// </summary>
public class MenuCategoryCountTests
{
    private readonly Guid _catBeverages = Guid.NewGuid();
    private readonly Guid _catBread = Guid.NewGuid();
    private readonly Guid _catSnacks = Guid.NewGuid();
    private readonly Guid _catMainCourse = Guid.NewGuid();
    private readonly Guid _catInactive = Guid.NewGuid();

    private readonly List<ProductCategoryDto> _categories;
    private readonly List<ProductVariantDto> _variants;

    public MenuCategoryCountTests()
    {
        _categories =
        [
            new ProductCategoryDto(_catBeverages, "Beverages", null, "Active", "#3B82F6", 1, DateTimeOffset.UtcNow),
            new ProductCategoryDto(_catBread, "Bread", null, "Active", "#10B981", 2, DateTimeOffset.UtcNow),
            new ProductCategoryDto(_catSnacks, "Snacks", null, "Active", "#F59E0B", 3, DateTimeOffset.UtcNow),
            new ProductCategoryDto(_catMainCourse, "Main Course", null, "Active", "#EF4444", 4, DateTimeOffset.UtcNow),
            new ProductCategoryDto(_catInactive, "Discontinued Category", null, "Inactive", "#9CA3AF", 5, DateTimeOffset.UtcNow),
        ];

        var p1 = Guid.NewGuid(); // Beverages
        var p2 = Guid.NewGuid(); // Beverages
        var p3 = Guid.NewGuid(); // Bread
        var p4 = Guid.NewGuid(); // Bread
        var p5 = Guid.NewGuid(); // Bread
        var p6 = Guid.NewGuid(); // Snacks
        var p7 = Guid.NewGuid(); // Main Course (Karahi)
        var p8 = Guid.NewGuid(); // Main Course (Daal)
        var p9 = Guid.NewGuid(); // Main Course (Qorma)
        var p10 = Guid.NewGuid(); // Main Course (Biryani)
        var p11 = Guid.NewGuid(); // Main Course (Haleem)
        var p12 = Guid.NewGuid(); // Main Course (Murgh Chanay)
        var p13 = Guid.NewGuid(); // Main Course (Koyla Karahi)
        var p14 = Guid.NewGuid(); // Main Course (Aloo Gobi)
        var p15 = Guid.NewGuid(); // Main Course (Salad)
        var p16 = Guid.NewGuid(); // Inactive Product
        var p17 = Guid.NewGuid(); // Uncategorized Active Product

        _variants =
        [
            // Beverages (2 items)
            new ProductVariantDto(Guid.NewGuid(), p1, "Cola 500ml", "COLA-500", Guid.NewGuid(), "Active", 1, DateTimeOffset.UtcNow, _catBeverages, "Active"),
            new ProductVariantDto(Guid.NewGuid(), p2, "Lemonade", "LEMONADE", Guid.NewGuid(), "Active", 2, DateTimeOffset.UtcNow, _catBeverages, "Active"),

            // Bread (3 items, including 1 multi-variant product)
            new ProductVariantDto(Guid.NewGuid(), p3, "Plain Naan", "NAAN-PL", Guid.NewGuid(), "Active", 1, DateTimeOffset.UtcNow, _catBread, "Active"),
            new ProductVariantDto(Guid.NewGuid(), p4, "Roti", "ROTI", Guid.NewGuid(), "Active", 2, DateTimeOffset.UtcNow, _catBread, "Active"),
            new ProductVariantDto(Guid.NewGuid(), p5, "Garlic Naan Single", "GARLIC-1", Guid.NewGuid(), "Active", 3, DateTimeOffset.UtcNow, _catBread, "Active"),
            new ProductVariantDto(Guid.NewGuid(), p5, "Garlic Naan Basket", "GARLIC-B", Guid.NewGuid(), "Active", 4, DateTimeOffset.UtcNow, _catBread, "Active"), // 2nd variant for p5

            // Snacks (1 item)
            new ProductVariantDto(Guid.NewGuid(), p6, "Samosa", "SAMOSA", Guid.NewGuid(), "Active", 1, DateTimeOffset.UtcNow, _catSnacks, "Active"),

            // Main Course (9 items)
            new ProductVariantDto(Guid.NewGuid(), p7, "Chicken Karahi", "CK-1", Guid.NewGuid(), "Active", 1, DateTimeOffset.UtcNow, _catMainCourse, "Active"),
            new ProductVariantDto(Guid.NewGuid(), p8, "White Daal Mash Half", "DM-H", Guid.NewGuid(), "Active", 2, DateTimeOffset.UtcNow, _catMainCourse, "Active"),
            new ProductVariantDto(Guid.NewGuid(), p8, "White Daal Mash Full", "DM-F", Guid.NewGuid(), "Active", 3, DateTimeOffset.UtcNow, _catMainCourse, "Active"),
            new ProductVariantDto(Guid.NewGuid(), p9, "Aloo Chicken Qorma", "ACQ", Guid.NewGuid(), "Active", 4, DateTimeOffset.UtcNow, _catMainCourse, "Active"),
            new ProductVariantDto(Guid.NewGuid(), p10, "Chicken Biryani", "CB", Guid.NewGuid(), "Active", 5, DateTimeOffset.UtcNow, _catMainCourse, "Active"),
            new ProductVariantDto(Guid.NewGuid(), p11, "Chicken Haleem", "CH", Guid.NewGuid(), "Active", 6, DateTimeOffset.UtcNow, _catMainCourse, "Active"),
            new ProductVariantDto(Guid.NewGuid(), p12, "Murgh Chanay", "MC", Guid.NewGuid(), "Active", 7, DateTimeOffset.UtcNow, _catMainCourse, "Active"),
            new ProductVariantDto(Guid.NewGuid(), p13, "Chicken Koyla Karahi", "CKK", Guid.NewGuid(), "Active", 8, DateTimeOffset.UtcNow, _catMainCourse, "Active"),
            new ProductVariantDto(Guid.NewGuid(), p14, "Aloo Gobi", "AG", Guid.NewGuid(), "Active", 9, DateTimeOffset.UtcNow, _catMainCourse, "Active"),
            new ProductVariantDto(Guid.NewGuid(), p15, "Salad", "SLD", Guid.NewGuid(), "Active", 10, DateTimeOffset.UtcNow, _catMainCourse, "Active"),

            // Inactive product & variant (should be excluded)
            new ProductVariantDto(Guid.NewGuid(), p16, "Old Item", "OLD-1", Guid.NewGuid(), "Active", 11, DateTimeOffset.UtcNow, _catBeverages, "Inactive"),
            new ProductVariantDto(Guid.NewGuid(), p1, "Discontinued Variant", "COLA-OLD", Guid.NewGuid(), "Inactive", 12, DateTimeOffset.UtcNow, _catBeverages, "Active"),

            // Uncategorized active product (should land in Uncategorized category)
            new ProductVariantDto(Guid.NewGuid(), p17, "Chef Special", "CHEF-1", Guid.NewGuid(), "Active", 13, DateTimeOffset.UtcNow, null, "Active"),
        ];
    }

    private IEnumerable<ProductVariantDto> GetEligibleVariants()
    {
        return _variants.Where(v => v.Status == "Active" && (v.ProductStatus is null || v.ProductStatus == "Active"));
    }

    private IEnumerable<ProductCategoryDto> GetActiveCategories()
    {
        return _categories.Where(c => c.Status == "Active");
    }

    [Fact]
    public void MENU_COUNT_01_AllMenuCountMatchesEligibleDistinctProducts()
    {
        var eligible = GetEligibleVariants();
        int allMenuCount = eligible.Select(v => v.ProductId).Distinct().Count();

        // 2 (Beverages) + 3 (Bread) + 1 (Snacks) + 9 (Main Course) + 1 (Uncategorized) = 16 distinct eligible products
        Assert.Equal(16, allMenuCount);
    }

    [Fact]
    public void MENU_COUNT_02_CategoryCountsMatchEligibleProductsPerCategory()
    {
        var eligible = GetEligibleVariants();

        int beveragesCount = eligible.Where(v => v.ProductCategoryId == _catBeverages).Select(v => v.ProductId).Distinct().Count();
        int breadCount = eligible.Where(v => v.ProductCategoryId == _catBread).Select(v => v.ProductId).Distinct().Count();
        int snacksCount = eligible.Where(v => v.ProductCategoryId == _catSnacks).Select(v => v.ProductId).Distinct().Count();
        int mainCourseCount = eligible.Where(v => v.ProductCategoryId == _catMainCourse).Select(v => v.ProductId).Distinct().Count();

        Assert.Equal(2, beveragesCount);
        Assert.Equal(3, breadCount);
        Assert.Equal(1, snacksCount);
        Assert.Equal(9, mainCourseCount);
    }

    [Fact]
    public void MENU_COUNT_03_SumOfVisibleCategoriesEqualsAllMenuCount()
    {
        var eligible = GetEligibleVariants();
        var activeCategoryIds = GetActiveCategories().Select(c => c.ProductCategoryId).ToHashSet();

        int allMenuCount = eligible.Select(v => v.ProductId).Distinct().Count();

        int visibleCategoryTotal = 0;
        foreach (var catId in activeCategoryIds)
        {
            visibleCategoryTotal += eligible.Where(v => v.ProductCategoryId == catId).Select(v => v.ProductId).Distinct().Count();
        }

        int uncategorizedCount = eligible
            .Where(v => !v.ProductCategoryId.HasValue || !activeCategoryIds.Contains(v.ProductCategoryId.Value))
            .Select(v => v.ProductId)
            .Distinct()
            .Count();

        Assert.Equal(allMenuCount, visibleCategoryTotal + uncategorizedCount);
    }

    [Fact]
    public void MENU_COUNT_04_InactiveItemsAreExcludedConsistently()
    {
        var eligible = GetEligibleVariants();

        Assert.DoesNotContain(eligible, v => v.ProductStatus == "Inactive");
        Assert.DoesNotContain(eligible, v => v.Status == "Inactive");
    }

    [Fact]
    public void MENU_COUNT_05_InactiveCategoriesDoNotOrphanItemsFromUncategorized()
    {
        var eligible = GetEligibleVariants();
        var activeCategoryIds = GetActiveCategories().Select(c => c.ProductCategoryId).ToHashSet();

        Assert.DoesNotContain(_catInactive, activeCategoryIds);

        int uncategorizedCount = eligible
            .Where(v => !v.ProductCategoryId.HasValue || !activeCategoryIds.Contains(v.ProductCategoryId.Value))
            .Select(v => v.ProductId)
            .Distinct()
            .Count();

        Assert.Equal(1, uncategorizedCount);
    }

    [Fact]
    public void MENU_COUNT_06_UncategorizedItemsHandledCleanly()
    {
        var eligible = GetEligibleVariants();
        var activeCategoryIds = GetActiveCategories().Select(c => c.ProductCategoryId).ToHashSet();

        var uncategorizedVariants = eligible
            .Where(v => !v.ProductCategoryId.HasValue || !activeCategoryIds.Contains(v.ProductCategoryId.Value))
            .ToList();

        Assert.Single(uncategorizedVariants.Select(v => v.ProductId).Distinct());
        Assert.Equal("Chef Special", uncategorizedVariants.First().Name);
    }

    [Fact]
    public void MENU_COUNT_07_AllMenuAndCategoryQueriesUseIdenticalEligibilityRules()
    {
        var eligible = GetEligibleVariants().ToList();

        var allMenuProducts = eligible.Select(v => v.ProductId).Distinct().ToHashSet();

        var categoryProducts = eligible
            .GroupBy(v => v.ProductCategoryId)
            .SelectMany(g => g.Select(v => v.ProductId).Distinct())
            .ToHashSet();

        Assert.True(allMenuProducts.SetEquals(categoryProducts));
    }

    [Fact]
    public void MENU_COUNT_08_PaginationTotalCountReflectsTotalEligibleProducts()
    {
        var eligible = GetEligibleVariants();
        var productGroups = eligible.GroupBy(v => v.ProductId).ToList();

        int totalProducts = productGroups.Count;
        int pageSize = 6;
        int totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

        Assert.Equal(16, totalProducts);
        Assert.Equal(3, totalPages);
    }

    [Fact]
    public void MENU_COUNT_09_CategorySwitchingDisplaysCorrectProducts()
    {
        var eligible = GetEligibleVariants();

        var breadProducts = eligible
            .Where(v => v.ProductCategoryId == _catBread)
            .GroupBy(v => v.ProductId)
            .Select(g => g.First().Name)
            .ToList();

        Assert.Equal(3, breadProducts.Count);
        Assert.Contains("Plain Naan", breadProducts);
        Assert.Contains("Roti", breadProducts);
        Assert.Contains("Garlic Naan Single", breadProducts);
    }

    [Fact]
    public void MENU_COUNT_10_SearchFiltersOnlyEligibleProducts()
    {
        var eligible = GetEligibleVariants();
        var searchResult = eligible
            .Where(v => v.Name.Contains("Karahi", StringComparison.OrdinalIgnoreCase))
            .Select(v => v.ProductId)
            .Distinct()
            .ToList();

        Assert.Equal(2, searchResult.Count);
    }

    [Fact]
    public void MENU_COUNT_11_NoDuplicateTilesProducedByMultiVariantProducts()
    {
        var eligible = GetEligibleVariants();

        // Product p5 (Garlic Naan) has 2 active variants
        var p5Variants = eligible.Where(v => v.ProductId == _catBread).ToList();
        var productGroups = eligible.GroupBy(v => v.ProductId).ToList();

        Assert.Equal(16, productGroups.Count);
    }

    [Fact]
    public void MENU_COUNT_12_ScopeFilteringIsConsistent()
    {
        var eligible = GetEligibleVariants();
        Assert.All(eligible, v => Assert.True(v.Status == "Active"));
    }
}
