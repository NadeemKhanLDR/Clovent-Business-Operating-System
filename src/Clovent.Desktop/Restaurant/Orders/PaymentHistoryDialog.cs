using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Restaurant.Application.ActivityLogs.Commands;
using Clovent.Restaurant.Application.Orders.Queries;
using Clovent.Restaurant.Application.PaymentMethods.Queries;
using Clovent.Restaurant.Application.Payments.Commands;
using Clovent.Restaurant.Application.Payments.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>
/// Small popup listing every payment recorded against one order, with Void
/// Payment and Receipt Preview - the secondary, infrequent actions split out
/// of the payment tender strip so that
/// strip can stay a short, fixed-height row instead of growing to fit a
/// grid. Reuses the launching <c>RestaurantPosForm</c>'s own <see cref="IMediator"/>
/// (already serialized through its <c>ScreenOperationGate</c>) rather than
/// opening its own DI scope - this dialog performs its own async MediatR
/// calls while modal, and the launching screen's own background work (the
/// menu-items change notifier) keeps running underneath it, but both funnel
/// through the same serialization gate, so no separate scope is needed here.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class PaymentHistoryDialog : XtraForm
{
    private readonly IMediator _mediator;
    private readonly ILogger _logger;
    private readonly Guid _orderId;
    private readonly string _performedBy;

    /// <summary>Raised whenever a payment is voided, so the launching POS screen can refresh its own totals.</summary>
    public event EventHandler? PaymentsChanged;

    /// <summary>Design-time-only constructor for the Visual Studio WinForms Designer - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public PaymentHistoryDialog()
    {
        _mediator = null!;
        _logger = null!;
        _performedBy = null!;

        InitializeComponent();
    }

    /// <summary>
    /// Builds the dialog for the order identified by <paramref name="orderId"/>.
    /// Reuses the launching screen's own <paramref name="mediator"/> and
    /// <paramref name="logger"/> rather than opening a separate DI scope (see
    /// this class's own doc comment). <paramref name="performedBy"/> is
    /// recorded as the actor on any Void Payment issued from this dialog.
    /// </summary>
    public PaymentHistoryDialog(IMediator mediator, ILogger logger, Guid orderId, string performedBy) : base()
    {
        InitializeComponent();

        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
        {
            _mediator = null!;
            _logger = null!;
            _performedBy = null!;
            return;
        }

        _mediator = mediator;
        _logger = logger;
        _orderId = orderId;
        _performedBy = performedBy;
    }

    private void AppearanceManager_Changed(object? sender, EventArgs e) => AppearanceManager.Apply(this, "Restaurant", nameof(PaymentHistoryDialog));

    private async void PaymentHistoryDialog_Load(object? sender, EventArgs e)
    {
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;
        await GuardedAction.RunAsync(this, _logger, LoadAsync, "load the payment history");
    }
    private Task TryRunAsync(Func<Task> action, string actionDescription) =>
        GuardedAction.RunAsync(this, _logger, action, actionDescription, LoadAsync);

    private async Task LoadAsync()
    {
        AppearanceManager.Apply(this, "Restaurant", nameof(PaymentHistoryDialog));

        var payments = await _mediator.Send(new ListPaymentsByOrderQuery(_orderId));
        var methods = await _mediator.Send(new ListPaymentMethodsQuery());
        var methodNames = methods.ToDictionary(m => m.PaymentMethodId, m => m.Name);

        _paymentsGrid.DataSource = payments.Select(p => new PaymentRow(
            p.PaymentId,
            methodNames.GetValueOrDefault(p.PaymentMethodId, "(unknown)"),
            p.Amount,
            p.IsVoided ? "Voided" : "Recorded",
            p.CreatedAtUtc)).ToList();
    }

    private async Task VoidPaymentAsync()
    {
        if (_paymentsGridView.GetFocusedRow() is not PaymentRow row)
        {
            XtraMessageBox.Show(this, "Select a payment in the list first.", "No Payment Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (row.Status == "Voided")
        {
            XtraMessageBox.Show(this, "This payment has already been voided.", "Already Voided", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        await _mediator.Send(new VoidPaymentCommand(row.PaymentId));
        await LoadAsync();
        PaymentsChanged?.Invoke(this, EventArgs.Empty);
        await LogActivityAsync("Refund", $"Voided payment of {CurrencyDisplay.Format(row.Amount)} ({row.MethodName})");
    }

    private async Task ShowReceiptAsync()
    {
        var order = await _mediator.Send(new GetOrderByIdQuery(_orderId));
        var receipt = await ReceiptFormatter.FormatAsync(_mediator, order);

        using var preview = new ReceiptPreviewForm(receipt);
        preview.ShowDialog(this);
        await LogActivityAsync("Print", order.OrderNumber);
    }

    /// <summary>Records one Restaurant activity log entry - see <c>RestaurantPosView.LogActivityAsync</c>'s identical reasoning for why failures here are deliberately swallowed rather than surfaced.</summary>
    private async Task LogActivityAsync(string action, string? details = null)
    {
        try
        {
            await _mediator.Send(new RecordActivityCommand(action, details, _performedBy, Environment.MachineName));
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
        }
    }

    private sealed record PaymentRow(Guid PaymentId, string MethodName, decimal Amount, string Status, DateTimeOffset CreatedAtUtc);
}
