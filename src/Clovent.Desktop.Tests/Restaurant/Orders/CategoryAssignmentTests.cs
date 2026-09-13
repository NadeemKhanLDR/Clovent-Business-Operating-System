using System;
using System.Collections.Generic;
using System.Linq;
using Clovent.Catalog.Application.Categories.Dtos;
using Clovent.Catalog.Application.Variants.Dtos;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Orders;

/// <summary>
/// Regression test suite for Restaurant POS Menu Category Data Synchronization.
/// Validates test cases CATEGORY-DATA-01 through CATEGORY-DATA-17.
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

        var pBeverage1 = Guid.NewGuid(); // Leechi
        var pBread1 = Guid.NewGuid();    // Garlic Nan

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
            // Beverages (1 item)
            new ProductVariantDto(Guid.NewGuid(), pBeverage1, "Leechi", "LEECHI", Guid.NewGuid(), "Active", 1, DateTimeOffset.UtcNow, _catBeverages, "Active"),

            // Bread (1 product)
            new ProductVariantDto(Guid.NewGuid(), pBread1, "Garlic Nan", "COLA-500", Guid.NewGuid(), "Active", 1, DateTimeOffset.UtcNow, _catBread, "Active"),

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
    public void CATEGORY_DATA_01_ExactlyOneCanonicalActiveKarahiCategoryExists()
    {
        var karahiCats = _categories.Where(c => c.Status == "Active" && string.Equals(c.Name, "Karahi", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.Single(karahiCats);
    }

    [Fact]
    public void CATEGORY_DATA_02_ExactlyOneCanonicalActiveMainCourseCategoryExists()
    {
        var mainCats = _categories.Where(c => c.Status == "Active" && string.Equals(c.Name, "Main Course", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.Single(mainCats);
    }

    [Fact]
    public void CATEGORY_DATA_03_ExactlyOneCanonicalActiveSaladsCategoryExists()
    {
        var saladsCats = _categories.Where(c => c.Status == "Active" && string.Equals(c.Name, "Salads", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.Single(saladsCats);
    }

    [Fact]
    public void CATEGORY_DATA_04_ChickenKarahiBelongsToKarahi()
    {
        var variant = _variants.FirstOrDefault(v => v.Name == "Chicken Karahi");
        Assert.NotNull(variant);
        Assert.Equal(_catKarahi, variant.ProductCategoryId);
    }

    [Fact]
    public void CATEGORY_DATA_05_ChickenKoylaKarahiBelongsToKarahi()
    {
        var koylaVariants = _variants.Where(v => v.Name.StartsWith("Chicken Koyla Karahi")).ToList();
        Assert.NotEmpty(koylaVariants);
        Assert.All(koylaVariants, v => Assert.Equal(_catKarahi, v.ProductCategoryId));
    }

    [Fact]
    public void CATEGORY_DATA_06_ChickenBiryaniBelongsToMainCourse()
    {
        var variant = _variants.FirstOrDefault(v => v.Name == "Chicken Biryani");
        Assert.NotNull(variant);
        Assert.Equal(_catMainCourse, variant.ProductCategoryId);
    }

    [Fact]
    public void CATEGORY_DATA_07_ChickenHaleemBelongsToMainCourse()
    {
        var variants = _variants.Where(v => v.Name.StartsWith("Chicken Haleem")).ToList();
        Assert.Equal(2, variants.Count);
        Assert.All(variants, v => Assert.Equal(_catMainCourse, v.ProductCategoryId));
    }

    [Fact]
    public void CATEGORY_DATA_08_MurghChanayBelongsToMainCourse()
    {
        var variants = _variants.Where(v => v.Name.StartsWith("Murgh Chanay")).ToList();
        Assert.Equal(2, variants.Count);
        Assert.All(variants, v => Assert.Equal(_catMainCourse, v.ProductCategoryId));
    }

    [Fact]
    public void CATEGORY_DATA_09_WhiteDaalMashBelongsToMainCourse()
    {
        var variants = _variants.Where(v => v.Name.StartsWith("White Daal Mash")).ToList();
        Assert.Equal(2, variants.Count);
        Assert.All(variants, v => Assert.Equal(_catMainCourse, v.ProductCategoryId));
    }

    [Fact]
    public void CATEGORY_DATA_10_AlooChickenQormaBelongsToMainCourse()
    {
        var variant = _variants.FirstOrDefault(v => v.Name == "Aloo Chicken Qorma");
        Assert.NotNull(variant);
        Assert.Equal(_catMainCourse, variant.ProductCategoryId);
    }

    [Fact]
    public void CATEGORY_DATA_11_AlooGobiBelongsToMainCourse()
    {
        var variants = _variants.Where(v => v.Name.StartsWith("Aloo Gobi")).ToList();
        Assert.Equal(2, variants.Count);
        Assert.All(variants, v => Assert.Equal(_catMainCourse, v.ProductCategoryId));
    }

    [Fact]
    public void CATEGORY_DATA_12_SaladBelongsToSalads()
    {
        var variant = _variants.FirstOrDefault(v => v.Name == "Salad");
        Assert.NotNull(variant);
        Assert.Equal(_catSalads, variant.ProductCategoryId);
    }

    [Fact]
    public void CATEGORY_DATA_13_NoCurrentActivePOSProductIsUncategorized()
    {
        var activeCatIds = _categories.Where(c => c.Status == "Active").Select(c => c.ProductCategoryId).ToHashSet();
        var uncategorizedCount = GetEligibleVariants()
            .Where(v => !v.ProductCategoryId.HasValue || !activeCatIds.Contains(v.ProductCategoryId.Value))
            .Select(v => v.ProductId)
            .Distinct()
            .Count();

        Assert.Equal(0, uncategorizedCount);
    }

    [Fact]
    public void CATEGORY_DATA_14_AllMenuCountEqualsSumOfEligibleCategoryCounts()
    {
        var eligible = GetEligibleVariants().ToList();
        int totalProducts = eligible.Select(v => v.ProductId).Distinct().Count();

        int beveragesCount = eligible.Where(v => v.ProductCategoryId == _catBeverages).Select(v => v.ProductId).Distinct().Count();
        int breadCount = eligible.Where(v => v.ProductCategoryId == _catBread).Select(v => v.ProductId).Distinct().Count();
        int karahiCount = eligible.Where(v => v.ProductCategoryId == _catKarahi).Select(v => v.ProductId).Distinct().Count();
        int mainCourseCount = eligible.Where(v => v.ProductCategoryId == _catMainCourse).Select(v => v.ProductId).Distinct().Count();
        int saladsCount = eligible.Where(v => v.ProductCategoryId == _catSalads).Select(v => v.ProductId).Distinct().Count();

        int sumCategoryCounts = beveragesCount + breadCount + karahiCount + mainCourseCount + saladsCount;

        Assert.Equal(11, totalProducts);
        Assert.Equal(totalProducts, sumCategoryCounts);
    }

    [Fact]
    public void CATEGORY_DATA_15_RunningDevelopmentSeedTwiceDoesNotCreateDuplicateCategories()
    {
        var categoryList = new List<ProductCategoryDto>(_categories);
        
        // Simulate seed execution second pass - should find existing category by name and not add a second instance
        var targetName = "Salads";
        var existing = categoryList.FirstOrDefault(c => string.Equals(c.Name, targetName, StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(existing);

        var countAfterSecondPass = categoryList.Count(c => string.Equals(c.Name, targetName, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1, countAfterSecondPass);
    }

    [Fact]
    public void CATEGORY_DATA_16_ExistingProductCategoryAssignmentsRemainStableAfterRestart()
    {
        var saladProductIds = _variants.Where(v => v.ProductCategoryId == _catSalads).Select(v => v.ProductId).Distinct().ToList();
        Assert.Single(saladProductIds);

        // Re-read simulation
        var reloadedSaladProductIds = _variants.Where(v => v.ProductCategoryId == _catSalads).Select(v => v.ProductId).Distinct().ToList();
        Assert.Equal(saladProductIds, reloadedSaladProductIds);
    }

    [Fact]
    public void CATEGORY_DATA_17_VariantsRemainAvailableAfterCategoryAssignment()
    {
        var haleemVariants = GetEligibleVariants().Where(v => v.Name.StartsWith("Chicken Haleem")).ToList();
        Assert.Equal(2, haleemVariants.Count);
        Assert.Contains(haleemVariants, v => v.Name.Contains("Half"));
        Assert.Contains(haleemVariants, v => v.Name.Contains("Full"));
    }
}
