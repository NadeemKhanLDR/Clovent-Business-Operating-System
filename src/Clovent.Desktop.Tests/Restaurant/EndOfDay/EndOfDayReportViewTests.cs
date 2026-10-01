using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Restaurant.EndOfDay;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Restaurant.Application.EndOfDay.Dtos;
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

            // Verify all 11 tabs exist
            var tabControl = GetAllControls(view).OfType<DevExpress.XtraTab.XtraTabControl>().FirstOrDefault();
            Assert.NotNull(tabControl);
            Assert.Equal(11, tabControl.TabPages.Count);

            var tabTitles = tabControl.TabPages.Select(p => p.Text).ToList();
            Assert.Contains("Summary", tabTitles);
            Assert.Contains("Orders / Bills", tabTitles);
            Assert.Contains("Items", tabTitles);
            Assert.Contains("Customers", tabTitles);
            Assert.Contains("Payments", tabTitles);
            Assert.Contains("Receivables Movement", tabTitles);
            Assert.Contains("Order Types", tabTitles);
            Assert.Contains("Item Types / Profitability", tabTitles);
            Assert.Contains("Cash Summary", tabTitles);
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

            // The 10 detail grid pages should contain the GridControl directly and no redundant toolbars
            var detailPages = tabControl.TabPages.Skip(1).ToList(); // Skip Summary tab
            Assert.Equal(10, detailPages.Count);

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

    [Fact]
    public void EndOfDayReportView_ScaleLayoutAtRuntime_EnforcesDpiScaling()
    {
        var (view, _) = CreateView();
        using (view)
        {
            view.ScaleLayoutAtRuntime();

            var periodCombo = (ComboBoxEdit)typeof(EndOfDayReportView).GetField("_periodCombo", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var fromDate = (DateEdit)typeof(EndOfDayReportView).GetField("_fromDateEdit", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var toDate = (DateEdit)typeof(EndOfDayReportView).GetField("_toDateEdit", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var generateBtn = (SimpleButton)typeof(EndOfDayReportView).GetField("_generateButton", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var printSummaryBtn = (SimpleButton)typeof(EndOfDayReportView).GetField("_printSummaryButton", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;

            int expectedPeriodW = DesktopDpi.Scale(110, view);
            int expectedFromW = DesktopDpi.Scale(115, view);
            int expectedToW = DesktopDpi.Scale(115, view);
            int expectedGenW = DesktopDpi.Scale(80, view);
            int expectedSummaryW = DesktopDpi.Scale(95, view);

            Assert.True(periodCombo.MinimumSize.Width >= expectedPeriodW);
            Assert.True(fromDate.MinimumSize.Width >= expectedFromW);
            Assert.True(toDate.MinimumSize.Width >= expectedToW);
            Assert.True(generateBtn.MinimumSize.Width >= expectedGenW);
            Assert.True(printSummaryBtn.MinimumSize.Width >= expectedSummaryW);
        }
    }

    [Fact]
    public void EndOfDayReportView_KpiCards_HaveAccurateSubtitlesAndCaptions()
    {
        var (view, _) = CreateView();
        using (view)
        {
            var tabControl = GetAllControls(view).OfType<DevExpress.XtraTab.XtraTabControl>().FirstOrDefault();
            Assert.NotNull(tabControl);
            var summaryPage = tabControl.TabPages[0];
            var cardsRow = summaryPage.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
            Assert.NotNull(cardsRow);

            var statCards = cardsRow.Controls.OfType<PanelControl>().ToList();
            Assert.Equal(4, statCards.Count);

            // Find all label controls in the cards
            var allLabels = statCards.SelectMany(c => c.Controls.OfType<LabelControl>()).ToList();

            // Total Sales subtitle
            var totalSalesSubtitle = allLabels.FirstOrDefault(l => l.Text == "Completed bill sales including fees");
            Assert.NotNull(totalSalesSubtitle);

            // Cash Sales title and subtitle
            var cashSalesTitle = allLabels.FirstOrDefault(l => l.Text == "CASH SALES");
            var cashSalesSubtitle = allLabels.FirstOrDefault(l => l.Text == "Cash order settlements");
            Assert.NotNull(cashSalesTitle);
            Assert.NotNull(cashSalesSubtitle);
        }
    }

    [Fact]
    public void EndOfDayReportView_ItemsGridFooters_DoNotSumAverages()
    {
        var (view, _) = CreateView();
        using (view)
        {
            var itemsGrid = (DevExpress.XtraGrid.GridControl)typeof(EndOfDayReportView)
                .GetField("_itemsGrid", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var itemsView = (DevExpress.XtraGrid.Views.Grid.GridView)itemsGrid.MainView;

            // Unit Price (Avg Price) must NOT be summed
            var unitPriceCol = itemsView.Columns["UnitPrice"];
            Assert.NotNull(unitPriceCol);
            Assert.Equal(DevExpress.Data.SummaryItemType.None, unitPriceCol.SummaryItem.SummaryType);

            // Cost Price (Unit Cost) must NOT be summed
            var costPriceCol = itemsView.Columns["CostPrice"];
            Assert.NotNull(costPriceCol);
            Assert.Equal(DevExpress.Data.SummaryItemType.None, costPriceCol.SummaryItem.SummaryType);

            // Quantity Sold must be SUM
            var qtyCol = itemsView.Columns["QuantitySold"];
            Assert.NotNull(qtyCol);
            Assert.Equal(DevExpress.Data.SummaryItemType.Sum, qtyCol.SummaryItem.SummaryType);

            // Total Sales must be SUM
            var salesCol = itemsView.Columns["TotalSales"];
            Assert.NotNull(salesCol);
            Assert.Equal(DevExpress.Data.SummaryItemType.Sum, salesCol.SummaryItem.SummaryType);

            // Gross Profit summary format must be "Known GP" or "Known"
            var gpCol = itemsView.Columns["GrossProfit"];
            Assert.NotNull(gpCol);
            Assert.Equal(DevExpress.Data.SummaryItemType.Custom, gpCol.SummaryItem.SummaryType);
            Assert.Contains("{0}", gpCol.SummaryItem.DisplayFormat);
        }
    }

    [Fact]
    public void EndOfDayReportView_ItemsFooter_WhenAllItemsHaveUnknownCost_ShowsNA_NotZero()
    {
        var (view, _) = CreateView();
        using (view)
        {
            var itemsGrid = (DevExpress.XtraGrid.GridControl)typeof(EndOfDayReportView)
                .GetField("_itemsGrid", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var itemsView = (DevExpress.XtraGrid.Views.Grid.GridView)itemsGrid.MainView;

            var items = new List<ExpandedItemRowDto>
            {
                new(Guid.NewGuid(), "Main Course", "Chicken Haleem", "Prepared", 5, 400m, null, 2000m, null, null, null, 50m),
                new(Guid.NewGuid(), "Main Course", "Chicken Biryani", "Prepared", 3, 500m, null, 1500m, null, null, null, 50m)
            };

            itemsGrid.DataSource = items;
            itemsGrid.ForceInitialize();
            itemsView.UpdateTotalSummary();

            var costCol = itemsView.Columns["EstimatedCost"];
            var gpCol = itemsView.Columns["GrossProfit"];

            Assert.NotNull(costCol);
            Assert.NotNull(gpCol);

            // Summary type must be Custom
            Assert.Equal(DevExpress.Data.SummaryItemType.Custom, costCol.SummaryItem.SummaryType);
            Assert.Equal(DevExpress.Data.SummaryItemType.Custom, gpCol.SummaryItem.SummaryType);

            // With zero costed items, the summary value must NOT be 0.00
            Assert.Equal("N/A", costCol.SummaryItem.SummaryValue?.ToString());
            Assert.Equal("Known GP: N/A", gpCol.SummaryItem.SummaryValue?.ToString());

            // Test display text
            var costDisplayText = costCol.SummaryText;
            var gpDisplayText = gpCol.SummaryText;

            Assert.Contains("N/A", costDisplayText);
            Assert.DoesNotContain("0.00", costDisplayText);

            Assert.Contains("Known GP: N/A", gpDisplayText);
            Assert.DoesNotContain("0.00", gpDisplayText);
        }
    }

    [Fact]
    public void EndOfDayReportView_ItemsFooter_WithMixedDataset_SumsOnlyKnownCostRows()
    {
        var (view, _) = CreateView();
        using (view)
        {
            var itemsGrid = (DevExpress.XtraGrid.GridControl)typeof(EndOfDayReportView)
                .GetField("_itemsGrid", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var itemsView = (DevExpress.XtraGrid.Views.Grid.GridView)itemsGrid.MainView;

            var items = new List<ExpandedItemRowDto>
            {
                // Naan: Sales 575, Cost 460, GP 115
                new(Guid.NewGuid(), "Breads", "Naan", "PurchasedResale", 5, 115m, 92m, 575m, 460m, 115m, 20m, 30m),
                // Food Heating: Sales 60, Known Cost 0, GP 60
                new(Guid.NewGuid(), "Services", "Food Heating", "Service", 1, 60m, 0m, 60m, 0m, 60m, 100m, 5m),
                // Prepared: Sales 1200, Cost unknown
                new(Guid.NewGuid(), "Main Course", "Chicken Biryani", "Prepared", 2, 600m, null, 1200m, null, null, null, 65m)
            };

            itemsGrid.DataSource = items;
            itemsGrid.ForceInitialize();
            itemsView.UpdateTotalSummary();

            var costCol = itemsView.Columns["EstimatedCost"];
            var gpCol = itemsView.Columns["GrossProfit"];

            Assert.NotNull(costCol);
            Assert.NotNull(gpCol);

            // Known Cost = 460 + 0 = 460
            Assert.NotNull(costCol.SummaryItem.SummaryValue);
            Assert.Equal("460.00", costCol.SummaryItem.SummaryValue?.ToString());

            // Known GP = 115 + 60 = 175
            Assert.NotNull(gpCol.SummaryItem.SummaryValue);
            Assert.Equal("Known GP: 175.00", gpCol.SummaryItem.SummaryValue?.ToString());

            // Display text formatting
            Assert.Contains("460", costCol.SummaryText);
            Assert.Contains("Known GP: 175", gpCol.SummaryText);
        }
    }

    [Fact]
    public void EndOfDayReportView_ItemsFooter_DoesNotSumAveragesOrPercentages()
    {
        var (view, _) = CreateView();
        using (view)
        {
            var itemsGrid = (DevExpress.XtraGrid.GridControl)typeof(EndOfDayReportView)
                .GetField("_itemsGrid", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var itemsView = (DevExpress.XtraGrid.Views.Grid.GridView)itemsGrid.MainView;

            // Average/Rate columns must be None
            Assert.Equal(DevExpress.Data.SummaryItemType.None, itemsView.Columns["UnitPrice"].SummaryItem.SummaryType);
            Assert.Equal(DevExpress.Data.SummaryItemType.None, itemsView.Columns["CostPrice"].SummaryItem.SummaryType);
            Assert.Equal(DevExpress.Data.SummaryItemType.None, itemsView.Columns["MarginPercent"].SummaryItem.SummaryType);
            Assert.Equal(DevExpress.Data.SummaryItemType.None, itemsView.Columns["PercentOfTotalSales"].SummaryItem.SummaryType);

            // Quantity and Sales must be Sum
            Assert.Equal(DevExpress.Data.SummaryItemType.Sum, itemsView.Columns["QuantitySold"].SummaryItem.SummaryType);
            Assert.Equal(DevExpress.Data.SummaryItemType.Sum, itemsView.Columns["TotalSales"].SummaryItem.SummaryType);

            // Cost Amount and Gross Profit must be Custom
            Assert.Equal(DevExpress.Data.SummaryItemType.Custom, itemsView.Columns["EstimatedCost"].SummaryItem.SummaryType);
            Assert.Equal(DevExpress.Data.SummaryItemType.Custom, itemsView.Columns["GrossProfit"].SummaryItem.SummaryType);
        }
    }

    [Fact]
    public void EndOfDayReportView_HighDpi_HasNoUnnecessaryWorkspaceHorizontalScrollbar()
    {
        var (view, _) = CreateView();
        using (view)
        {
            // View itself must have AutoScroll disabled and empty AutoScrollMinSize
            Assert.False(view.AutoScroll);
            Assert.Equal(System.Drawing.Size.Empty, view.AutoScrollMinSize);

            // All child tab pages must have AutoScroll disabled
            var tabControl = (DevExpress.XtraTab.XtraTabControl)typeof(EndOfDayReportView)
                .GetField("_tabControl", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;

            foreach (DevExpress.XtraTab.XtraTabPage page in tabControl.TabPages)
            {
                Assert.False(page.AutoScroll);
            }

            // Verify in a simulated high-DPI shell window (1920x1080 at 240 DPI)
            var form = new System.Windows.Forms.Form { Width = 1920, Height = 1080, AutoScroll = false };
            form.Controls.Add(view);
            view.Dock = System.Windows.Forms.DockStyle.Fill;
            form.Show();

            view.ScaleLayoutAtRuntime();

            // Form and view must not show horizontal scrollbars
            Assert.False(form.HorizontalScroll.Visible);
            Assert.False(view.HorizontalScroll.Visible);

            form.Close();
        }
    }

    [Fact]
    public void EndOfDayReportView_Toolbar_IsSingleRowTableLayoutPanel_WithZeroWrapping_AndExactButtonOrder()
    {
        var (view, _) = CreateView();
        using (view)
        {
            var genBtn = (SimpleButton)typeof(EndOfDayReportView).GetField("_generateButton", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var prevBtn = (SimpleButton)typeof(EndOfDayReportView).GetField("_previewButton", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var printBtn = (SimpleButton)typeof(EndOfDayReportView).GetField("_printButton", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var pdfBtn = (SimpleButton)typeof(EndOfDayReportView).GetField("_exportPdfButton", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var excelBtn = (SimpleButton)typeof(EndOfDayReportView).GetField("_exportExcelButton", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var summaryBtn = (SimpleButton)typeof(EndOfDayReportView).GetField("_printSummaryButton", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var picker = (Control)typeof(EndOfDayReportView).GetField("_warehousePicker", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;

            var tlp = Assert.IsType<TableLayoutPanel>(genBtn.Parent);
            Assert.Equal(1, tlp.RowCount);
            Assert.Equal(10, tlp.ColumnCount);

            // Columns and order
            Assert.Equal(0, tlp.GetColumn(picker));
            Assert.Equal(4, tlp.GetColumn(genBtn));
            Assert.Equal(5, tlp.GetColumn(prevBtn));
            Assert.Equal(6, tlp.GetColumn(printBtn));
            Assert.Equal(7, tlp.GetColumn(pdfBtn));
            Assert.Equal(8, tlp.GetColumn(excelBtn));
            Assert.Equal(9, tlp.GetColumn(summaryBtn));

            // Tab index sequence
            Assert.True(picker.TabIndex < genBtn.TabIndex);
            Assert.True(genBtn.TabIndex < prevBtn.TabIndex);
            Assert.True(prevBtn.TabIndex < printBtn.TabIndex);
            Assert.True(printBtn.TabIndex < pdfBtn.TabIndex);
            Assert.True(pdfBtn.TabIndex < excelBtn.TabIndex);
            Assert.True(excelBtn.TabIndex < summaryBtn.TabIndex);
        }
    }

    [Fact]
    public void EndOfDayReportView_CustomersTab_GrossSales_AndMoneyColumns_ConfiguredForTwoDecimals()
    {
        var (view, _) = CreateView();
        using (view)
        {
            var customersGrid = (DevExpress.XtraGrid.GridControl)typeof(EndOfDayReportView)
                .GetField("_customersGrid", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var customersView = (DevExpress.XtraGrid.Views.Grid.GridView)customersGrid.MainView;

            Assert.Contains("GrossSales", EndOfDayReportView.MoneyFieldNames);
            Assert.Contains("ItemSales", EndOfDayReportView.MoneyFieldNames);
            Assert.Contains("BillTotal", EndOfDayReportView.MoneyFieldNames);

            // Verify DisplayFormat
            Assert.Equal(DevExpress.Utils.FormatType.Numeric, customersView.Columns["GrossSales"].DisplayFormat.FormatType);
            Assert.Equal("n2", customersView.Columns["GrossSales"].DisplayFormat.FormatString);
            Assert.Equal(DevExpress.Utils.HorzAlignment.Far, customersView.Columns["GrossSales"].AppearanceCell.TextOptions.HAlignment);
        }
    }

    [Fact]
    public void EndOfDayReportView_CashSummary_ShiftNumber_HasNoSumSummary_AndInventoryMovementHasNoQuantitySum()
    {
        var (view, _) = CreateView();
        using (view)
        {
            var cashGrid = (DevExpress.XtraGrid.GridControl)typeof(EndOfDayReportView)
                .GetField("_cashSummaryGrid", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var cashView = (DevExpress.XtraGrid.Views.Grid.GridView)cashGrid.MainView;

            Assert.NotEqual(DevExpress.Data.SummaryItemType.Sum, cashView.Columns["ShiftNumber"].SummaryItem.SummaryType);
            Assert.Equal("Total", cashView.Columns["ShiftNumber"].SummaryItem.DisplayFormat);

            var invGrid = (DevExpress.XtraGrid.GridControl)typeof(EndOfDayReportView)
                .GetField("_inventoryMovementGrid", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var invView = (DevExpress.XtraGrid.Views.Grid.GridView)invGrid.MainView;

            Assert.Equal(DevExpress.Data.SummaryItemType.None, invView.Columns["Quantity"].SummaryItem.SummaryType);
        }
    }
}




