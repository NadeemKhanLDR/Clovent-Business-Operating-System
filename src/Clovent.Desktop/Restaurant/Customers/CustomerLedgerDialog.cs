using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Restaurant.Application.Customers.Dtos;
using Clovent.Restaurant.Application.Customers.Queries;
using DevExpress.XtraEditors;
using MediatR;

namespace Clovent.Desktop.Restaurant.Customers;

/// <summary>
/// Dialog showing the transaction ledger (debits, credits, running balance)
/// for a single customer. Supports filtering, print previews, and PDF/Excel exports.
/// Visual Studio Designer compatible.
/// </summary>
public sealed partial class CustomerLedgerDialog : XtraForm
{
    private readonly IMediator _mediator;
    private readonly CustomerDto _customer;

    private List<CustomerLedgerEntryDto> _allEntries = [];
    private bool _isUpdatingPeriod;
    private bool _isLoading;

    /// <summary>Design-time-only constructor for Visual Studio Designer.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public CustomerLedgerDialog()
    {
        _mediator = null!;
        _customer = null!;
        InitializeComponent();
        ScaleLayoutAtRuntime();
    }

    /// <summary>Builds the dialog for a specific customer.</summary>
    public CustomerLedgerDialog(IMediator mediator, CustomerDto customer)
    {
        _mediator = mediator;
        _customer = customer;

        InitializeComponent();
        ScaleLayoutAtRuntime();
    }

    private void AppearanceManager_Changed(object? sender, EventArgs e) =>
        AppearanceManager.Apply(this, "Restaurant", nameof(CustomerLedgerDialog));

    private async void CustomerLedgerDialog_Load(object? sender, EventArgs e)
    {
        if (DesignModeHelper.IsInDesignMode)
            return;

        Clovent.Desktop.Forms.Base.Localization.LocalizationHelper.LocalizeControl(this);
        AppearanceManager.Apply(this, "Restaurant", nameof(CustomerLedgerDialog));
        ConfigureReportPeriodOptions();
        Text = $"{_customer.Name} ({_customer.Code}) - Ledger Statement";
        _customerLabel.Text = $"— {_customer.Name} ({_customer.Code})";

        _isUpdatingPeriod = true;
        try
        {
            _periodCombo.SelectedItem = "This Month";
            var range = ReportPeriodCalculator.CalculateRange(ReportPeriod.ThisMonth, DateOnly.FromDateTime(DateTime.Today));
            _dateFrom.EditValue = range.From.ToDateTime(TimeOnly.MinValue);
            _dateTo.EditValue = range.To.ToDateTime(TimeOnly.MinValue);
        }
        finally
        {
            _isUpdatingPeriod = false;
        }

        DesktopDialogSizing.Apply(this, 1040, 680, 900, 560, null, true);
        await LoadLedgerAsync();
    }

    private void ConfigureReportPeriodOptions()
    {
        if (DesignModeHelper.IsInDesignMode) return;
        _periodCombo.Properties.Items.Clear();
        foreach (var (_, name) in ReportPeriodCalculator.GetAllOptions())
        {
            _periodCombo.Properties.Items.Add(name);
        }
        _periodCombo.Properties.Items.Add("All Time");
    }

    private async Task LoadLedgerAsync()
    {
        if (_isLoading) return;
        _isLoading = true;
        _btnLoadLedger.Enabled = false;
        Cursor = Cursors.WaitCursor;
        _lblStatus.Text = "Loading ledger transactions...";
        try
        {
            var entries = await _mediator.Send(new GetCustomerLedgerQuery(_customer.CustomerId));
            _allEntries = [.. entries];

            // Fetch latest customer details to show correct outstanding balance cards
            var currentCustomer = await _mediator.Send(new GetCustomerByIdQuery(_customer.CustomerId));
            var outstanding = currentCustomer?.OutstandingBalance ?? _customer.OutstandingBalance;
            var limit = currentCustomer?.CreditLimit ?? _customer.CreditLimit;
            var available = Math.Max(0m, limit - outstanding);

            _outstandingVal.Text = CurrencyDisplay.FormatPlain(outstanding);
            _limitVal.Text = CurrencyDisplay.FormatPlain(limit);
            _availableVal.Text = CurrencyDisplay.FormatPlain(available);

            ApplyFilters();
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"Error loading ledger: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            _btnLoadLedger.Enabled = true;
            Cursor = Cursors.Default;
        }
    }

    private void ApplyFilters()
    {
        DateTimeOffset? fromUtc = null;
        DateTimeOffset? toUtcExclusive = null;

        if (_dateFrom.EditValue is DateTime fromDate && _dateTo.EditValue is DateTime toDate)
        {
            var range = new DateRange(DateOnly.FromDateTime(fromDate), DateOnly.FromDateTime(toDate));
            if (range.IsValid)
            {
                fromUtc = range.GetStartUtc(DateTimeDisplay.BusinessTimeZone);
                toUtcExclusive = range.GetEndUtcExclusive(DateTimeDisplay.BusinessTimeZone);
            }
        }
        else
        {
            if (_dateFrom.EditValue is DateTime fDate)
            {
                var fDateOnly = DateOnly.FromDateTime(fDate);
                fromUtc = new DateRange(fDateOnly, fDateOnly).GetStartUtc(DateTimeDisplay.BusinessTimeZone);
            }
            if (_dateTo.EditValue is DateTime tDate)
            {
                var tDateOnly = DateOnly.FromDateTime(tDate);
                toUtcExclusive = new DateRange(tDateOnly, tDateOnly).GetEndUtcExclusive(DateTimeDisplay.BusinessTimeZone);
            }
        }

        // Sort entries chronologically
        var sortedEntries = _allEntries.OrderBy(x => x.Date).ThenBy(x => x.Id).ToList();

        // 1. Calculate Period Opening Balance (Balance Brought Forward)
        decimal periodOpeningBalance = 0m;
        List<CustomerLedgerEntryDto> priorEntries = [];
        List<CustomerLedgerEntryDto> periodEntries = [];

        if (fromUtc.HasValue)
        {
            priorEntries = sortedEntries.Where(x => x.Date < fromUtc.Value).ToList();
            periodOpeningBalance = priorEntries.Sum(x => x.Debit) - priorEntries.Sum(x => x.Credit);

            periodEntries = sortedEntries.Where(x => x.Date >= fromUtc.Value && (!toUtcExclusive.HasValue || x.Date < toUtcExclusive.Value)).ToList();
        }
        else
        {
            periodEntries = sortedEntries.Where(x => !toUtcExclusive.HasValue || x.Date < toUtcExclusive.Value).ToList();
        }

        // 2. Build rows with running balance
        var displayRows = new List<LedgerRow>();
        decimal currentRunning = periodOpeningBalance;

        // Add Balance Brought Forward row if date filter starts after beginning and there is prior activity
        if (fromUtc.HasValue && (periodOpeningBalance != 0m || priorEntries.Count > 0))
        {
            displayRows.Add(new LedgerRow(
                fromUtc.Value,
                "OPENING",
                "Balance Brought Forward",
                periodOpeningBalance > 0 ? periodOpeningBalance : 0m,
                periodOpeningBalance < 0 ? Math.Abs(periodOpeningBalance) : 0m,
                periodOpeningBalance,
                IsOpeningRow: true));
        }

        // 3. Add period transactions
        foreach (var entry in periodEntries)
        {
            currentRunning += (entry.Debit - entry.Credit);
            displayRows.Add(new LedgerRow(
                entry.Date,
                entry.Reference,
                entry.Description,
                entry.Debit,
                entry.Credit,
                currentRunning,
                IsOpeningRow: false));
        }

        // 4. Apply in-memory secondary filters (Type, Search text)
        var filteredRows = displayRows.AsEnumerable();

        var typeFilter = _comboType.Text;
        if (typeFilter == "Sales (Debits)")
        {
            filteredRows = filteredRows.Where(x => x.Debit > 0);
        }
        else if (typeFilter == "Payments (Credits)")
        {
            filteredRows = filteredRows.Where(x => x.Credit > 0);
        }
        else if (typeFilter == "Opening Balance")
        {
            filteredRows = filteredRows.Where(x => x.IsOpeningRow || x.Reference.Equals("OPENING", StringComparison.OrdinalIgnoreCase));
        }

        var searchText = _txtSearchRef.Text.Trim();
        if (!string.IsNullOrEmpty(searchText))
        {
            filteredRows = filteredRows.Where(x =>
                x.Reference.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                x.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        var list = filteredRows.ToList();
        _ledgerGrid.DataSource = list;

        // Recalculate totals of period transactions
        var periodDebits = periodEntries.Sum(x => x.Debit);
        var periodCredits = periodEntries.Sum(x => x.Credit);
        _totalDebitVal.Text = CurrencyDisplay.FormatPlain(periodDebits);
        _totalCreditVal.Text = CurrencyDisplay.FormatPlain(periodCredits);

        // Update action buttons state
        _btnPrint.Enabled = list.Count > 0;
        _btnExportPdf.Enabled = list.Count > 0;
        _btnExportExcel.Enabled = list.Count > 0;

        // Status update
        if (list.Count == 0)
        {
            _lblStatus.Text = "0 transactions loaded";
        }
        else if (list.Count == 1 && list[0].IsOpeningRow)
        {
            _lblStatus.Text = "Opening balance brought forward: " + CurrencyDisplay.FormatPlain(list[0].RunningBalance);
        }
        else if (list.Count == 1)
        {
            _lblStatus.Text = "1 transaction loaded";
        }
        else
        {
            _lblStatus.Text = $"{list.Count} transactions loaded";
        }
    }

    // --- BUTTON EVENT WIRING ---

    private void PeriodCombo_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_isUpdatingPeriod) return;

        var periodName = _periodCombo.SelectedItem?.ToString();
        if (periodName == "All Time")
        {
            _isUpdatingPeriod = true;
            try
            {
                _dateFrom.EditValue = null;
                _dateTo.EditValue = null;
            }
            finally
            {
                _isUpdatingPeriod = false;
            }

            ApplyFilters();
            return;
        }

        var period = ReportPeriodCalculator.ParseDisplayName(periodName);
        if (period == ReportPeriod.Custom) return;

        _isUpdatingPeriod = true;
        try
        {
            var range = ReportPeriodCalculator.CalculateRange(period, DateOnly.FromDateTime(DateTime.Today));
            _dateFrom.EditValue = range.From.ToDateTime(TimeOnly.MinValue);
            _dateTo.EditValue = range.To.ToDateTime(TimeOnly.MinValue);
        }
        finally
        {
            _isUpdatingPeriod = false;
        }

        ApplyFilters();
    }

    private void DateEdit_EditValueChanged(object? sender, EventArgs e)
    {
        if (_isUpdatingPeriod) return;

        if (_periodCombo.SelectedItem?.ToString() != "Custom")
        {
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

        ApplyFilters();
    }

    private void Filter_EditValueChanged(object? sender, EventArgs e) => ApplyFilters();

    private void BtnClear_Click(object? sender, EventArgs e)
    {
        _isUpdatingPeriod = true;
        try
        {
            _periodCombo.SelectedItem = "All Time";
            _dateFrom.EditValue = null;
            _dateTo.EditValue = null;
        }
        finally
        {
            _isUpdatingPeriod = false;
        }

        _comboType.SelectedIndex = 0; // "All Transactions"
        _txtSearchRef.Text = string.Empty;
        ApplyFilters();
    }

    private async void BtnLoadLedger_Click(object? sender, EventArgs e) => await LoadLedgerAsync();

    private void BtnPrint_Click(object? sender, EventArgs e)
    {
        _ledgerGrid.ShowPrintPreview();
    }

    private void BtnExportPdf_Click(object? sender, EventArgs e)
    {
        using var save = new SaveFileDialog { Filter = "PDF files (*.pdf)|*.pdf", FileName = $"{_customer.Name}_Ledger.pdf" };
        if (save.ShowDialog(this) == DialogResult.OK)
        {
            _ledgerGrid.ExportToPdf(save.FileName);
            XtraMessageBox.Show(this, "Ledger exported to PDF successfully.", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void BtnExportExcel_Click(object? sender, EventArgs e)
    {
        using var save = new SaveFileDialog { Filter = "Excel files (*.xlsx)|*.xlsx", FileName = $"{_customer.Name}_Ledger.xlsx" };
        if (save.ShowDialog(this) == DialogResult.OK)
        {
            _ledgerGrid.ExportToXlsx(save.FileName);
            XtraMessageBox.Show(this, "Ledger exported to Excel successfully.", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void ScaleLayoutAtRuntime()
    {
        if (DesignModeHelper.IsInDesignMode) return;

        root.RowStyles[1] = new RowStyle(SizeType.Absolute, DesktopDpi.Scale(78, this));
        root.RowStyles[5] = new RowStyle(SizeType.Absolute, DesktopDpi.Scale(48, this));

        filterPanel.ColumnStyles[0] = new ColumnStyle(SizeType.Absolute, DesktopDpi.Scale(140, this));
        filterPanel.ColumnStyles[1] = new ColumnStyle(SizeType.Absolute, DesktopDpi.Scale(130, this));
        filterPanel.ColumnStyles[2] = new ColumnStyle(SizeType.Absolute, DesktopDpi.Scale(130, this));
        filterPanel.ColumnStyles[3] = new ColumnStyle(SizeType.Absolute, DesktopDpi.Scale(160, this));

        int filterBtnH = DesktopDpi.Scale(32, this);
        _btnLoadLedger.MinimumSize = new Size(DesktopDpi.Scale(110, this), filterBtnH);
        _btnClear.MinimumSize = new Size(DesktopDpi.Scale(70, this), filterBtnH);
        _btnPrint.MinimumSize = new Size(DesktopDpi.Scale(70, this), filterBtnH);
        _btnExportPdf.MinimumSize = new Size(DesktopDpi.Scale(60, this), filterBtnH);
        _btnExportExcel.MinimumSize = new Size(DesktopDpi.Scale(65, this), filterBtnH);

        _closeButton.MinimumSize = new Size(DesktopDpi.Scale(120, this), DesktopDpi.Scale(36, this));
        _ledgerGridView.RowHeight = DesktopDpi.Scale(30, this);
        _ledgerGridView.ColumnPanelRowHeight = DesktopDpi.Scale(32, this);

        _ledgerGridView.Columns["Date"].MinWidth = DesktopDpi.Scale(110, this);
        _ledgerGridView.Columns["Reference"].MinWidth = DesktopDpi.Scale(90, this);
        _ledgerGridView.Columns["Description"].MinWidth = DesktopDpi.Scale(160, this);
        _ledgerGridView.Columns["Debit"].MinWidth = DesktopDpi.Scale(95, this);
        _ledgerGridView.Columns["Credit"].MinWidth = DesktopDpi.Scale(95, this);
        _ledgerGridView.Columns["RunningBalance"].MinWidth = DesktopDpi.Scale(115, this);
    }

    private void LedgerGridView_CustomDrawEmptyForeground(object? sender, DevExpress.XtraGrid.Views.Base.CustomDrawEventArgs e)
    {
        if (_ledgerGridView.RowCount > 0) return;
        string message = (_customer.Code == "C000" || _customer.IsDefault)
            ? "Walk-in guest has no credit ledger transactions. Counter sales are settled immediately upon payment."
            : "No ledger transactions found for the selected period.";
        using var font = new Font("Segoe UI", 10.5F, FontStyle.Regular);
        using var brush = new SolidBrush(Color.FromArgb(100, 116, 139));
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        e.Graphics.DrawString(message, font, brush, e.Bounds, sf);
    }

    private void LedgerGridView_CustomColumnDisplayText(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventArgs e)
    {
        if (e.Column.FieldName is "Debit" or "Credit" or "RunningBalance" && e.Value != null && e.Value != DBNull.Value)
        {
            try
            {
                var val = Convert.ToDecimal(e.Value);
                e.DisplayText = val == 0 && e.Column.FieldName is "Debit" or "Credit" ? "-" : CurrencyDisplay.FormatPlain(val);
            }
            catch
            {
                // Fallback
            }
        }
        else if (e.Column.FieldName == "Date" && e.Value is DateTimeOffset dt)
        {
            e.DisplayText = DateTimeDisplay.Format(dt);
        }
    }

    private sealed record LedgerRow(
        DateTimeOffset Date,
        string Reference,
        string Description,
        decimal Debit,
        decimal Credit,
        decimal RunningBalance,
        bool IsOpeningRow = false);
}
