using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Restaurant.Application.Customers.Commands;
using Clovent.Restaurant.Application.Customers.Dtos;
using Clovent.Restaurant.Application.Customers.Queries;
using Clovent.Restaurant.Application.PaymentMethods.Queries;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Restaurant.Customers;

/// <summary>
/// Back-office Customer Receivables / Accounts Receivable Aging report view.
/// Shows current customer balances, advance credit, aging distribution,
/// and allows receiving individual or bulk payments and inspecting ledgers.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class CustomerReceivablesReportView : XtraUserControl
{
    private const string FeatureCode = "customerreceivables";

    private readonly IServiceScope? _scope;
    private readonly ScreenOperationGate _gate = new();
    private readonly IMediator? _mediator;
    private readonly IFeatureAuthorizationPolicy? _featurePolicy;
    private readonly ICurrentSession? _currentSession;

    private bool _isLoading;

    /// <summary>Builds the report view and starts its DI scope.</summary>
    public CustomerReceivablesReportView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = new SerializedMediator(_scope.ServiceProvider.GetRequiredService<IMediator>(), _gate);
        _featurePolicy = new SerializedFeatureAuthorizationPolicy(_scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>(), _gate);
        _currentSession = currentSession;

        AppearanceManager.Changed += AppearanceManager_Changed;

        InitializeComponent();
    }

    /// <summary>Design-time only constructor.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public CustomerReceivablesReportView()
    {
        InitializeComponent();
    }

    private void AppearanceManager_Changed(object? sender, EventArgs e) =>
        AppearanceManager.Apply(this, "Restaurant", nameof(CustomerReceivablesReportView));

    private async void CustomerReceivablesReportView_Load(object? sender, EventArgs e)
    {
        if (DesignModeHelper.IsInDesignMode || _mediator == null) return;

        ScaleLayoutAtRuntime();
        DpiChangedAfterParent += (_, _) => ScaleLayoutAtRuntime();

        AppearanceManager.Apply(this, "Restaurant", nameof(CustomerReceivablesReportView));

        _btnReceivePayment.Click += async (_, _) => await ReceivePaymentSelectedAsync();
        _btnBulkReceive.Click += async (_, _) => await OpenBulkReceiveDialogAsync();
        _btnLedger.Click += async (_, _) => await ViewLedgerSelectedAsync();
        _btnStatement.Click += async (_, _) => await PrintStatementSelectedAsync();

        await RefreshReportAsync();
    }

    /// <inheritdoc/>
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        ScaleLayoutAtRuntime();
    }

    private async void RefreshReport()
    {
        await RefreshReportAsync();
    }

    private async Task RefreshReportAsync()
    {
        if (_mediator == null || _isLoading) return;

        _isLoading = true;
        _refreshButton.Enabled = false;
        Cursor = Cursors.WaitCursor;

        try
        {
            await CurrencyDisplayLoader.ConfigureAsync(_mediator);

            var asOfDate = _asOfDateEdit.EditValue is DateTime dt
                ? (DateTimeOffset)new DateTimeOffset(DateTime.SpecifyKind(dt.Date.AddDays(1).AddTicks(-1), DateTimeKind.Local))
                : DateTimeOffset.UtcNow;

            var filter = _filterCombo.SelectedIndex switch
            {
                1 => CustomerReceivablesFilter.HasBalanceOnly,
                2 => CustomerReceivablesFilter.OverLimitOnly,
                3 => CustomerReceivablesFilter.AdvanceOnly,
                _ => CustomerReceivablesFilter.All
            };

            var searchText = string.IsNullOrWhiteSpace(_searchEdit.Text) ? null : _searchEdit.Text.Trim();

            var query = new GetCustomerReceivablesReportQuery(asOfDate, filter, searchText);
            var report = await _mediator.Send(query);

            _totalReceivablesLabel.Text = CurrencyDisplay.Format(report.TotalReceivables);
            _totalAdvancesLabel.Text = CurrencyDisplay.Format(report.TotalAdvances);
            _totalOverLimitLabel.Text = CurrencyDisplay.Format(report.TotalOverCreditLimit);
            _accountsWithBalanceLabel.Text = report.ActiveAccountsWithBalanceCount.ToString();

            _grid.DataSource = report.Rows;
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show(this, $"Failed to load receivables report: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _isLoading = false;
            _refreshButton.Enabled = true;
            Cursor = Cursors.Default;
        }
    }

    private CustomerReceivableRowDto? GetFocusedRow() =>
        _gridView.GetFocusedRow() as CustomerReceivableRowDto;

    private async Task ReceivePaymentSelectedAsync()
    {
        if (_mediator == null) return;
        var row = GetFocusedRow();
        if (row == null)
        {
            XtraMessageBox.Show(this, "Select a customer from the grid first.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var customer = await _mediator.Send(new GetCustomerByIdQuery(row.CustomerId));
        if (customer == null) return;

        var paymentMethods = await _mediator.Send(new ListPaymentMethodsQuery());
        var activeMethodNames = paymentMethods
            .Where(m => m.Status == "Active")
            .Select(m => m.Name)
            .ToList();

        if (activeMethodNames.Count == 0)
        {
            XtraMessageBox.Show(this, "No active payment methods configured.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var form = new CustomerPaymentForm(customer, activeMethodNames);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            var res = await _mediator.Send(new RecordCustomerPaymentCommand(
                customer.CustomerId,
                form.Amount,
                form.PaymentMethod,
                form.Reference,
                form.Notes));

            var detailMsg = $"Received payment of {CurrencyDisplay.FormatPlain(form.Amount)} for {customer.Name} ({customer.Code}). Outstanding: {CurrencyDisplay.FormatPlain(res.OutstandingAfter)}.";
            XtraMessageBox.Show(this, detailMsg, "Payment Recorded", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await RefreshReportAsync();
        }
    }

    private async Task OpenBulkReceiveDialogAsync()
    {
        if (_mediator == null) return;
        var focusedRow = GetFocusedRow();
        using var bulkDialog = new BulkCustomerPaymentForm(_mediator, focusedRow?.CustomerId);
        if (bulkDialog.ShowDialog(this) == DialogResult.OK)
        {
            await RefreshReportAsync();
        }
    }

    private async Task ViewLedgerSelectedAsync()
    {
        if (_mediator == null) return;
        var row = GetFocusedRow();
        if (row == null)
        {
            XtraMessageBox.Show(this, "Select a customer from the grid first.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var customer = await _mediator.Send(new GetCustomerByIdQuery(row.CustomerId));
        if (customer != null)
        {
            using var ledgerDialog = new CustomerLedgerDialog(_mediator, customer);
            ledgerDialog.ShowDialog(this);
            await RefreshReportAsync();
        }
    }

    private async Task PrintStatementSelectedAsync()
    {
        if (_mediator == null) return;
        var row = GetFocusedRow();
        if (row == null)
        {
            XtraMessageBox.Show(this, "Select a customer from the grid first.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var customer = await _mediator.Send(new GetCustomerByIdQuery(row.CustomerId));
        if (customer != null)
        {
            using var ledgerDialog = new CustomerLedgerDialog(_mediator, customer);
            ledgerDialog.ShowDialog(this);
            await RefreshReportAsync();
        }
    }

    private async void GridView_DoubleClick(object? sender, EventArgs e)
    {
        await ViewLedgerSelectedAsync();
    }

    private static readonly string[] MoneyColumns =
    [
        "CreditLimit", "Receivable", "Advance", "CurrentBalance", "AvailableCredit",
        "CurrentBucket", "Days1To7Bucket", "Days8To15Bucket",
        "Days16To30Bucket", "Days31To60Bucket", "Days60PlusBucket"
    ];

    private static readonly string[] AgingBucketColumns =
    [
        "CurrentBucket", "Days1To7Bucket", "Days8To15Bucket",
        "Days16To30Bucket", "Days31To60Bucket", "Days60PlusBucket"
    ];

    private void GridView_CustomColumnDisplayText(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventArgs e)
    {
        if (e.Value != null && e.Value != DBNull.Value)
        {
            if (MoneyColumns.Contains(e.Column.FieldName))
            {
                try
                {
                    var val = Convert.ToDecimal(e.Value);
                    if (val == 0 && AgingBucketColumns.Contains(e.Column.FieldName))
                    {
                        e.DisplayText = "-";
                    }
                    else
                    {
                        e.DisplayText = CurrencyDisplay.FormatPlain(val);
                    }
                }
                catch { }
            }
            else if (e.Column.FieldName == "LastTransactionDate" && e.Value is DateTimeOffset dt)
            {
                e.DisplayText = DateTimeDisplay.Format(dt);
            }
        }
    }

    private void GridView_RowStyle(object sender, RowStyleEventArgs e)
    {
        if (_gridView.GetRow(e.RowHandle) is CustomerReceivableRowDto row)
        {
            if (row.IsOverCreditLimit)
            {
                e.Appearance.BackColor = Color.FromArgb(254, 242, 242); // Soft red background
                e.Appearance.ForeColor = Color.FromArgb(185, 28, 28);   // Dark red text
                e.Appearance.Options.UseBackColor = true;
                e.Appearance.Options.UseForeColor = true;
            }
        }
    }

    private void GridView_CustomDrawEmptyForeground(object? sender, DevExpress.XtraGrid.Views.Base.CustomDrawEventArgs e)
    {
        if (_gridView.RowCount > 0) return;
        e.Handled = true;
        using var font = new Font("Segoe UI", 10F);
        TextRenderer.DrawText(e.Graphics, "No customer receivables found matching criteria.", font, e.Bounds, Color.Gray,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    private void PreviewButton_Click(object? sender, EventArgs e)
    {
        var asOfText = _asOfDateEdit.EditValue is DateTime dt
            ? dt.ToString("dd-MMM-yyyy")
            : BusinessDateTimeService.Instance.Today.ToString("dd-MMM-yyyy");
        GridReportingPrintService.ShowPreview(_grid, "CUSTOMER RECEIVABLES / A/R AGING", $"As of Date: {asOfText}", this);
    }

    private void PrintButton_Click(object? sender, EventArgs e)
    {
        var asOfText = _asOfDateEdit.EditValue is DateTime dt
            ? dt.ToString("dd-MMM-yyyy")
            : BusinessDateTimeService.Instance.Today.ToString("dd-MMM-yyyy");
        GridReportingPrintService.Print(_grid, "CUSTOMER RECEIVABLES / A/R AGING", $"As of Date: {asOfText}", this);
    }

    private void ExportPdfButton_Click(object? sender, EventArgs e)
    {
        var asOfText = _asOfDateEdit.EditValue is DateTime dt
            ? dt.ToString("dd-MMM-yyyy")
            : BusinessDateTimeService.Instance.Today.ToString("dd-MMM-yyyy");
        GridReportingPrintService.ExportToPdf(_grid, "CUSTOMER RECEIVABLES / A/R AGING", $"As of Date: {asOfText}", this);
    }

    private void ExportExcelButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog { Filter = "Excel files (*.xlsx)|*.xlsx", FileName = "CustomerReceivablesAging.xlsx" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _grid.ExportToXlsx(dialog.FileName);
            XtraMessageBox.Show(this, "Customer Receivables report exported to Excel successfully.", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            AppearanceManager.Changed -= AppearanceManager_Changed;
            _scope?.Dispose();
            _gate.Dispose();
        }

        base.Dispose(disposing);
    }
}
