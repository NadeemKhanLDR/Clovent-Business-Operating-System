using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Restaurant.EndOfDay;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.EndOfDay;

public class EndOfDayReportViewTests
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

    private sealed class ReportTestMediator : IMediator
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

    private sealed class AllowAllFeaturePolicy : IFeatureAuthorizationPolicy
    {
        public Task<bool> CanUseFeatureAsync(Guid userId, string featureCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private static (EndOfDayReportView View, ReportTestMediator Mediator) CreateView()
    {
        var session = new FakeCurrentSession();
        var mediator = new ReportTestMediator();
        var provider = new FakeServiceProvider();
        provider.Register<IMediator>(mediator);
        provider.Register<IFeatureAuthorizationPolicy>(new AllowAllFeaturePolicy());

        var view = new EndOfDayReportView(new FakeServiceScopeFactory(provider), session);
        return (view, mediator);
    }

    [Fact]
    public void EndOfDayReportView_HasPeriodCombo_AndPreselectedToday()
    {
        var (view, _) = CreateView();
        using (view)
        {
            var periodCombo = (ComboBoxEdit)typeof(EndOfDayReportView).GetField("_periodCombo", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var fromDate = (DateEdit)typeof(EndOfDayReportView).GetField("_fromDateEdit", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var toDate = (DateEdit)typeof(EndOfDayReportView).GetField("_toDateEdit", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;

            Assert.NotNull(periodCombo);
            Assert.NotNull(fromDate);
            Assert.NotNull(toDate);

            // Default period is Today
            Assert.Equal("Today", periodCombo.SelectedItem?.ToString());
            Assert.Equal(DateTime.Today, fromDate.DateTime.Date);
            Assert.Equal(DateTime.Today, toDate.DateTime.Date);
        }
    }

    [Fact]
    public void EndOfDayReportView_ChangingPeriodUpdatesDates_AndManualDateEditSwitchesToCustom()
    {
        var (view, _) = CreateView();
        using (view)
        {
            var periodCombo = (ComboBoxEdit)typeof(EndOfDayReportView).GetField("_periodCombo", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var fromDate = (DateEdit)typeof(EndOfDayReportView).GetField("_fromDateEdit", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var toDate = (DateEdit)typeof(EndOfDayReportView).GetField("_toDateEdit", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;

            // Switch to Yesterday
            periodCombo.SelectedItem = "Yesterday";
            Assert.Equal(DateTime.Today.AddDays(-1), fromDate.DateTime.Date);
            Assert.Equal(DateTime.Today.AddDays(-1), toDate.DateTime.Date);

            // Switch to This Month
            periodCombo.SelectedItem = "This Month";
            var expectedMonthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            Assert.Equal(expectedMonthStart, fromDate.DateTime.Date);

            // Manual date change switches to Custom
            fromDate.DateTime = DateTime.Today.AddDays(-20);
            Assert.Equal("Custom", periodCombo.SelectedItem?.ToString());
        }
    }

    [Fact]
    public void EndOfDayReportView_RedundantDateButtonsDoNotExist_AndGenerateAndPrintVisible()
    {
        var (view, _) = CreateView();
        using (view)
        {
            // Verify _todayButton and _yesterdayButton fields do not exist on the type
            var todayField = typeof(EndOfDayReportView).GetField("_todayButton", BindingFlags.Instance | BindingFlags.NonPublic);
            var yesterdayField = typeof(EndOfDayReportView).GetField("_yesterdayButton", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.Null(todayField);
            Assert.Null(yesterdayField);

            var generateButton = (SimpleButton)typeof(EndOfDayReportView).GetField("_generateButton", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var printButton = (SimpleButton)typeof(EndOfDayReportView).GetField("_printSummaryButton", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;

            Assert.NotNull(generateButton);
            Assert.Equal("Generate", generateButton.Text);
            Assert.True(generateButton.Visible);

            Assert.NotNull(printButton);
            Assert.Equal("Print Summary", printButton.Text);
            Assert.True(printButton.Visible);

            // Verify no shortcut buttons named Today or Yesterday exist in the control hierarchy
            var allButtons = GetAllControls(view).OfType<SimpleButton>().ToList();
            Assert.DoesNotContain(allButtons, b => b.Text == "Today");
            Assert.DoesNotContain(allButtons, b => b.Text == "Yesterday");
        }
    }

    [Fact]
    public void EndOfDayReportView_PeriodDropdown_ContainsStandardPresets()
    {
        var (view, _) = CreateView();
        using (view)
        {
            var periodCombo = (ComboBoxEdit)typeof(EndOfDayReportView).GetField("_periodCombo", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var items = periodCombo.Properties.Items.Cast<object>().Select(o => o.ToString()).ToList();

            Assert.Contains("Today", items);
            Assert.Contains("Yesterday", items);
            Assert.Contains("This Week", items);
            Assert.Contains("Last Week", items);
            Assert.Contains("This Month", items);
            Assert.Contains("Last Month", items);
            Assert.Contains("This Quarter", items);
            Assert.Contains("Last Quarter", items);
            Assert.Contains("This Year", items);
            Assert.Contains("Last Year", items);
            Assert.Contains("Custom", items);
        }
    }

    [Fact]
    public void EndOfDayReportView_FilterControlsDoNotOverlap_TabsAndKpiCardsVisible()
    {
        var (view, _) = CreateView();
        using (view)
        {
            view.Size = new System.Drawing.Size(1366, 768);
            view.CreateControl();
            view.PerformLayout();

            // Verify all 6 tabs exist
            var tabControl = GetAllControls(view).OfType<DevExpress.XtraTab.XtraTabControl>().FirstOrDefault();
            Assert.NotNull(tabControl);
            Assert.Equal(6, tabControl.TabPages.Count);

            var tabTitles = tabControl.TabPages.Select(p => p.Text).ToList();
            Assert.Contains("Summary", tabTitles);
            Assert.Contains("Top Selling Items", tabTitles);
            Assert.Contains("Cash Summary", tabTitles);
            Assert.Contains("Bills", tabTitles);
            Assert.Contains("Inventory Movement", tabTitles);
            Assert.Contains("Stock Remaining", tabTitles);

            // Verify KPI cards exist on Summary page
            var summaryPage = tabControl.TabPages[0];
            var cardsRow = summaryPage.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
            Assert.NotNull(cardsRow);
            Assert.Equal(4, cardsRow.ColumnCount);

            // Verify secondary metrics strip exists
            var secondary = summaryPage.Controls.OfType<FlowLayoutPanel>().FirstOrDefault();
            Assert.NotNull(secondary);
            var voidedLabel = (LabelControl)typeof(EndOfDayReportView).GetField("_voidedCountLabel", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var averageLabel = (LabelControl)typeof(EndOfDayReportView).GetField("_averageSaleLabel", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            Assert.NotNull(voidedLabel);
            Assert.NotNull(averageLabel);
        }
    }

    [Fact]
    public void EndOfDayReportView_MainToolbar_HasConsolidatedActionButtons_BeforePrintSummary()
    {
        var (view, _) = CreateView();
        using (view)
        {
            var previewBtn = (SimpleButton)typeof(EndOfDayReportView).GetField("_previewButton", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var printBtn = (SimpleButton)typeof(EndOfDayReportView).GetField("_printButton", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var exportPdfBtn = (SimpleButton)typeof(EndOfDayReportView).GetField("_exportPdfButton", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var exportExcelBtn = (SimpleButton)typeof(EndOfDayReportView).GetField("_exportExcelButton", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var printSummaryBtn = (SimpleButton)typeof(EndOfDayReportView).GetField("_printSummaryButton", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;

            Assert.NotNull(previewBtn);
            Assert.NotNull(printBtn);
            Assert.NotNull(exportPdfBtn);
            Assert.NotNull(exportExcelBtn);
            Assert.NotNull(printSummaryBtn);

            Assert.Equal("Preview", previewBtn.Text);
            Assert.Equal("Print", printBtn.Text);
            Assert.Equal("Export PDF", exportPdfBtn.Text);
            Assert.Equal("Export Excel", exportExcelBtn.Text);
            Assert.Equal("Print Summary", printSummaryBtn.Text);

            var parent = previewBtn.Parent;
            Assert.NotNull(parent);
            Assert.Same(parent, printBtn.Parent);
            Assert.Same(parent, exportPdfBtn.Parent);
            Assert.Same(parent, exportExcelBtn.Parent);
            Assert.Same(parent, printSummaryBtn.Parent);

            // Verify order: Preview, Print, Export PDF, Export Excel all come before Print Summary
            int previewIdx = parent.Controls.GetChildIndex(previewBtn);
            int printIdx = parent.Controls.GetChildIndex(printBtn);
            int pdfIdx = parent.Controls.GetChildIndex(exportPdfBtn);
            int excelIdx = parent.Controls.GetChildIndex(exportExcelBtn);
            int summaryIdx = parent.Controls.GetChildIndex(printSummaryBtn);

            Assert.True(previewIdx < summaryIdx);
            Assert.True(printIdx < summaryIdx);
            Assert.True(pdfIdx < summaryIdx);
            Assert.True(excelIdx < summaryIdx);
        }
    }

    [Fact]
    public void EndOfDayReportView_TabPages_DoNotContainRedundantToolbars()
    {
        var (view, _) = CreateView();
        using (view)
        {
            var tabControl = GetAllControls(view).OfType<DevExpress.XtraTab.XtraTabControl>().FirstOrDefault();
            Assert.NotNull(tabControl);

            // The 5 detail grid pages should contain the GridControl directly and no redundant toolbars
            var detailPages = tabControl.TabPages.Skip(1).ToList(); // Skip Summary tab
            Assert.Equal(5, detailPages.Count);

            foreach (var page in detailPages)
            {
                // Verify grid control is docked directly
                var grid = page.Controls.OfType<DevExpress.XtraGrid.GridControl>().FirstOrDefault();
                Assert.NotNull(grid);
                Assert.Equal(DockStyle.Fill, grid.Dock);

                // Verify there are no nested FlowLayoutPanels or toolbars inside the detail page
                var toolbars = page.Controls.OfType<FlowLayoutPanel>().ToList();
                Assert.Empty(toolbars);
            }
        }
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
}
