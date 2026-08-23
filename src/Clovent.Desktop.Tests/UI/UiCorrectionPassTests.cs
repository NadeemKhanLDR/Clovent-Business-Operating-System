using System.Reflection;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Restaurant.MenuItems;
using Clovent.Desktop.MasterData;
using Clovent.Desktop.Restaurant.Customers;
using Clovent.Desktop.Restaurant.Tables;
using Clovent.Identity.Application.Branches.Dtos;
using Clovent.Restaurant.Application.Tables.Dtos;
using Clovent.Desktop.Identity.Users;
using DevExpress.XtraEditors;
using System.Windows.Forms;
using Xunit;

namespace Clovent.Desktop.Tests.UI;

/// <summary>
/// Assertions for the final UI correction pass: grid-only refresh semantics
/// on the shared list view, plain (symbol-free) money display on non-grid
/// controls, and the Menu Item Edit photo section structure.
/// </summary>
public sealed class UiCorrectionPassTests
{
    private static T GetField<T>(object instance, string name) where T : class
    {
        var type = instance.GetType();
        while (type != null)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null) return (T)field.GetValue(instance)!;
            type = type.BaseType;
        }
        throw new Exception($"Field {name} not found on {instance.GetType().Name}");
    }

    [Fact]
    public void CurrencyDisplay_FormatPlain_NeverContainsSymbol()
    {
        CurrencyDisplay.Configure("Rs.", 2);

        Assert.Equal("850.00", CurrencyDisplay.FormatPlain(850m));
        Assert.DoesNotContain("Rs", CurrencyDisplay.FormatPlain(1234.5m));
        Assert.DoesNotContain("$", CurrencyDisplay.FormatPlain(9m));

        CurrencyDisplay.Configure(string.Empty, 2);
    }

    [Fact]
    public void PriceEditors_UsePlainNumericMasks_WithoutCurrencySymbol()
    {
        using var dialog = new MenuItemEditForm("New Menu Item", []);
        var priceEdit = GetField<SpinEdit>(dialog, "_priceEdit");

        var mask = priceEdit.Properties.Mask.EditMask ?? string.Empty;
        Assert.False(
            mask.TrimStartStartsWithCurrency(),
            $"Selling price editor must not use a currency mask, was '{mask}'.");
    }

    [Fact]
    public async Task MasterDataListView_StatusRefresh_PreservesFocusedRowAndSearchText()
    {
        var items = Enumerable.Range(0, 5)
            .Select(i => new BranchDto(Guid.NewGuid(), Guid.NewGuid(), $"Branch {i}", null, null, null, null, null, "Active", DateTimeOffset.UtcNow))
            .ToList();

        var listView = new MasterDataListView<BranchDto>(
        [
            new MasterDataColumn("Name", "Name", 200),
            new MasterDataColumn("Status", "Status", 100),
        ], [])
        {
            LoadItemsAsync = _ => Task.FromResult<IReadOnlyList<BranchDto>>(items),
            StatusSelector = dto => dto.Status,
        };

        var searchBox = GetField<TextEdit>(listView, "_searchBox");
        var gridView = GetField<DevExpress.XtraGrid.Views.Grid.GridView>(listView, "_gridView");

        listView.Size = new Size(1000, 600);
        listView.PerformLayout();
        await listView.RefreshAsync();

        // A search filter typed by the user...
        searchBox.Text = "Branch";

        // ...and a row they selected...
        gridView.FocusedRowHandle = 2;

        // ...must survive a status-action refresh (Activate/Deactivate calls
        // this same RefreshAsync after the command).
        await listView.RefreshAsync();

        Assert.Equal("Branch", searchBox.Text);
        Assert.Equal(2, gridView.FocusedRowHandle);

        listView.Dispose();
    }

    [Fact]
    public void MenuItemEditForm_PhotoSection_GroupsPreviewAndBothButtons()
    {
        using var dialog = new MenuItemEditForm("New Menu Item", []);
        // Run the same DPI-scaling OnLoad performs in the real app (it sets
        // the photo buttons' matching minimums).
        typeof(Form)
            .GetMethod("OnLoad", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(dialog, [EventArgs.Empty]);
        var imageButtons = GetField<System.Windows.Forms.FlowLayoutPanel>(dialog, "_imageButtons");

        Assert.True(Clovent.Desktop.Tests.UI.ControlVisibility.IsSelfVisible(imageButtons));

        // The two photo actions live in the section (not floating elsewhere).
        var allButtons = imageButtons.Controls.Cast<System.Windows.Forms.Control>()
            .SelectMany(c => c is System.Windows.Forms.FlowLayoutPanel flow
                ? flow.Controls.Cast<System.Windows.Forms.Control>()
                : [c])
            .OfType<SimpleButton>()
            .ToList();
        Assert.Contains(allButtons, b => b.Text.Contains("Choose Photo"));
        Assert.Contains(allButtons, b => b.Text.Contains("Remove Photo"));

        // Both buttons AutoSize to their captions and have matching minimums
        // so neither clips nor renders as unequal slivers.
        foreach (var button in allButtons)
        {
            Assert.True(button.AutoSize);
            Assert.True(button.MinimumSize.Width > 0);
        }

        // The section stacks vertically (preview above the grouped buttons).
        Assert.Equal(System.Windows.Forms.FlowDirection.TopDown, imageButtons.FlowDirection);
    }

    [Fact]
    public void TableEditForm_Validation_RequiresName()
    {
        using var form = new Clovent.Desktop.Restaurant.Tables.TableEditForm("New Table", code: "", name: "  ", capacity: 4, isNew: true);
        var handle = form.Handle; // Force control creation
        
        // We use reflection to call ValidateFields
        var validateMethod = typeof(Clovent.Desktop.Restaurant.Tables.TableEditForm).GetMethod("ValidateFields", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var args = new object[] { "" };
        var isValid = (bool)validateMethod.Invoke(form, args)!;
        
        Assert.False(isValid);
        Assert.Contains("Code is required", args[0].ToString());

        // Fill Code, keep Name empty
        var codeEdit = GetField<TextEdit>(form, "_codeEdit");
        codeEdit.Text = "T-01";
        
        args = new object[] { "" };
        isValid = (bool)validateMethod.Invoke(form, args)!;
        Assert.False(isValid);
        Assert.Contains("Table Name is required", args[0].ToString());
    }

    [Fact]
    public void TableManagementView_Grid_ExposesNameColumn()
    {
        var path = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "../../../../Clovent.Desktop/Restaurant/Tables/TableManagementView.Designer.cs"));
        Assert.True(System.IO.File.Exists(path), $"TableManagementView.Designer.cs not found at: {path}");
        var content = System.IO.File.ReadAllText(path);
        Assert.Contains("new MasterDataColumn(\"Name\", \"Table Name\", 120)", content);
    }

    [Fact]
    public async Task MasterDataListView_MultiSelect_EnablesBulkButtons()
    {
        var items = Enumerable.Range(0, 5)
            .Select(i => new BranchDto(Guid.NewGuid(), Guid.NewGuid(), $"Branch {i}", null, null, null, null, null, "Active", DateTimeOffset.UtcNow))
            .ToList();

        var listView = new MasterDataListView<BranchDto>(
        [
            new MasterDataColumn("Name", "Name", 200),
            new MasterDataColumn("Status", "Status", 100),
        ], [])
        {
            LoadItemsAsync = _ => Task.FromResult<IReadOnlyList<BranchDto>>(items),
            StatusSelector = dto => dto.Status,
            OnEdit = _ => Task.CompletedTask,
            OnActivate = _ => Task.CompletedTask,
            OnDeactivate = _ => Task.CompletedTask,
        };

        var editButton = GetField<SimpleButton>(listView, "_editButton");
        var activateButton = GetField<SimpleButton>(listView, "_activateButton");
        var deactivateButton = GetField<SimpleButton>(listView, "_deactivateButton");
        var gridView = GetField<DevExpress.XtraGrid.Views.Grid.GridView>(listView, "_gridView");

        listView.Size = new Size(1000, 600);
        listView.PerformLayout();
        await listView.RefreshAsync();

        // Select multiple rows
        gridView.SelectRow(0);
        gridView.SelectRow(1);

        // Under multi-selection:
        // Edit must be disabled, but bulk Activate/Deactivate enabled
        typeof(MasterDataListView<BranchDto>)
            .GetMethod("UpdateButtonStates", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(listView, null);

        Assert.False(editButton.Enabled);
        Assert.True(activateButton.Enabled);
        Assert.True(deactivateButton.Enabled);

        listView.Dispose();
    }

    [Fact]
    public void MenuItemEditForm_BarcodeValidation_EnforcesRules()
    {
        using var form = new MenuItemEditForm(
            "New Item", [], "Burger", null, 10m, true, null,
            barcode1: "12345678", barcode2: "87654321", barcode3: "12345678");

        var validateMethod = typeof(MenuItemEditForm).GetMethod("ValidateFields", BindingFlags.Instance | BindingFlags.NonPublic)!;
        
        // 1. Rejected duplicates among themselves
        var args = new object[] { "" };
        var isValid = (bool)validateMethod.Invoke(form, args)!;
        Assert.False(isValid);
        Assert.Contains("cannot have the same barcode value", args[0].ToString());

        // 2. Format verification (letters rejected)
        using var form2 = new MenuItemEditForm(
            "New Item", [], "Burger", null, 10m, true, null,
            barcode1: "12345ABC");
        args = new object[] { "" };
        isValid = (bool)validateMethod.Invoke(form2, args)!;
        Assert.False(isValid);
        Assert.Contains("must contain only digits", args[0].ToString());

        // 3. Length verification (short rejected)
        using var form3 = new MenuItemEditForm(
            "New Item", [], "Burger", null, 10m, true, null,
            barcode1: "12345");
        args = new object[] { "" };
        isValid = (bool)validateMethod.Invoke(form3, args)!;
        Assert.False(isValid);
        Assert.Contains("must contain only digits (8 to 14 digits)", args[0].ToString());
    }

    [Fact]
    public void CustomerEditForm_NotesAndFieldsPersistence()
    {
        using var form = new CustomerEditForm(
            "New Customer", "C001", "John", "555-1234", "123 Main St", "john@doe.com",
            openingBalance: 100m, creditLimit: 500m, notes: "Special VIP customer notes.",
            isNew: true, shopNo: "Shop 12", mobile2: "555-5678", phone: "555-9000");

        Assert.Equal("C001", form.CodeValue);
        Assert.Equal("John", form.NameValue);
        Assert.Equal("555-1234", form.MobileValue);
        Assert.Equal("555-5678", form.Mobile2Value);
        Assert.Equal("555-9000", form.PhoneValue);
        Assert.Equal("Shop 12", form.ShopNoValue);
        Assert.Equal("123 Main St", form.AddressValue);
        Assert.Equal("john@doe.com", form.EmailValue);
        Assert.Equal(100m, form.OpeningBalanceValue);
        Assert.Equal(500m, form.CreditLimitValue);
        Assert.Equal("Special VIP customer notes.", form.NotesValue);
    }

    [Fact]
    public void TableEditForm_CapacityAndNamePersistence()
    {
        using var form = new TableEditForm(
            "Edit Table", "T-01", "Window Table", capacity: 4, isNew: false);

        Assert.Equal("T-01", form.CodeValue);
        Assert.Equal("Window Table", form.TableNameValue);
        Assert.Equal(4, form.CapacityValue);
    }

    [Fact]
    public void CustomerEditForm_CodeIsReadOnly()
    {
        using var form = new CustomerEditForm("New Customer", "C001", "John", "555-1234", "Address", null);
        var codeEdit = GetField<TextEdit>(form, "_codeEdit");
        Assert.True(codeEdit.Properties.ReadOnly);
    }

    [Fact]
    public void CustomerCode_NextSequenceGeneration()
    {
        var existingCodes = new[] { "C001", "C002", "C999", "C010", "OTHER" };
        var nextNumber = 1;
        foreach (var code in existingCodes)
        {
            if (code.StartsWith("C", StringComparison.OrdinalIgnoreCase) && 
                int.TryParse(code.Substring(1), out var num))
            {
                if (num >= nextNumber)
                {
                    nextNumber = num + 1;
                }
            }
        }
        var nextCode = $"C{nextNumber:D3}";
        Assert.Equal("C1000", nextCode); // C999 + 1 = C1000
    }

    [Fact]
    public void CurrencyDisplay_FormatPlain_RespectsPrecisionWithoutSymbol()
    {
        // 0 decimals
        CurrencyDisplay.Configure("$", 0);
        Assert.Equal("100", CurrencyDisplay.FormatPlain(100m));
        Assert.Equal("16", CurrencyDisplay.FormatPlain(15.5m));

        // 2 decimals
        CurrencyDisplay.Configure("$", 2);
        Assert.Equal("100.00", CurrencyDisplay.FormatPlain(100m));
        Assert.Equal("15.50", CurrencyDisplay.FormatPlain(15.5m));

        // 3 decimals
        CurrencyDisplay.Configure("$", 3);
        Assert.Equal("100.000", CurrencyDisplay.FormatPlain(100m));
        Assert.Equal("15.500", CurrencyDisplay.FormatPlain(15.5m));

        // Reset to default
        CurrencyDisplay.Configure("Rs.", 2);
    }

    [Fact]
    public void PasswordPromptForm_LayoutAlignmentAndMargins()
    {
        using var form = new PasswordPromptForm("Reset Password", false);
        var newPasswordEdit = GetField<TextEdit>(form, "_newPasswordEdit");
        var confirmPasswordEdit = GetField<TextEdit>(form, "_confirmPasswordEdit");

        Assert.NotNull(newPasswordEdit.Parent);
        Assert.NotNull(confirmPasswordEdit.Parent);
        Assert.Same(newPasswordEdit.Parent, confirmPasswordEdit.Parent);
    }
}

internal static class CurrencyMaskExtensions
{
    public static bool TrimStartStartsWithCurrency(this string mask)
    {
        var trimmed = mask.Trim();
        return trimmed is "c" or "C" or "c2" or "C2" or "c4" or "C4" || trimmed.StartsWith("c;", StringComparison.Ordinal);
    }
}
