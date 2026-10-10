using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Authorization;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Restaurant.Shared;

/// <summary>
/// Structured context passed when authorizing a credit limit override on account.
/// </summary>
public sealed record CreditLimitOverrideContext(
    string CustomerName,
    decimal CurrentOutstanding,
    decimal CreditLimit,
    decimal SaleAmount)
{
    /// <summary>The resulting balance if this sale is approved.</summary>
    public decimal BalanceAfterSale => CurrentOutstanding + SaleAmount;

    /// <summary>Amount by which the credit limit is exceeded.</summary>
    public decimal ExceededBy => Math.Max(0m, BalanceAfterSale - CreditLimit);
}

/// <summary>
/// Clean, accessible dialog for challenging and verifying manager authorization
/// before a privileged action (such as credit limit override or order void).
/// </summary>
public sealed partial class ManagerAuthorizationForm : XtraForm
{
    private readonly IManagerAuthorizationService? _authService;
    private readonly string _featureCode = "pos.exceedcreditlimit";

    /// <summary>Designer/reflection-only parameterless constructor.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public ManagerAuthorizationForm()
    {
        InitializeComponent();
        WireEvents();
        ScaleLayoutAtRuntime();
    }

    /// <summary>Constructs a manager authorization challenge dialog for generic operations (e.g. pos.void).</summary>
    public ManagerAuthorizationForm(
        string title,
        string detail,
        IManagerAuthorizationService? authService = null,
        string featureCode = "pos.exceedcreditlimit",
        string? currentUserName = null)
    {
        InitializeComponent();
        _authService = authService;
        _featureCode = string.IsNullOrWhiteSpace(featureCode) ? "pos.exceedcreditlimit" : featureCode;

        Text = title;
        _headerLabel.Text = title;
        _lblSubtitle.Text = "Privileged action requires manager approval.";
        _lblNotice.Visible = false;
        _financialsGrid.Visible = false;
        _detailLabel.Text = detail;
        _detailLabel.Visible = true;
        _lblInstruction.Text = "Manager authorization is required to continue.";

        if (!string.IsNullOrWhiteSpace(currentUserName))
        {
            _userNameEdit.Text = currentUserName;
        }

        WireEvents();
        ScaleLayoutAtRuntime();
    }

    /// <summary>Constructs a manager authorization challenge dialog with full credit limit context.</summary>
    public ManagerAuthorizationForm(
        string title,
        CreditLimitOverrideContext context,
        IManagerAuthorizationService? authService = null,
        string featureCode = "pos.exceedcreditlimit",
        string? currentUserName = null)
    {
        InitializeComponent();
        _authService = authService;
        _featureCode = string.IsNullOrWhiteSpace(featureCode) ? "pos.exceedcreditlimit" : featureCode;
        Context = context;

        Text = title;
        _headerLabel.Text = "Manager Authorization";
        _lblSubtitle.Text = "Credit Limit Override";
        _lblNotice.Text = "Credit limit exceeded.";
        _lblNotice.Visible = true;
        _financialsGrid.Visible = true;
        _detailLabel.Visible = false;

        _lblCustomerVal.Text = context.CustomerName;
        _lblOutstandingVal.Text = CurrencyDisplay.FormatPlain(context.CurrentOutstanding);
        _lblLimitVal.Text = CurrencyDisplay.FormatPlain(context.CreditLimit);
        _lblSaleVal.Text = CurrencyDisplay.FormatPlain(context.SaleAmount);
        _lblNewBalanceVal.Text = CurrencyDisplay.FormatPlain(context.BalanceAfterSale);

        _lblInstruction.Text = "Manager authorization is required to approve this credit sale.";

        if (!string.IsNullOrWhiteSpace(currentUserName))
        {
            _userNameEdit.Text = currentUserName;
        }

        WireEvents();
        ScaleLayoutAtRuntime();
    }

    /// <summary>Factory method to create a credit-limit override authorization dialog.</summary>
    public static ManagerAuthorizationForm ForCreditLimit(
        string customerName,
        decimal currentOutstanding,
        decimal creditLimit,
        decimal saleAmount,
        IManagerAuthorizationService? authService = null,
        string featureCode = "pos.exceedcreditlimit",
        string? currentUserName = null)
    {
        var context = new CreditLimitOverrideContext(customerName, currentOutstanding, creditLimit, saleAmount);
        return new ManagerAuthorizationForm(
            "Manager Authorization - Credit Limit Override",
            context,
            authService,
            featureCode,
            currentUserName);
    }

    private bool _inlineErrorVisible;

    /// <summary>Credit limit context if created for a credit limit override.</summary>
    public CreditLimitOverrideContext? Context { get; }

    /// <summary>The submitted manager username, trimmed.</summary>
    public string ManagerUserName => _userNameEdit.Text.Trim();

    /// <summary>The submitted manager password, exactly as typed.</summary>
    public string ManagerPassword => _passwordEdit.Text;

    /// <summary>The resulting authorization outcome, populated upon successful authorization.</summary>
    public ManagerAuthorizationResult? AuthorizationResult { get; private set; }

    /// <summary>The current inline error message displayed on the dialog, or null if hidden.</summary>
    public string? CurrentInlineError => _inlineErrorVisible ? _lblErrorMessage.Text : null;

    /// <summary>Whether an inline error is currently visible.</summary>
    public bool InlineErrorVisible => _inlineErrorVisible;

    private void WireEvents()
    {
        _btnAuthorize.Click += async (s, e) => await PerformAuthorizeAsync();
        _userNameEdit.TextChanged += (s, e) => ClearInlineError();
        _passwordEdit.TextChanged += (s, e) => ClearInlineError();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        ScaleLayoutAtRuntime();
    }

    /// <summary>Scales layout using the form's current device DPI.</summary>
    public void ScaleLayoutAtRuntime()
    {
        int dpi = DeviceDpi > 0 ? DeviceDpi : 96;
        ScaleLayoutForDpi(dpi);
    }

    /// <summary>Scales layout using an explicit target DPI (useful for multi-DPI runtime testing).</summary>
    public void ScaleLayoutForDpi(int dpi)
    {
        if (DesignModeHelper.IsInDesignMode) return;
        DesktopDialogSizing.Apply(this, 540, 450, 480, 380, Owner ?? Parent, false);

        int credLabelColW = DesktopDpi.Scale(150, dpi);
        if (_credentialsPanel.ColumnStyles.Count > 0)
        {
            _credentialsPanel.ColumnStyles[0] = new ColumnStyle(SizeType.Absolute, credLabelColW);
        }

        int credRowH = DesktopDpi.Scale(36, dpi);
        int editMinH = DesktopDpi.Scale(28, dpi);
        _userNameEdit.MinimumSize = new Size(0, editMinH);
        _passwordEdit.MinimumSize = new Size(0, editMinH);
        if (_credentialsPanel.RowStyles.Count >= 2)
        {
            _credentialsPanel.RowStyles[0] = new RowStyle(SizeType.Absolute, credRowH);
            _credentialsPanel.RowStyles[1] = new RowStyle(SizeType.Absolute, credRowH);
        }

        int gridLabelColW = DesktopDpi.Scale(170, dpi);
        if (_financialsGrid.ColumnStyles.Count > 0)
        {
            _financialsGrid.ColumnStyles[0] = new ColumnStyle(SizeType.Absolute, gridLabelColW);
        }

        int finRowH = DesktopDpi.Scale(26, dpi);
        for (int i = 0; i < _financialsGrid.RowStyles.Count; i++)
        {
            _financialsGrid.RowStyles[i] = new RowStyle(SizeType.Absolute, finRowH);
        }

        _btnAuthorize.Size = new Size(DesktopDpi.Scale(110, dpi), DesktopDpi.Scale(36, dpi));
        _btnCancel.Size = new Size(DesktopDpi.Scale(90, dpi), DesktopDpi.Scale(36, dpi));
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        if (!string.IsNullOrWhiteSpace(_userNameEdit.Text))
        {
            _passwordEdit.Focus();
        }
        else
        {
            _userNameEdit.Focus();
        }
    }

    /// <summary>
    /// Executes the authorization verification synchronously or asynchronously.
    /// Can be invoked directly from automated tests.
    /// </summary>
    public async Task<bool> PerformAuthorizeAsync()
    {
        ClearInlineError();

        var username = _userNameEdit.Text.Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            ShowInlineError("Enter the manager username.");
            _userNameEdit.Focus();
            return false;
        }

        var password = _passwordEdit.Text;
        if (string.IsNullOrEmpty(password))
        {
            ShowInlineError("Enter the manager password.");
            _passwordEdit.Focus();
            return false;
        }

        if (_authService != null)
        {
            SetBusy(true);
            try
            {
                var result = await _authService.AuthorizeAsync(username, password, _featureCode);
                if (!result.Succeeded)
                {
                    _passwordEdit.Text = string.Empty;
                    ShowInlineError(result.ErrorMessage ?? "Manager username or password is incorrect.");
                    _passwordEdit.Focus();
                    return false;
                }

                AuthorizationResult = result;
                DialogResult = DialogResult.OK;
                Close();
                return true;
            }
            catch (Exception ex)
            {
                _passwordEdit.Text = string.Empty;
                ShowInlineError("Authorization error: " + ex.Message);
                _passwordEdit.Focus();
                return false;
            }
            finally
            {
                SetBusy(false);
            }
        }

        // Fail closed when no authorization service is available
        _passwordEdit.Text = string.Empty;
        ShowInlineError("Manager authorization service is unavailable. Authorization denied.");
        _passwordEdit.Focus();
        return false;
    }

    /// <summary>Displays an inline error on the dialog without opening secondary modals.</summary>
    public void ShowInlineError(string message)
    {
        _inlineErrorVisible = true;
        _lblErrorMessage.Text = message;
        _lblErrorMessage.Visible = true;
    }

    /// <summary>Hides the inline error display.</summary>
    public void ClearInlineError()
    {
        _inlineErrorVisible = false;
        _lblErrorMessage.Text = string.Empty;
        _lblErrorMessage.Visible = false;
    }

    private void SetBusy(bool busy)
    {
        _btnAuthorize.Enabled = !busy;
        _btnCancel.Enabled = !busy;
        _userNameEdit.Enabled = !busy;
        _passwordEdit.Enabled = !busy;
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }
}
