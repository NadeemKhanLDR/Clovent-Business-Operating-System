using System;
using System.Collections.Generic;
using Clovent.Desktop.MasterData;
using Xunit;

namespace Clovent.Desktop.Tests.MasterData;

public sealed class EntityPickerConcurrencyTests
{
    [Fact]
    public void LoadItems_WithMultipleItems_FiresSelectionChangedExactlyOnce()
    {
        using var picker = new EntityPicker("Warehouse:", comboWidth: 260);
        picker.CreateControl();

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var items = new List<(Guid Id, string Display)>
        {
            (id1, "Main Warehouse"),
            (id2, "Kitchen Backup Warehouse")
        };

        var eventCount = 0;
        picker.SelectionChanged += (_, _) => eventCount++;

        picker.LoadItems(items);

        Assert.Equal(1, eventCount);
        Assert.Equal(id1, picker.SelectedId);
    }

    [Fact]
    public void LoadItems_WithEmptyList_FiresSelectionChangedExactlyOnce_AndSelectedIdIsNull()
    {
        using var picker = new EntityPicker("Warehouse:", comboWidth: 260);
        picker.CreateControl();

        var eventCount = 0;
        picker.SelectionChanged += (_, _) => eventCount++;

        picker.LoadItems([]);

        Assert.Equal(1, eventCount);
        Assert.Null(picker.SelectedId);
    }
}
