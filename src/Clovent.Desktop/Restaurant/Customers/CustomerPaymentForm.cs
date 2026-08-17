using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Restaurant.Application.Customers.Dtos;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Restaurant.Customers;

/// <summary>
/// Receive Customer Payment Dialog: collects payment details (amount, method, ref, notes)
/// and validates the values. Visual Studio Designer compatible.
/// </summary>
public sealed partial class CustomerPaymentForm : XtraForm
{
    private readonly CustomerDto _customer;

    /// <summary>Design-time-only constructor for Visual Studio Designer.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public CustomerPaymentForm()
    {
        _customer = null!;
        InitializeComponent();
        ScaleLayoutAtRuntime();
    }

    /// <summary>Builds the payment receipt dialog for a customer.</summary>
    /// <param name="customer">The customer the payment is being received from.</param>
    /// <param name="paymentMethodNames">
    /// The active payment methods, as configured by the owner and read from
    /// the same source the POS tender strip uses. Supplied by the caller
    /// rather than fetched here so this dialog keeps no data access of its
    /// own, and never falls back to a hardcoded list that would drift out of
    /// step with what is actually configured (defect D9).
    /// </param>
    public CustomerPaymentForm(CustomerDto customer, IReadOnlyList<string> paymentMethodNames)
    {
        ArgumentNullException.ThrowIfNull(paymentMethodNames);

        _customer = customer;
        InitializeComponent();
        ScaleLayoutAtRuntime();

        if (DesignModeHelper.IsInDesignMode)
            return;

        _txtCustomer.Text = $"{_customer.Name} ({_customer.Code})";
        _txtOutstanding.Text = CurrencyDisplay.Format(_customer.OutstandingBalance);

        // Prepopulate amount with outstanding balance (gated at min 0.01)
        _spinAmount.Value = Math.Max(0.01m, _customer.OutstandingBalance);

        _comboPaymentMethod.Properties.Items.Clear();
        foreach (var name in paymentMethodNames)
        {
            _comboPaymentMethod.Properties.Items.Add(name);
        }

        if (_comboPaymentMethod.Properties.Items.Count > 0)
        {
            _comboPaymentMethod.SelectedIndex = 0;
        }
    }

    /// <summary>The validated payment amount.</summary>
    public decimal Amount => _spinAmount.Value;

    /// <summary>Selected payment method name.</summary>
    public string PaymentMethod => _comboPaymentMethod.Text;

    /// <summary>Entered payment reference code (optional).</summary>
    public string? Reference => string.IsNullOrWhiteSpace(_txtReference.Text) ? null : _txtReference.Text.Trim();

    /// <summary>Entered payment notes (optional).</summary>
    public string? Notes => string.IsNullOrWhiteSpace(_txtNotes.Text) ? null : _txtNotes.Text.Trim();

    private void AppearanceManager_Changed(object? sender, EventArgs e) =>
        AppearanceManager.Apply(this, "Restaurant", nameof(CustomerPaymentForm));

    private void CustomerPaymentForm_Load(object? sender, EventArgs e)
    {
        if (DesignModeHelper.IsInDesignMode)
            return;

        AppearanceManager.Apply(this, "Restaurant", nameof(CustomerPaymentForm));
    }

    private void BtnSubmit_Click(object? sender, EventArgs e)
    {
        if (_spinAmount.Value <= 0)
        {
            XtraMessageBox.Show(this, "Payment amount must be greater than zero.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_comboPaymentMethod.Text))
        {
            XtraMessageBox.Show(this, "Payment method is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private void ScaleLayoutAtRuntime()
    {
        if (DesignModeHelper.IsInDesignMode) return;

        Size = LogicalToDeviceUnits(new Size(460, 390));
        MinimumSize = LogicalToDeviceUnits(new Size(460, 390));

        root.RowStyles[1] = new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(50));

        fieldTable.ColumnStyles[0] = new ColumnStyle(SizeType.Absolute, LogicalToDeviceUnits(130));
        fieldTable.RowStyles[0] = new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(36));
        fieldTable.RowStyles[1] = new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(36));
        fieldTable.RowStyles[2] = new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(36));
        fieldTable.RowStyles[3] = new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(36));
        fieldTable.RowStyles[4] = new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(36));
        fieldTable.RowStyles[5] = new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(80));

        _btnCancel.MinimumSize = LogicalToDeviceUnits(new Size(95, 34));
        _btnSubmit.MinimumSize = LogicalToDeviceUnits(new Size(130, 34));
    }
}
