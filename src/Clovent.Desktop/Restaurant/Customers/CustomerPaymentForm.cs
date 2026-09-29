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
/// Receive Customer Payment Dialog: collects payment details (amount, method, ref, notes),
/// shows live calculations for A/R application vs advance created, and validates values.
/// Visual Studio Designer compatible.
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
    public CustomerPaymentForm(CustomerDto customer, IReadOnlyList<string> paymentMethodNames)
    {
        ArgumentNullException.ThrowIfNull(paymentMethodNames);

        _customer = customer;
        InitializeComponent();
        ScaleLayoutAtRuntime();

        if (DesignModeHelper.IsInDesignMode)
            return;

        _txtCustomer.Text = $"{_customer.Name} ({_customer.Code})";
        var recBal = _customer.ReceivableBalance > 0 ? _customer.ReceivableBalance : _customer.OutstandingBalance;
        _txtOutstanding.Text = CurrencyDisplay.FormatPlain(recBal);
        _txtAdvance.Text = CurrencyDisplay.FormatPlain(_customer.AdvanceBalance);

        _spinAmount.Value = recBal > 0 ? recBal : 0.00m;
        _spinAmount.Properties.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.Numeric;
        _spinAmount.Properties.Mask.EditMask = "F" + CurrencyDisplay.DecimalPlaces;
        _spinAmount.Properties.Mask.UseMaskAsDisplayFormat = true;
        _spinAmount.EditValueChanged += (s, e) => UpdateCalculations();

        _comboPaymentMethod.Properties.Items.Clear();
        foreach (var name in paymentMethodNames)
        {
            _comboPaymentMethod.Properties.Items.Add(name);
        }

        if (_comboPaymentMethod.Properties.Items.Count > 0)
        {
            _comboPaymentMethod.SelectedIndex = 0;
        }

        UpdateCalculations();
    }

    private void UpdateCalculations()
    {
        if (DesignModeHelper.IsInDesignMode || _customer == null) return;

        var amount = _spinAmount.Value;
        var rec = _customer.ReceivableBalance > 0 ? _customer.ReceivableBalance : Math.Max(0, _customer.OutstandingBalance);
        var applied = Math.Min(amount, rec);
        var newAdvance = Math.Max(0, amount - rec);

        _txtApplied.Text = CurrencyDisplay.FormatPlain(applied);
        _txtNewAdvance.Text = CurrencyDisplay.FormatPlain(newAdvance);
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

        Clovent.Desktop.Forms.Base.Localization.LocalizationHelper.LocalizeControl(this);
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

        DesktopDialogSizing.Apply(this, 540, 520, 500, 480, null, false);

        root.RowStyles[1] = new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(50));

        fieldTable.ColumnStyles[0] = new ColumnStyle(SizeType.Absolute, LogicalToDeviceUnits(150));
        fieldTable.RowStyles[0] = new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(34));
        fieldTable.RowStyles[1] = new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(34));
        fieldTable.RowStyles[2] = new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(34));
        fieldTable.RowStyles[3] = new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(34));
        fieldTable.RowStyles[4] = new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(34));
        fieldTable.RowStyles[5] = new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(34));
        fieldTable.RowStyles[6] = new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(34));
        fieldTable.RowStyles[7] = new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(34));

        _btnCancel.MinimumSize = LogicalToDeviceUnits(new Size(95, 34));
        _btnSubmit.MinimumSize = LogicalToDeviceUnits(new Size(140, 34));
    }
}
