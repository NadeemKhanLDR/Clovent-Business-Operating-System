using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Desktop.Restaurant.EndOfDay;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.Desktop.Sessions;
using Clovent.MasterData.Application.Warehouses.Queries;
using Clovent.Restaurant.Application.Orders.Queries;
using Clovent.Restaurant.Application.Refunds.Dtos;
using Clovent.Restaurant.Application.Reports;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Restaurant.Reports;

/// <summary>
/// WinForms view for the auditable refund register report.
/// Tracks compensating refund transactions, business justifications, approving authorities,
/// settlement method breakdowns, and provides direct reprint access to refund credit notes.
/// </summary>
public sealed partial class RefundAuditReportView : XtraUserControl
{
    private readonly IServiceScope _scope;
    private readonly ScreenOperationGate _gate = new();
    private readonly IMediator _mediator;
    private readonly ICurrentSession _currentSession;

    private readonly Dictionary<string, Guid> _warehousesByDisplay = [];
    private bool _isUpdatingPeriod;

    /// <summary>Design-time constructor.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public RefundAuditReportView()
    {
        _scope = null!;
        _mediator = null!;
        _currentSession = null!;

        InitializeComponent();
    }

    /// <summary>Builds the view with required dependencies.</summary>
    public RefundAuditReportView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        InitializeComponent();

        if (DesignModeHelper.IsInDesignMode)
        {
            _scope = null!;
            _mediator = null!;
            _currentSession = null!;
            return;
        }

        _scope = scopeFactory.CreateScope();
        _mediator = new SerializedMediator(_scope.ServiceProvider.GetRequiredService<IMediator>(), _gate);
        _currentSession = currentSession;
    }

    private void AppearanceManager_Changed(object? sender, EventArgs e) =>
        AppearanceManager.Apply(this, "Restaurant", nameof(RefundAuditReportView));

    private async void RefundAuditReportView_Load(object? sender, EventArgs e)
    {
        if (DesignModeHelper.IsInDesignMode)
            return;

        ScaleLayoutAtRuntime();
        DpiChangedAfterParent += (_, _) => ScaleLayoutAtRuntime();
        ConfigurePeriods();
        await LoadWarehousesAsync();
        await GenerateReportAsync();
    }

    /// <inheritdoc/>
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        ScaleLayoutAtRuntime();
    }

    private void ConfigurePeriods()
    {
        _periodCombo.Properties.Items.Clear();
        foreach (var (_, name) in ReportPeriodCalculator.GetAllOptions())
        {
            _periodCombo.Properties.Items.Add(name);
        }
        _periodCombo.SelectedItem = "Today";

        var today = BusinessDateTimeService.Instance.Today;
        _fromDateEdit.EditValue = today.ToDateTime(TimeOnly.MinValue);
        _toDateEdit.EditValue = today.ToDateTime(TimeOnly.MinValue);
    }

    private void WireEvents()
    {
        _gridView.Columns.Clear();

        var colNum = _gridView.Columns.AddVisible(nameof(RefundDto.RefundNumber), "Credit Note #");
        colNum.Width = 140;

        var colDate = _gridView.Columns.AddVisible(nameof(RefundDto.RefundedAtUtc), "Date & Time");
        colDate.Width = 150;

        var colCashier = _gridView.Columns.AddVisible(nameof(RefundDto.CashierName), "Cashier");
        colCashier.Width = 110;

        var colAppr = _gridView.Columns.AddVisible(nameof(RefundDto.ApprovedByUserName), "Approved By");
        colAppr.Width = 110;

        var colReason = _gridView.Columns.AddVisible(nameof(RefundDto.Reason), "Reason");
        colReason.Width = 160;

        var colSettlement = _gridView.Columns.AddVisible(nameof(RefundDto.SettlementMethod), "Settlement");
        colSettlement.Width = 120;

        var colRef = _gridView.Columns.AddVisible(nameof(RefundDto.SettlementReference), "Reference");
        colRef.Width = 110;

        var colSub = _gridView.Columns.AddVisible(nameof(RefundDto.SubtotalRefunded), "Subtotal");
        colSub.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Custom;
        colSub.DisplayFormat.FormatString = "F2";
        colSub.Width = 100;

        var colTax = _gridView.Columns.AddVisible(nameof(RefundDto.TaxReversedTotal), "Tax Reversed");
        colTax.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Custom;
        colTax.DisplayFormat.FormatString = "F2";
        colTax.Width = 100;

        var colGrand = _gridView.Columns.AddVisible(nameof(RefundDto.GrandTotalRefunded), "Grand Total");
        colGrand.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Custom;
        colGrand.DisplayFormat.FormatString = "F2";
        colGrand.Width = 110;

        _gridView.CustomColumnDisplayText += (s, e) =>
        {
            if (e.Column.FieldName is nameof(RefundDto.SubtotalRefunded) or
                nameof(RefundDto.TaxReversedTotal) or
                nameof(RefundDto.GrandTotalRefunded) && e.Value != null && e.Value != DBNull.Value)
            {
                try
                {
                    e.DisplayText = CurrencyDisplay.FormatPlain(Convert.ToDecimal(e.Value));
                }
                catch { }
            }
            else if (e.Column.FieldName == nameof(RefundDto.RefundedAtUtc) && e.Value is DateTimeOffset dt)
            {
                e.DisplayText = DateTimeDisplay.Format(dt);
            }
        };

        _gridView.DoubleClick += async (s, e) =>
        {
            if (_gridView.GetFocusedRow() is RefundDto refund)
            {
                await ShowCreditNotePreviewAsync(refund);
            }
        };

        _periodCombo.SelectedIndexChanged += (s, e) =>
        {
            if (_isUpdatingPeriod) return;
            var periodName = _periodCombo.SelectedItem?.ToString();
            var period = ReportPeriodCalculator.ParseDisplayName(periodName);
            if (period == ReportPeriod.Custom) return;

            _isUpdatingPeriod = true;
            try
            {
                var range = ReportPeriodCalculator.CalculateRange(period, BusinessDateTimeService.Instance.Today);
                _fromDateEdit.EditValue = range.From.ToDateTime(TimeOnly.MinValue);
                _toDateEdit.EditValue = range.To.ToDateTime(TimeOnly.MinValue);
            }
            finally
            {
                _isUpdatingPeriod = false;
            }
        };

        _fromDateEdit.EditValueChanged += (s, e) => OnCustomDateSelected();
        _toDateEdit.EditValueChanged += (s, e) => OnCustomDateSelected();
        _refreshButton.Click += async (s, e) => await GenerateReportAsync();
    }

    private void OnCustomDateSelected()
    {
        if (_isUpdatingPeriod) return;
        _isUpdatingPeriod = true;
        try
        {
            _periodCombo.SelectedItem = "Custom";
        }
        finally
        {
            _isUpdatingPeriod = false;
        }
    }

    private async Task ShowCreditNotePreviewAsync(RefundDto refund)
    {
        try
        {
            var order = await _mediator.Send(new GetOrderByIdQuery(refund.OrderId));
            var orderNumber = order?.OrderNumber ?? "ORD-HISTORIC";
            var text = ReceiptFormatter.FormatRefundCreditNote(refund, orderNumber);
            using var preview = new ReceiptPreviewForm(text);
            preview.ShowDialog(this);
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show(this, $"Failed to preview credit note: {ex.Message}", "Preview Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task LoadWarehousesAsync()
    {
        try
        {
            var warehouses = await _mediator.Send(new ListAllWarehousesQuery());
            _warehouseCombo.Properties.Items.Clear();
            _warehousesByDisplay.Clear();

            foreach (var w in warehouses)
            {
                var display = $"{w.Code} - {w.Name}";
                _warehousesByDisplay[display] = w.WarehouseId;
                _warehouseCombo.Properties.Items.Add(display);
            }

            if (_warehouseCombo.Properties.Items.Count > 0)
            {
                _warehouseCombo.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show(this, $"Failed to load warehouses: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task GenerateReportAsync()
    {
        if (_warehouseCombo.SelectedItem == null)
            return;

        var display = _warehouseCombo.SelectedItem.ToString();
        if (display == null || !_warehousesByDisplay.TryGetValue(display, out var warehouseId))
            return;

        var fromDt = _fromDateEdit.DateTime;
        var toDt = _toDateEdit.DateTime;
        if (fromDt > toDt)
        {
            XtraMessageBox.Show(this, "From Date cannot be later than To Date.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var fromDate = DateOnly.FromDateTime(fromDt);
        var toDate = DateOnly.FromDateTime(toDt);

        Cursor = Cursors.WaitCursor;
        _refreshButton.Enabled = false;

        try
        {
            var report = await _mediator.Send(new GetRefundReportQuery(warehouseId, fromDate, toDate));

            _kpiCount.Text = $"Refunds: {report.TotalRefundsCount}";
            _kpiGrossRefunded.Text = $"Gross: {CurrencyDisplay.FormatPlain(report.TotalGrossRefunded)}";
            _kpiTaxReversed.Text = $"Tax Reversed: {CurrencyDisplay.FormatPlain(report.TotalTaxReversed)}";
            _kpiNetRefunded.Text = $"Total Refunded: {CurrencyDisplay.FormatPlain(report.TotalNetRefunded)}";
            _kpiCashPayouts.Text = $"Cash Payouts: {CurrencyDisplay.FormatPlain(report.CashPayoutsTotal)}";
            _kpiCardRefunds.Text = $"Acct Credits: {CurrencyDisplay.FormatPlain(report.CustomerCreditTotal)} | Card: {CurrencyDisplay.FormatPlain(report.CardRefundsTotal)}";

            _gridControl.DataSource = report.Refunds;
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show(this, $"Failed to generate refund audit report: {ex.Message}", "Report Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
            _refreshButton.Enabled = true;
        }
    }
}
