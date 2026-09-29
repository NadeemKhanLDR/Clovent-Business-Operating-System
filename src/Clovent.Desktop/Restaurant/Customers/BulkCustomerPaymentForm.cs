using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Restaurant.Application.Customers.Commands;
using Clovent.Restaurant.Application.Customers.Dtos;
using Clovent.Restaurant.Application.Customers.Queries;
using Clovent.Restaurant.Application.PaymentMethods.Queries;
using Clovent.Restaurant.Application.Shifts.Queries;
using Clovent.Restaurant.Shifts;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Repository;
using MediatR;

namespace Clovent.Desktop.Restaurant.Customers;

/// <summary>
/// Bulk Customer Collections dialog: collects payments from multiple customers
/// in a single atomic batch transaction. Visual Studio Designer compatible.
/// </summary>
public sealed partial class BulkCustomerPaymentForm : XtraForm
{
    private readonly IMediator? _mediator;
    private readonly Guid? _preselectedCustomerId;
    private readonly List<BulkPaymentRow> _rows = [];

    /// <summary>Design-time constructor.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public BulkCustomerPaymentForm()
    {
        InitializeComponent();
    }

    /// <summary>Builds bulk payment dialog with optional customer pre-selection.</summary>
    public BulkCustomerPaymentForm(IMediator mediator, Guid? preselectedCustomerId = null)
    {
        _mediator = mediator;
        _preselectedCustomerId = preselectedCustomerId;
        InitializeComponent();
        ConfigureGrid();
        ScaleLayoutAtRuntime();
        Load += BulkCustomerPaymentForm_Load;
    }

    private async void BulkCustomerPaymentForm_Load(object? sender, EventArgs e)
    {
        if (DesignModeHelper.IsInDesignMode || _mediator == null) return;

        ScaleLayoutAtRuntime();
        AppearanceManager.Apply(this, "Restaurant", nameof(BulkCustomerPaymentForm));
        await LoadPaymentMethodsAsync();
        await LoadCustomersAsync();
    }

    private void ConfigureGrid()
    {
        _gridView.Columns.Clear();

        var colSelected = _gridView.Columns.AddVisible("IsSelected", "Select");
        colSelected.Width = 55;
        var chkRepo = new RepositoryItemCheckEdit();
        chkRepo.CheckedChanged += (s, e) =>
        {
            _gridView.PostEditor();
        };
        _gridControl.RepositoryItems.Add(chkRepo);
        colSelected.ColumnEdit = chkRepo;

        var colCode = _gridView.Columns.AddVisible("CustomerCode", "Code");
        colCode.Width = 130;
        colCode.MinWidth = 100;
        colCode.OptionsColumn.AllowEdit = false;

        var colName = _gridView.Columns.AddVisible("CustomerName", "Customer Name");
        colName.Width = 200;
        colName.OptionsColumn.AllowEdit = false;

        var colMobile = _gridView.Columns.AddVisible("Mobile", "Mobile");
        colMobile.Width = 110;
        colMobile.OptionsColumn.AllowEdit = false;

        var colReceivable = _gridView.Columns.AddVisible("ReceivableBalance", "Receivable A/R");
        colReceivable.Width = 115;
        colReceivable.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
        colReceivable.DisplayFormat.FormatString = "n2";
        colReceivable.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        colReceivable.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        colReceivable.OptionsColumn.AllowEdit = false;

        var colAdvance = _gridView.Columns.AddVisible("AdvanceBalance", "Existing Advance");
        colAdvance.Width = 115;
        colAdvance.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
        colAdvance.DisplayFormat.FormatString = "n2";
        colAdvance.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        colAdvance.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        colAdvance.OptionsColumn.AllowEdit = false;

        var colPayment = _gridView.Columns.AddVisible("PaymentAmount", "Payment Amount");
        colPayment.Width = 130;
        var spinRepo = new RepositoryItemSpinEdit
        {
            MinValue = 0,
            MaxValue = 99999999,
            EditMask = "n2",
            UseMaskAsDisplayFormat = true
        };
        _gridControl.RepositoryItems.Add(spinRepo);
        colPayment.ColumnEdit = spinRepo;
        colPayment.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        colPayment.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;

        var colApplied = _gridView.Columns.AddVisible("AppliedAmount", "Applied A/R");
        colApplied.Width = 115;
        colApplied.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
        colApplied.DisplayFormat.FormatString = "n2";
        colApplied.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        colApplied.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        colApplied.OptionsColumn.AllowEdit = false;

        var colNewAdvance = _gridView.Columns.AddVisible("NewAdvance", "New Advance");
        colNewAdvance.Width = 115;
        colNewAdvance.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
        colNewAdvance.DisplayFormat.FormatString = "n2";
        colNewAdvance.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        colNewAdvance.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        colNewAdvance.OptionsColumn.AllowEdit = false;

        _gridView.CellValueChanged += (s, e) =>
        {
            if (_gridView.GetRow(e.RowHandle) is BulkPaymentRow row)
            {
                if (e.Column == colPayment)
                {
                    row.Recalculate();
                    if (row.PaymentAmount > 0 && !row.IsSelected)
                    {
                        row.IsSelected = true;
                    }
                }
                else if (e.Column == colSelected)
                {
                    // Selecting does NOT auto-clear or force fill balance; keeps debt clearance deliberate.
                    row.Recalculate();
                }
                _gridView.RefreshRow(e.RowHandle);
                UpdateSummary();
            }
        };

        _btnSelectAll.Click += (s, e) =>
        {
            foreach (var r in _rows)
            {
                r.IsSelected = true;
                r.Recalculate();
            }
            _gridControl.RefreshDataSource();
            UpdateSummary();
        };

        _btnClearSelection.Click += (s, e) =>
        {
            foreach (var r in _rows)
            {
                r.IsSelected = false;
                r.PaymentAmount = 0m;
                r.Recalculate();
            }
            _gridControl.RefreshDataSource();
            UpdateSummary();
        };

        _btnFillOutstanding.Click += (s, e) =>
        {
            var count = 0;
            foreach (var r in _rows.Where(x => x.IsSelected))
            {
                r.PaymentAmount = r.ReceivableBalance > 0 ? r.ReceivableBalance : 0m;
                r.Recalculate();
                count++;
            }

            if (count == 0)
            {
                XtraMessageBox.Show(this, "Select at least one customer row to fill with outstanding balance.", "No Rows Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _gridControl.RefreshDataSource();
            UpdateSummary();
        };

        _btnSubmit.Click += async (s, e) => await SubmitBatchAsync();
    }

    private async Task LoadPaymentMethodsAsync()
    {
        if (_mediator == null) return;
        var methods = await _mediator.Send(new ListPaymentMethodsQuery());
        _comboPaymentMethod.Properties.Items.Clear();
        foreach (var m in methods.Where(x => x.Status == "Active"))
        {
            _comboPaymentMethod.Properties.Items.Add(m.Name);
        }
        if (_comboPaymentMethod.Properties.Items.Count > 0)
        {
            _comboPaymentMethod.SelectedIndex = 0;
        }
    }

    private async Task LoadCustomersAsync()
    {
        if (_mediator == null) return;
        var customers = await _mediator.Send(new ListCustomersQuery());
        _rows.Clear();

        // Safe from accidental debt clearance:
        // 1. Exclude Walk-in Guest C000 entirely
        // 2. Default PaymentAmount is 0.00
        // 3. Unselected by default unless preselected
        var filtered = customers
            .Where(x => x.IsActive && !string.Equals(x.Code, "C000", StringComparison.OrdinalIgnoreCase) && !string.Equals(x.Name, "Walk-in Guest", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.ReceivableBalance > 0 ? x.ReceivableBalance : x.OutstandingBalance)
            .ThenBy(x => x.Name);

        int preselectedRowHandle = -1;
        int rowIndex = 0;

        foreach (var c in filtered)
        {
            var rec = c.ReceivableBalance > 0 ? c.ReceivableBalance : Math.Max(0, c.OutstandingBalance);
            var adv = c.AdvanceBalance;
            var isPreselected = _preselectedCustomerId.HasValue && c.CustomerId == _preselectedCustomerId.Value;

            var r = new BulkPaymentRow
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.Code,
                CustomerName = c.Name,
                Mobile = c.MobileNumber,
                ReceivableBalance = rec,
                AdvanceBalance = adv,
                PaymentAmount = 0.00m,
                IsSelected = isPreselected
            };
            r.Recalculate();
            _rows.Add(r);

            if (isPreselected)
            {
                preselectedRowHandle = rowIndex;
            }
            rowIndex++;
        }

        _gridControl.DataSource = _rows;
        _gridControl.RefreshDataSource();

        if (preselectedRowHandle >= 0)
        {
            _gridView.FocusedRowHandle = preselectedRowHandle;
        }

        UpdateSummary();
    }

    private void UpdateSummary()
    {
        var selectedRows = _rows.Where(r => r.IsSelected).ToList();
        var payingRows = selectedRows.Where(r => r.PaymentAmount > 0).ToList();

        var totalAmount = payingRows.Sum(r => r.PaymentAmount);
        var totalApplied = payingRows.Sum(r => r.AppliedAmount);
        var totalAdvance = payingRows.Sum(r => r.NewAdvance);

        _lblSelectedCount.Text = $"Selected: {selectedRows.Count}";
        _lblTotalAmount.Text = $"Total Payment: {CurrencyDisplay.FormatPlain(totalAmount)}";
        _lblTotalApplied.Text = $"Applied to A/R: {CurrencyDisplay.FormatPlain(totalApplied)}";
        _lblTotalAdvance.Text = $"New Advances: {CurrencyDisplay.FormatPlain(totalAdvance)}";
    }

    private async Task SubmitBatchAsync()
    {
        if (_mediator == null) return;

        var selected = _rows.Where(r => r.IsSelected && r.PaymentAmount > 0).ToList();
        if (selected.Count == 0)
        {
            XtraMessageBox.Show(this, "Please select at least one customer with a payment amount greater than zero.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var method = _comboPaymentMethod.Text.Trim();
        if (string.IsNullOrWhiteSpace(method))
        {
            XtraMessageBox.Show(this, "Please select a payment method for the collection batch.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _comboPaymentMethod.Focus();
            return;
        }

        // Active cashier shift check for Cash collections
        Guid? shiftId = null;
        var isCash = string.Equals(method, "Cash", StringComparison.OrdinalIgnoreCase);
        if (isCash)
        {
            var cashierId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var activeShift = await _mediator.Send(new GetActiveShiftQuery(CashierId: cashierId));
            if (activeShift == null)
            {
                var openShifts = await _mediator.Send(new ListShiftsQuery(Status: ShiftStatus.Open));
                activeShift = openShifts.FirstOrDefault();
            }

            if (activeShift == null)
            {
                XtraMessageBox.Show(
                    this,
                    "An active cashier shift is required to record Cash collections.\n\n" +
                    "Please open a shift at a POS terminal before processing cash collections, or select a non-cash payment method (e.g. Bank Transfer, Cheque).",
                    "Active Shift Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            shiftId = activeShift.ShiftId;
        }

        var batchRef = string.IsNullOrWhiteSpace(_txtBatchReference.Text) ? null : _txtBatchReference.Text.Trim();
        var notes = string.IsNullOrWhiteSpace(_txtNotes.Text) ? null : _txtNotes.Text.Trim();

        var confirmMsg = $"Record bulk collections for {selected.Count} customer(s) totaling {CurrencyDisplay.FormatPlain(selected.Sum(r => r.PaymentAmount))} via {method}?";
        if (XtraMessageBox.Show(this, confirmMsg, "Confirm Bulk Collections", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        Cursor = Cursors.WaitCursor;
        _btnSubmit.Enabled = false;
        try
        {
            var entries = selected.Select(s => new BulkCustomerPaymentItem(
                s.CustomerId,
                s.PaymentAmount,
                method,
                batchRef,
                notes)).ToList();

            var result = await _mediator.Send(new RecordBulkCustomerPaymentsCommand(entries, shiftId, batchRef));

            var successMsg = $"Successfully recorded bulk collections:\n\n" +
                             $"• Batch Total: {CurrencyDisplay.FormatPlain(result.TotalReceived)}\n" +
                             $"• Applied to Receivables: {CurrencyDisplay.FormatPlain(result.TotalAppliedToReceivables)}\n" +
                             $"• New Advances Created: {CurrencyDisplay.FormatPlain(result.TotalNewAdvances)}\n" +
                             $"• Processed Customers: {result.Results.Count}";

            XtraMessageBox.Show(this, successMsg, "Batch Collection Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show(this, $"Failed to record bulk collections: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnSubmit.Enabled = true;
            Cursor = Cursors.Default;
        }
    }

    private void ScaleLayoutAtRuntime()
    {
        if (DesignModeHelper.IsInDesignMode) return;

        configPanel.ColumnStyles[0] = new ColumnStyle(SizeType.Absolute, DesktopDpi.Scale(240, this));
        configPanel.ColumnStyles[1] = new ColumnStyle(SizeType.Absolute, DesktopDpi.Scale(240, this));

        int btnH = DesktopDpi.Scale(32, this);
        _btnSelectAll.MinimumSize = new Size(DesktopDpi.Scale(90, this), btnH);
        _btnClearSelection.MinimumSize = new Size(DesktopDpi.Scale(90, this), btnH);
        _btnFillOutstanding.MinimumSize = new Size(DesktopDpi.Scale(220, this), btnH);

        _btnCancel.MinimumSize = new Size(DesktopDpi.Scale(100, this), DesktopDpi.Scale(36, this));
        _btnSubmit.MinimumSize = new Size(DesktopDpi.Scale(200, this), DesktopDpi.Scale(36, this));

        _gridView.RowHeight = DesktopDpi.Scale(30, this);
        _gridView.ColumnPanelRowHeight = DesktopDpi.Scale(32, this);

        DesktopDialogSizing.Apply(this, 1060, 680, 850, 480, this.Owner ?? this.Parent, true);
    }

    private sealed class BulkPaymentRow
    {
        public bool IsSelected { get; set; }
        public Guid CustomerId { get; set; }
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public decimal ReceivableBalance { get; set; }
        public decimal AdvanceBalance { get; set; }
        public decimal PaymentAmount { get; set; }
        public decimal AppliedAmount { get; set; }
        public decimal NewAdvance { get; set; }

        public void Recalculate()
        {
            AppliedAmount = Math.Min(PaymentAmount, Math.Max(0, ReceivableBalance));
            NewAdvance = Math.Max(0, PaymentAmount - Math.Max(0, ReceivableBalance));
        }
    }
}
