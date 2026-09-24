using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Restaurant.SmartPos;
using Clovent.Restaurant.Application.SmartCombos;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.SmartPos;

public class OpportunityAndTemplateUiStructuralTests
{
    private static void RunSta(Action action)
    {
        Exception? err = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                err = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        if (!thread.Join(TimeSpan.FromSeconds(20)))
        {
            thread.Interrupt();
            throw new TimeoutException("STA test timed out.");
        }
        if (err != null) throw err;
    }

    private static IEnumerable<Control> AllControls(Control parent) =>
        parent.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(AllControls(c)));

    [Fact]
    public void SmartComboOpportunityDialog_Structure_And_Geometry_Are_Professional_And_DpiAware()
    {
        RunSta(() =>
        {
            CurrencyDisplay.Configure("Rs.", 2);
            var item1 = new ComboItem(Guid.NewGuid(), "Chicken Biryani", "Standard", 450.00m, 250.00m);
            var item2 = new ComboItem(Guid.NewGuid(), "Salad", "Standard", 30.00m, null);
            var opportunity = new ComboOpportunity(
                "sig-1",
                "Chicken Biryani Combo",
                [item1, item2],
                6,
                129,
                0.0465m,
                0.60m,
                1.33m,
                "Chicken Biryani",
                "Salad",
                480.00m,
                456.00m,
                null);

            var analysis = new ComboAnalysis(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow.AddDays(-30),
                DateTimeOffset.UtcNow,
                129,
                0,
                0,
                [opportunity]);

            using var dialog = new SmartComboPreviewDialog(opportunity, analysis, canCreate: true, canDismiss: true);
            dialog.CreateControl();
            dialog.Show();
            Application.DoEvents();

            // 1. DPI-aware preferred size and within working area
            Assert.True(dialog.ClientSize.Width >= 680, $"Expected ClientSize.Width >= 680, was {dialog.ClientSize.Width}");
            Assert.True(dialog.ClientSize.Height >= 580, $"Expected ClientSize.Height >= 580, was {dialog.ClientSize.Height}");
            var work = Screen.FromControl(dialog).WorkingArea;
            Assert.True(dialog.Width <= work.Width);
            Assert.True(dialog.Height <= work.Height);

            // 2. Clean title without debug watermark
            Assert.Equal("Smart Combo Opportunity", dialog.Text);

            var controls = AllControls(dialog).ToList();

            // 3. Item grid exists with expected business columns visible
            var grid = controls.OfType<GridControl>().FirstOrDefault();
            Assert.NotNull(grid);
            var view = Assert.IsAssignableFrom<GridView>(grid.MainView);
            Assert.True(view.OptionsView.ColumnAutoWidth);
            Assert.NotNull(view.Columns[nameof(SmartComboPreviewDialog.PreviewItemRow.ProductName)]);
            Assert.NotNull(view.Columns[nameof(SmartComboPreviewDialog.PreviewItemRow.VariantName)]);
            Assert.NotNull(view.Columns[nameof(SmartComboPreviewDialog.PreviewItemRow.Quantity)]);
            Assert.NotNull(view.Columns[nameof(SmartComboPreviewDialog.PreviewItemRow.PriceText)]);
            Assert.Equal("Product", view.Columns[nameof(SmartComboPreviewDialog.PreviewItemRow.ProductName)].Caption);
            Assert.Equal("Variant / Portion", view.Columns[nameof(SmartComboPreviewDialog.PreviewItemRow.VariantName)].Caption);
            Assert.Equal("Qty", view.Columns[nameof(SmartComboPreviewDialog.PreviewItemRow.Quantity)].Caption);
            Assert.Equal("Price", view.Columns[nameof(SmartComboPreviewDialog.PreviewItemRow.PriceText)].Caption);

            // 4. Deal Price, Discount, Dismiss Reason visible
            var spins = controls.OfType<SpinEdit>().ToList();
            Assert.Equal(2, spins.Count);
            var priceSpin = spins.FirstOrDefault(s => s.Value == 456.00m);
            var discountSpin = spins.FirstOrDefault(s => s.Value == 5.00m);
            Assert.NotNull(priceSpin);
            Assert.NotNull(discountSpin);
            Assert.True(priceSpin.Visible);
            Assert.True(discountSpin.Visible);

            var reasonCombo = controls.OfType<ComboBoxEdit>().FirstOrDefault();
            Assert.NotNull(reasonCombo);
            Assert.True(reasonCombo.Visible);
            Assert.True(reasonCombo.Properties.Items.Count >= 4);

            // 5. Actions visible
            var createBtn = controls.OfType<SimpleButton>().FirstOrDefault(b => b.Text == "Create Deal");
            var dismissBtn = controls.OfType<SimpleButton>().FirstOrDefault(b => b.Text == "Dismiss");
            var closeBtn = controls.OfType<SimpleButton>().FirstOrDefault(b => b.Text == "Close");
            Assert.NotNull(createBtn);
            Assert.NotNull(dismissBtn);
            Assert.NotNull(closeBtn);
            Assert.True(createBtn.Visible && createBtn.Enabled);
            Assert.True(dismissBtn.Visible && dismissBtn.Enabled);
            Assert.True(closeBtn.Visible && closeBtn.Enabled);

            // 6. No controls outside client bounds
            foreach (var c in controls.Where(c => c.Visible && c.Parent == dialog))
            {
                Assert.True(c.Right <= dialog.ClientSize.Width + 2, $"{c} Right ({c.Right}) exceeds ClientSize.Width ({dialog.ClientSize.Width})");
                Assert.True(c.Bottom <= dialog.ClientSize.Height + 2, $"{c} Bottom ({c.Bottom}) exceeds ClientSize.Height ({dialog.ClientSize.Height})");
            }

            dialog.Close();
            Application.DoEvents();
        });
    }

    [Fact]
    public void QuickOrderTemplateEditForm_ItemsGrid_FillsAvailableWidth_And_ColumnsAreUsable()
    {
        RunSta(() =>
        {
            var options = new List<ProductOptionRow>
            {
                new(Guid.NewGuid(), Guid.NewGuid(), "Chicken Haleem", "Full Plate", 420.00m),
                new(Guid.NewGuid(), Guid.NewGuid(), "Garlic Nan", "Standard", 50.00m),
                new(Guid.NewGuid(), Guid.NewGuid(), "Fresh Salad", "Standard", 30.00m)
            };

            var existing = new QuickOrderTemplateEditModel(
                "Special Lunch Deal",
                "1 Chicken Haleem Full Plate + 2 Garlic Nan + 1 Fresh Salad",
                3,
                [
                    (options[0].VariantId, 1m, 420.00m),
                    (options[1].VariantId, 2m, 50.00m),
                    (options[2].VariantId, 1m, 30.00m)
                ],
                true);

            using var form = new QuickOrderTemplateEditForm("Edit Quick Order Template", options, existing);
            form.CreateControl();
            form.Show();
            Application.DoEvents();

            // 1. Clean title without debug watermark
            Assert.Equal("Edit Quick Order Template", form.Text);

            var controls = AllControls(form).ToList();

            // 2. Items grid fills available width (ColumnAutoWidth = true)
            var grid = controls.OfType<GridControl>().FirstOrDefault();
            Assert.NotNull(grid);
            var view = Assert.IsAssignableFrom<GridView>(grid.MainView);
            Assert.True(view.OptionsView.ColumnAutoWidth, "GridView.ColumnAutoWidth MUST be true to fill dialog width");

            // 3. Expected business columns exist and no unexplained technical columns
            Assert.Equal(6, view.Columns.Count);
            var colProduct = view.Columns[nameof(TemplateItemRow.ProductName)];
            var colVariant = view.Columns[nameof(TemplateItemRow.VariantName)];
            var colQty = view.Columns[nameof(TemplateItemRow.Quantity)];
            var colDealPrice = view.Columns[nameof(TemplateItemRow.UnitPriceText)];
            var colCatalogPrice = view.Columns[nameof(TemplateItemRow.CatalogPriceText)];
            var colTotal = view.Columns[nameof(TemplateItemRow.EffectiveTotalText)];

            Assert.NotNull(colProduct);
            Assert.NotNull(colVariant);
            Assert.NotNull(colQty);
            Assert.NotNull(colDealPrice);
            Assert.NotNull(colCatalogPrice);
            Assert.NotNull(colTotal);

            Assert.Equal("Product", colProduct.Caption);
            Assert.Equal("Variant / Portion", colVariant.Caption);
            Assert.Equal("Qty", colQty.Caption);
            Assert.Equal("Deal Price", colDealPrice.Caption);
            Assert.Equal("Catalog Price", colCatalogPrice.Caption);
            Assert.Equal("Line Total", colTotal.Caption);

            // 4. Columns usable widths
            Assert.True(colProduct.Width >= 180);
            Assert.True(colVariant.Width >= 120);
            Assert.True(colQty.Width >= 50);
            Assert.True(colDealPrice.Width >= 80);
            Assert.True(colCatalogPrice.Width >= 80);
            Assert.True(colTotal.Width >= 90);

            // 5. Grid remains substantial height and footer remains visible
            Assert.True(grid.Height >= 120, $"Grid height was {grid.Height}");
            var saveBtn = controls.OfType<SimpleButton>().FirstOrDefault(b => b.Text == "Save Changes");
            Assert.NotNull(saveBtn);
            Assert.True(saveBtn.Visible);

            form.Close();
            Application.DoEvents();
        });
    }

    [Fact]
    public void SmartComboBuilderView_GridColumns_Readable_And_PreviewAccessible()
    {
        RunSta(() =>
        {
            var services = new ServiceCollection();
            services.AddScoped<IMediator>(_ => new FakeMediator());
            services.AddScoped<ISmartComboAccess>(_ => new FakeAccess());
            using var provider = services.BuildServiceProvider();

            using var view = new SmartComboBuilderView(provider.GetRequiredService<IServiceScopeFactory>(), new());
            view.Size = new Size(1366, 768);
            view.CreateControl();
            Application.DoEvents();

            var controls = AllControls(view).ToList();
            var grid = controls.OfType<GridControl>().FirstOrDefault();
            Assert.NotNull(grid);
            var gridView = Assert.IsAssignableFrom<GridView>(grid.MainView);

            // Column captions readable
            Assert.Equal("Suggested Combo", gridView.Columns["Combo"].Caption);
            Assert.Equal("Items Composition", gridView.Columns["Items"].Caption);
            Assert.Equal("Bought Together", gridView.Columns["BoughtTogether"].Caption);
            Assert.Equal("Support", gridView.Columns["Support"].Caption);
            Assert.Equal("Attach Rate", gridView.Columns["AttachRate"].Caption);
            Assert.Equal("Lift", gridView.Columns["Lift"].Caption);
            Assert.Equal("Normal Price", gridView.Columns["NormalPrice"].Caption);
            Assert.Equal("Suggested Price", gridView.Columns["SuggestedPrice"].Caption);
            Assert.Equal("Discount", gridView.Columns["Discount"].Caption);
            Assert.Equal("Est. Margin", gridView.Columns["EstimatedMargin"].Caption);
            Assert.Equal("Status", gridView.Columns["Status"].Caption);

            // Suggested Price has generous width to fit caption + filter icon
            Assert.True(gridView.Columns["SuggestedPrice"].Width >= 130);
            Assert.True(gridView.Columns["SuggestedPrice"].MinWidth >= 120);

            // Preview button exists
            var previewBtn = controls.OfType<SimpleButton>().FirstOrDefault(b => b.Text == "Preview");
            Assert.NotNull(previewBtn);
        });
    }

    private sealed class FakeMediator : IMediator
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            Task.FromResult<TResponse>(default!);
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest =>
            Task.CompletedTask;
        public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
            Task.FromResult<object?>(null);
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
        public Task Publish(object notification, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification =>
            Task.CompletedTask;
    }

    private sealed class FakeAccess : ISmartComboAccess
    {
        public Task<Guid> RequireAsync(string operation, Guid warehouseId, CancellationToken ct) => Task.FromResult(Guid.NewGuid());
        public Task<IReadOnlyList<ComboLocation>> LocationsAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ComboLocation>>([new(Guid.NewGuid(), "Main Warehouse")]);
        public Task<ComboCurrency> CurrencyAsync(Guid warehouseId, CancellationToken ct) =>
            Task.FromResult(new ComboCurrency(Guid.NewGuid(), 2, "Rs."));
    }
}
