using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Restaurant.EndOfDay;
using Clovent.Desktop.Sessions;
using Clovent.Restaurant.Application.DayClose.Dtos;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraTab;
using MediatR;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.EndOfDay;

public class EndOfDayCloseDialogTests
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

    private static EndOfDayCloseDialog CreateDialog(Guid? branchId = null, string branchName = "Main Branch", DateOnly? businessDate = null)
    {
        var session = new FakeCurrentSession();
        var mediator = new FakeMediator();
        return new EndOfDayCloseDialog(
            mediator,
            session,
            branchId ?? Guid.NewGuid(),
            branchName,
            businessDate ?? DateOnly.FromDateTime(DateTime.Today));
    }

    private static T GetField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return (T)field.GetValue(instance)!;
    }

    private static void InvokeMethod(object instance, string methodName, params object?[] parameters)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method.Invoke(instance, parameters);
    }

    [Fact]
    public void EndOfDayCloseDialog_InitializesControlsProperly()
    {
        using var dialog = CreateDialog();
        Assert.NotNull(dialog);

        var tabControl = GetField<XtraTabControl>(dialog, "_tabControl");
        Assert.NotNull(tabControl);
        Assert.Equal(7, tabControl.TabPages.Count);

        var expectedTabTitles = new[]
        {
            "Cashier Shifts",
            "Sales Summary",
            "Tax Breakdown",
            "Payment Tenders",
            "Drawer Audit",
            "Cashier Breakdown",
            "Order Types"
        };

        for (int i = 0; i < expectedTabTitles.Length; i++)
        {
            Assert.Equal(expectedTabTitles[i], tabControl.TabPages[i].Text);
        }

        var btnPrintReport = GetField<SimpleButton>(dialog, "_btnPrintReport");
        var btnExportPdf = GetField<SimpleButton>(dialog, "_btnExportPdf");
        var btnExportExcel = GetField<SimpleButton>(dialog, "_btnExportExcel");
        var btnCloseDay = GetField<SimpleButton>(dialog, "_btnCloseDay");
        var btnCancel = GetField<SimpleButton>(dialog, "_btnCancel");

        Assert.NotNull(btnPrintReport);
        Assert.NotNull(btnExportPdf);
        Assert.NotNull(btnExportExcel);
        Assert.NotNull(btnCloseDay);
        Assert.NotNull(btnCancel);
    }

    [Fact]
    public void EndOfDayCloseDialog_BindSummary_SetsAllBreakdownTabsAndMetrics()
    {
        using var dialog = CreateDialog();

        var shift1 = new ShiftDayCloseItemDto(
            ShiftId: Guid.NewGuid(),
            ShiftNumber: 1,
            CashierName: "Cashier Alice",
            TerminalId: Guid.NewGuid(),
            OpenedAtUtc: DateTimeOffset.UtcNow.AddHours(-8),
            ClosedAtUtc: DateTimeOffset.UtcNow.AddHours(-4),
            Status: "Closed",
            StartingCash: 5000m,
            ExpectedCash: 19400m,
            CountedCash: 19400m,
            CashVariance: 0m,
            VarianceReason: null,
            CashRefunds: 500m,
            CustomerAccountCreditRefunds: 0m);

        var shift2 = new ShiftDayCloseItemDto(
            ShiftId: Guid.NewGuid(),
            ShiftNumber: 2,
            CashierName: "Cashier Bob",
            TerminalId: Guid.NewGuid(),
            OpenedAtUtc: DateTimeOffset.UtcNow.AddHours(-4),
            ClosedAtUtc: DateTimeOffset.UtcNow,
            Status: "Closed",
            StartingCash: 5000m,
            ExpectedCash: 19400m,
            CountedCash: 19400m,
            CashVariance: 0m,
            VarianceReason: null,
            CashRefunds: 1000m,
            CustomerAccountCreditRefunds: 500m);

        var taxBreakdown = new List<DayCloseTaxItemDto>
        {
            new("PRA", "GST-16", 16.00m, 45000m, 7200m),
            new("FBR", "FED-4", 4.00m, 10000m, 400m)
        };

        var tenderBreakdown = new List<DayCloseTenderItemDto>
        {
            new("Cash", 15, 30000m),
            new("Card", 10, 20000m),
            new("Bank Transfer", 3, 5100m)
        };

        var cashierBreakdown = new List<DayCloseCashierSalesDto>
        {
            new("Cashier Alice", 14, 27550m),
            new("Cashier Bob", 14, 27550m)
        };

        var orderTypeBreakdown = new List<DayCloseOrderTypeSalesDto>
        {
            new("DineIn", 18, 35000m),
            new("Takeaway", 8, 15100m),
            new("Delivery", 2, 5000m)
        };

        var summary = new BusinessDaySummaryDto(
            BranchId: Guid.NewGuid(),
            BusinessDate: DateOnly.FromDateTime(DateTime.Today),
            IsAlreadyClosed: false,
            ExistingClose: null,
            OpenShifts: [],
            ClosedShifts: [shift1, shift2],
            TotalShifts: 2,
            TotalOrders: 28,
            TotalSales: 55100m,
            CashSales: 30000m,
            CardSales: 20000m,
            OtherSales: 5100m,
            Refunds: 2000m,
            Discounts: 2500m,
            Tax: 7600m,
            CashIn: 500m,
            CashOut: 200m,
            TotalShiftVariance: 0m,
            CanClose: true,
            BlockingReason: null,
            CashRefunds: 1500m,
            CustomerAccountCreditRefunds: 500m,
            OtherRefunds: 0m,
            GrossSales: 50000m,
            ItemDiscounts: 1000m,
            OrderDiscounts: 1500m,
            NetSales: 47500m,
            CancelledOrdersCount: 1,
            TaxableSales: 45000m,
            TaxExemptSales: 2500m,
            TaxBreakdown: taxBreakdown,
            BankTransferSales: 5100m,
            DigitalSales: 0m,
            SplitPaymentsCount: 0,
            SplitPaymentsTotal: 0m,
            TotalCollections: 55100m,
            OutstandingReceivables: 0m,
            TenderBreakdown: tenderBreakdown,
            OpeningFloat: 10000m,
            ExpectedClosingCash: 38800m,
            ActualClosingCash: 38800m,
            CompletedOrdersCount: 25,
            RefundedOrdersCount: 2,
            AverageOrderValue: 1900m,
            CashierBreakdown: cashierBreakdown,
            OrderTypeBreakdown: orderTypeBreakdown);

        // Inject summary into dialog via BindSummary
        dialog.BindSummary(summary);

        // Verify KPI labels
        var lblGrossSales = GetField<LabelControl>(dialog, "_lblGrossSales");
        var lblDiscounts = GetField<LabelControl>(dialog, "_lblDiscounts");
        var lblNetSales = GetField<LabelControl>(dialog, "_lblNetSales");
        var lblTax = GetField<LabelControl>(dialog, "_lblTax");
        var lblExpectedCash = GetField<LabelControl>(dialog, "_lblExpectedCash");
        var lblActualCash = GetField<LabelControl>(dialog, "_lblActualCash");
        var lblCashVariance = GetField<LabelControl>(dialog, "_lblCashVariance");
        var lblRefunds = GetField<LabelControl>(dialog, "_lblRefunds");

        Assert.Equal(CurrencyDisplay.Format(50000m), lblGrossSales.Text);
        Assert.Equal(CurrencyDisplay.Format(2500m), lblDiscounts.Text);
        Assert.Equal(CurrencyDisplay.Format(47500m), lblNetSales.Text);
        Assert.Equal(CurrencyDisplay.Format(7600m), lblTax.Text);
        Assert.Equal(CurrencyDisplay.Format(38800m), lblExpectedCash.Text);
        Assert.Equal(CurrencyDisplay.Format(38800m), lblActualCash.Text);
        Assert.Equal(CurrencyDisplay.Format(0m), lblCashVariance.Text);
        Assert.Equal(CurrencyDisplay.Format(2000m), lblRefunds.Text);

        // Verify Data Grids
        var gridShifts = GetField<GridControl>(dialog, "_gridShifts");
        var gridSalesMetrics = GetField<GridControl>(dialog, "_gridSalesMetrics");
        var gridTaxBreakdown = GetField<GridControl>(dialog, "_gridTaxBreakdown");
        var gridTenders = GetField<GridControl>(dialog, "_gridTenders");
        var gridCashDrawer = GetField<GridControl>(dialog, "_gridCashDrawer");
        var gridCashiers = GetField<GridControl>(dialog, "_gridCashiers");
        var gridOrderTypes = GetField<GridControl>(dialog, "_gridOrderTypes");

        Assert.NotNull(gridShifts.DataSource);
        Assert.Equal(2, ((ICollection)gridShifts.DataSource).Count);

        Assert.NotNull(gridSalesMetrics.DataSource);
        Assert.True(((ICollection)gridSalesMetrics.DataSource).Count >= 8);

        Assert.NotNull(gridTaxBreakdown.DataSource);
        Assert.Equal(2, ((ICollection)gridTaxBreakdown.DataSource).Count);

        Assert.NotNull(gridTenders.DataSource);
        Assert.Equal(3, ((ICollection)gridTenders.DataSource).Count);

        Assert.NotNull(gridCashDrawer.DataSource);
        Assert.True(((ICollection)gridCashDrawer.DataSource).Count >= 8);

        Assert.NotNull(gridCashiers.DataSource);
        Assert.Equal(2, ((ICollection)gridCashiers.DataSource).Count);

        Assert.NotNull(gridOrderTypes.DataSource);
        Assert.Equal(3, ((ICollection)gridOrderTypes.DataSource).Count);

        // Verify Action Buttons
        var btnPrintReport = GetField<SimpleButton>(dialog, "_btnPrintReport");
        var btnExportPdf = GetField<SimpleButton>(dialog, "_btnExportPdf");
        var btnExportExcel = GetField<SimpleButton>(dialog, "_btnExportExcel");
        var btnCloseDay = GetField<SimpleButton>(dialog, "_btnCloseDay");

        Assert.True(btnPrintReport.Enabled);
        Assert.True(btnExportPdf.Enabled);
        Assert.True(btnExportExcel.Enabled);
        Assert.True(btnCloseDay.Enabled);
    }

    [Fact]
    public void EndOfDayCloseDialog_BindSummary_WhenAlreadyClosed_DisablesCloseButtonAndSetsGreenBanner()
    {
        using var dialog = CreateDialog();

        var closeRecord = new BusinessDayCloseDto(
            Id: Guid.NewGuid(),
            BranchId: Guid.NewGuid(),
            BusinessDate: DateOnly.FromDateTime(DateTime.Today),
            ClosedAtUtc: DateTimeOffset.UtcNow.AddHours(-1),
            ClosedByUserId: Guid.NewGuid(),
            ClosedByUserName: "Manager Alice",
            Status: "Closed",
            TotalSales: 25000m,
            CashSales: 25000m,
            CardSales: 0m,
            OtherPayments: 0m,
            Refunds: 0m,
            Discounts: 0m,
            Tax: 4000m,
            CashIn: 0m,
            CashOut: 0m,
            ShiftCount: 1,
            TotalShiftVariance: 0m,
            OrderCount: 10,
            Notes: null,
            CreatedAtUtc: DateTimeOffset.UtcNow.AddHours(-1));

        var summary = new BusinessDaySummaryDto(
            BranchId: Guid.NewGuid(),
            BusinessDate: DateOnly.FromDateTime(DateTime.Today),
            IsAlreadyClosed: true,
            ExistingClose: closeRecord,
            OpenShifts: [],
            ClosedShifts: [],
            TotalShifts: 1,
            TotalOrders: 10,
            TotalSales: 25000m,
            CashSales: 25000m,
            CardSales: 0m,
            OtherSales: 0m,
            Refunds: 0m,
            Discounts: 0m,
            Tax: 4000m,
            CashIn: 0m,
            CashOut: 0m,
            TotalShiftVariance: 0m,
            CanClose: false);

        dialog.BindSummary(summary);

        var btnCloseDay = GetField<SimpleButton>(dialog, "_btnCloseDay");
        var bannerLabel = GetField<LabelControl>(dialog, "_lblBanner");
        var bannerPanel = GetField<PanelControl>(dialog, "_bannerPanel");

        Assert.False(btnCloseDay.Enabled);
        Assert.Equal("Day Already Closed", btnCloseDay.Text);
        Assert.Contains("ALREADY CLOSED", bannerLabel.Text);
        Assert.Equal(Color.FromArgb(240, 253, 244), bannerPanel.Appearance.BackColor);
    }

    [Fact]
    public void EndOfDayCloseDialog_BindSummary_WhenOpenShiftsExist_DisablesCloseButtonAndSetsRedBanner()
    {
        using var dialog = CreateDialog();

        var openShift = new ShiftDayCloseItemDto(
            ShiftId: Guid.NewGuid(),
            ShiftNumber: 2,
            CashierName: "Cashier Bob",
            TerminalId: Guid.NewGuid(),
            OpenedAtUtc: DateTimeOffset.UtcNow.AddHours(-2),
            ClosedAtUtc: null,
            Status: "Open",
            StartingCash: 5000m,
            ExpectedCash: 12000m,
            CountedCash: 0m,
            CashVariance: -12000m,
            VarianceReason: null);

        var summary = new BusinessDaySummaryDto(
            BranchId: Guid.NewGuid(),
            BusinessDate: DateOnly.FromDateTime(DateTime.Today),
            IsAlreadyClosed: false,
            ExistingClose: null,
            OpenShifts: [openShift],
            ClosedShifts: [],
            TotalShifts: 2,
            TotalOrders: 15,
            TotalSales: 12000m,
            CashSales: 12000m,
            CardSales: 0m,
            OtherSales: 0m,
            Refunds: 0m,
            Discounts: 0m,
            Tax: 1000m,
            CashIn: 0m,
            CashOut: 0m,
            TotalShiftVariance: -12000m,
            CanClose: false,
            BlockingReason: "Cannot close business day: 1 open shift(s) must be closed first.");

        dialog.BindSummary(summary);

        var btnCloseDay = GetField<SimpleButton>(dialog, "_btnCloseDay");
        var bannerLabel = GetField<LabelControl>(dialog, "_lblBanner");
        var bannerPanel = GetField<PanelControl>(dialog, "_bannerPanel");

        Assert.False(btnCloseDay.Enabled);
        Assert.Contains("1 open shift(s)", bannerLabel.Text);
        Assert.Equal(Color.FromArgb(254, 242, 242), bannerPanel.Appearance.BackColor);
    }

    [Fact]
    public void EndOfDayCloseDialog_BindSummary_WhenReadyToClose_EnablesCloseButtonAndSetsBlueBanner()
    {
        using var dialog = CreateDialog();

        var summary = new BusinessDaySummaryDto(
            BranchId: Guid.NewGuid(),
            BusinessDate: DateOnly.FromDateTime(DateTime.Today),
            IsAlreadyClosed: false,
            ExistingClose: null,
            OpenShifts: [],
            ClosedShifts: [],
            TotalShifts: 1,
            TotalOrders: 10,
            TotalSales: 17400m,
            CashSales: 17400m,
            CardSales: 0m,
            OtherSales: 0m,
            Refunds: 0m,
            Discounts: 0m,
            Tax: 2400m,
            CashIn: 0m,
            CashOut: 0m,
            TotalShiftVariance: 0m,
            CanClose: true,
            NetSales: 15000m);

        dialog.BindSummary(summary);

        var btnCloseDay = GetField<SimpleButton>(dialog, "_btnCloseDay");
        var bannerLabel = GetField<LabelControl>(dialog, "_lblBanner");
        var bannerPanel = GetField<PanelControl>(dialog, "_bannerPanel");

        Assert.True(btnCloseDay.Enabled);
        Assert.Contains("Ready to perform Day Close", bannerLabel.Text);
        Assert.Equal(Color.FromArgb(240, 249, 255), bannerPanel.Appearance.BackColor);
    }
}
