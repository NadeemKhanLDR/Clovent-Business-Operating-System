using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Desktop.Restaurant.EndOfDay;
using Clovent.Desktop.Sessions;
using Clovent.MasterData.Application.Warehouses.Queries;
using Clovent.Restaurant.Application.Reports;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Restaurant.Reports;

/// <summary>
/// WinForms view for the statutory Pakistan Sales Tax Summary report.
/// Aggregates gross sales, discounts, taxable bases, statutory classifications,
/// sales tax charged, tax refunded from credit notes, and net tax payable by tax authority.
/// </summary>
public sealed partial class TaxSummaryReportView : XtraUserControl
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
    public TaxSummaryReportView()
    {
        _scope = null!;
        _mediator = null!;
        _currentSession = null!;

        InitializeComponent();
    }

    /// <summary>Builds the view with required dependencies.</summary>
    public TaxSummaryReportView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
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
        AppearanceManager.Apply(this, "Restaurant", nameof(TaxSummaryReportView));

    private async void TaxSummaryReportView_Load(object? sender, EventArgs e)
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

        var colAuth = _gridView.Columns.AddVisible(nameof(TaxAuthorityReportItemDto.Authority), "Authority");
        colAuth.Width = 90;

        var colCode = _gridView.Columns.AddVisible(nameof(TaxAuthorityReportItemDto.TaxCode), "Tax Code");
        colCode.Width = 120;

        var colRate = _gridView.Columns.AddVisible(nameof(TaxAuthorityReportItemDto.RatePercentage), "Rate %");
        colRate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
        colRate.DisplayFormat.FormatString = "0.##'%'";
        colRate.Width = 80;

        var colClass = _gridView.Columns.AddVisible(nameof(TaxAuthorityReportItemDto.TaxClassification), "Classification");
        colClass.Width = 110;

        var colBase = _gridView.Columns.AddVisible(nameof(TaxAuthorityReportItemDto.TaxableBase), "Taxable Base");
        colBase.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Custom;
        colBase.DisplayFormat.FormatString = "F2";
        colBase.Width = 110;

        var colCharged = _gridView.Columns.AddVisible(nameof(TaxAuthorityReportItemDto.TaxCharged), "Tax Charged");
        colCharged.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Custom;
        colCharged.DisplayFormat.FormatString = "F2";
        colCharged.Width = 110;

        var colRef = _gridView.Columns.AddVisible(nameof(TaxAuthorityReportItemDto.TaxRefunded), "Tax Refunded");
        colRef.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Custom;
        colRef.DisplayFormat.FormatString = "F2";
        colRef.Width = 110;

        var colNet = _gridView.Columns.AddVisible(nameof(TaxAuthorityReportItemDto.NetTax), "Net Tax Payable");
        colNet.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Custom;
        colNet.DisplayFormat.FormatString = "F2";
        colNet.Width = 120;

        _gridView.CustomColumnDisplayText += (s, e) =>
        {
            if (e.Column.FieldName is nameof(TaxAuthorityReportItemDto.TaxableBase) or
                nameof(TaxAuthorityReportItemDto.TaxCharged) or
                nameof(TaxAuthorityReportItemDto.TaxRefunded) or
                nameof(TaxAuthorityReportItemDto.NetTax) && e.Value != null && e.Value != DBNull.Value)
            {
                try
                {
                    e.DisplayText = CurrencyDisplay.FormatPlain(Convert.ToDecimal(e.Value));
                }
                catch { }
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
            var report = await _mediator.Send(new GetTaxSummaryReportQuery(warehouseId, fromDate, toDate));

            _kpiGrossSales.Text = $"Gross Sales: {CurrencyDisplay.FormatPlain(report.GrossSalesBeforeDiscounts)}";
            _kpiTaxableBase.Text = $"Taxable Base: {CurrencyDisplay.FormatPlain(report.TaxableSalesBase)}";
            _kpiTaxCharged.Text = $"Tax Charged: {CurrencyDisplay.FormatPlain(report.TotalTaxCharged)}";
            _kpiTaxRefunded.Text = $"Tax Refunded: {CurrencyDisplay.FormatPlain(report.TotalTaxRefunded)}";
            _kpiNetTax.Text = $"Net Tax Payable: {CurrencyDisplay.FormatPlain(report.NetTaxPayable)}";
            _kpiNetSales.Text = $"Net Sales: {CurrencyDisplay.FormatPlain(report.NetSales)}";

            _gridControl.DataSource = report.TaxBreakdown;
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show(this, $"Failed to generate tax summary report: {ex.Message}", "Report Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
            _refreshButton.Enabled = true;
        }
    }
}
