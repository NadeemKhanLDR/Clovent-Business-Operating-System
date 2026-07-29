using Clovent.Desktop.MasterData;
using Clovent.Restaurant.Application.Orders.Queries;
using Clovent.Restaurant.Application.PaymentMethods.Queries;
using Clovent.Restaurant.Application.Payments.Commands;
using Clovent.Restaurant.Application.Payments.Queries;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using MediatR;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>
/// The Payment Screen: records one or more payments against an order
/// (partial payments, multiple tenders, and split-bill are all just several
/// of these - see <c>RecordPaymentCommand</c>'s doc comment), shows the
/// running balance as each is recorded, and lets a payment recorded in error
/// be voided. Unlike the other Restaurant dialogs (built on
/// <c>MasterDataEditFormBase</c>), this one performs its own MediatR calls
/// rather than handing values back to a caller, since it is inherently
/// stateful - the balance changes after every action taken inside it.
/// </summary>
public sealed class PaymentForm : XtraForm
{
    private readonly IMediator _mediator;
    private readonly Guid _orderId;

    private readonly LabelControl _grandTotalLabel = new();
    private readonly LabelControl _paidTotalLabel = new();
    private readonly LabelControl _balanceLabel = new();
    private readonly ComboBoxEdit _paymentMethodCombo = new();
    private readonly SpinEdit _amountEdit = new() { Properties = { MinValue = 0, MaxValue = 1_000_000, Increment = 0.01m } };
    private readonly SimpleButton _recordButton = new() { Text = "Record Payment" };
    private readonly SimpleButton _voidButton = new() { Text = "Void Payment" };
    private readonly SimpleButton _receiptButton = new() { Text = "Receipt Preview" };
    private readonly SimpleButton _closeButton = new() { Text = "Close", DialogResult = DialogResult.OK };
    private readonly GridControl _paymentsGrid = new() { Dock = DockStyle.Fill };
    private readonly GridView _paymentsGridView = new();
    private Dictionary<string, Guid?> _paymentMethodsByDisplay = [];

    /// <summary>Raised whenever a payment is recorded or voided, so the launching POS screen can refresh its own totals.</summary>
    public event EventHandler? PaymentsChanged;

    /// <summary>Builds the Payment Screen for the given order.</summary>
    public PaymentForm(IMediator mediator, Guid orderId)
    {
        _mediator = mediator;
        _orderId = orderId;

        Text = "Payment";
        Width = 640;
        Height = 520;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;

        _paymentsGrid.MainView = _paymentsGridView;
        _paymentsGrid.ViewCollection.Add(_paymentsGridView);
        _paymentsGridView.OptionsBehavior.Editable = false;
        _paymentsGridView.OptionsSelection.MultiSelect = false;
        _paymentsGridView.OptionsView.ShowGroupPanel = false;
        _paymentsGridView.Columns.AddVisible("MethodName", "Method");
        _paymentsGridView.Columns.AddVisible("Amount", "Amount");
        _paymentsGridView.Columns.AddVisible("Status", "Status");
        _paymentsGridView.Columns.AddVisible("CreatedAtUtc", "Recorded (UTC)");

        var summaryPanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 90, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(8) };
        summaryPanel.Controls.Add(_grandTotalLabel);
        summaryPanel.Controls.Add(_paidTotalLabel);
        summaryPanel.Controls.Add(_balanceLabel);

        var entryPanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(8, 0, 8, 0) };
        _paymentMethodCombo.Width = 160;
        _amountEdit.Width = 120;
        entryPanel.Controls.Add(new LabelControl { Text = "Method:", Padding = new Padding(0, 6, 4, 0) });
        entryPanel.Controls.Add(_paymentMethodCombo);
        entryPanel.Controls.Add(new LabelControl { Text = "Amount:", Padding = new Padding(12, 6, 4, 0) });
        entryPanel.Controls.Add(_amountEdit);
        entryPanel.Controls.Add(_recordButton);

        var actionPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, FlowDirection = FlowDirection.RightToLeft };
        actionPanel.Controls.Add(_closeButton);
        actionPanel.Controls.Add(_receiptButton);
        actionPanel.Controls.Add(_voidButton);

        Controls.Add(_paymentsGrid);
        Controls.Add(entryPanel);
        Controls.Add(summaryPanel);
        Controls.Add(actionPanel);

        _recordButton.Click += async (_, _) => await RecordPaymentAsync();
        _voidButton.Click += async (_, _) => await VoidPaymentAsync();
        _receiptButton.Click += async (_, _) => await ShowReceiptAsync();

        Load += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var methods = await _mediator.Send(new ListPaymentMethodsQuery());
        var activeMethods = methods.Where(m => m.Status == "Active").Select(m => (m.PaymentMethodId, m.Name)).ToList();
        _paymentMethodsByDisplay = ComboBoxBinder.Bind(_paymentMethodCombo, activeMethods, includeEmpty: false);

        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        var totals = await _mediator.Send(new GetOrderSummaryQuery(_orderId));
        var payments = await _mediator.Send(new ListPaymentsByOrderQuery(_orderId));
        var methods = await _mediator.Send(new ListPaymentMethodsQuery());
        var methodNames = methods.ToDictionary(m => m.PaymentMethodId, m => m.Name);

        _grandTotalLabel.Text = $"Grand Total: {totals.GrandTotal:N2}";
        _paidTotalLabel.Text = $"Paid Total: {totals.PaidTotal:N2}";
        _balanceLabel.Text = $"Balance: {totals.Balance:N2}";
        _amountEdit.Value = Math.Max(totals.Balance, 0m);

        _paymentsGrid.DataSource = payments.Select(p => new PaymentRow(
            p.PaymentId,
            methodNames.GetValueOrDefault(p.PaymentMethodId, "(unknown)"),
            p.Amount,
            p.IsVoided ? "Voided" : "Recorded",
            p.CreatedAtUtc)).ToList();
    }

    private async Task RecordPaymentAsync()
    {
        if (ComboBoxBinder.GetSelectedId(_paymentMethodCombo, _paymentMethodsByDisplay) is not { } paymentMethodId)
        {
            XtraMessageBox.Show(this, "Select a payment method.", "No Payment Method Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_amountEdit.Value <= 0)
        {
            XtraMessageBox.Show(this, "Enter a positive amount.", "Invalid Amount", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        await _mediator.Send(new RecordPaymentCommand(_orderId, paymentMethodId, _amountEdit.Value));
        await RefreshAsync();
        PaymentsChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task VoidPaymentAsync()
    {
        if (_paymentsGridView.GetFocusedRow() is not PaymentRow row)
        {
            return;
        }

        await _mediator.Send(new VoidPaymentCommand(row.PaymentId));
        await RefreshAsync();
        PaymentsChanged?.Invoke(this, EventArgs.Empty);
    }

    private sealed record PaymentRow(Guid PaymentId, string MethodName, decimal Amount, string Status, DateTimeOffset CreatedAtUtc);

    private async Task ShowReceiptAsync()
    {
        var order = await _mediator.Send(new GetOrderByIdQuery(_orderId));
        var receipt = await ReceiptFormatter.FormatAsync(_mediator, order);

        using var preview = new ReceiptPreviewForm(receipt);
        preview.ShowDialog(this);
    }
}
