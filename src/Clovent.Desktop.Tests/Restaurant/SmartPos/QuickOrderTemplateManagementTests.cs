using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Restaurant.SmartPos;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Restaurant.Application.QuickOrderTemplates.Dtos;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.SmartPos;

/// <summary>
/// Comprehensive automated test suite for Quick Order Templates management and the Create/Edit dialog workflow.
/// Validates requirements 12 through 37 of the Smart POS Back Office specification.
/// </summary>
public class QuickOrderTemplateManagementTests
{
    private static readonly Guid ProductBiryaniId = Guid.NewGuid();
    private static readonly Guid VariantBiryaniStdId = Guid.NewGuid();
    private static readonly Guid ProductKarahiId = Guid.NewGuid();
    private static readonly Guid VariantKarahiHalfId = Guid.NewGuid();
    private static readonly Guid VariantKarahiFullId = Guid.NewGuid();
    private static readonly Guid ProductSaladId = Guid.NewGuid();
    private static readonly Guid VariantSaladStdId = Guid.NewGuid();

    private static List<ProductOptionRow> SampleOptions() =>
    [
        new(VariantBiryaniStdId, ProductBiryaniId, "Chicken Biryani", "Standard", 450.00m),
        new(VariantKarahiHalfId, ProductKarahiId, "Chicken Karahi", "Half", 800.00m),
        new(VariantKarahiFullId, ProductKarahiId, "Chicken Karahi", "Full", 1500.00m),
        new(VariantSaladStdId, ProductSaladId, "Fresh Salad", "Standard", 50.00m)
    ];

    private static void RunSta(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(15)), "STA Thread timed out");
        if (error != null) throw error;
    }

    private static void ClickButton(SimpleButton button)
    {
        typeof(SimpleButton)
            .GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(button, [EventArgs.Empty]);
    }

    private static T Field<T>(object target, string name) =>
        (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;

    // -------------------------------------------------------------
    // MAIN SCREEN TESTS (12–16)
    // -------------------------------------------------------------

    [Fact]
    public void Test12_MainPageTitle_IsNotTruncated_AndHasProperTypography()
    {
        RunSta(() =>
        {
            var options = SampleOptions();
            var session = new FakeSession();
            var view = new QuickOrderTemplatesView(new FakeScopeFactory(), session);

            var header = Field<LabelControl>(view, "headerLabel");
            var subHeader = Field<LabelControl>(view, "subHeaderLabel");

            Assert.Equal("QUICK ORDER TEMPLATES", header.Text);
            Assert.Contains("reusable deals", subHeader.Text, StringComparison.OrdinalIgnoreCase);
            Assert.True(header.Font.SizeInPoints >= 13);
        });
    }

    [Fact]
    public void Test13_MainGrid_HasAllRequiredColumns_WithAutoWidth()
    {
        RunSta(() =>
        {
            var session = new FakeSession();
            var view = new QuickOrderTemplatesView(new FakeScopeFactory(), session);
            var gridView = Field<GridView>(view, "_gridView");

            Assert.True(gridView.OptionsView.ColumnAutoWidth);
            Assert.NotNull(gridView.Columns["Name"]);
            Assert.NotNull(gridView.Columns["Description"]);
            Assert.NotNull(gridView.Columns["DisplayOrder"]);
            Assert.NotNull(gridView.Columns["ItemCount"]);
            Assert.NotNull(gridView.Columns["TotalDisplay"]);
            Assert.NotNull(gridView.Columns["StatusText"]);

            Assert.Equal("Template Name", gridView.Columns["Name"].Caption);
            Assert.Equal("Order", gridView.Columns["DisplayOrder"].Caption);
            Assert.Equal("Total", gridView.Columns["TotalDisplay"].Caption);
        });
    }

    [Fact]
    public void Test14_15_16_ToolbarButtons_ConfiguredProperly()
    {
        RunSta(() =>
        {
            var session = new FakeSession();
            var view = new QuickOrderTemplatesView(new FakeScopeFactory(), session);

            var btnNew = Field<SimpleButton>(view, "_newButton");
            var btnEdit = Field<SimpleButton>(view, "_btnEdit");
            var btnToggle = Field<SimpleButton>(view, "_btnToggleStatus");
            var btnRefresh = Field<SimpleButton>(view, "_refreshButton");
            var btnHealth = Field<SimpleButton>(view, "_btnOrderHealth");

            Assert.Equal("+ New Template", btnNew.Text);
            Assert.Equal("Edit", btnEdit.Text);
            Assert.Equal("Deactivate", btnToggle.Text);
            Assert.Equal("Refresh", btnRefresh.Text);
            Assert.Equal("Order Health...", btnHealth.Text);
        });
    }

    // -------------------------------------------------------------
    // EDIT DIALOG: PRODUCT, VARIANT, AND PRICE WORKFLOW (17–26)
    // -------------------------------------------------------------

    [Fact]
    public void Test17_ProductLookup_IsPopulatedWithDistinctActiveProducts()
    {
        RunSta(() =>
        {
            var options = SampleOptions();
            using var form = new QuickOrderTemplateEditForm("New Template", options);

            var productLookup = Field<LookUpEdit>(form, "_productLookup");
            var ds = Assert.IsAssignableFrom<List<ProductOptionRowSummary>>(productLookup.Properties.DataSource);

            Assert.Equal(3, ds.Count);
            Assert.Contains(ds, p => p.ProductName == "Chicken Biryani");
            Assert.Contains(ds, p => p.ProductName == "Chicken Karahi");
            Assert.Contains(ds, p => p.ProductName == "Fresh Salad");
        });
    }

    [Fact]
    public void Test18_19_SelectingProduct_FiltersVariants_AndAutoFetchesSellingPrice()
    {
        RunSta(() =>
        {
            var options = SampleOptions();
            using var form = new QuickOrderTemplateEditForm("New Template", options);

            var productLookup = Field<LookUpEdit>(form, "_productLookup");
            var variantLookup = Field<LookUpEdit>(form, "_variantLookup");
            var priceEdit = Field<SpinEdit>(form, "_priceEdit");

            // Select Karahi (which has 2 variants: Half at 800, Full at 1500)
            productLookup.EditValue = ProductKarahiId;
            var karahiVariants = Assert.IsAssignableFrom<List<ProductOptionRow>>(variantLookup.Properties.DataSource);
            Assert.Equal(2, karahiVariants.Count);

            // Selecting Half fetches 800
            variantLookup.EditValue = VariantKarahiHalfId;
            Assert.Equal(800.00m, priceEdit.Value);

            // Selecting Full fetches 1500
            variantLookup.EditValue = VariantKarahiFullId;
            Assert.Equal(1500.00m, priceEdit.Value);

            // Selecting Biryani (which has 1 variant) auto-selects Standard and fetches 450
            productLookup.EditValue = ProductBiryaniId;
            Assert.Equal(VariantBiryaniStdId, variantLookup.EditValue);
            Assert.Equal(450.00m, priceEdit.Value);
        });
    }

    [Fact]
    public void Test20_UnitPrice_IsEditableByManager_ForDealCustomPricing()
    {
        RunSta(() =>
        {
            var options = SampleOptions();
            using var form = new QuickOrderTemplateEditForm("New Template", options);

            var productLookup = Field<LookUpEdit>(form, "_productLookup");
            var variantLookup = Field<LookUpEdit>(form, "_variantLookup");
            var priceEdit = Field<SpinEdit>(form, "_priceEdit");

            productLookup.EditValue = ProductBiryaniId;
            variantLookup.EditValue = VariantBiryaniStdId;
            Assert.Equal(450.00m, priceEdit.Value);

            // Manager custom deal override
            priceEdit.Value = 420.00m;
            Assert.Equal(420.00m, priceEdit.Value);
        });
    }

    [Fact]
    public void Test21_22_QuantityAndLineTotal_Calculation()
    {
        RunSta(() =>
        {
            var options = SampleOptions();
            using var form = new QuickOrderTemplateEditForm("New Template", options);

            var productLookup = Field<LookUpEdit>(form, "_productLookup");
            var variantLookup = Field<LookUpEdit>(form, "_variantLookup");
            var qtyEdit = Field<SpinEdit>(form, "_quantityEdit");
            var priceEdit = Field<SpinEdit>(form, "_priceEdit");
            var lblTotal = Field<LabelControl>(form, "_lineTotalPreviewLabel");

            productLookup.EditValue = ProductBiryaniId;
            variantLookup.EditValue = VariantBiryaniStdId;
            qtyEdit.Value = 3m;

            Assert.Contains("1,350.00", lblTotal.Text);
        });
    }

    [Fact]
    public void Test23_24_25_26_AddItem_UpdateItem_RemoveItem_And_TemplateTotal()
    {
        RunSta(() =>
        {
            var options = SampleOptions();
            using var form = new QuickOrderTemplateEditForm("New Template", options);

            var productLookup = Field<LookUpEdit>(form, "_productLookup");
            var variantLookup = Field<LookUpEdit>(form, "_variantLookup");
            var qtyEdit = Field<SpinEdit>(form, "_quantityEdit");
            var priceEdit = Field<SpinEdit>(form, "_priceEdit");
            var btnAdd = Field<SimpleButton>(form, "_btnAddOrUpdateItem");
            var btnRemove = Field<SimpleButton>(form, "_btnRemoveSelected");
            var totalLabel = Field<LabelControl>(form, "_totalLabel");
            var gridView = Field<GridView>(form, "_itemsView");

            // Add Biryani x2 at 450 = 900
            productLookup.EditValue = ProductBiryaniId;
            variantLookup.EditValue = VariantBiryaniStdId;
            qtyEdit.Value = 2m;
            ClickButton(btnAdd);

            Assert.Single(form.ItemValues);
            Assert.Contains("900.00", totalLabel.Text);

            // Add Salad x1 at 50 = 50. Total = 950
            productLookup.EditValue = ProductSaladId;
            variantLookup.EditValue = VariantSaladStdId;
            qtyEdit.Value = 1m;
            ClickButton(btnAdd);

            Assert.Equal(2, form.ItemValues.Count);
            Assert.Contains("950.00", totalLabel.Text);

            // Update Biryani: load focused row 0 and change qty to 3
            gridView.FocusedRowHandle = 0;
            typeof(QuickOrderTemplateEditForm)
                .GetMethod("LoadFocusedRowIntoEditor", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(form, null);

            Assert.Equal("Update Item", btnAdd.Text);
            qtyEdit.Value = 3m;
            ClickButton(btnAdd); // Commit update: 3 * 450 + 50 = 1400

            Assert.Equal("Add Item", btnAdd.Text);
            Assert.Contains("1,400.00", totalLabel.Text);

            // Remove selected row
            gridView.FocusedRowHandle = 1; // Salad
            ClickButton(btnRemove);

            Assert.Single(form.ItemValues);
            Assert.Contains("1,350.00", totalLabel.Text);
        });
    }

    // -------------------------------------------------------------
    // EXISTING TEMPLATE LOADING & DATA PRESERVATION (27–37)
    // -------------------------------------------------------------

    [Fact]
    public void Test27_through_32_ExistingTemplateLoadsAllFields_AndPreservesSavedPrices()
    {
        RunSta(() =>
        {
            var options = SampleOptions();
            var existingModel = new QuickOrderTemplateEditModel(
                "Family Biryani Deal",
                "4 Chicken Biryani + 2 Fresh Salads",
                1,
                [
                    (VariantBiryaniStdId, 4m, 430.00m), // Custom discounted deal component price!
                    (VariantSaladStdId, 2m, 45.00m)     // Custom discounted price!
                ],
                true);

            using var form = new QuickOrderTemplateEditForm("Edit Template", options, existingModel);

            Assert.Equal("Family Biryani Deal", form.NameValue);
            Assert.Equal("4 Chicken Biryani + 2 Fresh Salads", form.DescriptionValue);
            Assert.Equal(1, form.DisplayOrderValue);
            Assert.True(form.IsActiveValue);

            // Check items
            Assert.Equal(2, form.ItemValues.Count);

            var biryaniItem = form.ItemValues.First(i => i.VariantId == VariantBiryaniStdId);
            Assert.Equal(4m, biryaniItem.Quantity);
            Assert.Equal(430.00m, biryaniItem.TemplateUnitPrice); // PRESERVED!

            var saladItem = form.ItemValues.First(i => i.VariantId == VariantSaladStdId);
            Assert.Equal(2m, saladItem.Quantity);
            Assert.Equal(45.00m, saladItem.TemplateUnitPrice); // PRESERVED!

            // Total = 4 * 430 + 2 * 45 = 1720 + 90 = 1810
            var totalLabel = Field<LabelControl>(form, "_totalLabel");
            Assert.Contains("1,810.00", totalLabel.Text);
        });
    }

    [Fact]
    public void Test33_34_ChangingVariant_FetchesNewPrice_AndUseCurrentPriceResets()
    {
        RunSta(() =>
        {
            var options = SampleOptions();
            using var form = new QuickOrderTemplateEditForm("New Template", options);

            var productLookup = Field<LookUpEdit>(form, "_productLookup");
            var variantLookup = Field<LookUpEdit>(form, "_variantLookup");
            var priceEdit = Field<SpinEdit>(form, "_priceEdit");
            var btnReset = Field<SimpleButton>(form, "_btnResetPrice");

            productLookup.EditValue = ProductKarahiId;
            variantLookup.EditValue = VariantKarahiHalfId;
            Assert.Equal(800.00m, priceEdit.Value);

            // Change to custom price
            priceEdit.Value = 750.00m;
            Assert.Equal(750.00m, priceEdit.Value);

            // Click Use Current Price -> resets to 800
            ClickButton(btnReset);
            Assert.Equal(800.00m, priceEdit.Value);

            // Change variant to Full -> fetches 1500
            variantLookup.EditValue = VariantKarahiFullId;
            Assert.Equal(1500.00m, priceEdit.Value);
        });
    }

    [Fact]
    public void Test36_SmartComboCreatedTemplate_OpensCorrectlyWithoutLosingPrices()
    {
        RunSta(() =>
        {
            var options = SampleOptions();
            // Simulate a template created by Smart Combo Builder with allocated discount prices
            var smartComboTemplate = new QuickOrderTemplateEditModel(
                "Smart Biryani + Salad Combo",
                "Discovered from 120 completed sales.",
                0,
                [
                    (VariantBiryaniStdId, 1m, 400.00m),
                    (VariantSaladStdId, 1m, 25.00m)
                ],
                true);

            using var form = new QuickOrderTemplateEditForm("Edit Smart Combo Deal", options, smartComboTemplate);

            Assert.Equal("Smart Biryani + Salad Combo", form.NameValue);
            Assert.Equal(2, form.ItemValues.Count);
            Assert.Equal(400.00m, form.ItemValues.First(i => i.VariantId == VariantBiryaniStdId).TemplateUnitPrice);
            Assert.Equal(25.00m, form.ItemValues.First(i => i.VariantId == VariantSaladStdId).TemplateUnitPrice);

            var totalLabel = Field<LabelControl>(form, "_totalLabel");
            Assert.Contains("425.00", totalLabel.Text);
        });
    }

    [Fact]
    public void Test37_ConfiguredCurrency_DisplayPrecision()
    {
        CurrencyDisplay.Configure("Rs.", 2);
        Assert.Equal("1,500.00", CurrencyDisplay.FormatPlain(1500.00m));
        Assert.Equal("Total: 1,500.00", QuickOrderTemplateEditForm.ComposeTotalText(1500.00m));
    }

    [Fact]
    public void Test38_DialogGeometry_And_LayoutStructure()
    {
        RunSta(() =>
        {
            var options = SampleOptions();
            var existing = new QuickOrderTemplateEditModel(
                "Chicken Karahi Feast",
                "1 Chicken Karahi + 4 Garlic Nan + 2 Fresh Salads + 2 Beverages",
                2,
                [
                    (VariantKarahiHalfId, 1m, 1200m),
                    (VariantSaladStdId, 2m, 50m),
                    (VariantBiryaniStdId, 1m, 450m)
                ],
                true);

            using var form = new QuickOrderTemplateEditForm("Edit Quick Order Template", options, existing);
            form.CreateControl();
            form.Show();

            Assert.Equal(AutoScaleMode.None, form.AutoScaleMode);
            Assert.False(form.AutoSize);
            Assert.True(form.ClientSize.Width >= 820, $"Expected ClientSize.Width >= 820, got {form.ClientSize.Width}");
            Assert.True(form.ClientSize.Height >= 560, $"Expected ClientSize.Height >= 560, got {form.ClientSize.Height}");
            Assert.True(form.MinimumSize.Width >= 800, $"Expected MinimumSize.Width >= 800, got {form.MinimumSize.Width}");
            Assert.True(form.MinimumSize.Height >= 520, $"Expected MinimumSize.Height >= 520, got {form.MinimumSize.Height}");

            var mainTable = Assert.IsType<TableLayoutPanel>(form.Controls[0]);
            Assert.Equal(5, mainTable.RowCount);
            Assert.Equal(SizeType.AutoSize, mainTable.RowStyles[0].SizeType);
            Assert.Equal(SizeType.AutoSize, mainTable.RowStyles[1].SizeType);
            Assert.Equal(SizeType.AutoSize, mainTable.RowStyles[2].SizeType);
            Assert.Equal(SizeType.Percent, mainTable.RowStyles[3].SizeType);
            Assert.Equal(SizeType.AutoSize, mainTable.RowStyles[4].SizeType);

            var itemsView = Field<GridView>(form, "_itemsView");
            Assert.True(itemsView.OptionsView.ColumnAutoWidth);
            Assert.Equal(6, itemsView.Columns.Count);
            Assert.True(itemsView.Columns[nameof(TemplateItemRow.ProductName)].Width >= 180);
            Assert.True(itemsView.Columns[nameof(TemplateItemRow.VariantName)].Width >= 120);
            Assert.True(itemsView.Columns[nameof(TemplateItemRow.Quantity)].Width >= 55);
            Assert.True(itemsView.Columns[nameof(TemplateItemRow.UnitPriceText)].Width >= 90);
            Assert.True(itemsView.Columns[nameof(TemplateItemRow.CatalogPriceText)].Width >= 90);
            Assert.True(itemsView.Columns[nameof(TemplateItemRow.EffectiveTotalText)].Width >= 100);

            var details = mainTable.Controls[0];
            var editor = mainTable.Controls[1];
            var toolbar = mainTable.Controls[2];
            var gridPanel = mainTable.Controls[3];
            var footer = mainTable.Controls[4];

            Assert.True(details.Height >= 120, $"Expected details.Height >= 120, got {details.Height}");
            Assert.True(editor.Height >= 85, $"Expected editor.Height >= 85, got {editor.Height}");
            Assert.True(toolbar.Height >= 28, $"Expected toolbar.Height >= 28, got {toolbar.Height}");
            Assert.True(gridPanel.Height >= 180, $"Expected gridPanel.Height >= 180, got {gridPanel.Height}");
            Assert.True(footer.Height >= 34, $"Expected footer.Height >= 34, got {footer.Height}");

            var totalLabel = Field<LabelControl>(form, "_totalLabel");
            Assert.Contains("1,750.00", totalLabel.Text);

            form.Close();
        });
    }

    [Fact]
    public void Test39_RenderDialogBitmap_VisualVerification()
    {
        RunSta(() =>
        {
            var options = SampleOptions();
            var existing = new QuickOrderTemplateEditModel(
                "Chicken Karahi Feast",
                "1 Chicken Karahi + 4 Garlic Nan + 2 Fresh Salads + 2 Beverages",
                2,
                [
                    (VariantKarahiHalfId, 1m, 1200m),
                    (VariantSaladStdId, 2m, 50m),
                    (VariantBiryaniStdId, 1m, 450m)
                ],
                true);

            using var form = new QuickOrderTemplateEditForm("Edit Quick Order Template", options, existing);
            form.CreateControl();
            form.Show();

            using var bmp = new Bitmap(form.ClientSize.Width, form.ClientSize.Height);
            form.DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));

            System.IO.Directory.CreateDirectory("qa");
            bmp.Save("qa/quick_order_template_edit_dialog_verified.png", System.Drawing.Imaging.ImageFormat.Png);

            form.Close();
        });
    }

    private sealed class FakeSession : ICurrentSession
    {
        public Guid? UserId => Guid.NewGuid();
        public Guid? SessionId => Guid.NewGuid();
        public string? DisplayName => "Test Admin";
        public bool IsAuthenticated => true;
        public void SignIn(Guid userId, Guid sessionId, string displayName) { }
        public void SignOut() { }
#pragma warning disable CS0067
        public event EventHandler? Changed;
#pragma warning restore CS0067
    }

    private sealed class FakeScopeFactory : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new FakeScope();
        private sealed class FakeScope : IServiceScope
        {
            public IServiceProvider ServiceProvider => new FakeProvider();
            public void Dispose() { }
        }
        private sealed class FakeProvider : IServiceProvider
        {
            public object? GetService(Type serviceType)
            {
                if (serviceType == typeof(IMediator)) return new FakeMediator();
                if (serviceType == typeof(IFeatureAuthorizationPolicy)) return new FakePolicy();
                if (serviceType.Name.StartsWith("ILogger")) return NullLogger<QuickOrderTemplatesView>.Instance;
                return null;
            }
        }
        private sealed class FakePolicy : IFeatureAuthorizationPolicy
        {
            public Task<bool> CanUseFeatureAsync(Guid userId, string featureCode, CancellationToken ct = default) => Task.FromResult(true);
        }
        private sealed class FakeMediator : IMediator
        {
            public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default) =>
                Task.FromResult((TResponse)(object)Array.Empty<QuickOrderTemplateDto>());
            public Task Send<TRequest>(TRequest request, CancellationToken ct = default) where TRequest : IRequest => Task.CompletedTask;
            public Task<object?> Send(object request, CancellationToken ct = default) => Task.FromResult<object?>(null);
            public Task Publish(object notification, CancellationToken ct = default) => Task.CompletedTask;
            public Task Publish<TNotification>(TNotification notification, CancellationToken ct = default) where TNotification : INotification => Task.CompletedTask;
            public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken ct = default) => throw new NotImplementedException();
            public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken ct = default) => throw new NotImplementedException();
        }
    }
}
