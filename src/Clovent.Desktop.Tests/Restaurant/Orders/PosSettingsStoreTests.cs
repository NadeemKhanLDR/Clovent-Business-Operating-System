using System;
using System.IO;
using Clovent.Desktop.Forms.Base;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Orders;

public class PosSettingsStoreTests : IDisposable
{
    private readonly string _testSettingsPath;

    public PosSettingsStoreTests()
    {
        _testSettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Clovent", "pos_settings.json");

        // Clear any existing test settings
        if (File.Exists(_testSettingsPath))
        {
            try
            {
                File.Delete(_testSettingsPath);
            }
            catch
            {
                // Ignore
            }
        }
    }

    public void Dispose()
    {
        if (File.Exists(_testSettingsPath))
        {
            try
            {
                File.Delete(_testSettingsPath);
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
}
