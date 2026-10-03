using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Restaurant.SmartPos;
using Clovent.Desktop.Sessions;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.SmartPos;

public class UpsellPerformanceViewTests
{
    private sealed class FakeCurrentSession : ICurrentSession
    {
        public Guid? UserId { get; set; } = Guid.NewGuid();
        public Guid? SessionId { get; set; } = Guid.NewGuid();
        public string? DisplayName { get; set; } = "Manager User";
        public bool IsAuthenticated => UserId.HasValue;
        public void SignIn(Guid userId, Guid sessionId, string displayName) { }
        public void SignOut() { }
#pragma warning disable CS0067
        public event EventHandler? Changed;
#pragma warning restore CS0067
    }

    private sealed class FakeMediator : IMediator
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            Task.FromResult(default(TResponse)!);

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest => Task.CompletedTask;
        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => Task.FromResult<object?>(null);
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class FakeServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = [];
        public void Register<T>(T service) where T : class => _services[typeof(T)] = service;
        public object? GetService(Type serviceType) => _services.TryGetValue(serviceType, out var service) ? service : null;
    }

    private sealed class FakeServiceScope(IServiceProvider serviceProvider) : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = serviceProvider;
        public void Dispose() { }
    }

    private sealed class FakeServiceScopeFactory(IServiceProvider serviceProvider) : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new FakeServiceScope(serviceProvider);
    }

    private static UpsellPerformanceView CreateView()
    {
        var session = new FakeCurrentSession();
        var mediator = new FakeMediator();
        var provider = new FakeServiceProvider();
        provider.Register<IMediator>(mediator);
        provider.Register<ILogger<UpsellPerformanceView>>(NullLogger<UpsellPerformanceView>.Instance);

        return new UpsellPerformanceView(new FakeServiceScopeFactory(provider), session);
    }

    [Fact]
    public void UpsellPerformanceView_HasEnterpriseHeader_AndSubtitle()
    {
        using var view = CreateView();

        var headerField = typeof(UpsellPerformanceView).GetField("_headerPanel", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(headerField);
        var headerPanel = (PanelControl)headerField.GetValue(view)!;
        Assert.NotNull(headerPanel);

        var labels = GetAllControls(headerPanel).OfType<LabelControl>().ToList();
        var titleLabel = labels.FirstOrDefault(l => l.Text == "UPSELL PERFORMANCE");
        var subLabel = labels.FirstOrDefault(l => l.Text.Contains("Analyze recommendation effectiveness"));

        Assert.NotNull(titleLabel);
        Assert.NotNull(subLabel);
    }

    private static List<Control> GetAllControls(Control root)
    {
        var list = new List<Control>();
        foreach (Control c in root.Controls)
        {
            list.Add(c);
            list.AddRange(GetAllControls(c));
        }
        return list;
    }

    [Fact]
    public void UpsellPerformanceView_PeriodCombo_DefaultsToLast30Days_AndHasAllPresets()
    {
        using var view = CreateView();

        var periodField = typeof(UpsellPerformanceView).GetField("_periodCombo", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(periodField);
        var periodCombo = (ComboBoxEdit)periodField.GetValue(view)!;
        Assert.NotNull(periodCombo);

        Assert.Equal("Last 30 Days", periodCombo.SelectedItem?.ToString());

        var items = periodCombo.Properties.Items.Cast<object>().Select(o => o.ToString()).ToList();
        Assert.Contains("Today", items);
        Assert.Contains("Yesterday", items);
        Assert.Contains("This Week", items);
        Assert.Contains("Last Week", items);
        Assert.Contains("This Month", items);
        Assert.Contains("Last Month", items);
        Assert.Contains("Last 7 Days", items);
        Assert.Contains("Last 30 Days", items);
        Assert.Contains("Custom", items);
    }

    [Fact]
    public void UpsellPerformanceView_PeriodChange_UpdatesDateEdits()
    {
        using var view = CreateView();

        var periodField = typeof(UpsellPerformanceView).GetField("_periodCombo", BindingFlags.Instance | BindingFlags.NonPublic);
        var fromField = typeof(UpsellPerformanceView).GetField("_fromEdit", BindingFlags.Instance | BindingFlags.NonPublic);
        var toField = typeof(UpsellPerformanceView).GetField("_toEdit", BindingFlags.Instance | BindingFlags.NonPublic);

        var periodCombo = (ComboBoxEdit)periodField!.GetValue(view)!;
        var fromEdit = (DateEdit)fromField!.GetValue(view)!;
        var toEdit = (DateEdit)toField!.GetValue(view)!;

        // Change to Today
        periodCombo.SelectedItem = "Today";
        Assert.Equal(DateTime.Today, fromEdit.DateTime.Date);
        Assert.Equal(DateTime.Today, toEdit.DateTime.Date);

        // Change to Yesterday
        periodCombo.SelectedItem = "Yesterday";
        Assert.Equal(DateTime.Today.AddDays(-1), fromEdit.DateTime.Date);
        Assert.Equal(DateTime.Today.AddDays(-1), toEdit.DateTime.Date);
    }

    [Fact]
    public void UpsellPerformanceView_Toolbar_IsSingleRowTableLayoutPanel()
    {
        using var view = CreateView();

        var toolbarField = typeof(UpsellPerformanceView).GetField("_toolbar", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(toolbarField);
        var toolbar = Assert.IsType<TableLayoutPanel>(toolbarField.GetValue(view));

        Assert.Equal(1, toolbar.RowCount);
        Assert.Equal(4, toolbar.ColumnCount);
        Assert.Equal(DockStyle.Fill, toolbar.Dock);
    }

    [Fact]
    public void UpsellPerformanceView_GridColumns_HaveFullCaptions_AndSufficientWidth()
    {
        using var view = CreateView();

        var bindMethod = typeof(UpsellPerformanceView).GetMethod("BindGrid", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(bindMethod);
        bindMethod.Invoke(view, [new List<Clovent.Restaurant.Application.SmartRecommendations.Dtos.UpsellPerformanceDto>()]);

        var gridField = typeof(UpsellPerformanceView).GetField("_gridControl", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(gridField);
        var grid = (DevExpress.XtraGrid.GridControl)gridField.GetValue(view)!;
        var gridView = (DevExpress.XtraGrid.Views.Grid.GridView)grid.MainView;

        // G. Full captions exist without ellipsis
        Assert.Equal("Recommended Item", gridView.Columns["ProductName"].Caption);
        Assert.Equal("Variant", gridView.Columns["VariantName"].Caption);
        Assert.Equal("Offers", gridView.Columns["Offers"].Caption);
        Assert.Equal("Accepted", gridView.Columns["Accepted"].Caption);
        Assert.Equal("Dismissed", gridView.Columns["Dismissed"].Caption);
        Assert.Equal("Conversion %", gridView.Columns["ConversionText"].Caption);
        Assert.Equal("Upsell Revenue", gridView.Columns["UpsellRevenueText"].Caption);

        // Alignment: Text Near, Numeric Far
        Assert.Equal(DevExpress.Utils.HorzAlignment.Near, gridView.Columns["ProductName"].AppearanceCell.TextOptions.HAlignment);
        Assert.Equal(DevExpress.Utils.HorzAlignment.Near, gridView.Columns["VariantName"].AppearanceCell.TextOptions.HAlignment);
        Assert.Equal(DevExpress.Utils.HorzAlignment.Far, gridView.Columns["Offers"].AppearanceCell.TextOptions.HAlignment);
        Assert.Equal(DevExpress.Utils.HorzAlignment.Far, gridView.Columns["Accepted"].AppearanceCell.TextOptions.HAlignment);
        Assert.Equal(DevExpress.Utils.HorzAlignment.Far, gridView.Columns["Dismissed"].AppearanceCell.TextOptions.HAlignment);
        Assert.Equal(DevExpress.Utils.HorzAlignment.Far, gridView.Columns["ConversionText"].AppearanceCell.TextOptions.HAlignment);
        Assert.Equal(DevExpress.Utils.HorzAlignment.Far, gridView.Columns["UpsellRevenueText"].AppearanceCell.TextOptions.HAlignment);

        // H. Proportions: Recommended Item does not monopolize grid (target: 38-42%, not 60-70%)
        int totalWidth = gridView.Columns.Cast<DevExpress.XtraGrid.Columns.GridColumn>().Sum(c => c.Width);
        Assert.True(totalWidth > 0);

        double recItemPct = (double)gridView.Columns["ProductName"].Width / totalWidth * 100.0;
        double variantPct = (double)gridView.Columns["VariantName"].Width / totalWidth * 100.0;
        double offersPct = (double)gridView.Columns["Offers"].Width / totalWidth * 100.0;
        double acceptedPct = (double)gridView.Columns["Accepted"].Width / totalWidth * 100.0;
        double dismissedPct = (double)gridView.Columns["Dismissed"].Width / totalWidth * 100.0;
        double convPct = (double)gridView.Columns["ConversionText"].Width / totalWidth * 100.0;
        double revPct = (double)gridView.Columns["UpsellRevenueText"].Width / totalWidth * 100.0;

        // Recommended Item must be between 38% and 42%, NOT 60-70%
        Assert.InRange(recItemPct, 38.0, 42.0);
        Assert.InRange(variantPct, 12.0, 14.0);
        Assert.InRange(offersPct, 6.0, 7.5);
        Assert.InRange(acceptedPct, 8.0, 9.5);
        Assert.InRange(dismissedPct, 8.0, 9.5);
        Assert.InRange(convPct, 10.0, 11.5);
        Assert.InRange(revPct, 11.0, 13.5);

        // Safe MinWidths at current DPI
        Assert.True(gridView.Columns["Accepted"].MinWidth >= 80);
        Assert.True(gridView.Columns["Dismissed"].MinWidth >= 85);
        Assert.True(gridView.Columns["ConversionText"].MinWidth >= 100);
        Assert.True(gridView.Columns["UpsellRevenueText"].MinWidth >= 115);
    }

    [Fact]
    public void UpsellPerformanceView_At240Dpi_CaptionsHaveSufficientRenderedWidth()
    {
        // I. Verify at simulated 240 DPI (2.5x scaling) that header text + glyph padding does not exceed scaled MinWidth
        int dpi = 240;
        using var font240 = new System.Drawing.Font("Segoe UI", (float)(9.5 * dpi / 96.0), System.Drawing.FontStyle.Bold);

        var columnsToCheck = new (string Caption, int LogicalMinWidth)[]
        {
            ("Recommended Item", 160),
            ("Variant", 85),
            ("Offers", 55),
            ("Accepted", 80),
            ("Dismissed", 85),
            ("Conversion %", 100),
            ("Upsell Revenue", 115)
        };

        foreach (var (caption, logicalMin) in columnsToCheck)
        {
            int textWidth = TextRenderer.MeasureText(caption, font240).Width;
            int scaledMin = (int)Math.Round(logicalMin * dpi / 96.0);

            // The scaled minimum width must easily fit the caption at 240 DPI
            Assert.True(scaledMin >= textWidth,
                $"Caption '{caption}' at 240 DPI requires {textWidth}px but scaled min width is only {scaledMin}px.");
        }
    }
}
