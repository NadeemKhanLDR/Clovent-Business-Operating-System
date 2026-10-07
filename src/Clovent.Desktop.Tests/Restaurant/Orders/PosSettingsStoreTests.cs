using System;
using System.IO;
using Clovent.Desktop.Forms.Base;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Orders;

public class PosSettingsStoreTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _testSettingsPath;

    public PosSettingsStoreTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "cbos_pos_settings_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
        _testSettingsPath = Path.Combine(_testDir, "pos_settings.json");

        PosSettingsStore.SetTestingOverrides(_testSettingsPath);
    }

    public void Dispose()
    {
        PosSettingsStore.ResetTestingOverrides();

        if (Directory.Exists(_testDir))
        {
            try
            {
                Directory.Delete(_testDir, true);
            }
            catch
            {
                // Ignore
            }
        }
    }

    [Fact]
    public void LoadItemsPerRow_DefaultsToFour()
    {
        var val = PosSettingsStore.LoadItemsPerRow();
        Assert.Equal(4, val);
    }

    [Fact]
    public void SaveAndLoadItemsPerRow_PreservesValue()
    {
        PosSettingsStore.SaveItemsPerRow(6);
        var val = PosSettingsStore.LoadItemsPerRow();
        Assert.Equal(6, val);

        PosSettingsStore.SaveItemsPerRow(4);
        val = PosSettingsStore.LoadItemsPerRow();
        Assert.Equal(4, val);

        PosSettingsStore.SaveItemsPerRow(8);
        val = PosSettingsStore.LoadItemsPerRow();
        Assert.Equal(8, val);
    }

    [Fact]
    public void LoadViewMode_DefaultsToGrid()
    {
        var mode = PosSettingsStore.LoadViewMode();
        Assert.Equal("Grid", mode);
    }

    [Fact]
    public void SaveAndLoadViewMode_PreservesValue()
    {
        PosSettingsStore.SaveViewMode("List");
        var mode = PosSettingsStore.LoadViewMode();
        Assert.Equal("List", mode);

        PosSettingsStore.SaveViewMode("Grid");
        mode = PosSettingsStore.LoadViewMode();
        Assert.Equal("Grid", mode);
    }

    [Fact]
    public void LoadDefaultOrderMode_DefaultsToDineIn()
    {
        var mode = PosSettingsStore.LoadDefaultOrderMode();
        Assert.Equal("DineIn", mode);

        var type = PosSettingsStore.LoadDefaultOrderType();
        Assert.Equal(Clovent.Restaurant.Orders.OrderType.DineIn, type);
    }

    [Fact]
    public void SaveAndLoadDefaultOrderMode_PreservesValue()
    {
        PosSettingsStore.SaveDefaultOrderMode("TakeAway");
        Assert.Equal("TakeAway", PosSettingsStore.LoadDefaultOrderMode());
        Assert.Equal(Clovent.Restaurant.Orders.OrderType.TakeAway, PosSettingsStore.LoadDefaultOrderType());

        PosSettingsStore.SaveDefaultOrderType(Clovent.Restaurant.Orders.OrderType.Delivery);
        Assert.Equal("Delivery", PosSettingsStore.LoadDefaultOrderMode());
        Assert.Equal(Clovent.Restaurant.Orders.OrderType.Delivery, PosSettingsStore.LoadDefaultOrderType());

        PosSettingsStore.SaveDefaultOrderType(Clovent.Restaurant.Orders.OrderType.DineIn);
        Assert.Equal("DineIn", PosSettingsStore.LoadDefaultOrderMode());
    }
}
