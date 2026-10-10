using System;
using System.Linq;
using Clovent.Desktop.Navigation;
using Xunit;

namespace Clovent.Desktop.Tests.Navigation;

public sealed class NavigationRegistryTests
{
    [Fact]
    public void OrderedPages_ContainsExactEightPagesInStrictOrder()
    {
        var expected = new[]
        {
            NavigationPage.Masters,
            NavigationPage.Inventory,
            NavigationPage.Purchases,
            NavigationPage.Pos,
            NavigationPage.ManagerPanel,
            NavigationPage.Users,
            NavigationPage.Reports,
            NavigationPage.Settings
        };

        Assert.Equal(expected, NavigationPage.OrderedPages);
        Assert.DoesNotContain("Home", NavigationPage.OrderedPages);
    }

    [Theory]
    [InlineData("Home")]
    [InlineData("Restaurant")]
    [InlineData("Catalog")]
    [InlineData("Administration")]
    public void ObsoletePages_AreNotPresentInOrderedPages(string obsoletePage)
    {
        Assert.DoesNotContain(obsoletePage, NavigationPage.OrderedPages);
    }

    [Fact]
    public void PermissionGatedPages_ExcludesMasters()
    {
        Assert.DoesNotContain(NavigationPage.Masters, NavigationPage.PermissionGatedPages);
        Assert.Contains(NavigationPage.Inventory, NavigationPage.PermissionGatedPages);
        Assert.Contains(NavigationPage.Purchases, NavigationPage.PermissionGatedPages);
        Assert.Contains(NavigationPage.Pos, NavigationPage.PermissionGatedPages);
        Assert.Contains(NavigationPage.ManagerPanel, NavigationPage.PermissionGatedPages);
        Assert.Contains(NavigationPage.Users, NavigationPage.PermissionGatedPages);
        Assert.Contains(NavigationPage.Reports, NavigationPage.PermissionGatedPages);
        Assert.Contains(NavigationPage.Settings, NavigationPage.PermissionGatedPages);
    }

    [Fact]
    public void AllItems_HaveNonEmptyKeysAndCaptions()
    {
        foreach (var item in NavigationRegistry.AllItems)
        {
            Assert.False(string.IsNullOrWhiteSpace(item.Key), $"Item {item.Caption} has empty Key");
            Assert.False(string.IsNullOrWhiteSpace(item.Caption), $"Item {item.Key} has empty Caption");
            Assert.False(string.IsNullOrWhiteSpace(item.RibbonGroup), $"Item {item.Key} has empty RibbonGroup");
            Assert.False(string.IsNullOrWhiteSpace(item.IconUri), $"Item {item.Key} has empty IconUri");
            Assert.Contains(item.RibbonPage, NavigationPage.OrderedPages);
        }
    }

    [Fact]
    public void NoDuplicateNavigationKeys_EnforcesAbsoluteSingleLocationRule()
    {
        var allKeys = NavigationRegistry.AllItems.Select(x => x.Key).ToList();
        var duplicates = allKeys.GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.Empty(duplicates);
    }

    [Theory]
    [InlineData("dashboard", NavigationPage.Masters, "Workspace")]
    [InlineData("pos", NavigationPage.Pos, "Operations")]
    [InlineData("customerreceivables", NavigationPage.ManagerPanel, "Financial / A/R")]
    [InlineData("endofday", NavigationPage.Reports, "Sales")]
    [InlineData("shifts", NavigationPage.Reports, "Operations")]
    [InlineData("orderhistory", NavigationPage.Pos, "Orders")]
    [InlineData("warehousestocks", NavigationPage.Inventory, "Stock")]
    [InlineData("inventorytransactions", NavigationPage.Inventory, "Movements")]
    [InlineData("activitylog", NavigationPage.Users, "Audit")]
    [InlineData("upsellperformance", NavigationPage.Reports, "Analytics")]
    [InlineData("branchsync", NavigationPage.ManagerPanel, "Operations / Controls")]
    [InlineData("quickbooks", NavigationPage.ManagerPanel, "Operations / Controls")]
    [InlineData("auditanalytics", NavigationPage.ManagerPanel, "Operations / Controls")]
    public void SingleCanonicalLocations_AreStrictlyEnforced(string key, string expectedPage, string expectedGroup)
    {
        var item = NavigationRegistry.AllItems.SingleOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(item);
        Assert.Equal(expectedPage, item.RibbonPage);
        Assert.Equal(expectedGroup, item.RibbonGroup);
    }

    [Fact]
    public void CustomerReceivables_OwnedByManagerPanel_AndNotOnReports()
    {
        var receivablesItems = NavigationRegistry.AllItems
            .Where(x => string.Equals(x.Key, "customerreceivables", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Exactly one canonical registration
        var canonical = Assert.Single(receivablesItems);
        Assert.Equal(NavigationPage.ManagerPanel, canonical.RibbonPage);
        Assert.Equal("Financial / A/R", canonical.RibbonGroup);

        // Must not exist anywhere under Reports
        var reportsReceivables = NavigationRegistry.AllItems
            .Where(x => x.RibbonPage == NavigationPage.Reports && string.Equals(x.Key, "customerreceivables", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.Empty(reportsReceivables);
    }

    [Fact]
    public void CanonicalCaptions_AreCorrect()
    {
        var itemQuickOrders = NavigationRegistry.AllItems.Single(x => x.Key == "quickordertemplates");
        Assert.Equal("Quick Order Templates", itemQuickOrders.Caption);

        var itemSmartCombos = NavigationRegistry.AllItems.Single(x => x.Key == "smartcombos");
        Assert.Equal("Smart Combo Builder", itemSmartCombos.Caption);

        var itemRecommendations = NavigationRegistry.AllItems.Single(x => x.Key == "recommendationrules");
        Assert.Equal("Recommendation Rules", itemRecommendations.Caption);

        var itemPosSetup = NavigationRegistry.AllItems.Single(x => x.Key == "restaurantsetup");
        Assert.Equal("POS Setup", itemPosSetup.Caption);
    }

    [Fact]
    public void AllItems_IconUrisAreValidDevExpressSvgFormats()
    {
        foreach (var item in NavigationRegistry.AllItems)
        {
            var uri = NavigationRegistry.GetIconUri(item.Key);
            Assert.False(string.IsNullOrWhiteSpace(uri));
            Assert.EndsWith(".svg", uri, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [InlineData("dashboard", NavigationPage.Masters)]
    [InlineData("customers", NavigationPage.Masters)]
    [InlineData("categories", NavigationPage.Masters)]
    [InlineData("products", NavigationPage.Masters)]
    [InlineData("warehousestocks", NavigationPage.Inventory)]
    [InlineData("pos", NavigationPage.Pos)]
    [InlineData("customerreceivables", NavigationPage.ManagerPanel)]
    [InlineData("quickordertemplates", NavigationPage.ManagerPanel)]
    [InlineData("endofday", NavigationPage.Reports)]
    [InlineData("shifts", NavigationPage.Reports)]
    [InlineData("recommendationrules", NavigationPage.ManagerPanel)]
    [InlineData("users", NavigationPage.Users)]
    [InlineData("roles", NavigationPage.Users)]
    [InlineData("upsellperformance", NavigationPage.Reports)]
    [InlineData("businesssettings", NavigationPage.Settings)]
    [InlineData("branchsync", NavigationPage.ManagerPanel)]
    [InlineData("quickbooks", NavigationPage.ManagerPanel)]
    [InlineData("auditanalytics", NavigationPage.ManagerPanel)]
    public void GetCanonicalPageForKey_ResolvesCorrectPage(string key, string expectedPage)
    {
        var actualPage = NavigationRegistry.GetCanonicalPageForKey(key);
        Assert.Equal(expectedPage, actualPage);
    }

    [Fact]
    public void PrimaryActions_AreAppropriatelyDesignated()
    {
        var primaryKeys = NavigationRegistry.AllItems
            .Where(x => x.IsPrimaryAction)
            .Select(x => x.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("dashboard", primaryKeys);
        Assert.Contains("pos", primaryKeys);
        Assert.Contains("customers", primaryKeys);
        Assert.Contains("endofday", primaryKeys);
        Assert.Contains("users", primaryKeys);
        Assert.Contains("businesssettings", primaryKeys);
    }

    [Fact]
    public void MastersPage_CategoriesAppearsBeforeMenuItems()
    {
        var categories = NavigationRegistry.AllItems.First(i => i.Key == "categories");
        var menuItems = NavigationRegistry.AllItems.First(i => i.Key == "menuitems");
        Assert.Equal(NavigationPage.Masters, categories.RibbonPage);
        Assert.Equal(NavigationPage.Masters, menuItems.RibbonPage);
        Assert.True(categories.Order < menuItems.Order, $"Categories order ({categories.Order}) must be less than Menu Items order ({menuItems.Order})");
    }
}
