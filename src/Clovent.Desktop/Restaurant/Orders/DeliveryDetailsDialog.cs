using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Desktop.Restaurant.Customers;
using Clovent.Restaurant.Application.Customers.Commands;
using Clovent.Restaurant.Application.Customers.Dtos;
using Clovent.Restaurant.Application.Customers.Queries;
using Clovent.Restaurant.Orders;
using DevExpress.XtraEditors;
using MediatR;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>
/// Dialog for capturing or modifying delivery destination details, customer phone,
/// order source channel, delivery fee, rider assignment, and customer lookup/quick-creation.
/// </summary>
public sealed partial class DeliveryDetailsDialog : XtraForm
{
    private readonly IMediator? _mediator;
    private List<CustomerDto> _customers = [];

    /// <summary>Design-time constructor.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public DeliveryDetailsDialog()
    {
        InitializeComponent();
        ConfigureCustomerLookup();
        ScaleLayoutAtRuntime();
    }

    /// <summary>Builds delivery details dialog with optional pre-filled values.</summary>
    public DeliveryDetailsDialog(
        string? customerName = null,
        string? customerPhone = null,
        string? address = null,
        OrderSource source = OrderSource.Phone,
        decimal fee = 100.00m,
        string? riderName = null,
        string? notes = null,
        IMediator? mediator = null,
        string? riderPhone = null)
    {
        _mediator = mediator ?? (Program.Services?.GetService(typeof(IMediator)) as IMediator);
        InitializeComponent();
        ScaleLayoutAtRuntime();

        if (DesignModeHelper.IsInDesignMode) return;

        _txtName.Text = customerName ?? string.Empty;
        _txtPhone.Text = customerPhone ?? string.Empty;
        _txtAddress.Text = address ?? string.Empty;

        _comboSource.SelectedIndex = source switch
        {
            OrderSource.Online => 1,
            OrderSource.WalkIn => 2,
            _ => 0 // Phone
        };

        _spinDeliveryFee.Value = fee > 0 ? fee : 100.00m;

        if (string.IsNullOrWhiteSpace(riderPhone) && !string.IsNullOrWhiteSpace(riderName) && riderName.Contains('(') && riderName.EndsWith(')'))
        {
            int startIdx = riderName.LastIndexOf('(');
            _txtRider.Text = riderName[..startIdx].Trim();
            _txtRiderPhone.Text = riderName[(startIdx + 1)..^1].Trim();
        }
        else
        {
            _txtRider.Text = riderName ?? string.Empty;
            _txtRiderPhone.Text = riderPhone ?? string.Empty;
        }

        _txtNotes.Text = notes ?? string.Empty;

        ConfigureCustomerLookup();

        _btnConfirm.Click += BtnConfirm_Click;
        _btnNewCustomer.Click += async (s, e) => await QuickCreateCustomerAsync();
        _customerLookup.EditValueChanged += CustomerLookup_EditValueChanged;

        Load += async (s, e) => await LoadCustomersAsync();
    }

    private void ConfigureCustomerLookup()
    {
        _customerLookup.Properties.NullText = "Search existing customer...";
        _customerLookup.Properties.DisplayMember = "Name";
        _customerLookup.Properties.ValueMember = "CustomerId";
        _customerLookup.Properties.ShowClearButton = true;
        _customerLookup.Properties.ShowFooter = true;
        _customerLookup.Properties.PopupSizeable = true;
        _customerLookup.Properties.CloseUpKey = new DevExpress.Utils.KeyShortcut(Keys.Escape);

        // Logical baseline: 520px width, 250px height before DPI scaling
        int popupW = DesktopDpi.Scale(520, this);
        int popupH = DesktopDpi.Scale(250, this);
        int minW = DesktopDpi.Scale(420, this);
        int minH = DesktopDpi.Scale(180, this);

        _customerLookup.Properties.PopupFormSize = new Size(popupW, popupH);
        _customerLookup.Properties.PopupFormMinSize = new Size(minW, minH);

        if (_customerLookup.Properties.PopupView is DevExpress.XtraGrid.Views.Grid.GridView popupView)
        {
            popupView.Columns.Clear();

            // 1. Customer Name (~135)
            var colName = popupView.Columns.AddVisible("Name", "Customer Name");
            colName.Width = DesktopDpi.Scale(135, this);
            colName.MinWidth = DesktopDpi.Scale(105, this);
            colName.AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.NoWrap;
            colName.AppearanceCell.TextOptions.Trimming = DevExpress.Utils.Trimming.EllipsisCharacter;

            // 2. Mobile (~100)
            var colPhone = popupView.Columns.AddVisible("MobileNumber", "Mobile");
            colPhone.Width = DesktopDpi.Scale(100, this);
            colPhone.MinWidth = DesktopDpi.Scale(80, this);
            colPhone.AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.NoWrap;

            // 3. Address (remaining space ~200)
            var colAddress = popupView.Columns.AddVisible("Address", "Address");
            colAddress.Width = DesktopDpi.Scale(200, this);
            colAddress.MinWidth = DesktopDpi.Scale(135, this);
            colAddress.AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.NoWrap;
            colAddress.AppearanceCell.TextOptions.Trimming = DevExpress.Utils.Trimming.EllipsisCharacter;

            // 4. Balance (~85, right-aligned)
            var colBalance = popupView.Columns.AddVisible("BalanceDisplay", "Balance");
            colBalance.Width = DesktopDpi.Scale(85, this);
            colBalance.MinWidth = DesktopDpi.Scale(70, this);
            colBalance.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            colBalance.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            colBalance.AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.NoWrap;

            popupView.OptionsView.ShowGroupPanel = false;
            popupView.OptionsView.ShowIndicator = false;
            popupView.OptionsView.ColumnAutoWidth = true;
            popupView.OptionsView.RowAutoHeight = false;
            popupView.OptionsView.ShowHorizontalLines = DevExpress.Utils.DefaultBoolean.True;
            popupView.OptionsView.ShowVerticalLines = DevExpress.Utils.DefaultBoolean.False;
            popupView.RowHeight = DesktopDpi.Scale(26, this);

            popupView.FocusRectStyle = DevExpress.XtraGrid.Views.Grid.DrawFocusRectStyle.RowFocus;
            popupView.OptionsSelection.EnableAppearanceFocusedRow = true;
            popupView.OptionsSelection.EnableAppearanceFocusedCell = false;

            popupView.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    _customerLookup.ClosePopup();
                }
            };
        }

        _customerLookup.QueryPopUp += (s, e) =>
        {
            var screen = Screen.FromControl(this);
            var workingArea = screen.WorkingArea;
            int desiredW = DesktopDpi.Scale(520, this);
            int desiredH = DesktopDpi.Scale(250, this);
            var editorScreen = _customerLookup.RectangleToScreen(_customerLookup.ClientRectangle);
            var bounds = CalculateClampedPopupBounds(new Size(desiredW, desiredH), editorScreen, workingArea, DeviceDpi);
            _customerLookup.Properties.PopupFormSize = bounds.Size;
        };

        _customerLookup.Popup += (s, e) =>
        {
            if (s is DevExpress.Utils.Win.IPopupControl popupControl &&
                popupControl.PopupWindow is Form popupForm)
            {
                var screen = Screen.FromControl(this);
                var workingArea = screen.WorkingArea;
                int desiredW = DesktopDpi.Scale(520, this);
                int desiredH = DesktopDpi.Scale(250, this);
                var editorScreen = _customerLookup.RectangleToScreen(_customerLookup.ClientRectangle);
                var bounds = CalculateClampedPopupBounds(new Size(desiredW, desiredH), editorScreen, workingArea, DeviceDpi);
                popupForm.Bounds = bounds;
            }
        };
    }

    /// <summary>
    /// Calculates the clamped popup bounds for the customer search lookup to ensure it remains
    /// completely visible within the monitor's working area without clipping right or bottom edges.
    /// </summary>
    public static Rectangle CalculateClampedPopupBounds(
        Size desiredSize,
        Rectangle editorScreenBounds,
        Rectangle workingArea,
        int dpi)
    {
        int minW = DesktopDpi.Scale(400, dpi);
        int minH = DesktopDpi.Scale(180, dpi);

        int maxW = Math.Max(minW, workingArea.Width - 16);
        int maxH = Math.Max(minH, workingArea.Height - 16);

        int clampedWidth = Math.Clamp(desiredSize.Width, minW, maxW);
        int clampedHeight = Math.Clamp(desiredSize.Height, minH, maxH);

        // By default, align with the left edge of the editor control
        int x = editorScreenBounds.Left;

        // If extending beyond the right edge of the monitor working area, shift left
        if (x + clampedWidth > workingArea.Right - 8)
        {
            x = workingArea.Right - clampedWidth - 8;
        }

        // Clamp to left edge
        if (x < workingArea.Left + 8)
        {
            x = workingArea.Left + 8;
        }

        // Vertical positioning: default below editor
        int y = editorScreenBounds.Bottom + 1;
        int spaceBelow = workingArea.Bottom - editorScreenBounds.Bottom - 8;
        int spaceAbove = editorScreenBounds.Top - workingArea.Top - 8;

        if (clampedHeight > spaceBelow && spaceAbove > spaceBelow)
        {
            clampedHeight = Math.Min(clampedHeight, spaceAbove);
            y = editorScreenBounds.Top - clampedHeight - 1;
        }
        else
        {
            clampedHeight = Math.Min(clampedHeight, spaceBelow);
        }

        return new Rectangle(x, y, clampedWidth, clampedHeight);
    }

    private async Task LoadCustomersAsync()
    {
        var mediator = _mediator ?? (Program.Services?.GetService(typeof(IMediator)) as IMediator);
        if (mediator == null) return;
        try
        {
            var custs = await mediator.Send(new ListCustomersQuery());
            _customers = custs.Where(c => c.IsActive).ToList();

            var lookupItems = _customers.Select(c => new CustomerLookupItem(
                c.CustomerId,
                c.Name,
                c.MobileNumber,
                c.Address,
                CurrencyDisplay.FormatPlain(c.OutstandingBalance))).ToList();

            _customerLookup.Properties.DataSource = lookupItems;
        }
        catch { }
    }

    private void CustomerLookup_EditValueChanged(object? sender, EventArgs e)
    {
        if (_customerLookup.EditValue is Guid custId && custId != Guid.Empty)
        {
            var customer = _customers.FirstOrDefault(c => c.CustomerId == custId);
            if (customer != null)
            {
                _txtName.Text = customer.Name;
                _txtPhone.Text = customer.MobileNumber;
                _txtAddress.Text = customer.Address;
                SelectedCustomerId = customer.CustomerId;
            }
        }
    }

    private async Task QuickCreateCustomerAsync()
    {
        using var form = new CustomerEditForm("Quick New Customer");
        if (form.ShowDialog(this) == DialogResult.OK && _mediator != null)
        {
            try
            {
                var created = await _mediator.Send(new CreateCustomerCommand(
                    form.CodeValue,
                    form.NameValue,
                    form.MobileValue,
                    form.AddressValue,
                    form.EmailValue,
                    form.OpeningBalanceValue,
                    form.CreditLimitValue,
                    form.NotesValue,
                    form.ShopNoValue,
                    form.Mobile2Value,
                    form.PhoneValue,
                    form.IsDefaultValue,
                    form.IsCreditAllowedValue));

                await LoadCustomersAsync();
                _customerLookup.EditValue = created.CustomerId;
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(this, $"Failed to create customer: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void ScaleLayoutAtRuntime()
    {
        if (DesignModeHelper.IsInDesignMode) return;
        DesktopDialogSizing.Apply(this, 620, 455, 580, 420, this.Owner ?? this.Parent, false);

        int labelColW = DesktopDpi.Scale(150, this);
        formPanel.ColumnStyles[0] = new ColumnStyle(SizeType.Absolute, labelColW);

        _customerPickerPanel.ColumnStyles[1] = new ColumnStyle(SizeType.Absolute, DesktopDpi.Scale(75, this));

        int rowH = DesktopDpi.Scale(34, this);
        int addressH = DesktopDpi.Scale(62, this);
        int notesH = DesktopDpi.Scale(54, this);

        formPanel.RowStyles[0] = new RowStyle(SizeType.Absolute, rowH);
        formPanel.RowStyles[1] = new RowStyle(SizeType.Absolute, rowH);
        formPanel.RowStyles[2] = new RowStyle(SizeType.Absolute, rowH);
        formPanel.RowStyles[3] = new RowStyle(SizeType.Absolute, addressH);
        formPanel.RowStyles[4] = new RowStyle(SizeType.Absolute, rowH);
        formPanel.RowStyles[5] = new RowStyle(SizeType.Absolute, rowH);
        formPanel.RowStyles[6] = new RowStyle(SizeType.Absolute, rowH);
        formPanel.RowStyles[7] = new RowStyle(SizeType.Absolute, rowH);
        formPanel.RowStyles[8] = new RowStyle(SizeType.Absolute, notesH);

        while (formPanel.RowStyles.Count > 9)
        {
            formPanel.RowStyles.RemoveAt(formPanel.RowStyles.Count - 1);
        }

        _txtNotes.MinimumSize = new Size(0, DesktopDpi.Scale(50, this));

        int btnH = DesktopDpi.Scale(32, this);
        _btnConfirm.MinimumSize = new Size(DesktopDpi.Scale(135, this), btnH);
        _btnCancel.MinimumSize = new Size(DesktopDpi.Scale(85, this), btnH);
    }

    private void BtnConfirm_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtName.Text))
        {
            XtraMessageBox.Show(this, "Customer name is required for delivery orders.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtName.Focus();
            DialogResult = DialogResult.None;
            return;
        }

        if (string.IsNullOrWhiteSpace(_txtPhone.Text))
        {
            XtraMessageBox.Show(this, "Customer phone number is required for delivery orders.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtPhone.Focus();
            DialogResult = DialogResult.None;
            return;
        }

        if (string.IsNullOrWhiteSpace(_txtAddress.Text))
        {
            XtraMessageBox.Show(this, "Delivery address is required for delivery orders.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtAddress.Focus();
            DialogResult = DialogResult.None;
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    /// <summary>Selected customer ID if picked from lookup or created.</summary>
    public Guid? SelectedCustomerId { get; private set; }

    /// <summary>Captured customer name.</summary>
    public string CustomerName => _txtName.Text.Trim();

    /// <summary>Captured phone number.</summary>
    public string CustomerPhone => _txtPhone.Text.Trim();

    /// <summary>Captured delivery street address.</summary>
    public string DeliveryAddress => _txtAddress.Text.Trim();

    /// <summary>Order source channel (Phone, Online, Walk-In).</summary>
    public OrderSource OrderSource => _comboSource.SelectedIndex switch
    {
        1 => OrderSource.Online,
        2 => OrderSource.WalkIn,
        _ => OrderSource.Phone
    };

    /// <summary>Delivery service fee.</summary>
    public decimal DeliveryFee => _spinDeliveryFee.Value;

    /// <summary>Assigned rider or courier name.</summary>
    public string DeliveryRiderName => _txtRider.Text.Trim();

    /// <summary>Assigned rider or courier phone number.</summary>
    public string DeliveryRiderPhone => _txtRiderPhone.Text.Trim();

    /// <summary>Delivery notes or landmarks.</summary>
    public string DeliveryNotes => _txtNotes.Text.Trim();

    private sealed record CustomerLookupItem(Guid CustomerId, string Name, string MobileNumber, string Address, string BalanceDisplay);
}
