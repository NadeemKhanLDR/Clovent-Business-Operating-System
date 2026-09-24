using Clovent.Desktop.Forms.Base;
using DevExpress.LookAndFeel;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>
/// Dialog for dividing an order's balance across several payment methods in
/// one action - "Rs.600 cash and the rest on card" - without leaving the POS
/// payment panel. The method rows are built dynamically from whatever active
/// payment methods the POS passes in (the same <c>ListPaymentMethodsQuery</c>
/// source the POS payment buttons use), so Back Office additions/removals
/// appear here without any code change; the list scrolls when there are many
/// methods. Pure allocation UI: the resulting amounts are recorded by
/// <c>RestaurantPosForm</c> through the same <c>RecordPaymentCommand</c> the
/// Record Payment button uses, one per method, so payment validation, audit,
/// and history are shared with the single-method flow. Currency convention:
/// the Balance/Remaining/Change summaries use the configured symbol (like the
/// POS's Balance Due and Change), the individual amount fields stay plain.
/// </summary>
public sealed class SplitPaymentDialog : XtraForm
{
    private readonly decimal _balance;
    private readonly List<(Guid PaymentMethodId, string Name, TextEdit AmountEdit)> _rows = [];
    private LabelControl _remainingLabel = null!;

    /// <summary>The allocations accepted by the cashier (only positive amounts, in dialog order).</summary>
    public IReadOnlyList<(Guid PaymentMethodId, string Name, decimal Amount)> Allocations { get; private set; } = [];

    /// <summary>Builds the split dialog for <paramref name="balance"/> across the currently active <paramref name="methods"/>.</summary>
    public SplitPaymentDialog(decimal balance, IReadOnlyList<(Guid PaymentMethodId, string Name)> methods)
    {
        _balance = balance;

        Text = "Split Payment";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        LookAndFeel.UseDefaultLookAndFeel = false;
        LookAndFeel.Style = LookAndFeelStyle.Flat;

        // Top: balance summary. Middle: scrollable method list. Then the
        // live remaining summary, then the buttons. Explicit dialog size with
        // a floor so the dialog can never collapse (an AutoSize form sized
        // from not-yet-laid-out docked controls once rendered tiny).
        var header = new LabelControl
        {
            Text = $"Balance to split: {CurrencyDisplay.FormatPlain(balance)}",
            Appearance = { Font = new Font("Segoe UI", 10F, FontStyle.Bold) },
            AutoSizeMode = LabelAutoSizeMode.Horizontal,
            Dock = DockStyle.Fill,
            Margin = new Padding(12, 12, 12, 4),
        };

        _remainingLabel = new LabelControl
        {
            Text = RemainingText(),
            Appearance = { Font = new Font("Segoe UI", 10F, FontStyle.Bold) },
            AutoSizeMode = LabelAutoSizeMode.Horizontal,
            Dock = DockStyle.Fill,
            Margin = new Padding(12, 4, 12, 4),
        };

        var rows = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(12, 0, 12, 0),
        };
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        foreach (var (methodId, name) in methods)
        {
            var edit = new TextEdit
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(2),
                MinimumSize = new Size(160, 0),
                Properties =
                {
                    Mask = { MaskType = DevExpress.XtraEditors.Mask.MaskType.Numeric, EditMask = "F" + CurrencyDisplay.DecimalPlaces, UseMaskAsDisplayFormat = true },
                },
            };
            edit.Properties.EditValueChanged += (_, _) => _remainingLabel.Text = RemainingText();
            _rows.Add((methodId, name, edit));
            rows.Controls.Add(new LabelControl { Text = name, AutoSizeMode = LabelAutoSizeMode.Horizontal, Dock = DockStyle.Fill, Margin = new Padding(2, 6, 8, 6) }, 0, _rows.Count - 1);
            rows.Controls.Add(edit, 1, _rows.Count - 1);
        }

        // Only this middle section scrolls when there are many methods - the
        // dialog itself stays a normal, readable size.
        var listHost = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(0, 4, 0, 4),
        };
        listHost.Controls.Add(rows);

        var okButton = new SimpleButton
        {
            Text = "Record Split Payment",
            Dock = DockStyle.Fill,
            AutoSize = true,
            MinimumSize = new Size(190, 40),
            Margin = new Padding(12, 2, 2, 12),
        };
        okButton.Click += (_, _) =>
        {
            if (ValidateAllocations())
            {
                Allocations = [.. _rows.Select(r => (r.PaymentMethodId, r.Name, Amount: ParseAmount(r.AmountEdit))).Where(a => a.Amount > 0)];
                DialogResult = DialogResult.OK;
            }
        };
        var cancelButton = new SimpleButton
        {
            Text = "Cancel",
            Dock = DockStyle.Fill,
            AutoSize = true,
            MinimumSize = new Size(110, 40),
            Margin = new Padding(2, 2, 12, 12),
            DialogResult = DialogResult.Cancel,
        };
        var buttons = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
        };
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
        buttons.Controls.Add(okButton, 0, 0);
        buttons.Controls.Add(cancelButton, 1, 0);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(0) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(header, 0, 0);
        root.Controls.Add(listHost, 0, 1);
        root.Controls.Add(_remainingLabel, 0, 2);
        root.Controls.Add(buttons, 0, 3);

        Controls.Add(root);
        AcceptButton = okButton;
        CancelButton = cancelButton;

        DesktopDialogSizing.Apply(this, 540, 460, 480, 360, null, true);
        PerformLayout();
    }

    private static decimal ParseAmount(TextEdit edit) =>
        decimal.TryParse(Convert.ToString(edit.EditValue), out var amount) ? amount : 0m;

    private decimal AllocatedSoFar() => _rows.Sum(r => ParseAmount(r.AmountEdit));

    private string RemainingText()
    {
        var remaining = _balance - AllocatedSoFar();
        return remaining >= 0
            ? $"Remaining: {CurrencyDisplay.FormatPlain(remaining)}"
            : $"Change: {CurrencyDisplay.FormatPlain(-remaining)}";
    }

    /// <summary>
    /// Applies the same rules the single-method Record Payment path enforces:
    /// amounts must be non-negative, the balance must be fully allocated, and
    /// non-cash methods can never exceed what is still unpaid at their point
    /// in the sequence - only a Cash line (naturally last) may exceed the
    /// balance, the excess being handed back as change. Cash is matched by
    /// display name here only because the payment-method domain offers no
    /// stable cash identity - see the class doc comment and the reported
    /// architectural limitation.
    /// </summary>
    private bool ValidateAllocations()
    {
        var allocated = AllocatedSoFar();
        if (allocated <= 0)
        {
            XtraMessageBox.Show(this, "Enter at least one payment amount.", "Split Payment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (_rows.Any(r => ParseAmount(r.AmountEdit) < 0))
        {
            XtraMessageBox.Show(this, "Payment amounts cannot be negative.", "Split Payment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (allocated < _balance - 0.005m)
        {
            XtraMessageBox.Show(
                this,
                $"The split must cover the full balance. {CurrencyDisplay.FormatPlain(_balance - allocated)} is still unallocated.",
                "Balance Not Fully Allocated",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }

        var remaining = _balance;
        var entries = _rows.Select(r => (r.Name, Amount: ParseAmount(r.AmountEdit))).Where(a => a.Amount > 0).ToList();
        for (var i = 0; i < entries.Count; i++)
        {
            var (name, amount) = entries[i];
            var isCash = string.Equals(name, "Cash", StringComparison.OrdinalIgnoreCase);
            var isLast = i == entries.Count - 1;

            if ((!isCash || !isLast) && amount > remaining + 0.005m)
            {
                XtraMessageBox.Show(
                    this,
                    $"{name} cannot take more than the remaining balance ({CurrencyDisplay.FormatPlain(Math.Max(remaining, 0m))}).\n\nOnly the final Cash amount may exceed the balance - the excess is handed back as change.",
                    "Amount Exceeds Balance",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            remaining -= Math.Min(amount, Math.Max(remaining, 0m));
        }

        return true;
    }
}
