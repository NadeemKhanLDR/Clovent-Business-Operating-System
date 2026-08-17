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

        AppearanceManager.Apply(this, "Restaurant", nameof(CustomerLedgerDialog));
        Text = $"{_customer.Name} ({_customer.Code}) - Ledger Statement";

        await LoadLedgerAsync();
    }

    private async Task LoadLedgerAsync()
    {
        Cursor = Cursors.WaitCursor;
        try
        {
            var entries = await _mediator.Send(new GetCustomerLedgerQuery(_customer.CustomerId));
            _allEntries = [.. entries];

            // Fetch latest customer details to show correct outstanding balance cards
            var currentCustomer = await _mediator.Send(new GetCustomerByIdQuery(_customer.CustomerId));
            var outstanding = currentCustomer?.OutstandingBalance ?? _customer.OutstandingBalance;
            var limit = currentCustomer?.CreditLimit ?? _customer.CreditLimit;
            var available = Math.Max(0m, limit - outstanding);

            _outstandingVal.Text = CurrencyDisplay.Format(outstanding);
            _limitVal.Text = CurrencyDisplay.Format(limit);
            _availableVal.Text = CurrencyDisplay.Format(available);

            ApplyFilters();
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allEntries.AsEnumerable();

        // 1. Date Range Filter
        //
        // DateEdit.EditValue surfaces DateTime.Kind=Local (the control
        // stitches the picked date onto DateTime.Now internally), but these
        // filters only care about the calendar date, not a timezone - they
        // treat it as UTC midnight to compare against CustomerLedgerEntryDto.
        // Date (a DateTimeOffset). Kind=Local paired with the Zero offset
        // below is otherwise rejected by DateTimeOffset's constructor
        // whenever the machine's local UTC offset isn't zero ("The UTC
        // Offset of the local dateTime parameter does not match the offset
        // argument"), so Kind is normalized to Unspecified first - the only
        // Kind DateTimeOffset accepts with an arbitrary explicit offset.
        if (_dateFrom.EditValue is DateTime fromDate)
        {
            var fromUtc = new DateTimeOffset(DateTime.SpecifyKind(fromDate.Date, DateTimeKind.Unspecified), TimeSpan.Zero);
            filtered = filtered.Where(x => x.Date >= fromUtc);
        }
        if (_dateTo.EditValue is DateTime toDate)
        {
            var toUtc = new DateTimeOffset(DateTime.SpecifyKind(toDate.Date.AddDays(1).AddTicks(-1), DateTimeKind.Unspecified), TimeSpan.Zero);
            filtered = filtered.Where(x => x.Date <= toUtc);
        }

        // 2. Transaction Type Filter
        var typeFilter = _comboType.Text;
        if (typeFilter == "Sales (Debits)")
        {
            filtered = filtered.Where(x => x.Debit > 0);
        }
        else if (typeFilter == "Payments (Credits)")
        {
            filtered = filtered.Where(x => x.Credit > 0);
        }
        else if (typeFilter == "Opening Balance")
        {
            filtered = filtered.Where(x => x.Reference.Equals("OPENING", StringComparison.OrdinalIgnoreCase));
        }

        // 3. Text search (Reference or Description)
        var searchText = _txtSearchRef.Text.Trim();
        if (!string.IsNullOrEmpty(searchText))
        {
            filtered = filtered.Where(x =>
                x.Reference.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                x.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        var list = filtered.ToList();
        _ledgerGrid.DataSource = list.Select(x => new LedgerRow(
            x.Date,
            x.Reference,
            x.Description,
            x.Debit,
            x.Credit,
            x.RunningBalance)).ToList();

        // Recalculate totals of filtered transactions
        var totalDebit = list.Sum(x => x.Debit);
        var totalCredit = list.Sum(x => x.Credit);

        _totalDebitVal.Text = CurrencyDisplay.Format(totalDebit);
        _totalCreditVal.Text = CurrencyDisplay.Format(totalCredit);
    }

    // --- BUTTON EVENT WIRING ---

    private void Filter_EditValueChanged(object? sender, EventArgs e) => ApplyFilters();

    private void BtnClear_Click(object? sender, EventArgs e)
    {
        _dateFrom.EditValue = null;
        _dateTo.EditValue = null;
        _comboType.SelectedIndex = 0; // "All Transactions"
        _txtSearchRef.Text = string.Empty;
        ApplyFilters();
    }

    private async void BtnRefresh_Click(object? sender, EventArgs e) => await LoadLedgerAsync();

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

        MinimumSize = LogicalToDeviceUnits(new Size(1000, 600));
        Size = LogicalToDeviceUnits(new Size(1000, 600));

        root.RowStyles[0] = new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(85));
        root.RowStyles[1] = new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(45));
        root.RowStyles[3] = new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(52));

        filterPanel.ColumnStyles[0] = new ColumnStyle(SizeType.Absolute, LogicalToDeviceUnits(130));
        filterPanel.ColumnStyles[1] = new ColumnStyle(SizeType.Absolute, LogicalToDeviceUnits(130));
        filterPanel.ColumnStyles[2] = new ColumnStyle(SizeType.Absolute, LogicalToDeviceUnits(140));
        filterPanel.ColumnStyles[3] = new ColumnStyle(SizeType.Absolute, LogicalToDeviceUnits(160));

        _btnClear.Size = LogicalToDeviceUnits(new Size(70, 28));
        _btnRefresh.Size = LogicalToDeviceUnits(new Size(80, 28));
        _btnPrint.Size = LogicalToDeviceUnits(new Size(70, 28));
        _btnExportPdf.Size = LogicalToDeviceUnits(new Size(60, 28));
        _btnExportExcel.Size = LogicalToDeviceUnits(new Size(70, 28));

        _closeButton.MinimumSize = LogicalToDeviceUnits(new Size(120, 40));
        _ledgerGridView.RowHeight = LogicalToDeviceUnits(30);
    }

    private void LedgerGridView_CustomColumnDisplayText(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventArgs e)
    {
        if (e.Column.FieldName is "Debit" or "Credit" or "RunningBalance" && e.Value is decimal val)
        {
            e.DisplayText = val == 0 && e.Column.FieldName is "Debit" or "Credit" ? "-" : CurrencyDisplay.Format(val);
        }
        else if (e.Column.FieldName == "Date" && e.Value is DateTimeOffset dt)
        {
            e.DisplayText = dt.ToLocalTime().ToString("g");
        }
    }

    private sealed record LedgerRow(
        DateTimeOffset Date,
        string Reference,
        string Description,
        decimal Debit,
        decimal Credit,
        decimal RunningBalance);
}
