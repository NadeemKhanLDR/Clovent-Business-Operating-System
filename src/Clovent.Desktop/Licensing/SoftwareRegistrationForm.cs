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
/// </summary>
public partial class SoftwareRegistrationForm : XtraForm
{
    private SimpleButton? _btnElevate;

    public SoftwareRegistrationForm()
    {
        InitializeComponent();

        if (DesignMode || System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime)
        {
            return;
        }

        Load += SoftwareRegistrationForm_Load;
        btnCopyMachineId.Click += BtnCopyMachineId_Click;
        btnImport.Click += BtnImport_Click;
    }

    private void SoftwareRegistrationForm_Load(object? sender, EventArgs e)
    {
        txtMachineId.Text = MachineFingerprint.GetCurrentMachineId();

        var entryAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var version = entryAssembly.GetName().Version?.ToString(3) ?? "1.0.0";
        lblVersionVal.Text = version;

        PopulateLicenseDetails();

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
            Location = new Point(174, 12),
            Size = new Size(130, 28),
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };
        _btnElevate.Click += (s, e) =>
        {
            WindowsCommissioningSecurity.EnsureElevatedForCommissioning("Software License Activation");
        };
        panelBottom.Controls.Add(_btnElevate);
        _btnElevate.BringToFront();
    }

    private void PopulateLicenseDetails()
    {
        var result = LicenseService.CurrentResult;
        var lic = result.License;

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
                lblStatus.Text = "Status: Unlicensed / Evaluation";
                lblStatus.Appearance.ForeColor = Color.DarkOrange;
                break;
        }

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
                : "All Standard Modules (POS, Back Office, Inventory)";
        }
        else
        {
            lblLicensedToVal.Text = "Unregistered Organization";
            lblLicenseIdVal.Text = "None";
            lblExpiryVal.Text = "N/A";
            lblTerminalsVal.Text = "Standard Terminal";
            lblModulesVal.Text = "Evaluation Mode";
        }
    }

    private void BtnCopyMachineId_Click(object? sender, EventArgs e)
    {
        try
        {
            Clipboard.SetText(txtMachineId.Text);
            XtraMessageBox.Show(
                this,
                "Hardware ID copied to clipboard.\nProvide this ID to Clovent support for machine-locked license provisioning.",
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
