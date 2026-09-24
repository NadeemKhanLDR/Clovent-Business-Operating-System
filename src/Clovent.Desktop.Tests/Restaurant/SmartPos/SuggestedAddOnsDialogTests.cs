using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Catalog.Variants;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.Desktop.Restaurant.SmartPos;
using Clovent.Desktop.Sessions;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Application.SmartRecommendations.Dtos;
using Clovent.Restaurant.Orders;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using MediatR;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.SmartPos;

/// <summary>
/// Unit and embedded-component tests for the Embedded Smart Upsell checked grid.
/// Validates row selection, Select All, Add Selected button captions,
/// partial selections, height calculations, and zero-suggestion behavior.
/// </summary>
public class SuggestedAddOnsDialogTests
{
    private static readonly Guid Leechi = Guid.NewGuid();
    private static readonly Guid HaleemHalf = Guid.NewGuid();
    private static readonly Guid GarlicNan = Guid.NewGuid();

    private static List<SuggestedAddOnRow> SampleRows() =>
    [
        new(Leechi, "Leechi", "-", 50m),
        new(HaleemHalf, "Chicken Haleem", "Half", 260m),
        new(GarlicNan, "Garlic Nan", "-", 50m),
    ];

    [Fact]
    public void ButtonText_ComposesSelectedCount()
    {
        Assert.Equal("Add Selected (0)", SuggestedAddOnUiHelper.ComposeAddSelectedText(0));
        Assert.Equal("Add Selected (3)", SuggestedAddOnUiHelper.ComposeAddSelectedText(3));
    }

    [Fact]
    public void SelectionRow_InitialState_NotSelected()
    {
        var row = new SuggestedAddOnSelectionRow(new SuggestedAddOnRow(Leechi, "Leechi", "-", 50m));
        Assert.False(row.Selected);
        Assert.Equal("Leechi", row.ItemName);
        Assert.Equal("-", row.PortionName);
        Assert.Equal("50.00", row.PriceText);
    }

    [Fact]
    public void CheckRows_TracksSelectedStateAccurately()
    {
        var rows = SampleRows().Select(r => new SuggestedAddOnSelectionRow(r)).ToList();

        rows[0].Selected = true;
        rows[1].Selected = true;
        rows[2].Selected = true;

        var selectedIds = rows.Where(r => r.Selected).Select(r => r.Row.VariantId).ToList();
        Assert.Equal(new[] { Leechi, HaleemHalf, GarlicNan }, selectedIds);

        // Uncheck one
        rows[2].Selected = false;
        selectedIds = rows.Where(r => r.Selected).Select(r => r.Row.VariantId).ToList();
        Assert.Equal(new[] { Leechi, HaleemHalf }, selectedIds);
    }

    [Fact]
    public void SelectAll_TogglesAllRows()
    {
        var rows = SampleRows().Select(r => new SuggestedAddOnSelectionRow(r)).ToList();

        // Select All
        foreach (var r in rows) r.Selected = true;
        Assert.All(rows, r => Assert.True(r.Selected));
        Assert.Equal(3, rows.Count(r => r.Selected));

        // Deselect All
        foreach (var r in rows) r.Selected = false;
        Assert.All(rows, r => Assert.False(r.Selected));
        Assert.Equal(0, rows.Count(r => r.Selected));
    }

    [Fact]
    public void PartialSelection_CorrectCount()
    {
        var rows = SampleRows().Select(r => new SuggestedAddOnSelectionRow(r)).ToList();
        rows[1].Selected = true;

        var selected = Assert.Single(rows, r => r.Selected);
        Assert.Equal(HaleemHalf, selected.Row.VariantId);
    }

    [Theory]
    [InlineData("Chicken Haleem", "Chicken Haleem Half Plate", "Chicken Haleem", "Half")]
    [InlineData("Chicken Haleem", "Chicken Haleem Full Plate", "Chicken Haleem", "Full")]
    [InlineData("Garlic Nan", "Garlic Nan", "Garlic Nan", "-")]
    [InlineData("Leechi", "", "Leechi", "-")]
    [InlineData("Aloo Gobi", "Aloo Gobi Regular", "Aloo Gobi", "-")]
    [InlineData("Aloo Gobi", "Aloo Gobi Standard", "Aloo Gobi", "-")]
    public void Naming_ResolvesPortionLikeTheCart(string product, string variant, string expectedItem, string expectedPortion)
    {
        var (itemName, portionName) = SuggestedAddOnNaming.Resolve(product, variant);
        Assert.Equal(expectedItem, itemName);
        Assert.Equal(expectedPortion, portionName);
    }

    private static T RunSta<T>(Func<T> func)
    {
        T? result = default;
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                result = func();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "STA thread execution exceeded 30 seconds.");
        if (failure is not null) throw failure;
        return result!;
    }

    [Fact]
    public void CompactTrigger_ZeroRecommendations_HidesSection()
    {
        RunSta(() =>
        {
            using var form = new RestaurantPosForm();
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;

            var hideMethod = typeof(RestaurantPosForm).GetMethod("HideSuggestionContent", flags)!;
            hideMethod.Invoke(form, null);

            var panel = (Panel)typeof(RestaurantPosForm).GetField("_suggestionPanel", flags)!.GetValue(form)!;
            var popupEdit = (PopupContainerEdit)typeof(RestaurantPosForm).GetField("_suggestionPopupEdit", flags)!.GetValue(form)!;
            Assert.NotNull(panel);
            Assert.False(panel.Visible);
            Assert.Equal(0, panel.Height);
            Assert.Equal("0 suggestions", popupEdit.EditValue?.ToString());

            return true;
        });
    }

    [Fact]
    public void CompactTrigger_MultipleRecommendations_PopulatesCompactTriggerAndDropdown()
    {
        RunSta(() =>
        {
            using var form = new RestaurantPosForm();
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;

            var recommendations = new List<BasketRecommendationDto>
            {
                new(Leechi, "Leechi", "-", 50m, RecommendationReason.ConfiguredRule),
                new(HaleemHalf, "Chicken Haleem", "Half Plate", 260m, RecommendationReason.ConfiguredRule),
                new(GarlicNan, "Garlic Nan", "-", 50m, RecommendationReason.ConfiguredRule),
            };

            var showMethod = typeof(RestaurantPosForm).GetMethod("ShowSuggestions", flags)!;
            showMethod.Invoke(form, new object[] { recommendations });

            var panel = (Panel)typeof(RestaurantPosForm).GetField("_suggestionPanel", flags)!.GetValue(form)!;
            var popupEdit = (PopupContainerEdit)typeof(RestaurantPosForm).GetField("_suggestionPopupEdit", flags)!.GetValue(form)!;
            var popupControl = (PopupContainerControl)typeof(RestaurantPosForm).GetField("_suggestionPopupControl", flags)!.GetValue(form)!;
            var grid = (GridControl)typeof(RestaurantPosForm).GetField("_suggestionGrid", flags)!.GetValue(form)!;
            var gridView = (GridView)typeof(RestaurantPosForm).GetField("_suggestionGridView", flags)!.GetValue(form)!;
            var rows = (List<SuggestedAddOnSelectionRow>)typeof(RestaurantPosForm).GetField("_suggestionRows", flags)!.GetValue(form)!;
            var addBtn = (SimpleButton)typeof(RestaurantPosForm).GetField("_suggestionAddSelectedButton", flags)!.GetValue(form)!;
            var selectAllCheck = (CheckEdit)typeof(RestaurantPosForm).GetField("_suggestionSelectAllCheck", flags)!.GetValue(form)!;

            // 1. Compact trigger in main POS screen must consume approximately ONE normal row only
            Assert.True(panel.Visible);
            Assert.True(panel.Height > 0);
            Assert.True(panel.Height <= 36, $"Panel height {panel.Height} exceeds single compact row threshold.");
            Assert.Equal("3 suggestions", popupEdit.EditValue?.ToString());

            // 2. The grid must NOT be inside the main panel (it lives inside the popup overlay)
            Assert.False(panel.Controls.Contains(grid), "Grid must NOT be inside the main cart panel.");
            Assert.True(popupControl.Controls.Contains(grid), "Grid must be inside the PopupContainerControl overlay.");
            Assert.Equal(popupControl, popupEdit.Properties.PopupControl);

            // 3. Dropdown grid columns and data
            Assert.Equal(3, rows.Count);
            Assert.Equal(4, gridView.Columns.Count);
            Assert.True(gridView.OptionsView.ShowColumnHeaders);
            Assert.False(addBtn.Enabled);
            Assert.Equal("Add Selected (0)", addBtn.Text);
            Assert.False(selectAllCheck.Checked);

            // 4. Select All via reflection
            var setAllMethod = typeof(RestaurantPosForm).GetMethod("SetAllSuggestionsSelected", flags)!;
            setAllMethod.Invoke(form, new object[] { true });

            Assert.True(addBtn.Enabled);
            Assert.Equal("Add Selected (3)", addBtn.Text);
            Assert.True(selectAllCheck.Checked);

            // 5. Uncheck one via SetSuggestionRowSelected
            var setRowMethod = typeof(RestaurantPosForm).GetMethod("SetSuggestionRowSelected", flags)!;
            setRowMethod.Invoke(form, new object[] { 1, false });

            Assert.True(addBtn.Enabled);
            Assert.Equal("Add Selected (2)", addBtn.Text);
            Assert.False(selectAllCheck.Checked);

            return true;
        });
    }

    [Fact]
    public void DropdownOverlay_DynamicHeight_CapsAtFourRows_AndTriggerStaysCompact()
    {
        RunSta(() =>
        {
            using var form = new RestaurantPosForm();
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;

            var heightMethod = typeof(RestaurantPosForm).GetMethod("UpdateSuggestionPanelHeight", flags)!;
            var panel = (Panel)typeof(RestaurantPosForm).GetField("_suggestionPanel", flags)!.GetValue(form)!;
            var popupControl = (PopupContainerControl)typeof(RestaurantPosForm).GetField("_suggestionPopupControl", flags)!.GetValue(form)!;
            var rows = (List<SuggestedAddOnSelectionRow>)typeof(RestaurantPosForm).GetField("_suggestionRows", flags)!.GetValue(form)!;

            // 0 rows -> hidden
            rows.Clear();
            heightMethod.Invoke(form, new object[] { 0 });
            Assert.False(panel.Visible);
            Assert.Equal(0, panel.Height);

            // Helper to set dummy rows for height calculations
            void SetRowCount(int count)
            {
                rows.Clear();
                for (int i = 0; i < count; i++)
                {
                    rows.Add(new SuggestedAddOnSelectionRow(new SuggestedAddOnRow(Guid.NewGuid(), $"Item {i}", "-", 10m)));
                }
                heightMethod.Invoke(form, new object[] { count });
            }

            // 1 row -> compact trigger visible, popup sized for 1 row
            SetRowCount(1);
            Assert.True(panel.Visible);
            Assert.True(panel.Height <= 36, "Trigger must remain a single compact row.");
            int popupH1 = popupControl.Height;

            // 2 rows -> trigger remains compact, popup overlay grows
            SetRowCount(2);
            Assert.True(panel.Height <= 36, "Trigger must remain a single compact row.");
            int popupH2 = popupControl.Height;
            Assert.True(popupH2 > popupH1, "Popup overlay for 2 rows should be taller than 1 row.");

            // 4 rows -> trigger remains compact, popup overlay grows
            SetRowCount(4);
            Assert.True(panel.Height <= 36, "Trigger must remain a single compact row.");
            int popupH4 = popupControl.Height;
            Assert.True(popupH4 > popupH2, "Popup overlay for 4 rows should be taller than 2 rows.");

            // 10 rows -> trigger remains compact, popup overlay caps at 4 rows
            SetRowCount(10);
            Assert.True(panel.Height <= 36, "Trigger must remain a single compact row.");
            int popupH10 = popupControl.Height;
            Assert.Equal(popupH4, popupH10);

            return true;
        });
    }

    [Fact]
    public void ShowSuggestions_HandlesDuplicateRecommendations_DeduplicatesByVariantId()
    {
        RunSta(() =>
        {
            using var form = new RestaurantPosForm();
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;

            var recommendations = new List<BasketRecommendationDto>
            {
                new(Leechi, "Leechi", "-", 50m, RecommendationReason.ConfiguredRule),
                new(Leechi, "Leechi", "-", 50m, RecommendationReason.ConfiguredRule),
                new(HaleemHalf, "Chicken Haleem", "Half Plate", 260m, RecommendationReason.ConfiguredRule),
            };

            var showMethod = typeof(RestaurantPosForm).GetMethod("ShowSuggestions", flags)!;
            showMethod.Invoke(form, new object[] { recommendations });

            var rows = (List<SuggestedAddOnSelectionRow>)typeof(RestaurantPosForm).GetField("_suggestionRows", flags)!.GetValue(form)!;
            Assert.Equal(2, rows.Count);
            Assert.Equal(Leechi, rows[0].Row.VariantId);
            Assert.Equal(HaleemHalf, rows[1].Row.VariantId);

            return true;
        });
    }

    [Fact]
    public void SuggestedAddOnNaming_PreservesFullProductNames_WithoutEllipsesOrTruncation()
    {
        var longNames = new[]
        {
            ("Chicken Biryani", "Chicken Biryani Standard", "Chicken Biryani", "-"),
            ("Chicken Koyla Karahi", "Full Plate", "Chicken Koyla Karahi", "Full"),
            ("White Daal Mash", "Half Plate", "White Daal Mash", "Half"),
            ("Chicken Haleem", "Chicken Haleem Half Plate", "Chicken Haleem", "Half")
        };

        foreach (var (product, variant, expectedItem, expectedPortion) in longNames)
        {
            var (item, portion) = SuggestedAddOnNaming.Resolve(product, variant);
            Assert.Equal(expectedItem, item);
            Assert.Equal(expectedPortion, portion);
            Assert.DoesNotContain("…", item);
            Assert.DoesNotContain("...", item);
        }
    }

    [Fact]
    public void Architecture_NoModalDialog_UsesDevExpressPopupContainerEditAndControl()
    {
        RunSta(() =>
        {
            using var form = new RestaurantPosForm();
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;

            var popupEdit = typeof(RestaurantPosForm).GetField("_suggestionPopupEdit", flags)!.GetValue(form);
            var popupControl = typeof(RestaurantPosForm).GetField("_suggestionPopupControl", flags)!.GetValue(form);

            Assert.NotNull(popupEdit);
            Assert.IsType<PopupContainerEdit>(popupEdit);
            Assert.NotNull(popupControl);
            Assert.IsType<PopupContainerControl>(popupControl);

            // Must NOT be a Form or XtraForm
            Assert.False(popupControl is Form, "Dropdown overlay must NOT be a separate Windows Form.");

            return true;
        });
    }

    [Fact]
    public void SuggestedAddOns_HeaderText_IsExactly_SuggestedAddOns()
    {
        RunSta(() =>
        {
            using var form = new RestaurantPosForm();
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;

            var label = (LabelControl)typeof(RestaurantPosForm).GetField("_suggestionHeaderLabel", flags)!.GetValue(form)!;
            Assert.NotNull(label);
            Assert.Equal("Suggested Add-ons", label.Text);

            return true;
        });
    }

    [Fact]
    public void SuggestedAddOns_BulbIcon_IsAttached_AndPositionedBefore_HeaderText()
    {
        RunSta(() =>
        {
            using var form = new RestaurantPosForm();
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;

            var label = (LabelControl)typeof(RestaurantPosForm).GetField("_suggestionHeaderLabel", flags)!.GetValue(form)!;
            var parent = label.Parent;
            Assert.NotNull(parent);

            var iconControl = parent.Controls.OfType<Control>().FirstOrDefault(c => c is PictureEdit or SimpleButton);
            Assert.NotNull(iconControl);

            int iconIndex = parent.Controls.IndexOf(iconControl);
            int labelIndex = parent.Controls.IndexOf(label);
            Assert.True(iconIndex < labelIndex, "Header icon control must be positioned/added before the header text label.");

            return true;
        });
    }

    [Fact]
    public void SuggestedAddOns_Columns_ItemHasDominantWidth_AndFixedPortionPriceCheckbox()
    {
        RunSta(() =>
        {
            using var form = new RestaurantPosForm();
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;

            var gridView = (GridView)typeof(RestaurantPosForm).GetField("_suggestionGridView", flags)!.GetValue(form)!;
            Assert.NotNull(gridView);
            Assert.True(gridView.OptionsView.ColumnAutoWidth, "ColumnAutoWidth must be true for dominant column expansion.");

            var selectCol = gridView.Columns[nameof(SuggestedAddOnSelectionRow.Selected)];
            var itemCol = gridView.Columns[nameof(SuggestedAddOnSelectionRow.ItemName)];
            var portionCol = gridView.Columns[nameof(SuggestedAddOnSelectionRow.PortionName)];
            var priceCol = gridView.Columns[nameof(SuggestedAddOnSelectionRow.PriceText)];

            Assert.NotNull(selectCol);
            Assert.NotNull(itemCol);
            Assert.NotNull(portionCol);
            Assert.NotNull(priceCol);

            Assert.True(selectCol.OptionsColumn.FixedWidth, "Checkbox column must be FixedWidth.");
            Assert.True(portionCol.OptionsColumn.FixedWidth, "Portion column must be FixedWidth.");
            Assert.True(priceCol.OptionsColumn.FixedWidth, "Price column must be FixedWidth.");

            Assert.False(itemCol.OptionsColumn.FixedWidth, "Item column must not be FixedWidth so it takes remaining space.");
            Assert.True(itemCol.MinWidth >= 180, $"Item column MinWidth {itemCol.MinWidth} should be >= 180.");

            Assert.Equal(DevExpress.Utils.HorzAlignment.Far, priceCol.AppearanceHeader.TextOptions.HAlignment);
            Assert.Equal(DevExpress.Utils.HorzAlignment.Far, priceCol.AppearanceCell.TextOptions.HAlignment);

            return true;
        });
    }

    [Fact]
    public void SuggestedAddOns_DropdownCaption_ShowsSingularAndPluralCorrectly()
    {
        RunSta(() =>
        {
            using var form = new RestaurantPosForm();
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;

            var heightMethod = typeof(RestaurantPosForm).GetMethod("UpdateSuggestionPanelHeight", flags)!;
            var popupEdit = (PopupContainerEdit)typeof(RestaurantPosForm).GetField("_suggestionPopupEdit", flags)!.GetValue(form)!;

            // 1 suggestion (singular)
            heightMethod.Invoke(form, new object[] { 1 });
            Assert.Equal("1 suggestion", popupEdit.EditValue?.ToString());

            // 2 suggestions (plural)
            heightMethod.Invoke(form, new object[] { 2 });
            Assert.Equal("2 suggestions", popupEdit.EditValue?.ToString());

            // 5 suggestions (plural)
            heightMethod.Invoke(form, new object[] { 5 });
            Assert.Equal("5 suggestions", popupEdit.EditValue?.ToString());

            return true;
        });
    }
}
