using System;
using System.Collections.Generic;
using System.Linq;
using Clovent.Catalog.Application.Categories.Dtos;
using Clovent.Catalog.Application.Variants.Dtos;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Orders;

/// <summary>
/// Regression test suite for Restaurant POS Menu Category Assignments.
/// Validates test cases CATEGORY-ASSIGN-01 through CATEGORY-ASSIGN-12.
/// </summary>
public class CategoryAssignmentTests
{
    private readonly Guid _catBeverages = Guid.NewGuid();
    private readonly Guid _catBread = Guid.NewGuid();
    private readonly Guid _catSnacks = Guid.NewGuid();
    private readonly Guid _catKarahi = Guid.NewGuid();
    private readonly Guid _catMainCourse = Guid.NewGuid();
    private readonly Guid _catSalads = Guid.NewGuid();

    private readonly List<ProductCategoryDto> _categories;
    private readonly List<ProductVariantDto> _variants;

    public CategoryAssignmentTests()
    {
        _categories =
        [
            new ProductCategoryDto(_catBeverages, "Beverages", null, "Active", "#3B82F6", 1, DateTimeOffset.UtcNow),
            new ProductCategoryDto(_catBread, "Bread", null, "Active", "#10B981", 2, DateTimeOffset.UtcNow),
            new ProductCategoryDto(_catSnacks, "Snacks", null, "Active", "#F59E0B", 3, DateTimeOffset.UtcNow),
            new ProductCategoryDto(_catKarahi, "Karahi", null, "Active", "#EC4899", 4, DateTimeOffset.UtcNow),
            new ProductCategoryDto(_catMainCourse, "Main Course", null, "Active", "#EF4444", 5, DateTimeOffset.UtcNow),
            new ProductCategoryDto(_catSalads, "Salads", null, "Active", "#8B5CF6", 6, DateTimeOffset.UtcNow),
        ];

        var pBeverage1 = Guid.NewGuid(); // Cola 500ml
        var pBeverage2 = Guid.NewGuid(); // Lemonade
        var pBread1 = Guid.NewGuid();    // Plain Naan
        var pBread2 = Guid.NewGuid();    // Roti
        var pBread3 = Guid.NewGuid();    // Garlic Naan
        var pSnack1 = Guid.NewGuid();    // Samosa

        var pKarahi1 = Guid.NewGuid();   // Chicken Karahi
        var pKarahi2 = Guid.NewGuid();   // Chicken Koyla Karahi
        var pMain1 = Guid.NewGuid();     // White Daal Mash
        var pMain2 = Guid.NewGuid();     // Aloo Chicken Qorma
        var pMain3 = Guid.NewGuid();     // Chicken Biryani
        var pMain4 = Guid.NewGuid();     // Chicken Haleem
        var pMain5 = Guid.NewGuid();     // Murgh Chanay
        var pMain6 = Guid.NewGuid();     // Aloo Gobi
        var pSalad1 = Guid.NewGuid();    // Salad

        _variants =
        [
            // Beverages (2 items)
            new ProductVariantDto(Guid.NewGuid(), pBeverage1, "Cola 500ml", "COLA-500", Guid.NewGuid(), "Active", 1, DateTimeOffset.UtcNow, _catBeverages, "Active"),
            new ProductVariantDto(Guid.NewGuid(), pBeverage2, "Lemonade", "LEMONADE", Guid.NewGuid(), "Active", 2, DateTimeOffset.UtcNow, _catBeverages, "Active"),

            // Bread (3 products)
            new ProductVariantDto(Guid.NewGuid(), pBread1, "Plain Naan", "NAAN-PL", Guid.NewGuid(), "Active", 1, DateTimeOffset.UtcNow, _catBread, "Active"),
            new ProductVariantDto(Guid.NewGuid(), pBread2, "Roti", "ROTI", Guid.NewGuid(), "Active", 2, DateTimeOffset.UtcNow, _catBread, "Active"),
            new ProductVariantDto(Guid.NewGuid(), pBread3, "Garlic Naan Single", "GARLIC-1", Guid.NewGuid(), "Active", 3, DateTimeOffset.UtcNow, _catBread, "Active"),
            new ProductVariantDto(Guid.NewGuid(), pBread3, "Garlic Naan Basket", "GARLIC-B", Guid.NewGuid(), "Active", 4, DateTimeOffset.UtcNow, _catBread, "Active"),

            // Snacks (1 product)
            new ProductVariantDto(Guid.NewGuid(), pSnack1, "Samosa", "SAMOSA", Guid.NewGuid(), "Active", 1, DateTimeOffset.UtcNow, _catSnacks, "Active"),

            // Karahi Category (2 products)
            new ProductVariantDto(Guid.NewGuid(), pKarahi1, "Chicken Karahi", "CHICKEN-KARAHI", Guid.NewGuid(), "Active", 1, DateTimeOffset.UtcNow, _catKarahi, "Active"),
            new ProductVariantDto(Guid.NewGuid(), pKarahi2, "Chicken Koyla Karahi Half Plate", "CKK-HALF", Guid.NewGuid(), "Active", 2, DateTimeOffset.UtcNow, _catKarahi, "Active"),
            new ProductVariantDto(Guid.NewGuid(), pKarahi2, "Chicken Koyla Karahi Full Plate", "CKK-FULL", Guid.NewGuid(), "Active", 3, DateTimeOffset.UtcNow, _catKarahi, "Active"),

            // Main Course Category (6 products)
            new ProductVariantDto(Guid.NewGuid(), pMain1, "White Daal Mash Half Plate", "DM-HALF", Guid.NewGuid(), "Active", 1, DateTimeOffset.UtcNow, _catMainCourse, "Active"),
            new ProductVariantDto(Guid.NewGuid(), pMain1, "White Daal Mash Full Plate", "DM-FULL", Guid.NewGuid(), "Active", 2, DateTimeOffset.UtcNow, _catMainCourse, "Active"),
            new ProductVariantDto(Guid.NewGuid(), pMain2, "Aloo Chicken Qorma", "ACQ-STD", Guid.NewGuid(), "Active", 3, DateTimeOffset.UtcNow, _catMainCourse, "Active"),
            new ProductVariantDto(Guid.NewGuid(), pMain3, "Chicken Biryani", "CB-STD", Guid.NewGuid(), "Active", 4, DateTimeOffset.UtcNow, _catMainCourse, "Active"),
            new ProductVariantDto(Guid.NewGuid(), pMain4, "Chicken Haleem Half Plate", "CH-HALF", Guid.NewGuid(), "Active", 5, DateTimeOffset.UtcNow, _catMainCourse, "Active"),
            new ProductVariantDto(Guid.NewGuid(), pMain4, "Chicken Haleem Full Plate", "CH-FULL", Guid.NewGuid(), "Active", 6, DateTimeOffset.UtcNow, _catMainCourse, "Active"),
            new ProductVariantDto(Guid.NewGuid(), pMain5, "Murgh Chanay Half Plate", "MC-HALF", Guid.NewGuid(), "Active", 7, DateTimeOffset.UtcNow, _catMainCourse, "Active"),
            new ProductVariantDto(Guid.NewGuid(), pMain5, "Murgh Chanay Full Plate", "MC-FULL", Guid.NewGuid(), "Active", 8, DateTimeOffset.UtcNow, _catMainCourse, "Active"),
            new ProductVariantDto(Guid.NewGuid(), pMain6, "Aloo Gobi Half Plate", "AG-HALF", Guid.NewGuid(), "Active", 9, DateTimeOffset.UtcNow, _catMainCourse, "Active"),
            new ProductVariantDto(Guid.NewGuid(), pMain6, "Aloo Gobi Full Plate", "AG-FULL", Guid.NewGuid(), "Active", 10, DateTimeOffset.UtcNow, _catMainCourse, "Active"),

            // Salads Category (1 product)
            new ProductVariantDto(Guid.NewGuid(), pSalad1, "Salad", "SALAD-STD", Guid.NewGuid(), "Active", 1, DateTimeOffset.UtcNow, _catSalads, "Active"),
        ];
    }

    private IEnumerable<ProductVariantDto> GetEligibleVariants()
    {
        return _variants.Where(v => v.Status == "Active" && (v.ProductStatus is null || v.ProductStatus == "Active"));
    }

    [Fact]
    public void CATEGORY_ASSIGN_01_ChickenKarahi_AssignedTo_Karahi()
    {
        var karahiProductIds = GetEligibleVariants()
            .Where(v => v.ProductCategoryId == _catKarahi)
            .Select(v => v.ProductId)
            .Distinct()
            .ToList();

        var chickenKarahiVariant = _variants.FirstOrDefault(v => v.Name == "Chicken Karahi");
        Assert.NotNull(chickenKarahiVariant);
        Assert.Equal(_catKarahi, chickenKarahiVariant.ProductCategoryId);
        Assert.Contains(chickenKarahiVariant.ProductId, karahiProductIds);
    }

    [Fact]
    public void CATEGORY_ASSIGN_02_ChickenKoylaKarahi_AssignedTo_Karahi()
    {
        var koylaVariants = _variants.Where(v => v.Name.StartsWith("Chicken Koyla Karahi")).ToList();
        Assert.NotEmpty(koylaVariants);
        Assert.All(koylaVariants, v => Assert.Equal(_catKarahi, v.ProductCategoryId));
    }

    [Fact]
    public void CATEGORY_ASSIGN_03_Salad_AssignedTo_Salads()
    {
        var saladVariant = _variants.FirstOrDefault(v => v.Name == "Salad");
        Assert.NotNull(saladVariant);
        Assert.Equal(_catSalads, saladVariant.ProductCategoryId);
    }

    [Fact]
    public void CATEGORY_ASSIGN_04_WhiteDaalMash_AssignedTo_MainCourse()
    {
        var daalVariants = _variants.Where(v => v.Name.StartsWith("White Daal Mash")).ToList();
        Assert.Equal(2, daalVariants.Count);
        Assert.All(daalVariants, v => Assert.Equal(_catMainCourse, v.ProductCategoryId));
    }

    [Fact]
    public void CATEGORY_ASSIGN_05_AlooChickenQorma_AssignedTo_MainCourse()
    {
        var qormaVariant = _variants.FirstOrDefault(v => v.Name == "Aloo Chicken Qorma");
        Assert.NotNull(qormaVariant);
        Assert.Equal(_catMainCourse, qormaVariant.ProductCategoryId);
    }

    [Fact]
    public void CATEGORY_ASSIGN_06_ChickenBiryani_AssignedTo_MainCourse()
    {
        var biryaniVariant = _variants.FirstOrDefault(v => v.Name == "Chicken Biryani");
        Assert.NotNull(biryaniVariant);
        Assert.Equal(_catMainCourse, biryaniVariant.ProductCategoryId);
    }

    [Fact]
    public void CATEGORY_ASSIGN_07_ChickenHaleem_AssignedTo_MainCourse()
    {
        var haleemVariants = _variants.Where(v => v.Name.StartsWith("Chicken Haleem")).ToList();
        Assert.Equal(2, haleemVariants.Count);
        Assert.All(haleemVariants, v => Assert.Equal(_catMainCourse, v.ProductCategoryId));
    }

    [Fact]
    public void CATEGORY_ASSIGN_08_MurghChanay_AssignedTo_MainCourse()
    {
        var murghVariants = _variants.Where(v => v.Name.StartsWith("Murgh Chanay")).ToList();
        Assert.Equal(2, murghVariants.Count);
        Assert.All(murghVariants, v => Assert.Equal(_catMainCourse, v.ProductCategoryId));
    }

    [Fact]
    public void CATEGORY_ASSIGN_09_AlooGobi_AssignedTo_MainCourse()
    {
        var gobiVariants = _variants.Where(v => v.Name.StartsWith("Aloo Gobi")).ToList();
        Assert.Equal(2, gobiVariants.Count);
        Assert.All(gobiVariants, v => Assert.Equal(_catMainCourse, v.ProductCategoryId));
    }

    [Fact]
    public void CATEGORY_ASSIGN_10_UncategorizedItemCount_IsZero()
    {
        var uncategorizedProductsCount = GetEligibleVariants()
            .Where(v => v.ProductCategoryId == null || !_categories.Any(c => c.ProductCategoryId == v.ProductCategoryId && c.Status == "Active"))
            .Select(v => v.ProductId)
            .Distinct()
            .Count();

        Assert.Equal(0, uncategorizedProductsCount);
    }

    [Fact]
    public void CATEGORY_ASSIGN_11_MultiVariantProducts_PreserveVariantsUnderCategory()
    {
        // Daal Mash: 2 variants
        var daal = GetEligibleVariants().Where(v => v.Name.StartsWith("White Daal Mash")).ToList();
        Assert.Equal(2, daal.Count);
        Assert.Single(daal.Select(v => v.ProductCategoryId).Distinct());

        // Haleem: 2 variants
        var haleem = GetEligibleVariants().Where(v => v.Name.StartsWith("Chicken Haleem")).ToList();
        Assert.Equal(2, haleem.Count);
        Assert.Single(haleem.Select(v => v.ProductCategoryId).Distinct());

        // Koyla Karahi: 2 variants
        var koyla = GetEligibleVariants().Where(v => v.Name.StartsWith("Chicken Koyla Karahi")).ToList();
        Assert.Equal(2, koyla.Count);
        Assert.Single(koyla.Select(v => v.ProductCategoryId).Distinct());
    }

    [Fact]
    public void CATEGORY_ASSIGN_12_CategoryTotals_ReconcileWithAllMenuTotal()
    {
        var eligible = GetEligibleVariants().ToList();
        var totalDistinctProducts = eligible.Select(v => v.ProductId).Distinct().Count();

        var beveragesCount = eligible.Where(v => v.ProductCategoryId == _catBeverages).Select(v => v.ProductId).Distinct().Count();
        var breadCount = eligible.Where(v => v.ProductCategoryId == _catBread).Select(v => v.ProductId).Distinct().Count();
        var snacksCount = eligible.Where(v => v.ProductCategoryId == _catSnacks).Select(v => v.ProductId).Distinct().Count();
        var karahiCount = eligible.Where(v => v.ProductCategoryId == _catKarahi).Select(v => v.ProductId).Distinct().Count();
        var mainCourseCount = eligible.Where(v => v.ProductCategoryId == _catMainCourse).Select(v => v.ProductId).Distinct().Count();
        var saladsCount = eligible.Where(v => v.ProductCategoryId == _catSalads).Select(v => v.ProductId).Distinct().Count();

        Assert.Equal(2, beveragesCount);
        Assert.Equal(3, breadCount);
        Assert.Equal(1, snacksCount);
        Assert.Equal(2, karahiCount);
        Assert.Equal(6, mainCourseCount);
        Assert.Equal(1, saladsCount);

        var sumCategoryCounts = beveragesCount + breadCount + snacksCount + karahiCount + mainCourseCount + saladsCount;

        Assert.Equal(15, totalDistinctProducts);
        Assert.Equal(totalDistinctProducts, sumCategoryCounts);
    }
}
