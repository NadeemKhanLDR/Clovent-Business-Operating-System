using Clovent.Desktop.Restaurant.Orders;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Orders;

/// <summary>
/// Tests the "table selection is Dine-In intent" policy used by
/// RestaurantPosForm.OnTableSelectedAsync: selecting a valid table with no
/// working order auto-starts Dine-In; "(No Table)" and an existing working
/// order must not trigger creation.
/// </summary>
public sealed class TableSelectionDineInPolicyTests
{
    [Fact]
    public void ValidTable_NoWorkingOrder_AutoStartsDineIn()
    {
        Assert.True(TableSelectionDineInPolicy.ShouldAutoStartDineIn(hasWorkingOrder: false, selectedTableId: Guid.NewGuid()));
    }

    [Fact]
    public void NoTableSelection_DoesNotStartDineIn()
    {
        Assert.False(TableSelectionDineInPolicy.ShouldAutoStartDineIn(hasWorkingOrder: false, selectedTableId: null));
    }

    [Fact]
    public void ExistingWorkingOrder_TableChange_NeverDuplicates()
    {
        Assert.False(TableSelectionDineInPolicy.ShouldAutoStartDineIn(hasWorkingOrder: true, selectedTableId: Guid.NewGuid()));
    }

    [Fact]
    public void EmptyDineInOrder_ZeroLinesAndPayments_CancelsOnClear()
    {
        Assert.True(TableSelectionDineInPolicy.ShouldCancelEmptyDraftOnClear("DineIn", lineCount: 0, paymentCount: 0));
    }

    [Fact]
    public void DineInOrder_WithLines_DoesNotCancelOnClear()
    {
        Assert.False(TableSelectionDineInPolicy.ShouldCancelEmptyDraftOnClear("DineIn", lineCount: 1, paymentCount: 0));
        Assert.False(TableSelectionDineInPolicy.ShouldCancelEmptyDraftOnClear("DineIn", lineCount: 5, paymentCount: 0));
    }

    [Fact]
    public void DineInOrder_WithPayments_DoesNotCancelOnClear()
    {
        Assert.False(TableSelectionDineInPolicy.ShouldCancelEmptyDraftOnClear("DineIn", lineCount: 0, paymentCount: 1));
    }

    [Fact]
    public void TakeAwayOrder_Empty_DoesNotCancelOnClear()
    {
        Assert.False(TableSelectionDineInPolicy.ShouldCancelEmptyDraftOnClear("TakeAway", lineCount: 0, paymentCount: 0));
    }

    [Fact]
    public void NoOrder_DoesNotCancelOnClear()
    {
        Assert.False(TableSelectionDineInPolicy.ShouldCancelEmptyDraftOnClear(null, lineCount: 0, paymentCount: 0));
    }

    [Fact]
    public void FindFirstAvailableTable_ReturnsFirstActiveAndAvailable()
    {
        var tables = new[]
        {
            new { Id = 1, Status = "Active", Occupancy = "Occupied" },
            new { Id = 2, Status = "Inactive", Occupancy = "Available" },
            new { Id = 3, Status = "Active", Occupancy = "Reserved" },
            new { Id = 4, Status = "Active", Occupancy = "Available" },
            new { Id = 5, Status = "Active", Occupancy = "Available" },
        };

        var result = TableSelectionDineInPolicy.FindFirstAvailableTable(tables, t => t.Status, t => t.Occupancy);
        Assert.NotNull(result);
        Assert.Equal(4, result.Id);
    }

    [Fact]
    public void FindFirstAvailableTable_WhenAllOccupiedOrInactive_ReturnsNull()
    {
        var tables = new[]
        {
            new { Id = 1, Status = "Active", Occupancy = "Occupied" },
            new { Id = 2, Status = "Inactive", Occupancy = "Available" },
            new { Id = 3, Status = "Active", Occupancy = "Reserved" },
            new { Id = 4, Status = "Active", Occupancy = "OutOfService" },
        };

        var result = TableSelectionDineInPolicy.FindFirstAvailableTable(tables, t => t.Status, t => t.Occupancy);
        Assert.Null(result);
    }

    [Fact]
    public void FindFirstAvailableTable_EmptyList_ReturnsNull()
    {
        var tables = Array.Empty<string>();
        var result = TableSelectionDineInPolicy.FindFirstAvailableTable(tables, t => t, t => t);
        Assert.Null(result);
    }

    [Theory]
    [InlineData(false, null, true)]
    [InlineData(false, "DineIn", true)]
    [InlineData(false, "All", true)]
    [InlineData(false, "TakeAway", false)]
    [InlineData(false, "takeaway", false)]
    [InlineData(true, null, false)]
    [InlineData(true, "DineIn", false)]
    [InlineData(true, "TakeAway", false)]
    public void ShouldAutoSelectTableOnItemTap_ReturnsExpected(bool hasWorkingOrder, string? filter, bool expected)
    {
        var actual = TableSelectionDineInPolicy.ShouldAutoSelectTableOnItemTap(hasWorkingOrder, filter);
        Assert.Equal(expected, actual);
    }
}
