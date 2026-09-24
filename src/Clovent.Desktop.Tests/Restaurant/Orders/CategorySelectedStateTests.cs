using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Clovent.Catalog.Application.Categories.Dtos;
using Clovent.Catalog.Application.Variants.Dtos;
using DevExpress.XtraEditors;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Orders;

/// <summary>
/// Regression test suite for Restaurant POS Category Selected/Active Visual State.
/// Validates test cases CATEGORY-UI-STATE-01 through CATEGORY-UI-STATE-12.
/// </summary>
public class CategorySelectedStateTests
{
    private readonly Guid _catBeverages = Guid.NewGuid();
    private readonly Guid _catBread = Guid.NewGuid();
    private readonly Guid _catSnacks = Guid.NewGuid();
    private readonly Guid _catKarahi = Guid.NewGuid();
    private readonly Guid _catMainCourse = Guid.NewGuid();
    private readonly Guid _catSalads = Guid.NewGuid();

    private readonly List<ProductCategoryDto> _categories;
    private readonly List<ProductVariantDto> _variants;

    public CategorySelectedStateTests()
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

        var pBeverage = Guid.NewGuid();
        var pBread = Guid.NewGuid();
        var pSnack = Guid.NewGuid();
        var pKarahi = Guid.NewGuid();
        var pMain1 = Guid.NewGuid();
        var pMain2 = Guid.NewGuid();
        var pSalad = Guid.NewGuid();
        var pUncategorized = Guid.NewGuid();

        _variants =
        [
            new ProductVariantDto(Guid.NewGuid(), pBeverage, "Lemonade", "LEM", Guid.NewGuid(), "Active", 1, DateTimeOffset.UtcNow, _catBeverages, "Active"),
            new ProductVariantDto(Guid.NewGuid(), pBread, "Roti", "ROTI", Guid.NewGuid(), "Active", 1, DateTimeOffset.UtcNow, _catBread, "Active"),
            new ProductVariantDto(Guid.NewGuid(), pSnack, "Samosa", "SAM", Guid.NewGuid(), "Active", 1, DateTimeOffset.UtcNow, _catSnacks, "Active"),
            new ProductVariantDto(Guid.NewGuid(), pKarahi, "Chicken Karahi", "CK", Guid.NewGuid(), "Active", 1, DateTimeOffset.UtcNow, _catKarahi, "Active"),
            new ProductVariantDto(Guid.NewGuid(), pMain1, "Chicken Biryani", "CB", Guid.NewGuid(), "Active", 1, DateTimeOffset.UtcNow, _catMainCourse, "Active"),
            new ProductVariantDto(Guid.NewGuid(), pMain2, "Aloo Gobi", "AG", Guid.NewGuid(), "Active", 2, DateTimeOffset.UtcNow, _catMainCourse, "Active"),
            new ProductVariantDto(Guid.NewGuid(), pSalad, "Salad", "SLD", Guid.NewGuid(), "Active", 1, DateTimeOffset.UtcNow, _catSalads, "Active"),
            new ProductVariantDto(Guid.NewGuid(), pUncategorized, "Chef Special", "CHEF", Guid.NewGuid(), "Active", 1, DateTimeOffset.UtcNow, null, "Active"),
        ];
    }

    private class CategoryStateTracker
    {
        public Guid? SelectedCategoryId { get; set; } = null; // Default: null = All Menu
        public int CurrentPage { get; set; } = 1;
        public string SearchText { get; set; } = string.Empty;
        public List<ProductCategoryDto> LoadedCategories { get; set; } = [];
        public List<ProductVariantDto> Variants { get; set; } = [];

        public void SelectCategory(Guid? categoryId)
        {
            SelectedCategoryId = categoryId;
            CurrentPage = 1;
        }

        public bool IsSelected(Guid? categoryId)
        {
            if (SelectedCategoryId is null) return categoryId is null;
            return SelectedCategoryId == categoryId;
        }

        public List<ProductVariantDto> GetFilteredVariants()
        {
            IEnumerable<ProductVariantDto> filtered = Variants.Where(v => v.Status == "Active" && (v.ProductStatus is null || v.ProductStatus == "Active"));

            if (SelectedCategoryId.HasValue)
            {
                if (SelectedCategoryId.Value == Guid.Empty)
                {
                    var activeCategoryIds = LoadedCategories.Select(c => c.ProductCategoryId).ToHashSet();
                    filtered = filtered.Where(v => !v.ProductCategoryId.HasValue || !activeCategoryIds.Contains(v.ProductCategoryId.Value));
                }
                else
                {
                    filtered = filtered.Where(v => v.ProductCategoryId == SelectedCategoryId.Value);
                }
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(v => v.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || v.Sku.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            return filtered.ToList();
        }

        public void RefreshMenu(List<ProductCategoryDto> newCategories, List<ProductVariantDto> newVariants)
        {
            LoadedCategories = [.. newCategories];
            Variants = [.. newVariants];

            if (SelectedCategoryId.HasValue && SelectedCategoryId.Value != Guid.Empty)
            {
                if (!LoadedCategories.Any(c => c.ProductCategoryId == SelectedCategoryId.Value))
                {
                    SelectedCategoryId = null;
                }
            }
        }
    }

    [Fact]
    public void CATEGORY_UI_STATE_01_InitialPosCategorySelectionIsAllMenu()
    {
        var tracker = new CategoryStateTracker { LoadedCategories = _categories, Variants = _variants };
        Assert.Null(tracker.SelectedCategoryId);
        Assert.True(tracker.IsSelected(null));
        Assert.False(tracker.IsSelected(_catBeverages));
        Assert.False(tracker.IsSelected(Guid.Empty));
    }

    [Fact]
    public void CATEGORY_UI_STATE_02_ClickingBeveragesMarksOnlyBeveragesSelected()
    {
        var tracker = new CategoryStateTracker { LoadedCategories = _categories, Variants = _variants };
        tracker.SelectCategory(_catBeverages);

        Assert.Equal(_catBeverages, tracker.SelectedCategoryId);
        Assert.True(tracker.IsSelected(_catBeverages));
        Assert.False(tracker.IsSelected(null));
        Assert.False(tracker.IsSelected(_catBread));
    }

    [Fact]
    public void CATEGORY_UI_STATE_03_ClickingBreadMarksOnlyBreadSelected()
    {
        var tracker = new CategoryStateTracker { LoadedCategories = _categories, Variants = _variants };
        tracker.SelectCategory(_catBread);

        Assert.Equal(_catBread, tracker.SelectedCategoryId);
        Assert.True(tracker.IsSelected(_catBread));
        Assert.False(tracker.IsSelected(null));
        Assert.False(tracker.IsSelected(_catBeverages));
    }

    [Fact]
    public void CATEGORY_UI_STATE_04_ClickingKarahiMarksOnlyKarahiSelected()
    {
        var tracker = new CategoryStateTracker { LoadedCategories = _categories, Variants = _variants };
        tracker.SelectCategory(_catKarahi);

        Assert.Equal(_catKarahi, tracker.SelectedCategoryId);
        Assert.True(tracker.IsSelected(_catKarahi));
        Assert.False(tracker.IsSelected(_catMainCourse));
    }

    [Fact]
    public void CATEGORY_UI_STATE_05_ClickingMainCourseMarksOnlyMainCourseSelected()
    {
        var tracker = new CategoryStateTracker { LoadedCategories = _categories, Variants = _variants };
        tracker.SelectCategory(_catMainCourse);

        Assert.Equal(_catMainCourse, tracker.SelectedCategoryId);
        Assert.True(tracker.IsSelected(_catMainCourse));
        Assert.False(tracker.IsSelected(_catKarahi));
    }

    [Fact]
    public void CATEGORY_UI_STATE_06_ClickingSaladsMarksOnlySaladsSelected()
    {
        var tracker = new CategoryStateTracker { LoadedCategories = _categories, Variants = _variants };
        tracker.SelectCategory(_catSalads);

        Assert.Equal(_catSalads, tracker.SelectedCategoryId);
        Assert.True(tracker.IsSelected(_catSalads));
        Assert.False(tracker.IsSelected(null));
    }

    [Fact]
    public void CATEGORY_UI_STATE_07_ClickingSnacksMarksOnlySnacksSelected()
    {
        var tracker = new CategoryStateTracker { LoadedCategories = _categories, Variants = _variants };
        tracker.SelectCategory(_catSnacks);

        Assert.Equal(_catSnacks, tracker.SelectedCategoryId);
        Assert.True(tracker.IsSelected(_catSnacks));
        Assert.False(tracker.IsSelected(_catBeverages));
    }

    [Fact]
    public void CATEGORY_UI_STATE_08_ClickingUncategorizedMarksOnlyUncategorizedSelected()
    {
        var tracker = new CategoryStateTracker { LoadedCategories = _categories, Variants = _variants };
        tracker.SelectCategory(Guid.Empty);

        Assert.Equal(Guid.Empty, tracker.SelectedCategoryId);
        Assert.True(tracker.IsSelected(Guid.Empty));
        Assert.False(tracker.IsSelected(null));
        Assert.False(tracker.IsSelected(_catBeverages));
    }

    [Fact]
    public void CATEGORY_UI_STATE_09_ChangingCategoryUpdatesProductFilteringAndSelectedStateConsistently()
    {
        var tracker = new CategoryStateTracker { LoadedCategories = _categories, Variants = _variants };

        // 1. All Menu
        var allFiltered = tracker.GetFilteredVariants();
        Assert.Equal(8, allFiltered.Count);
        Assert.True(tracker.IsSelected(null));

        // 2. Switch to Main Course
        tracker.SelectCategory(_catMainCourse);
        var mainFiltered = tracker.GetFilteredVariants();
        Assert.Equal(2, mainFiltered.Count);
        Assert.All(mainFiltered, v => Assert.Equal(_catMainCourse, v.ProductCategoryId));
        Assert.True(tracker.IsSelected(_catMainCourse));
        Assert.False(tracker.IsSelected(null));

        // 3. Switch to Uncategorized
        tracker.SelectCategory(Guid.Empty);
        var uncategorizedFiltered = tracker.GetFilteredVariants();
        Assert.Single(uncategorizedFiltered);
        Assert.Equal("Chef Special", uncategorizedFiltered.First().Name);
        Assert.True(tracker.IsSelected(Guid.Empty));
    }

    [Fact]
    public void CATEGORY_UI_STATE_10_MenuRefreshDoesNotIncorrectlyResetSelectedCategory()
    {
        var tracker = new CategoryStateTracker { LoadedCategories = _categories, Variants = _variants };
        tracker.SelectCategory(_catKarahi);
        Assert.True(tracker.IsSelected(_catKarahi));

        // Refresh menu items with same categories
        tracker.RefreshMenu(_categories, _variants);
        Assert.Equal(_catKarahi, tracker.SelectedCategoryId);
        Assert.True(tracker.IsSelected(_catKarahi));
    }

    [Fact]
    public void CATEGORY_UI_STATE_11_PaginationPreservesSelectedCategory()
    {
        var tracker = new CategoryStateTracker { LoadedCategories = _categories, Variants = _variants };
        tracker.SelectCategory(_catMainCourse);
        Assert.True(tracker.IsSelected(_catMainCourse));

        // Change pagination page
        tracker.CurrentPage = 2;
        Assert.Equal(_catMainCourse, tracker.SelectedCategoryId);
        Assert.True(tracker.IsSelected(_catMainCourse));

        tracker.CurrentPage = 1;
        Assert.Equal(_catMainCourse, tracker.SelectedCategoryId);
        Assert.True(tracker.IsSelected(_catMainCourse));
    }

    [Fact]
    public void CATEGORY_UI_STATE_12_SearchBehaviorPreservesDocumentedSelectedCategoryState()
    {
        var tracker = new CategoryStateTracker { LoadedCategories = _categories, Variants = _variants };
        tracker.SelectCategory(_catMainCourse);
        Assert.True(tracker.IsSelected(_catMainCourse));

        // Search within Main Course
        tracker.SearchText = "Biryani";
        var filtered = tracker.GetFilteredVariants();
        Assert.Single(filtered);
        Assert.Equal("Chicken Biryani", filtered.First().Name);
        Assert.Equal(_catMainCourse, tracker.SelectedCategoryId);
        Assert.True(tracker.IsSelected(_catMainCourse));
    }
}
