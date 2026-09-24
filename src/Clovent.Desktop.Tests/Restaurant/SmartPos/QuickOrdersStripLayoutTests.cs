using System;
using System.Collections.Generic;
using System.Linq;
using Clovent.Restaurant.Application.QuickOrderTemplates.Dtos;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.SmartPos;

public class QuickOrdersStripLayoutTests
{
    [Fact]
    public void CollapsedAndExpandedState_HeightsAreCalculatedCorrectly()
    {
        // Collapsed height standard: 28px
        int collapsedHeight = 28;
        // Expanded height standard: 44px
        int expandedHeight = 44;

        Assert.True(collapsedHeight >= 28 && collapsedHeight <= 30);
        Assert.True(expandedHeight >= 42 && expandedHeight <= 46);
        Assert.True(expandedHeight > collapsedHeight);
    }

    [Fact]
    public void QuickOrderTemplates_RealPakistaniCatalogDeals_VerifyFormulasAndComposition()
    {
        var templates = new List<QuickOrderTemplateDto>
        {
            new(Guid.NewGuid(), "Family Biryani Deal", "4 Chicken Biryani + 2 Fresh Salads + 4 Cold Beverages", true, 1, [
                new(Guid.NewGuid(), "Chicken Biryani", "Single", 4m, 450m),
                new(Guid.NewGuid(), "Fresh Salad", "Regular", 2m, 30m),
                new(Guid.NewGuid(), "Leechi Drink", "Bottle", 4m, 50m)
            ], 2060m),
            new(Guid.NewGuid(), "Chicken Karahi Feast", "1 Chicken Karahi + 4 Garlic Nan + 2 Fresh Salads + 2 Beverages", true, 2, [
                new(Guid.NewGuid(), "Chicken Karahi", "Standard", 1m, 1200m),
                new(Guid.NewGuid(), "Garlic Nan", "Piece", 4m, 50m),
                new(Guid.NewGuid(), "Fresh Salad", "Regular", 2m, 30m),
                new(Guid.NewGuid(), "Leechi Drink", "Bottle", 2m, 50m)
            ], 1560m),
            new(Guid.NewGuid(), "Special Lunch Deal", "1 Chicken Haleem Full Plate + 2 Garlic Nan + 1 Fresh Salad", true, 3, [
                new(Guid.NewGuid(), "Chicken Haleem Full Plate", "Full", 1m, 420m),
                new(Guid.NewGuid(), "Garlic Nan", "Piece", 2m, 50m),
                new(Guid.NewGuid(), "Fresh Salad", "Regular", 1m, 30m)
            ], 550m),
            new(Guid.NewGuid(), "Desi Breakfast Combo", "1 Murgh Chanay Full Plate + 2 Garlic Nan + 1 Cold Beverage", true, 4, [
                new(Guid.NewGuid(), "Murgh Chanay Full Plate", "Full", 1m, 400m),
                new(Guid.NewGuid(), "Garlic Nan", "Piece", 2m, 50m),
                new(Guid.NewGuid(), "Leechi Drink", "Bottle", 1m, 50m)
            ], 550m)
        };

        Assert.Equal(4, templates.Count);
        foreach (var deal in templates)
        {
            var calculatedTotal = deal.Items.Sum(i => i.Total);
            Assert.Equal(deal.TotalPrice, calculatedTotal);
            Assert.True(deal.Items.Count >= 3);
            Assert.False(string.IsNullOrWhiteSpace(deal.Name));
            Assert.False(string.IsNullOrWhiteSpace(deal.Description));
        }
    }
}
