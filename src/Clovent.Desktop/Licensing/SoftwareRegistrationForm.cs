using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Clovent.Desktop.Authorization;
using Clovent.Desktop.Commissioning.Security;
using Clovent.Desktop.Forms.Base;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Licensing;

/// <summary>
/// Registration and licensing status form displaying license validity, expiration,
/// allowed modules, and hardware fingerprint binding.
/// Fully DPI-aware and responsive across 100% to 250% scaling.
/// </summary>
public partial class SoftwareRegistrationForm : XtraForm
{
    private SimpleButton? _btnElevate;

    /// <summary>Parameterless constructor for designer and runtime initialization.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public SoftwareRegistrationForm()
    {
        InitializeComponent();

        if (DesignMode || System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime || DesignModeHelper.IsInDesignMode)
        {
            return;
        }

        DesktopDialogSizing.Apply(this, 680, 530, 560, 460, null, true);

        Load += SoftwareRegistrationForm_Load;
        Resize += (s, e) => ApplyResponsiveLayout();
        btnCopyMachineId.Click += BtnCopyMachineId_Click;
        btnImport.Click += BtnImport_Click;
    }

    private void SoftwareRegistrationForm_Load(object? sender, EventArgs e)
    {
        txtMachineId.Text = MachineFingerprint.GetCurrentMachineId();

        var entryAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var version = entryAssembly.GetName().Version?.ToString(3) ?? "1.1.1";
        lblVersionVal.Text = version;

        PopulateLicenseDetails();
        ApplyResponsiveLayout();

        if (!AdministrativePrivilegeChecker.IsSessionActive())
        {
            // Pre-login scenario: require Windows Administrator elevation
            if (!WindowsCommissioningSecurity.IsRunningAsAdministrator())
            {
                btnImport.Enabled = false;
                lblStatus.Text = "Elevation Required: Windows Administrator rights required to modify system configuration before login.";
                lblStatus.Appearance.ForeColor = Color.Firebrick;
                ShowElevateOption();
            }
        }
        else if (!AdministrativePrivilegeChecker.HasAdministrativePrivileges())
        {
            // Post-login scenario: require administrative privileges
            btnImport.Enabled = false;
        }
    }

    private void ShowElevateOption()
    {
        if (_btnElevate != null) return;

        _btnElevate = new SimpleButton
        {
            Text = "Elevate (UAC)...",
            Height = DesktopDpi.Scale(32, this),
            Width = DesktopDpi.Scale(130, this),
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };
        _btnElevate.Appearance.Font = DesktopStyle.ButtonFont;
        _btnElevate.Click += (s, e) =>
        {
            WindowsCommissioningSecurity.EnsureElevatedForCommissioning("Software License Activation");
        };
        panelBottom.Controls.Add(_btnElevate);
        _btnElevate.BringToFront();
        ApplyBottomPanelLayout();
    }

    private void PopulateLicenseDetails()
    {
        var result = LicenseService.CurrentResult;
        var lic = result.License;

        if (result.IsEvaluation)
        {
            if (result.Status == LicenseStatus.Valid)
            {
                lblStatus.Text = $"Status: Evaluation Mode ({result.DaysRemaining} days remaining)";
                lblStatus.Appearance.ForeColor = Color.ForestGreen;
            }
            else
            {
                lblStatus.Text = "Status: Evaluation Period Expired (Commercial License Required)";
                lblStatus.Appearance.ForeColor = Color.Firebrick;
            }

            lblProductVal.Text = LicenseService.ExpectedProductName;
            lblLicenseTypeVal.Text = "Trial (30-Day Evaluation)";
            lblLicensedToVal.Text = "Evaluation Workstation";
            lblLicenseIdVal.Text = "Evaluation Mode";
            lblExpiryVal.Text = lic != null
                ? $"{BusinessDateFormatter.Format(lic.ExpiryDate)} ({result.DaysRemaining} days remaining)"
                : $"{result.DaysRemaining} days remaining";
            lblTerminalsVal.Text = "All Terminals (Evaluation)";
            lblModulesVal.Text = "POS, Back Office, Inventory, Catalog, Reporting, Restaurant";
            return;
        }

        switch (result.Status)
        {
            case LicenseStatus.Valid:
                lblStatus.Text = $"Status: Active & Valid ({lic?.LicenseType ?? "Commercial"})";
                lblStatus.Appearance.ForeColor = Color.ForestGreen;
                break;
            case LicenseStatus.GracePeriod:
                lblStatus.Text = $"Status: Grace Period Warning ({result.Message})";
                lblStatus.Appearance.ForeColor = Color.DarkOrange;
                break;
            case LicenseStatus.Expired:
                lblStatus.Text = "Status: Expired (Renewal Required)";
                lblStatus.Appearance.ForeColor = Color.Firebrick;
                break;
            case LicenseStatus.ClockTampered:
                lblStatus.Text = "Status: License Inactive (Clock Rollback Detected)";
                lblStatus.Appearance.ForeColor = Color.Firebrick;
                break;
            case LicenseStatus.MachineMismatch:
                lblStatus.Text = "Status: Machine Mismatch (Hardware Bound)";
                lblStatus.Appearance.ForeColor = Color.Firebrick;
                break;
            case LicenseStatus.InvalidSignature:
                lblStatus.Text = "Status: Corrupt or Invalid Signature";
                lblStatus.Appearance.ForeColor = Color.Firebrick;
                break;
            case LicenseStatus.Unlicensed:
            default:
                lblStatus.Text = "Status: Unlicensed / Registration Required";
                lblStatus.Appearance.ForeColor = Color.DarkOrange;
                break;
        }

        lblProductVal.Text = lic?.Product ?? LicenseService.ExpectedProductName;
        lblLicenseTypeVal.Text = lic?.LicenseType ?? "Commercial";

        if (lic != null)
        {
            lblLicensedToVal.Text = string.IsNullOrWhiteSpace(lic.CompanyName)
                ? lic.CustomerName
                : $"{lic.CustomerName} ({lic.CompanyName})";
            lblLicenseIdVal.Text = lic.LicenseId.ToString();

            var isPerpetual = string.Equals(lic.LicenseType, "Perpetual", StringComparison.OrdinalIgnoreCase);
            string expiryText;
            if (isPerpetual)
            {
                expiryText = lic.MaintenanceExpiry.HasValue
                    ? $"Perpetual License (Maintenance until {BusinessDateFormatter.Format(lic.MaintenanceExpiry.Value)})"
                    : "Perpetual License";
            }
            else
            {
                expiryText = $"{BusinessDateFormatter.Format(lic.ExpiryDate)} ({result.DaysRemaining} days remaining)";
            }
            lblExpiryVal.Text = expiryText;

            lblTerminalsVal.Text = lic.MaxTerminals <= 0
                ? "Unlimited Terminals"
                : $"{lic.MaxTerminals} Terminal(s)";

            lblModulesVal.Text = lic.AllowedModules != null && lic.AllowedModules.Count > 0
                ? string.Join(", ", lic.AllowedModules)
                : "All Standard Modules (POS, Back Office, Inventory, Catalog, Reporting)";
        }
        else
        {
            lblLicensedToVal.Text = "Unregistered Workstation";
            lblLicenseIdVal.Text = "None";
            lblExpiryVal.Text = "N/A";
            lblTerminalsVal.Text = "Standard Terminal";
            lblModulesVal.Text = "Standard Modules";
        }
    }

    private void ApplyResponsiveLayout()
    {
        if (IsDisposed) return;

        SuspendLayout();
        try
        {
            int padX = DesktopDpi.Scale(24, this);
            int curY = DesktopDpi.Scale(16, this);

            // Header
            lblHeader.Location = new Point(padX, curY);
            curY = lblHeader.Bottom + DesktopDpi.Scale(8, this);

            // Status banner
            int contentW = Math.Max(300, ClientSize.Width - (padX * 2));
            lblStatus.Location = new Point(padX, curY);
            lblStatus.Width = contentW;
            lblStatus.Height = DesktopDpi.Scale(36, this);
            curY = lblStatus.Bottom + DesktopDpi.Scale(12, this);

            // Metadata rows
            int labelColW = DesktopDpi.Scale(130, this);
            int valueColX = padX + labelColW + DesktopDpi.Scale(10, this);
            int valueColW = Math.Max(180, ClientSize.Width - valueColX - padX);
            int rowGap = DesktopDpi.Scale(8, this);

            (LabelControl title, LabelControl val)[] rows =
            [
                (lblProductTitle, lblProductVal),
                (lblVersionTitle, lblVersionVal),
                (lblLicensedToTitle, lblLicensedToVal),
                (lblLicenseTypeTitle, lblLicenseTypeVal),
                (lblLicenseIdTitle, lblLicenseIdVal),
                (lblExpiryTitle, lblExpiryVal),
                (lblTerminalsTitle, lblTerminalsVal),
                (lblModulesTitle, lblModulesVal)
            ];

            foreach (var (title, val) in rows)
            {
                title.Location = new Point(padX, curY);
                val.Location = new Point(valueColX, curY);
                val.Width = valueColW;

                if (val == lblModulesVal)
                {
                    val.AutoSizeMode = LabelAutoSizeMode.Vertical;
                    curY = Math.Max(title.Bottom, val.Bottom) + rowGap;
                }
                else
                {
                    curY = Math.Max(title.Bottom, val.Bottom) + rowGap;
                }
            }

            // Hardware ID section
            curY += DesktopDpi.Scale(4, this);
            lblMachineIdTitle.Location = new Point(padX, curY);

            int copyBtnW = DesktopDpi.Scale(130, this);
            int editH = DesktopDpi.Scale(24, this);
            int hwEditW = Math.Max(160, contentW - copyBtnW - DesktopDpi.Scale(8, this));

            txtMachineId.Location = new Point(padX, lblMachineIdTitle.Bottom + DesktopDpi.Scale(4, this));
            txtMachineId.Size = new Size(hwEditW, editH);

            btnCopyMachineId.Location = new Point(txtMachineId.Right + DesktopDpi.Scale(8, this), txtMachineId.Top);
            btnCopyMachineId.Size = new Size(copyBtnW, editH);

            // Bottom action panel
            ApplyBottomPanelLayout();
        }
        finally
        {
            ResumeLayout(true);
        }
    }

    private void ApplyBottomPanelLayout()
    {
        int panelH = DesktopDpi.Scale(54, this);
        panelBottom.Height = panelH;

        int padX = DesktopDpi.Scale(24, this);
        int btnH = DesktopDpi.Scale(32, this);
        int btnY = (panelBottom.ClientSize.Height - btnH) / 2;

        int importW = DesktopDpi.Scale(150, this);
        int closeW = DesktopDpi.Scale(100, this);

        btnImport.Location = new Point(padX, btnY);
        btnImport.Size = new Size(importW, btnH);

        if (_btnElevate != null)
        {
            int elevateW = DesktopDpi.Scale(130, this);
            _btnElevate.Location = new Point(btnImport.Right + DesktopDpi.Scale(10, this), btnY);
            _btnElevate.Size = new Size(elevateW, btnH);
        }

        btnClose.Size = new Size(closeW, btnH);
        btnClose.Location = new Point(panelBottom.ClientSize.Width - padX - closeW, btnY);
    }

    private void BtnCopyMachineId_Click(object? sender, EventArgs e)
    {
        try
        {
            Clipboard.SetText(txtMachineId.Text);
            XtraMessageBox.Show(
                this,
                "Hardware ID copied to clipboard.\nProvide this ID to Clovent Operations to receive your software license.",
                "Copied",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show(this, $"Failed to copy to clipboard: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnImport_Click(object? sender, EventArgs e)
    {
        // Enforce Requirement 13 & Commissioning Security: Verify administrative privileges before allowing license import
        if (!AdministrativePrivilegeChecker.IsSessionActive())
        {
            if (!WindowsCommissioningSecurity.IsRunningAsAdministrator())
            {
                XtraMessageBox.Show(
                    this,
                    "Elevation Required: Windows Administrator rights required to modify system configuration before login.",
                    "Elevation Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
        }
        else if (!AdministrativePrivilegeChecker.HasAdministrativePrivileges())
        {
            XtraMessageBox.Show(
                this,
                "Access Denied: Only users with administrative privileges are authorized to import or activate software licenses.",
                "Access Denied",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        using var openFileDialog = new OpenFileDialog
        {
            Title = "Select Clovent License File",
            Filter = "License Files (*.lic)|*.lic|All Files (*.*)|*.*",
            RestoreDirectory = true
        };

        if (openFileDialog.ShowDialog(this) == DialogResult.OK)
        {
            try
            {
                var result = LicenseService.ImportLicense(openFileDialog.FileName);
                PopulateLicenseDetails();
                ApplyResponsiveLayout();

                if (result.IsAuthorized)
                {
                    XtraMessageBox.Show(
                        this,
                        "License imported and verified successfully!",
                        "License Activated",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    XtraMessageBox.Show(
                        this,
                        $"License imported, but validation returned: {result.Message}",
                        "License Status",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(
                    this,
                    $"Failed to import license file:\n\n{ex.Message}",
                    "Import Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
