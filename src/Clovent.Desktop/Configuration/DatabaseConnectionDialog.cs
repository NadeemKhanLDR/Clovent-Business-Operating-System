using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.Authorization;
using Clovent.Desktop.Commissioning.Security;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Configuration;

/// <summary>
/// Dialog allowing administrators to configure database connection parameters
/// (Windows Authentication or SQL Server Authentication with DPAPI encrypted password).
/// </summary>
public partial class DatabaseConnectionDialog : XtraForm
{
    private DatabaseConnectionSettings _currentSettings;
    private SimpleButton? _btnElevate;

    public DatabaseConnectionSettings ResultSettings => _currentSettings;

    public DatabaseConnectionDialog()
    {
        InitializeComponent();

        _currentSettings = DatabaseSecretStore.Load() ?? new DatabaseConnectionSettings();

        if (DesignMode || System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime)
        {
            return;
        }

        cmbAuth.SelectedIndexChanged += CmbAuth_SelectedIndexChanged;
        btnTest.Click += BtnTest_Click;
        btnSave.Click += BtnSave_Click;
        Load += DatabaseConnectionDialog_Load;
    }

    private void DatabaseConnectionDialog_Load(object? sender, EventArgs e)
    {
        txtServer.Text = _currentSettings.Server;
        txtDatabase.Text = _currentSettings.Database;

        if (_currentSettings.UseWindowsAuthentication)
        {
            cmbAuth.SelectedIndex = 0;
            txtUsername.Text = string.Empty;
            txtPassword.Text = string.Empty;
        }
        else
        {
            cmbAuth.SelectedIndex = 1;
            txtUsername.Text = _currentSettings.UserId ?? string.Empty;
            txtPassword.Text = !string.IsNullOrEmpty(_currentSettings.EncryptedPassword)
                ? DatabaseSecretStore.Unprotect(_currentSettings.EncryptedPassword)
                : string.Empty;
        }

        UpdateAuthControlState();

        if (!AdministrativePrivilegeChecker.IsSessionActive())
        {
            // Pre-login scenario: require Windows Administrator elevation
            if (!WindowsCommissioningSecurity.IsRunningAsAdministrator())
            {
                btnSave.Enabled = false;
                txtServer.Properties.ReadOnly = true;
                txtDatabase.Properties.ReadOnly = true;
                cmbAuth.Properties.ReadOnly = true;
                txtUsername.Properties.ReadOnly = true;
                txtPassword.Properties.ReadOnly = true;

                lblStatus.Text = "Elevation Required: Windows Administrator rights required to modify system configuration before login.";
                lblStatus.ForeColor = Color.Firebrick;

                ShowElevateOption();
            }
        }
        else if (!AdministrativePrivilegeChecker.HasAdministrativePrivileges())
        {
            // Post-login scenario: require application administrator privileges
            btnSave.Enabled = false;
            txtServer.Properties.ReadOnly = true;
            txtDatabase.Properties.ReadOnly = true;
            cmbAuth.Properties.ReadOnly = true;
            txtUsername.Properties.ReadOnly = true;
            txtPassword.Properties.ReadOnly = true;

            lblStatus.Text = "Read-Only: Administrative privileges are required to modify database settings.";
            lblStatus.ForeColor = Color.Firebrick;
        }
    }

    private void ShowElevateOption()
    {
        if (_btnElevate != null) return;

        _btnElevate = new SimpleButton
        {
            Text = "Elevate (UAC)...",
            Size = btnSave.Size,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnElevate.Click += (s, e) =>
        {
            WindowsCommissioningSecurity.EnsureElevatedForCommissioning("Database Configuration");
        };

        int gap = Forms.Base.DesktopDpi.Scale(8, this);
        _btnElevate.Location = new Point(btnSave.Left - _btnElevate.Width - gap, btnSave.Top);
        panelBottom.Controls.Add(_btnElevate);
        _btnElevate.BringToFront();
    }

    private void CmbAuth_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdateAuthControlState();
    }

    private void UpdateAuthControlState()
    {
        var isSqlAuth = cmbAuth.SelectedIndex == 1;
        lblUsername.Enabled = isSqlAuth;
        txtUsername.Enabled = isSqlAuth;
        lblPassword.Enabled = isSqlAuth;
        txtPassword.Enabled = isSqlAuth;
    }

    private DatabaseConnectionSettings BuildSettingsFromUi()
    {
        var isWindowsAuth = cmbAuth.SelectedIndex == 0;
        return new DatabaseConnectionSettings
        {
            Server = string.IsNullOrWhiteSpace(txtServer.Text) ? DatabaseConnectionSettings.DefaultServer : txtServer.Text.Trim(),
            Database = string.IsNullOrWhiteSpace(txtDatabase.Text) ? DatabaseConnectionSettings.DefaultDatabaseName : txtDatabase.Text.Trim(),
            UseWindowsAuthentication = isWindowsAuth,
            UserId = isWindowsAuth ? null : txtUsername.Text.Trim(),
            PlainTextPassword = isWindowsAuth ? null : txtPassword.Text,
            TrustServerCertificate = true,
            ConnectionTimeout = 15
        };
    }

    private void BtnTest_Click(object? sender, EventArgs e)
    {
        var settings = BuildSettingsFromUi();
        var connStr = settings.BuildConnectionString();

        lblStatus.Text = "Testing connection...";
        lblStatus.ForeColor = Color.DarkBlue;
        Update();

        var success = DatabaseSecretStore.TestConnection(connStr, out var error);
        if (success)
        {
            lblStatus.Text = "Connection successful!";
            lblStatus.ForeColor = Color.ForestGreen;
            XtraMessageBox.Show(
                this,
                $"Successfully connected to database '{settings.Database}' on SQL Server '{settings.Server}'.",
                "Connection Successful",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        else
        {
            lblStatus.Text = "Connection failed!";
            lblStatus.ForeColor = Color.Firebrick;
            XtraMessageBox.Show(
                this,
                $"Failed to connect to SQL Server:\n\n{error}",
                "Connection Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        // Enforce Requirement 12 & Commissioning Security: Verify administrative privileges
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
                "Access Denied: Only administrators are authorized to modify database connection settings.",
                "Access Denied",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var settings = BuildSettingsFromUi();

        if (!settings.UseWindowsAuthentication && string.IsNullOrWhiteSpace(settings.UserId))
        {
            XtraMessageBox.Show(
                this,
                "Please specify a User ID for SQL Server Authentication.",
                "Validation Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            txtUsername.Focus();
            return;
        }

        try
        {
            DatabaseSecretStore.Save(settings);
            _currentSettings = settings;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show(
                this,
                $"Failed to save database settings: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
