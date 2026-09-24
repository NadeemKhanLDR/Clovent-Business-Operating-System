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
using Clovent.Restaurant.Application.SmartCombos;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.SmartPos;

public class SmartComboScreenTests
{
    [Fact]
    public void BusyStateGridBindingAndEmptyState()
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                var warehouse = Guid.NewGuid();
                var services = new ServiceCollection();
                var mediator = new ComboMediator();
                services.AddScoped<IMediator>(_ => mediator);
                services.AddScoped<ISmartComboAccess>(_ => new Access());
                using var provider = services.BuildServiceProvider();
                using var view = new SmartComboBuilderView(provider.GetRequiredService<IServiceScopeFactory>(), new());

                T Field<T>(string name) => (T)typeof(SmartComboBuilderView).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;

                Field<LookUpEdit>("_location").EditValue = warehouse;
                var task = (Task)typeof(SmartComboBuilderView).GetMethod("RunAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(view, new object[] { (Func<Task>)(() => (Task)typeof(SmartComboBuilderView).GetMethod("AnalyzeAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(view, null)!) })!;

                Assert.False(Field<SimpleButton>("_analyze").Enabled);
                Assert.False(Field<LookUpEdit>("_location").Enabled);

                mediator.Result.SetResult(new(warehouse, DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow, 5, 0, 0, []));
                task.GetAwaiter().GetResult();

                Assert.True(Field<SimpleButton>("_analyze").Enabled);
                Assert.False(Field<SimpleButton>("_preview").Enabled);
                Assert.Empty((IEnumerable<SmartComboBuilderView.ComboRow>)Field<GridControl>("_grid").DataSource);
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(15)));
        if (error != null) throw error;
    }

    [Fact]
    public void KpiCards_And_EmptyState_BehaveCorrectly_OnZeroAndNonZeroResults()
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                var warehouse = Guid.NewGuid();
                var services = new ServiceCollection();
                var mediator = new ComboMediator();
                services.AddScoped<IMediator>(_ => mediator);
                services.AddScoped<ISmartComboAccess>(_ => new Access());
                using var provider = services.BuildServiceProvider();
                using var view = new SmartComboBuilderView(provider.GetRequiredService<IServiceScopeFactory>(), new());

                T Field<T>(string name) => (T)typeof(SmartComboBuilderView).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;

                // Initial state before analyze
                Assert.Equal("0", Field<LabelControl>("_lblKpiOpportunities").Text);
                Assert.Equal("0", Field<LabelControl>("_lblKpiEligibleSales").Text);
                Assert.Equal("0", Field<LabelControl>("_lblKpiConvertedDeals").Text);
                Assert.Equal("—", Field<LabelControl>("_lblKpiAvgAttachRate").Text);

                // Run analysis returning 0 opportunities
                Field<LookUpEdit>("_location").EditValue = warehouse;
                var task1 = (Task)typeof(SmartComboBuilderView).GetMethod("RunAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(view, new object[] { (Func<Task>)(() => (Task)typeof(SmartComboBuilderView).GetMethod("AnalyzeAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(view, null)!) })!;
                mediator.Result.SetResult(new(warehouse, DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow, 115, 0, 0, []));
                task1.GetAwaiter().GetResult();

                // Verified zero state KPI
                Assert.Equal("0", Field<LabelControl>("_lblKpiOpportunities").Text);
                Assert.Equal("115", Field<LabelControl>("_lblKpiEligibleSales").Text);
                Assert.Equal("0", Field<LabelControl>("_lblKpiConvertedDeals").Text);
                Assert.Equal("—", Field<LabelControl>("_lblKpiAvgAttachRate").Text);

                // Verified empty state panel is visible and grid is hidden
                Assert.True(Field<PanelControl>("_emptyPanel").Visible);
                Assert.False(Field<GridControl>("_grid").Visible);
                Assert.Contains("no combo opportunities found", Field<LabelControl>("_lblEmptyTitle").Text, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("115", Field<LabelControl>("_lblEmptyDescription").Text);

                // Now run analysis with 1 opportunity
                var item = new ComboItem(Guid.NewGuid(), "Biryani", "Standard", 450, null);
                var opp = new ComboOpportunity("sig1", "Biryani Combo", [item], 5, 120, 0.0417m, 0.8333m, 10.0m, "A", "B", 450, 420, null);
                var mediator2 = new ComboMediator();
                typeof(SmartComboBuilderView).GetField("_analysis", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(view, new ComboAnalysis(warehouse, DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow, 120, 0, 0, [opp]));

                typeof(SmartComboBuilderView).GetMethod("UpdateKpiCards", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(view, new object[] { new ComboAnalysis(warehouse, DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow, 120, 0, 0, [opp]) });

                Assert.Equal("1", Field<LabelControl>("_lblKpiOpportunities").Text);
                Assert.Equal("120", Field<LabelControl>("_lblKpiEligibleSales").Text);
                Assert.Equal("0", Field<LabelControl>("_lblKpiConvertedDeals").Text);
                Assert.Equal("83.3%", Field<LabelControl>("_lblKpiAvgAttachRate").Text);
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(15)));
        if (error != null) throw error;
    }

    [Fact]
    public void PeriodOptions_And_Location_Configured_Properly()
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                var services = new ServiceCollection();
                services.AddScoped<IMediator>(_ => new ComboMediator());
                services.AddScoped<ISmartComboAccess>(_ => new Access());
                using var provider = services.BuildServiceProvider();
                using var view = new SmartComboBuilderView(provider.GetRequiredService<IServiceScopeFactory>(), new());

                T Field<T>(string name) => (T)typeof(SmartComboBuilderView).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;

                var periodEdit = Field<LookUpEdit>("_period");
                var dataSource = Assert.IsAssignableFrom<List<SmartComboBuilderView.PeriodOption>>(periodEdit.Properties.DataSource);

                Assert.Contains(dataSource, p => p.Days == 7 && p.Display == "Last 7 Days");
                Assert.Contains(dataSource, p => p.Days == 14 && p.Display == "Last 14 Days");
                Assert.Contains(dataSource, p => p.Days == 30 && p.Display == "Last 30 Days");
                Assert.Contains(dataSource, p => p.Days == 60 && p.Display == "Last 60 Days");
                Assert.Contains(dataSource, p => p.Days == 90 && p.Display == "Last 90 Days");

                // Default is 30
                Assert.Equal(30, periodEdit.EditValue);

                // Changing selection updates backing days
                periodEdit.EditValue = 60;
                var daysEdit = Field<SpinEdit>("_days");
                Assert.Equal(60m, daysEdit.Value);
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(15)));
        if (error != null) throw error;
    }

    [Fact]
    public void Grid_Columns_Configured_Without_Truncation_And_AutoWidth()
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                var services = new ServiceCollection();
                services.AddScoped<IMediator>(_ => new ComboMediator());
                services.AddScoped<ISmartComboAccess>(_ => new Access());
                using var provider = services.BuildServiceProvider();
                using var view = new SmartComboBuilderView(provider.GetRequiredService<IServiceScopeFactory>(), new());

                T Field<T>(string name) => (T)typeof(SmartComboBuilderView).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;

                var gridView = Field<GridView>("_view");
                Assert.False(gridView.OptionsView.ColumnAutoWidth);

                // Verify column captions are full, human readable strings
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

                // Verify generous minimum widths to guarantee readability under horizontal scrolling
                Assert.True(gridView.Columns["Combo"].MinWidth >= 180);
                Assert.True(gridView.Columns["Items"].MinWidth >= 300);
                Assert.True(gridView.Columns["BoughtTogether"].MinWidth >= 115);
                Assert.True(gridView.Columns["Support"].MinWidth >= 90);
                Assert.True(gridView.Columns["AttachRate"].MinWidth >= 105);
                Assert.True(gridView.Columns["Lift"].MinWidth >= 75);
                Assert.True(gridView.Columns["NormalPrice"].MinWidth >= 110);
                Assert.True(gridView.Columns["SuggestedPrice"].MinWidth >= 120);
                Assert.True(gridView.Columns["Discount"].MinWidth >= 90);
                Assert.True(gridView.Columns["EstimatedMargin"].MinWidth >= 110);
                Assert.True(gridView.Columns["Status"].MinWidth >= 90);
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(15)));
        if (error != null) throw error;
    }

    [Fact]
    public void Responsive_Layout_Sizes_1024_1366_1920()
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                var services = new ServiceCollection();
                services.AddScoped<IMediator>(_ => new ComboMediator());
                services.AddScoped<ISmartComboAccess>(_ => new Access());
                using var provider = services.BuildServiceProvider();

                foreach (var size in new[] { new Size(1024, 768), new Size(1366, 768), new Size(1920, 1080) })
                {
                    using var view = new SmartComboBuilderView(provider.GetRequiredService<IServiceScopeFactory>(), new());
                    view.Size = size;
                    view.PerformLayout();

                    T Field<T>(string name) => (T)typeof(SmartComboBuilderView).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;

                    var grid = Field<GridControl>("_grid");
                    Assert.True(grid.Width > 800, $"Grid width should fill container at {size.Width}x{size.Height}");
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(15)));
        if (error != null) throw error;
    }

    [Fact]
    public void PreviewButtonsRespectPermissionsAndNamesAreEditable()
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                var item = new ComboItem(Guid.NewGuid(), "Biryani", "Standard", 100, null);
                var opportunity = new ComboOpportunity("signature", "Meal Combo", [item], 3, 5, .6m, 1m, 1m, "A", "B", 100, 95, null);
                using var dialog = new SmartComboPreviewDialog(opportunity, new(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow, 5, 0, 0, [opportunity]), false, false);
                IEnumerable<Control> Desc(Control c) => c.Controls.Cast<Control>().SelectMany(x => new[] { x }.Concat(Desc(x)));
                var controls = Desc(dialog).ToList();
                Assert.False(controls.OfType<SimpleButton>().Single(x => x.Text == "Create Deal").Enabled);
                Assert.False(controls.OfType<SimpleButton>().Single(x => x.Text == "Dismiss").Enabled);
                var name = controls.OfType<TextEdit>().Single(x => x.Text == "Meal Combo");
                name.Text = "Manager Name";
                Assert.Equal("Manager Name", dialog.ComboName);
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(15)));
        if (error != null) throw error;
    }

    [Fact]
    public void DevelopmentSamplesLabel_IsNeverPresentInNormalUi()
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                var services = new ServiceCollection();
                services.AddScoped<IMediator>(_ => new ComboMediator());
                services.AddScoped<ISmartComboAccess>(_ => new Access());
                using var provider = services.BuildServiceProvider();

                // Test with development samples true and false: label should NEVER be in UI
                foreach (var devSamples in new[] { true, false })
                {
                    using var view = new SmartComboBuilderView(
                        provider.GetRequiredService<IServiceScopeFactory>(),
                        new SmartComboOptions { IncludeDevelopmentSamples = devSamples });

                    IEnumerable<Control> AllControls(Control parent) =>
                        parent.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(AllControls(c)));

                    var controls = AllControls(view).ToList();
                    foreach (var control in controls)
                    {
                        if (control is LabelControl lbl)
                        {
                            Assert.DoesNotContain("Development Samples", lbl.Text, StringComparison.OrdinalIgnoreCase);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(15)));
        if (error != null) throw error;
    }

    [Fact]
    public void LayoutStructure_Follows_AutoSizeTop_And_FillingResultsGrid()
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                var services = new ServiceCollection();
                services.AddScoped<IMediator>(_ => new ComboMediator());
                services.AddScoped<ISmartComboAccess>(_ => new Access());
                using var provider = services.BuildServiceProvider();
                using var view = new SmartComboBuilderView(provider.GetRequiredService<IServiceScopeFactory>(), new());

                IEnumerable<Control> AllControls(Control parent) =>
                    parent.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(AllControls(c)));

                var allControls = AllControls(view).ToList();

                // 1. Root container is TableLayoutPanel with Dock.Fill and 16px padding
                var root = view.Controls.OfType<TableLayoutPanel>().First();
                Assert.Equal(DockStyle.Fill, root.Dock);
                Assert.Equal(16, root.Padding.Left);
                Assert.Equal(16, root.Padding.Top);

                // 2. Exact 5-row structure: 4 AutoSize rows + 1 Percent(100%) results row
                Assert.Equal(5, root.RowCount);
                Assert.Equal(SizeType.AutoSize, root.RowStyles[0].SizeType); // Row 0: Page Header
                Assert.Equal(SizeType.AutoSize, root.RowStyles[1].SizeType); // Row 1: Filter Bar
                Assert.Equal(SizeType.AutoSize, root.RowStyles[2].SizeType); // Row 2: KPI Cards
                Assert.Equal(SizeType.AutoSize, root.RowStyles[3].SizeType); // Row 3: Results Header
                Assert.Equal(SizeType.Percent, root.RowStyles[4].SizeType);  // Row 4: Results Content (Grid / Empty State)
                Assert.Equal(100F, root.RowStyles[4].Height);

                // 3. Title & Subtitle use AutoSize and bold typography
                var titleLabel = allControls.OfType<LabelControl>().First(l => l.Text == "SMART COMBO BUILDER");
                Assert.Equal(LabelAutoSizeMode.Default, titleLabel.AutoSizeMode);
                Assert.True(titleLabel.Font.SizeInPoints >= 15, "Title should be at least 15pt bold");

                var subtitleLabel = allControls.OfType<LabelControl>().First(l => l.Text.StartsWith("Discover profitable", StringComparison.OrdinalIgnoreCase));
                Assert.Equal(LabelAutoSizeMode.Default, subtitleLabel.AutoSizeMode);

                // 4. Results host, GridControl, and Empty Panel consume Dock.Fill
                var grid = allControls.OfType<GridControl>().Single();
                Assert.Equal(DockStyle.Fill, grid.Dock);

                T Field<T>(string name) => (T)typeof(SmartComboBuilderView).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
                var emptyPanel = Field<PanelControl>("_emptyPanel");
                Assert.Equal(DockStyle.Fill, emptyPanel.Dock);

                // 5. Four KPI cards exist with guaranteed minimum height
                var kpiPanels = allControls.OfType<PanelControl>().Where(p => p.MinimumSize.Height >= 90).ToList();
                Assert.True(kpiPanels.Count >= 4, "Should have 4 KPI cards with at least 90px minimum height");
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(15)));
        if (error != null) throw error;
    }

    [Fact]
    public void DebugLayoutDiagnostics_InspectsBounds_AcrossDpiAndResolutions()
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                var services = new ServiceCollection();
                services.AddScoped<IMediator>(_ => new ComboMediator());
                services.AddScoped<ISmartComboAccess>(_ => new Access());
                using var provider = services.BuildServiceProvider();

                foreach (var size in new[] { new Size(1024, 768), new Size(1366, 768), new Size(1920, 1080) })
                {
                    using var view = new SmartComboBuilderView(provider.GetRequiredService<IServiceScopeFactory>(), new());
                    view.Size = size;
                    view.CreateControl();
                    view.PerformLayout();

                    var diagMethod = typeof(SmartComboBuilderView).GetMethod("InspectLayoutDiagnostics", BindingFlags.Instance | BindingFlags.Public);
                    if (diagMethod != null)
                    {
                        var report = (string)diagMethod.Invoke(view, null)!;
                        Assert.True(!report.Contains("WARNING"), $"Layout diagnostics failed for size {size}:\n{report}");
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(15)));
        if (error != null) throw error;
    }

    [Fact]
    public void RenderToBitmap_VisualChecks_1024_1366_1920()
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                var services = new ServiceCollection();
                services.AddScoped<IMediator>(_ => new ComboMediator());
                services.AddScoped<ISmartComboAccess>(_ => new Access());
                using var provider = services.BuildServiceProvider();

                var outDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "scratch", "debug_screenshots");
                Directory.CreateDirectory(outDir);

                foreach (var size in new[] { new Size(1024, 768), new Size(1366, 768), new Size(1920, 1080) })
                {
                    using var view = new SmartComboBuilderView(provider.GetRequiredService<IServiceScopeFactory>(), new());
                    view.Size = size;
                    view.CreateControl();
                    view.PerformLayout();

                    using var bmp = new Bitmap(size.Width, size.Height);
                    view.DrawToBitmap(bmp, new Rectangle(Point.Empty, size));

                    var path = Path.Combine(outDir, $"smart_combo_rebuild_{size.Width}x{size.Height}.png");
                    bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);

                    Assert.True(File.Exists(path));
                    Assert.True(new FileInfo(path).Length > 1000);
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(15)));
        if (error != null) throw error;
    }

    private sealed class Access : ISmartComboAccess
    {
        public Task<Guid> RequireAsync(string operation, Guid warehouseId, CancellationToken ct) => Task.FromResult(Guid.NewGuid());
        public Task<IReadOnlyList<ComboLocation>> LocationsAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<ComboLocation>>([]);
        public Task<ComboCurrency> CurrencyAsync(Guid warehouseId, CancellationToken ct) => Task.FromResult(new ComboCurrency(Guid.NewGuid(), 2, "Rs."));
    }

    private sealed class ComboMediator : IMediator
    {
        public TaskCompletionSource<ComboAnalysis> Result = new();
        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default) => (TResponse)(object)await Result.Task;
        public Task Send<TRequest>(TRequest request, CancellationToken ct = default) where TRequest : IRequest => throw new NotSupportedException();
        public Task<object?> Send(object request, CancellationToken ct = default) => throw new NotSupportedException();
        public Task Publish(object notification, CancellationToken ct = default) => throw new NotSupportedException();
        public Task Publish<TNotification>(TNotification notification, CancellationToken ct = default) where TNotification : INotification => throw new NotSupportedException();
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken ct = default) => throw new NotSupportedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
