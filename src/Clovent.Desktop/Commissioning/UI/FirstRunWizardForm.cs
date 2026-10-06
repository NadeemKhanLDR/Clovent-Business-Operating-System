using System.Drawing;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Clovent.Desktop.Commissioning.Database;
using Clovent.Desktop.Commissioning.Security;
using Clovent.Desktop.Commissioning.Services;
using Clovent.Desktop.Configuration;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Licensing;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Commissioning.UI;

/// <summary>
/// Multi-step First-Run Setup &amp; Commissioning Wizard.
/// Implements Requirement 1: Complete clean-machine commissioning workflow across 8 steps.
/// Visual Studio Designer safe with clean PerMonitorV2 High-DPI layout.
/// </summary>
public partial class FirstRunWizardForm : XtraForm
{
    private readonly IServiceProvider? _services;
    private readonly DatabaseProvisioningService _databaseProvisioning;
    private readonly ICommissioningProvisioningCoordinator _provisioningCoordinator;

    private int _currentStep = 1;
    private const int TotalSteps = 8;

    private bool _isConnectionTested;
    private bool _areMigrationsApplied;
    private DatabaseConnectionSettings _connectionSettings = new();

    /// <summary>
    /// Parameterless constructor required for Visual Studio Designer and runtime initialization.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public FirstRunWizardForm() : this(null, Program.Services)
    {
    }

    /// <summary>
    /// Constructs the wizard with explicit coordinator and service provider injection.
    /// </summary>
    public FirstRunWizardForm(ICommissioningProvisioningCoordinator? provisioningCoordinator, IServiceProvider? services = null)
    {
        _services = services;
        _databaseProvisioning = new DatabaseProvisioningService();
        _provisioningCoordinator = provisioningCoordinator ?? new CommissioningProvisioningCoordinator();

        InitializeComponent();

        if (DesignMode || System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime || DesignModeHelper.IsInDesignMode)
        {
            return;
        }

        InitializeRuntime();
    }

    private void InitializeRuntime()
    {
        // Navigation buttons
        btnBack.Click += BtnBack_Click;
        btnNext.Click += BtnNext_Click;
        btnCancel.Click += BtnCancel_Click;
        btnFinish.Click += BtnFinish_Click;

        // Step 2: Connection controls
        btnTestConnection.Click += BtnTestConnection_Click;
        cmbAuth.SelectedIndexChanged += CmbAuth_SelectedIndexChanged;
        txtServer.TextChanged += ConnectionField_Changed;
        txtDatabase.TextChanged += ConnectionField_Changed;
        txtUsername.TextChanged += ConnectionField_Changed;
        txtPassword.TextChanged += ConnectionField_Changed;

        // Step 3: Migration controls
        btnApplyMigrations.Click += BtnApplyMigrations_Click;

        // Step 4: Hierarchy controls
        txtOrgName.TextChanged += HierarchyField_Changed;
        txtCompanyName.TextChanged += HierarchyField_Changed;
        txtBranchName.TextChanged += HierarchyField_Changed;

        // Step 5: Admin controls
        txtAdminUsername.TextChanged += AdminField_Changed;
        txtAdminEmail.TextChanged += AdminField_Changed;
        txtAdminPassword.TextChanged += AdminField_Changed;
        txtAdminConfirmPassword.TextChanged += AdminField_Changed;

        // Step 6: Regional controls
        cmbTimeZone.SelectedIndexChanged += RegionalField_Changed;
        cmbDateFormat.SelectedIndexChanged += RegionalField_Changed;
        cmbDateFormat.TextChanged += RegionalField_Changed;
        cmbTimeFormat.SelectedIndexChanged += RegionalField_Changed;
        cmbCurrency.SelectedIndexChanged += RegionalField_Changed;
        txtTerminalName.TextChanged += RegionalField_Changed;

        // Step 7: Licensing controls
        btnCopyHardwareId.Click += BtnCopyHardwareId_Click;
        btnImportLicense.Click += BtnImportLicense_Click;
        chkEvaluationMode.CheckedChanged += ChkEvaluationMode_CheckedChanged;

        Resize += (s, e) => ApplyResponsiveLayout();
        Load += FirstRunWizardForm_Load;

        ApplyWizardTypography();
        ApplyResponsiveLayout();
    }

    private void FirstRunWizardForm_Load(object? sender, EventArgs e)
    {
        if (DesignMode || System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime || DesignModeHelper.IsInDesignMode)
        {
            return;
        }

        ApplyScreenBoundsAndSizing();
        ApplyResponsiveLayout();

        PopulateStep1Detection();
        PopulateStep2ConnectionDefaults();
        PopulateStep6RegionalDefaults();
        PopulateStep7Licensing();

        SetStep(1);
    }

    #region Step Navigation

    private void SetStep(int step)
    {
        _currentStep = Math.Clamp(step, 1, TotalSteps);

        // Update step panels visibility
        panelStep1.Visible = (_currentStep == 1);
        panelStep2.Visible = (_currentStep == 2);
        panelStep3.Visible = (_currentStep == 3);
        panelStep4.Visible = (_currentStep == 4);
        panelStep5.Visible = (_currentStep == 5);
        panelStep6.Visible = (_currentStep == 6);
        panelStep7.Visible = (_currentStep == 7);
        panelStep8.Visible = (_currentStep == 8);

        // Update header subtitle and bottom progress
        lblStepIndicator.Text = $"Step {_currentStep} of {TotalSteps}";

        lblWizardSubtitle.Text = _currentStep switch
        {
            1 => "Step 1 of 8: Welcome & System Overview",
            2 => "Step 2 of 8: Database Connection Settings",
            3 => "Step 3 of 8: Database Initialization & EF Core Migrations",
            4 => "Step 4 of 8: Organization & Branch Hierarchy",
            5 => "Step 5 of 8: First Administrator Account",
            6 => "Step 6 of 8: Business & Regional Settings",
            7 => "Step 7 of 8: Software Registration & Licensing",
            8 => "Step 8 of 8: Review Configuration & Complete",
            _ => "First-Run Setup Wizard"
        };

        // Update sidebar step label styles
        UpdateSidebarStyles();

        // Update action buttons
        btnBack.Enabled = (_currentStep > 1);
        btnNext.Visible = (_currentStep < TotalSteps);
        btnFinish.Visible = (_currentStep == TotalSteps);

        if (_currentStep == 8)
        {
            BuildSummary();
        }

        UpdateNavigationState();
        ApplyStepContentLayout();
    }

    private void UpdateSidebarStyles()
    {
        LabelControl[] labels = [lblStep1, lblStep2, lblStep3, lblStep4, lblStep5, lblStep6, lblStep7, lblStep8];

        for (int i = 0; i < labels.Length; i++)
        {
            int stepIndex = i + 1;
            if (stepIndex == _currentStep)
            {
                labels[i].Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                labels[i].Appearance.ForeColor = Color.FromArgb(15, 23, 42); // Primary dark
            }
            else if (stepIndex < _currentStep)
            {
                labels[i].Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                labels[i].Appearance.ForeColor = Color.FromArgb(22, 101, 52); // Completed green
            }
            else
            {
                labels[i].Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                labels[i].Appearance.ForeColor = Color.FromArgb(100, 116, 139); // Inactive gray
            }
        }
    }

    private void UpdateNavigationState()
    {
        switch (_currentStep)
        {
            case 1:
                btnNext.Enabled = true;
                lblFooterStatus.Text = "Review prerequisites and click Next to continue.";
                lblFooterStatus.ForeColor = Color.FromArgb(100, 116, 139);
                break;

            case 2:
                btnNext.Enabled = _isConnectionTested;
                if (!_isConnectionTested)
                {
                    lblFooterStatus.Text = "You must test the database connection successfully before proceeding.";
                    lblFooterStatus.ForeColor = Color.Firebrick;
                }
                else
                {
                    lblFooterStatus.Text = "Connection verified successfully.";
                    lblFooterStatus.ForeColor = Color.ForestGreen;
                }
                break;

            case 3:
                btnNext.Enabled = _areMigrationsApplied;
                if (!_areMigrationsApplied)
                {
                    lblFooterStatus.Text = "Apply migrations across all schemas before proceeding.";
                    lblFooterStatus.ForeColor = Color.DarkOrange;
                }
                else
                {
                    lblFooterStatus.Text = "All database schemas are initialized and up-to-date.";
                    lblFooterStatus.ForeColor = Color.ForestGreen;
                }
                break;

            case 4:
                bool step4Valid = !string.IsNullOrWhiteSpace(txtOrgName.Text) &&
                                  !string.IsNullOrWhiteSpace(txtCompanyName.Text) &&
                                  !string.IsNullOrWhiteSpace(txtBranchName.Text);
                btnNext.Enabled = step4Valid;
                lblFooterStatus.Text = step4Valid ? "Hierarchy definition complete." : "Organization, Company, and Branch names are required.";
                lblFooterStatus.ForeColor = step4Valid ? Color.ForestGreen : Color.Firebrick;
                break;

            case 5:
                bool step5Valid = ValidateStep5Fields(out var step5Msg);
                btnNext.Enabled = step5Valid;
                lblFooterStatus.Text = step5Msg;
                lblFooterStatus.ForeColor = step5Valid ? Color.ForestGreen : Color.Firebrick;
                break;

            case 6:
                bool step6Valid = !string.IsNullOrWhiteSpace(txtTerminalName.Text) &&
                                  cmbTimeZone.SelectedItem != null &&
                                  !string.IsNullOrWhiteSpace(cmbDateFormat.Text) &&
                                  cmbCurrency.SelectedItem != null;
                btnNext.Enabled = step6Valid;
                lblFooterStatus.Text = step6Valid ? "Regional settings configured." : "Terminal name and format selections are required.";
                lblFooterStatus.ForeColor = step6Valid ? Color.ForestGreen : Color.Firebrick;
                break;

            case 7:
                var licResult = LicenseService.CurrentResult;
                bool isLicValid = licResult.IsAuthorized;
                bool isEval = chkEvaluationMode.Checked;
                bool step7Valid = isLicValid || isEval;
                btnNext.Enabled = step7Valid;
                lblFooterStatus.Text = step7Valid
                    ? (isLicValid ? "Commercial software license active." : "30-day evaluation mode selected.")
                    : "Import a valid license or select Evaluation Mode.";
                lblFooterStatus.ForeColor = step7Valid ? Color.ForestGreen : Color.Firebrick;
                break;

            case 8:
                btnFinish.Enabled = true;
                lblFooterStatus.Text = "Review summary and click 'Finish Setup' to complete commissioning.";
                lblFooterStatus.ForeColor = Color.FromArgb(30, 41, 59);
                break;
        }
    }

    private void BtnBack_Click(object? sender, EventArgs e)
    {
        if (_currentStep > 1)
        {
            SetStep(_currentStep - 1);
        }
    }

    private void BtnNext_Click(object? sender, EventArgs e)
    {
        if (_currentStep == 2 && !_isConnectionTested)
        {
            XtraMessageBox.Show(
                this,
                "Please click 'Test Connection' to verify SQL Server connectivity before proceeding.",
                "Connection Test Required",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        if (_currentStep == 3 && !_areMigrationsApplied)
        {
            XtraMessageBox.Show(
                this,
                "Please apply database migrations before proceeding to master data hierarchy setup.",
                "Migrations Required",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        if (_currentStep == 5 && !ValidateStep5Fields(out var err))
        {
            XtraMessageBox.Show(
                this,
                err,
                "Validation Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        if (_currentStep < TotalSteps)
        {
            SetStep(_currentStep + 1);
        }
    }

    private void BtnCancel_Click(object? sender, EventArgs e)
    {
        var choice = XtraMessageBox.Show(
            this,
            "Are you sure you want to cancel first-run commissioning? The system cannot be used until commissioning is completed.",
            "Cancel Setup",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (choice == DialogResult.Yes)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }

    #endregion

    #region Step 1: Welcome & Prerequisites

    private void ApplyWizardTypography()
    {
        // Step 1: Body checklist and workstation environment labels
        lblStep1Desc.Appearance.Font = DesktopStyle.BodyFont;
        lblStep1Desc.Appearance.Options.UseFont = true;

        lblPrereqSql.Appearance.Font = DesktopStyle.BodyFont;
        lblPrereqSql.Appearance.Options.UseFont = true;
        lblPrereqRuntime.Appearance.Font = DesktopStyle.BodyFont;
        lblPrereqRuntime.Appearance.Options.UseFont = true;
        lblPrereqDisplay.Appearance.Font = DesktopStyle.BodyFont;
        lblPrereqDisplay.Appearance.Options.UseFont = true;
        lblPrereqAdmin.Appearance.Font = DesktopStyle.BodyFont;
        lblPrereqAdmin.Appearance.Options.UseFont = true;

        lblDetectedOs.Appearance.Font = DesktopStyle.BodyFont;
        lblDetectedOs.Appearance.Options.UseFont = true;
        lblDetectedRuntime.Appearance.Font = DesktopStyle.BodyFont;
        lblDetectedRuntime.Appearance.Options.UseFont = true;
        lblDetectedDpi.Appearance.Font = DesktopStyle.BodyFont;
        lblDetectedDpi.Appearance.Options.UseFont = true;
        lblDetectedElevation.Appearance.Font = DesktopStyle.BodyFont;
        lblDetectedElevation.Appearance.Options.UseFont = true;
        lblDetectedElevation.Appearance.Options.UseForeColor = true;

        // Steps 2-8: Informational descriptions and policy notices
        lblStep2Desc.Appearance.Font = DesktopStyle.BodyFont;
        lblStep2Desc.Appearance.Options.UseFont = true;
        lblStep3Desc.Appearance.Font = DesktopStyle.BodyFont;
        lblStep3Desc.Appearance.Options.UseFont = true;
        lblStep4Desc.Appearance.Font = DesktopStyle.BodyFont;
        lblStep4Desc.Appearance.Options.UseFont = true;
        lblStep5Desc.Appearance.Font = DesktopStyle.BodyFont;
        lblStep5Desc.Appearance.Options.UseFont = true;
        lblStep6Desc.Appearance.Font = DesktopStyle.BodyFont;
        lblStep6Desc.Appearance.Options.UseFont = true;
        lblStep7Desc.Appearance.Font = DesktopStyle.BodyFont;
        lblStep7Desc.Appearance.Options.UseFont = true;
        lblStep8Desc.Appearance.Font = DesktopStyle.BodyFont;
        lblStep8Desc.Appearance.Options.UseFont = true;

        lblPasswordPolicy.Appearance.Font = DesktopStyle.CaptionFont;
        lblPasswordPolicy.Appearance.Options.UseFont = true;

        lblFinishNotice.Appearance.Font = DesktopStyle.BodyFontBold;
        lblFinishNotice.Appearance.Options.UseFont = true;
        lblFinishNotice.Appearance.Options.UseForeColor = true;
    }

    private void PopulateStep1Detection()
    {
        lblDetectedOs.Text = $"Operating System: {Environment.OSVersion.VersionString} ({(Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit")})";
        lblDetectedRuntime.Text = $".NET Runtime: .NET {Environment.Version} (Process: {(Environment.Is64BitProcess ? "x64" : "x86")})";

        var scalePercent = (int)Math.Round(DeviceDpi / 96.0 * 100);
        lblDetectedDpi.Text = $"Display DPI Scaling: {DeviceDpi} DPI ({scalePercent}% Scale, PerMonitorV2)";

        bool isAdmin = WindowsCommissioningSecurity.IsRunningAsAdministrator();
        lblDetectedElevation.Text = $"Elevation Status: {(isAdmin ? "Elevated (Administrator)" : "Standard User")}";
        lblDetectedElevation.Appearance.ForeColor = isAdmin ? Color.ForestGreen : Color.FromArgb(100, 116, 139);
        lblDetectedElevation.Appearance.Options.UseForeColor = true;
    }

    #endregion

    #region Step 2: Database Connection

    private void PopulateStep2ConnectionDefaults()
    {
        var existing = DatabaseSecretStore.Load();
        if (existing != null)
        {
            txtServer.Text = existing.Server;
            txtDatabase.Text = existing.Database;
            cmbAuth.SelectedIndex = existing.UseWindowsAuthentication ? 0 : 1;
            txtUsername.Text = existing.UserId ?? "cbos_app";
            if (!string.IsNullOrEmpty(existing.EncryptedPassword))
            {
                txtPassword.Text = DatabaseSecretStore.Unprotect(existing.EncryptedPassword);
            }
        }
        else
        {
            txtServer.Text = DatabaseConnectionSettings.DefaultServer;
            txtDatabase.Text = DatabaseConnectionSettings.DefaultDatabaseName;
            cmbAuth.SelectedIndex = 0; // Windows Auth default
            txtUsername.Text = "cbos_app";
            txtPassword.Text = string.Empty;
        }

        UpdateAuthControlState();
    }

    private void CmbAuth_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdateAuthControlState();
        ConnectionField_Changed(sender, e);
    }

    private void UpdateAuthControlState()
    {
        bool isSqlAuth = cmbAuth.SelectedIndex == 1;
        lblUsername.Enabled = isSqlAuth;
        txtUsername.Enabled = isSqlAuth;
        lblPassword.Enabled = isSqlAuth;
        txtPassword.Enabled = isSqlAuth;
    }

    private void ConnectionField_Changed(object? sender, EventArgs e)
    {
        _isConnectionTested = false;
        _areMigrationsApplied = false;
        lblConnectionStatus.Text = "Connection parameters changed. Retest required.";
        lblConnectionStatus.Appearance.ForeColor = Color.DarkOrange;
        UpdateNavigationState();
    }

    private DatabaseConnectionSettings BuildSettingsFromUi()
    {
        bool isWindowsAuth = cmbAuth.SelectedIndex == 0;
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

    private async void BtnTestConnection_Click(object? sender, EventArgs e)
    {
        btnTestConnection.Enabled = false;
        lblConnectionStatus.Text = "Testing connection to SQL Server...";
        lblConnectionStatus.Appearance.ForeColor = Color.DarkBlue;
        Update();

        _connectionSettings = BuildSettingsFromUi();

        try
        {
            var (success, message) = await _databaseProvisioning.TestConnectionAsync(_connectionSettings).ConfigureAwait(true);
            if (success)
            {
                _isConnectionTested = true;
                lblConnectionStatus.Text = "Connection successful!";
                lblConnectionStatus.Appearance.ForeColor = Color.ForestGreen;
            }
            else
            {
                _isConnectionTested = false;
                lblConnectionStatus.Text = "Connection failed.";
                lblConnectionStatus.Appearance.ForeColor = Color.Firebrick;

                XtraMessageBox.Show(
                    this,
                    $"Failed to connect to SQL Server:\n\n{message}",
                    "Connection Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            _isConnectionTested = false;
            var masked = DatabaseErrorMasker.Mask(ex.Message, _connectionSettings.PlainTextPassword);
            lblConnectionStatus.Text = "Error during test.";
            lblConnectionStatus.Appearance.ForeColor = Color.Firebrick;

            XtraMessageBox.Show(
                this,
                $"An error occurred while testing the connection:\n\n{masked}",
                "Connection Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            btnTestConnection.Enabled = true;
            UpdateNavigationState();
        }
    }

    #endregion

    #region Step 3: Database Initialization & Migrations

    private async void BtnApplyMigrations_Click(object? sender, EventArgs e)
    {
        btnApplyMigrations.Enabled = false;
        btnNext.Enabled = false;
        btnBack.Enabled = false;
        progressMigrations.Position = 0;
        memoMigrationLog.Text = string.Empty;

        AppendMigrationLog("Starting database initialization and migration sequence...");

        _connectionSettings = BuildSettingsFromUi();

        try
        {
            // 1. Create database if requested
            if (chkCreateDbIfMissing.Checked)
            {
                AppendMigrationLog($"Checking existence of database '{_connectionSettings.Database}'...");
                var (created, createMsg) = await _databaseProvisioning.CreateDatabaseAsync(_connectionSettings).ConfigureAwait(true);
                AppendMigrationLog(createMsg);

                if (!created)
                {
                    XtraMessageBox.Show(
                        this,
                        createMsg,
                        "Database Creation Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
            }

            // 2. Apply EF Core migrations
            if (chkApplyMigrations.Checked)
            {
                AppendMigrationLog("Applying migrations across 6 bounded contexts...");

                var targetConnStr = _connectionSettings.BuildConnectionString();
                var (migrated, migrateMsg) = await _databaseProvisioning.ApplyMigrationsAsync(
                    targetConnStr,
                    (stage, percent) =>
                    {
                        Invoke(() =>
                        {
                            progressMigrations.Position = percent;
                            lblMigrationStatus.Text = stage;
                            AppendMigrationLog($"[{percent}%] {stage}");
                        });
                    }).ConfigureAwait(true);

                if (migrated)
                {
                    _areMigrationsApplied = true;
                    lblMigrationStatus.Text = "All database schemas migrated successfully!";
                    lblMigrationStatus.Appearance.ForeColor = Color.ForestGreen;
                    AppendMigrationLog("COMPLETED: All EF Core migrations and schema definitions applied successfully.");
                }
                else
                {
                    _areMigrationsApplied = false;
                    lblMigrationStatus.Text = "Migration process failed.";
                    lblMigrationStatus.Appearance.ForeColor = Color.Firebrick;

                    XtraMessageBox.Show(
                        this,
                        migrateMsg,
                        "Migration Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            else
            {
                _areMigrationsApplied = true;
                progressMigrations.Position = 100;
                lblMigrationStatus.Text = "Skipped migrations as requested.";
                AppendMigrationLog("Migrations skipped by operator.");
            }
        }
        catch (Exception ex)
        {
            _areMigrationsApplied = false;
            var masked = DatabaseErrorMasker.Mask(ex.Message, _connectionSettings.PlainTextPassword);
            AppendMigrationLog($"ERROR: {masked}");

            XtraMessageBox.Show(
                this,
                $"Database initialization encountered an error:\n\n{masked}",
                "Initialization Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            btnApplyMigrations.Enabled = true;
            btnBack.Enabled = true;
            UpdateNavigationState();
        }
    }

    private void AppendMigrationLog(string message)
    {
        var time = DateTime.Now.ToString("HH:mm:ss");
        memoMigrationLog.AppendText($"[{time}] {message}\r\n");
    }

    #endregion

    #region Step 4: Organization & Hierarchy

    private void HierarchyField_Changed(object? sender, EventArgs e)
    {
        UpdateNavigationState();
    }

    #endregion

    #region Step 5: Administrator Account

    private void AdminField_Changed(object? sender, EventArgs e)
    {
        UpdatePasswordStrength();
        UpdateNavigationState();
    }

    private void UpdatePasswordStrength()
    {
        var password = txtAdminPassword.Text;
        if (string.IsNullOrEmpty(password))
        {
            lblPasswordStrength.Text = "Password Strength: (empty)";
            lblPasswordStrength.Appearance.ForeColor = Color.FromArgb(100, 116, 139);
            progressPasswordStrength.Position = 0;
            return;
        }

        int score = 0;
        if (password.Length >= 8) score++;
        if (password.Length >= 12) score++;
        if (password.Any(char.IsUpper)) score++;
        if (password.Any(char.IsLower)) score++;
        if (password.Any(char.IsDigit)) score++;
        if (password.Any(c => !char.IsLetterOrDigit(c))) score++;

        if (score <= 2)
        {
            lblPasswordStrength.Text = "Password Strength: Weak";
            lblPasswordStrength.Appearance.ForeColor = Color.Firebrick;
            progressPasswordStrength.Position = 30;
        }
        else if (score <= 4)
        {
            lblPasswordStrength.Text = "Password Strength: Medium";
            lblPasswordStrength.Appearance.ForeColor = Color.DarkOrange;
            progressPasswordStrength.Position = 65;
        }
        else
        {
            lblPasswordStrength.Text = "Password Strength: Strong";
            lblPasswordStrength.Appearance.ForeColor = Color.ForestGreen;
            progressPasswordStrength.Position = 100;
        }
    }

    private bool ValidateStep5Fields(out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(txtAdminUsername.Text))
        {
            errorMessage = "Administrator username is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(txtAdminFullName.Text))
        {
            errorMessage = "Administrator full display name is required.";
            return false;
        }

        var email = txtAdminEmail.Text.Trim();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@') || !email.Contains('.'))
        {
            errorMessage = "A valid administrator email address is required.";
            return false;
        }

        var password = txtAdminPassword.Text;
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            errorMessage = "Password must be at least 8 characters long.";
            return false;
        }

        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
        {
            errorMessage = "Password must contain both letters and digits.";
            return false;
        }

        if (string.Equals(password, "Admin123!", StringComparison.Ordinal) ||
            string.Equals(password, "password", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(password, "12345678", StringComparison.Ordinal))
        {
            errorMessage = "Default or trivial passwords are strictly prohibited for security.";
            return false;
        }

        if (password != txtAdminConfirmPassword.Text)
        {
            errorMessage = "Passwords do not match.";
            return false;
        }

        errorMessage = "Administrator credentials validated.";
        return true;
    }

    #endregion

    #region Step 6: Regional Settings

    private void PopulateStep6RegionalDefaults()
    {
        // Populate Time Zones
        cmbTimeZone.Properties.Items.Clear();
        var systemZones = TimeZoneInfo.GetSystemTimeZones();
        foreach (var zone in systemZones)
        {
            cmbTimeZone.Properties.Items.Add(zone.DisplayName);
        }

        var localZone = TimeZoneInfo.Local;
        int localIndex = cmbTimeZone.Properties.Items.IndexOf(localZone.DisplayName);
        cmbTimeZone.SelectedIndex = localIndex >= 0 ? localIndex : 0;

        // Populate Date Formats
        cmbDateFormat.SelectedIndex = 0; // "dd-MMM-yyyy"
        cmbTimeFormat.SelectedIndex = 0; // "12 Hour (hh:mm tt)"
        cmbCurrency.SelectedIndex = 0;   // "USD ($)"
        txtTerminalName.Text = Environment.MachineName;

        UpdateRegionalSamplePreview();
    }

    private void RegionalField_Changed(object? sender, EventArgs e)
    {
        UpdateRegionalSamplePreview();
        UpdateNavigationState();
    }

    private void UpdateRegionalSamplePreview()
    {
        try
        {
            var dateFormat = string.IsNullOrWhiteSpace(cmbDateFormat.Text) ? "dd-MMM-yyyy" : cmbDateFormat.Text.Trim();
            bool is24Hour = cmbTimeFormat.SelectedIndex == 1;
            var timePattern = is24Hour ? "HH:mm" : "hh:mm tt";

            var now = DateTime.Now;
            var sampleDate = now.ToString(dateFormat, System.Globalization.CultureInfo.InvariantCulture);
            var sampleTime = now.ToString(timePattern, System.Globalization.CultureInfo.InvariantCulture);
            lblSamplePreview.Text = $"Preview: {sampleDate} {sampleTime}  |  Sample Currency: $149.50";
        }
        catch
        {
            lblSamplePreview.Text = "Preview: Invalid format";
        }
    }

    #endregion

    #region Step 7: Licensing & Registration

    private void PopulateStep7Licensing()
    {
        txtHardwareId.Text = MachineFingerprint.GetCurrentMachineId();
        RefreshLicenseDisplay();
    }

    private void RefreshLicenseDisplay()
    {
        var result = LicenseService.ValidateCurrentLicense();
        var lic = result.License;

        if (result.IsAuthorized && lic != null)
        {
            lblLicenseStatus.Text = $"Status: Active & Valid ({lic.LicenseType})";
            lblLicenseStatus.Appearance.ForeColor = Color.ForestGreen;
            lblLicensedTo.Text = $"Licensed To: {lic.CustomerName} ({lic.CompanyName})";
            chkEvaluationMode.Checked = false;
        }
        else
        {
            lblLicenseStatus.Text = "Status: Unlicensed / Evaluation Mode";
            lblLicenseStatus.Appearance.ForeColor = Color.DarkOrange;
            lblLicensedTo.Text = "Licensed To: Unregistered";
            chkEvaluationMode.Checked = true;
        }
    }

    private void BtnCopyHardwareId_Click(object? sender, EventArgs e)
    {
        try
        {
            Clipboard.SetText(txtHardwareId.Text);
            XtraMessageBox.Show(
                this,
                "Workstation Hardware ID copied to clipboard.\nProvide this ID to Clovent Operations to receive your software license.",
                "Copied",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show(this, $"Failed to copy: {ex.Message}", "Clipboard Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void BtnImportLicense_Click(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "Clovent License Files (*.lic)|*.lic|All Files (*.*)|*.*",
            Title = "Select Clovent Software License File"
        };

        if (ofd.ShowDialog(this) == DialogResult.OK)
        {
            try
            {
                var result = LicenseService.ImportLicense(ofd.FileName);
                RefreshLicenseDisplay();

                if (result.IsAuthorized)
                {
                    XtraMessageBox.Show(
                        this,
                        "Software license imported and cryptographically validated successfully.",
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
            UpdateNavigationState();
        }
    }

    private void ChkEvaluationMode_CheckedChanged(object? sender, EventArgs e)
    {
        UpdateNavigationState();
    }

    #endregion

    #region Step 8: Review & Complete

    private void BuildSummary()
    {
        var sb = new StringBuilder();
        sb.AppendLine("================================================================================");
        sb.AppendLine("CLOVENT BUSINESS OPERATING SYSTEM - COMMISSIONING CONFIGURATION SUMMARY");
        sb.AppendLine("================================================================================");
        sb.AppendLine();
        sb.AppendLine("1. Database Configuration:");
        sb.AppendLine($"   SQL Server:           {txtServer.Text.Trim()}");
        sb.AppendLine($"   Database Name:        {txtDatabase.Text.Trim()}");
        sb.AppendLine($"   Authentication Mode:  {(cmbAuth.SelectedIndex == 0 ? "Windows Authentication" : "SQL Server Authentication")}");
        if (cmbAuth.SelectedIndex == 1)
        {
            sb.AppendLine($"   SQL User ID:          {txtUsername.Text.Trim()}");
            sb.AppendLine("   SQL Password:         ****** (Masked for Security)");
        }
        sb.AppendLine();

        sb.AppendLine("2. Enterprise Hierarchy:");
        sb.AppendLine($"   Organization Name:    {txtOrgName.Text.Trim()}");
        sb.AppendLine($"   Tax ID / Code:        {(string.IsNullOrWhiteSpace(txtTaxId.Text) ? "None" : txtTaxId.Text.Trim())}");
        sb.AppendLine($"   Company Name:         {txtCompanyName.Text.Trim()}");
        sb.AppendLine($"   Branch / Store Name:  {txtBranchName.Text.Trim()}");
        sb.AppendLine();

        sb.AppendLine("3. Primary Administrator:");
        sb.AppendLine($"   Username:             {txtAdminUsername.Text.Trim()}");
        sb.AppendLine($"   Display Name:         {txtAdminFullName.Text.Trim()}");
        sb.AppendLine($"   Email Address:        {txtAdminEmail.Text.Trim()}");
        sb.AppendLine("   Password:             ****** (Masked for Security)");
        sb.AppendLine();

        sb.AppendLine("4. Regional & Workstation Settings:");
        sb.AppendLine($"   Time Zone:            {cmbTimeZone.SelectedItem}");
        sb.AppendLine($"   Date Format:          {cmbDateFormat.Text.Trim()}");
        sb.AppendLine($"   Time Format:          {cmbTimeFormat.SelectedItem}");
        sb.AppendLine($"   Base Currency:        {cmbCurrency.SelectedItem}");
        sb.AppendLine($"   Terminal Name:        {txtTerminalName.Text.Trim()}");
        sb.AppendLine();

        sb.AppendLine("5. Software Licensing:");
        sb.AppendLine($"   Hardware ID:          {txtHardwareId.Text.Trim()}");
        sb.AppendLine($"   Licensing Mode:       {(chkEvaluationMode.Checked ? "30-Day Evaluation / Trial" : "Commercial License")}");
        sb.AppendLine("================================================================================");

        memoSummary.Text = sb.ToString();
        memoSummary.SelectionStart = 0;
        memoSummary.SelectionLength = 0;
    }

    private async void BtnFinish_Click(object? sender, EventArgs e)
    {
        btnFinish.Enabled = false;
        btnBack.Enabled = false;
        btnCancel.Enabled = false;
        Cursor = Cursors.WaitCursor;

        try
        {
            _connectionSettings = BuildSettingsFromUi();

            var currencyText = cmbCurrency.SelectedItem?.ToString() ?? "USD";
            var currencyParts = currencyText.Split(' ');
            var currencyCode = currencyParts.Length > 0 ? currencyParts[0] : "USD";

            var masterDataRequest = new InitialMasterDataProvisioningRequest
            {
                OrganizationName = txtOrgName.Text.Trim(),
                OrganizationCode = txtTaxId.Text.Trim(),
                CompanyName = txtCompanyName.Text.Trim(),
                BranchName = txtBranchName.Text.Trim(),
                TerminalName = txtTerminalName.Text.Trim(),
                CurrencyCode = currencyCode,
                DateFormat = cmbDateFormat.Text.Trim(),
                TimeFormat = cmbTimeFormat.SelectedIndex == 1 ? "24 Hour" : "12 Hour"
            };

            var adminRequest = new FirstAdminProvisioningRequest
            {
                UserName = txtAdminUsername.Text.Trim(),
                DisplayName = txtAdminFullName.Text.Trim(),
                Email = txtAdminEmail.Text.Trim(),
                Password = txtAdminPassword.Text,
                ConfirmPassword = txtAdminConfirmPassword.Text
            };

            var marker = new CommissioningMarker
            {
                CommissionedAtUtc = DateTimeOffset.UtcNow,
                ProductVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "1.1.2",
                MachineId = txtHardwareId.Text.Trim(),
                DatabaseServer = _connectionSettings.Server,
                DatabaseName = _connectionSettings.Database,
                OrganizationName = txtOrgName.Text.Trim(),
                CompanyName = txtCompanyName.Text.Trim(),
                BranchName = txtBranchName.Text.Trim(),
                TerminalName = txtTerminalName.Text.Trim(),
                AdminUserName = txtAdminUsername.Text.Trim(),
                IsEvaluation = chkEvaluationMode.Checked
            };

            var executionRequest = new CommissioningExecutionRequest
            {
                ConnectionSettings = _connectionSettings,
                MasterDataRequest = masterDataRequest,
                AdminRequest = adminRequest,
                Marker = marker
            };

            var result = await _provisioningCoordinator.ExecuteCommissioningAsync(executionRequest, _services).ConfigureAwait(true);
            if (!result.Success)
            {
                throw new InvalidOperationException(result.Message);
            }

            XtraMessageBox.Show(
                this,
                "Clovent Business Operating System commissioning has completed successfully!\n\nThe application will now proceed to the Sign-In screen.",
                "Setup Completed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            var masked = DatabaseErrorMasker.Mask(ex.Message, _connectionSettings.PlainTextPassword);
            XtraMessageBox.Show(
                this,
                $"An error occurred while finalizing commissioning:\n\n{masked}",
                "Commissioning Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
            btnFinish.Enabled = true;
            btnBack.Enabled = true;
            btnCancel.Enabled = true;
        }
    }

    #endregion

    #region Responsive Layout & High-DPI Sizing

    private void ApplyScreenBoundsAndSizing()
    {
        var screen = Screen.FromControl(this) ?? Screen.PrimaryScreen ?? Screen.AllScreens[0];
        var workArea = screen.WorkingArea;

        // Target 72% width and 76% height of usable screen area on 1920x1080 / high-DPI displays
        int targetW = (int)(workArea.Width * 0.72);
        int targetH = (int)(workArea.Height * 0.76);

        // Enforce DPI-scaled minimum bounds (980x660 baseline at 96 DPI)
        int minW = DesktopDpi.Scale(980, this);
        int minH = DesktopDpi.Scale(660, this);

        // Upper bounds clamped to 94% of workArea to ensure borders, title bar, and taskbar remain visible
        int maxW = (int)(workArea.Width * 0.94);
        int maxH = (int)(workArea.Height * 0.94);

        int finalW = Math.Clamp(Math.Max(targetW, minW), Math.Min(minW, maxW), maxW);
        int finalH = Math.Clamp(Math.Max(targetH, minH), Math.Min(minH, maxH), maxH);

        MinimumSize = new Size(Math.Min(minW, maxW), Math.Min(minH, maxH));
        Size = new Size(finalW, finalH);

        // Center dialog within working area
        int x = workArea.Left + (workArea.Width - finalW) / 2;
        int y = workArea.Top + (workArea.Height - finalH) / 2;
        Location = new Point(Math.Max(workArea.Left, x), Math.Max(workArea.Top, y));
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyResponsiveLayout();
    }

    private void ApplyResponsiveLayout()
    {
        if (IsDisposed)
        {
            return;
        }

        SuspendLayout();
        try
        {
            ApplyHeaderLayout();
            ApplySidebarLayout();
            ApplyFooterLayout();
            ApplyStepContentLayout();
        }
        finally
        {
            ResumeLayout(true);
        }
    }

    private void ApplyHeaderLayout()
    {
        panelHeader.Height = DesktopDpi.Scale(78, this);
        lblWizardTitle.Location = new Point(DesktopDpi.Scale(24, this), DesktopDpi.Scale(14, this));
        lblWizardSubtitle.Location = new Point(DesktopDpi.Scale(24, this), DesktopDpi.Scale(42, this));
    }

    private void ApplySidebarLayout()
    {
        panelSidebar.Width = DesktopDpi.Scale(240, this);
        lblSidebarHeader.Location = new Point(DesktopDpi.Scale(18, this), DesktopDpi.Scale(18, this));

        LabelControl[] steps = [lblStep1, lblStep2, lblStep3, lblStep4, lblStep5, lblStep6, lblStep7, lblStep8];
        int startY = DesktopDpi.Scale(48, this);
        int stepGap = DesktopDpi.Scale(36, this);

        for (int i = 0; i < steps.Length; i++)
        {
            steps[i].Location = new Point(DesktopDpi.Scale(18, this), startY + (i * stepGap));
            steps[i].AutoSize = true;
        }
    }

    private void ApplyFooterLayout()
    {
        panelBottom.Height = DesktopDpi.Scale(60, this);

        int pad = DesktopDpi.Scale(20, this);
        int btnH = DesktopDpi.Scale(32, this);
        int btnW = DesktopDpi.Scale(92, this);
        int finishW = DesktopDpi.Scale(110, this);
        int gap = DesktopDpi.Scale(8, this);
        int yPos = (panelBottom.ClientSize.Height - btnH) / 2;

        btnFinish.Size = new Size(finishW, btnH);
        btnNext.Size = new Size(btnW, btnH);
        btnBack.Size = new Size(btnW, btnH);
        btnCancel.Size = new Size(btnW, btnH);

        int rightEdge = panelBottom.ClientSize.Width - pad;
        btnFinish.Location = new Point(rightEdge - finishW, yPos);
        btnNext.Location = new Point(rightEdge - btnW, yPos);

        int nextOrFinishLeft = Math.Min(btnFinish.Left, btnNext.Left);
        btnBack.Location = new Point(nextOrFinishLeft - gap - btnW, yPos);
        btnCancel.Location = new Point(btnBack.Left - gap - btnW, yPos);

        lblStepIndicator.Location = new Point(pad, (panelBottom.ClientSize.Height - lblStepIndicator.Height) / 2);

        int statusLeft = lblStepIndicator.Right + DesktopDpi.Scale(16, this);
        int statusMaxRight = btnCancel.Left - DesktopDpi.Scale(16, this);
        int statusWidth = Math.Max(100, statusMaxRight - statusLeft);

        lblFooterStatus.Location = new Point(statusLeft, (panelBottom.ClientSize.Height - lblFooterStatus.Height) / 2);
        lblFooterStatus.AutoSizeMode = LabelAutoSizeMode.None;
        lblFooterStatus.Width = statusWidth;
        lblFooterStatus.AutoEllipsis = true;
    }

    private void ApplyStepContentLayout()
    {
        switch (_currentStep)
        {
            case 1:
                LayoutStep1();
                break;
            case 2:
                LayoutStep2();
                break;
            case 3:
                LayoutStep3();
                break;
            case 4:
                LayoutStep4();
                break;
            case 5:
                LayoutStep5();
                break;
            case 6:
                LayoutStep6();
                break;
            case 7:
                LayoutStep7();
                break;
            case 8:
                LayoutStep8();
                break;
        }
    }

    private void LayoutStep1()
    {
        lblStep1Title.Location = new Point(0, DesktopDpi.Scale(4, this));
        lblStep1Desc.Location = new Point(0, DesktopDpi.Scale(34, this));
        int contentW = Math.Max(320, panelStep1.ClientSize.Width - DesktopDpi.Scale(16, this));
        lblStep1Desc.Width = contentW;

        int grpW = contentW;
        int grpH = DesktopDpi.Scale(164, this);
        grpPrerequisites.Location = new Point(0, lblStep1Desc.Bottom + DesktopDpi.Scale(12, this));
        grpPrerequisites.Size = new Size(grpW, grpH);

        int innerPad = DesktopDpi.Scale(18, this);
        lblPrereqSql.Location = new Point(innerPad, DesktopDpi.Scale(34, this));
        lblPrereqRuntime.Location = new Point(innerPad, DesktopDpi.Scale(64, this));
        lblPrereqDisplay.Location = new Point(innerPad, DesktopDpi.Scale(94, this));
        lblPrereqAdmin.Location = new Point(innerPad, DesktopDpi.Scale(124, this));

        grpSystemDetection.Location = new Point(0, grpPrerequisites.Bottom + DesktopDpi.Scale(12, this));
        grpSystemDetection.Size = new Size(grpW, grpH);

        lblDetectedOs.Location = new Point(innerPad, DesktopDpi.Scale(34, this));
        lblDetectedRuntime.Location = new Point(innerPad, DesktopDpi.Scale(64, this));
        lblDetectedDpi.Location = new Point(innerPad, DesktopDpi.Scale(94, this));
        lblDetectedElevation.Location = new Point(innerPad, DesktopDpi.Scale(124, this));
    }

    private void LayoutStep2()
    {
        lblStep2Title.Location = new Point(0, DesktopDpi.Scale(4, this));
        lblStep2Desc.Location = new Point(0, DesktopDpi.Scale(34, this));
        int contentW = Math.Max(320, panelStep2.ClientSize.Width - DesktopDpi.Scale(16, this));
        lblStep2Desc.Width = contentW;

        int edW = Math.Min(DesktopDpi.Scale(500, this), contentW);
        int edH = DesktopDpi.Scale(26, this);
        int curY = lblStep2Desc.Bottom + DesktopDpi.Scale(14, this);

        lblServer.Location = new Point(0, curY);
        txtServer.Location = new Point(0, lblServer.Bottom + DesktopDpi.Scale(4, this));
        txtServer.Size = new Size(edW, edH);
        curY = txtServer.Bottom + DesktopDpi.Scale(10, this);

        lblDatabase.Location = new Point(0, curY);
        txtDatabase.Location = new Point(0, lblDatabase.Bottom + DesktopDpi.Scale(4, this));
        txtDatabase.Size = new Size(edW, edH);
        curY = txtDatabase.Bottom + DesktopDpi.Scale(10, this);

        lblAuth.Location = new Point(0, curY);
        cmbAuth.Location = new Point(0, lblAuth.Bottom + DesktopDpi.Scale(4, this));
        cmbAuth.Size = new Size(edW, edH);
        curY = cmbAuth.Bottom + DesktopDpi.Scale(10, this);

        lblUsername.Location = new Point(0, curY);
        txtUsername.Location = new Point(0, lblUsername.Bottom + DesktopDpi.Scale(4, this));
        txtUsername.Size = new Size(edW, edH);
        curY = txtUsername.Bottom + DesktopDpi.Scale(10, this);

        lblPassword.Location = new Point(0, curY);
        txtPassword.Location = new Point(0, lblPassword.Bottom + DesktopDpi.Scale(4, this));
        txtPassword.Size = new Size(edW, edH);
        curY = txtPassword.Bottom + DesktopDpi.Scale(16, this);

        btnTestConnection.Location = new Point(0, curY);
        btnTestConnection.Size = new Size(DesktopDpi.Scale(140, this), DesktopDpi.Scale(32, this));
        lblConnectionStatus.Location = new Point(btnTestConnection.Right + DesktopDpi.Scale(14, this), curY + (btnTestConnection.Height - lblConnectionStatus.Height) / 2);
    }

    private void LayoutStep3()
    {
        lblStep3Title.Location = new Point(0, DesktopDpi.Scale(4, this));
        lblStep3Desc.Location = new Point(0, DesktopDpi.Scale(34, this));
        int contentW = Math.Max(320, panelStep3.ClientSize.Width - DesktopDpi.Scale(16, this));
        lblStep3Desc.Width = contentW;

        int curY = lblStep3Desc.Bottom + DesktopDpi.Scale(14, this);

        chkCreateDbIfMissing.Location = new Point(0, curY);
        chkCreateDbIfMissing.Width = contentW;
        curY = chkCreateDbIfMissing.Bottom + DesktopDpi.Scale(8, this);

        chkApplyMigrations.Location = new Point(0, curY);
        chkApplyMigrations.Width = contentW;
        curY = chkApplyMigrations.Bottom + DesktopDpi.Scale(14, this);

        btnApplyMigrations.Location = new Point(0, curY);
        btnApplyMigrations.Size = new Size(DesktopDpi.Scale(210, this), DesktopDpi.Scale(34, this));
        curY = btnApplyMigrations.Bottom + DesktopDpi.Scale(14, this);

        progressMigrations.Location = new Point(0, curY);
        progressMigrations.Size = new Size(contentW, DesktopDpi.Scale(22, this));
        curY = progressMigrations.Bottom + DesktopDpi.Scale(8, this);

        lblMigrationStatus.Location = new Point(0, curY);
        curY = lblMigrationStatus.Bottom + DesktopDpi.Scale(10, this);

        memoMigrationLog.Location = new Point(0, curY);
        int logH = Math.Max(DesktopDpi.Scale(160, this), panelStep3.ClientSize.Height - curY - DesktopDpi.Scale(16, this));
        memoMigrationLog.Size = new Size(contentW, logH);
    }

    private void LayoutStep4()
    {
        lblStep4Title.Location = new Point(0, DesktopDpi.Scale(4, this));
        lblStep4Desc.Location = new Point(0, DesktopDpi.Scale(34, this));
        int contentW = Math.Max(320, panelStep4.ClientSize.Width - DesktopDpi.Scale(16, this));
        lblStep4Desc.Width = contentW;

        int edW = Math.Min(DesktopDpi.Scale(500, this), contentW);
        int edH = DesktopDpi.Scale(26, this);
        int curY = lblStep4Desc.Bottom + DesktopDpi.Scale(14, this);

        lblOrgName.Location = new Point(0, curY);
        txtOrgName.Location = new Point(0, lblOrgName.Bottom + DesktopDpi.Scale(4, this));
        txtOrgName.Size = new Size(edW, edH);
        curY = txtOrgName.Bottom + DesktopDpi.Scale(10, this);

        lblTaxId.Location = new Point(0, curY);
        txtTaxId.Location = new Point(0, lblTaxId.Bottom + DesktopDpi.Scale(4, this));
        txtTaxId.Size = new Size(edW, edH);
        curY = txtTaxId.Bottom + DesktopDpi.Scale(10, this);

        lblCompanyName.Location = new Point(0, curY);
        txtCompanyName.Location = new Point(0, lblCompanyName.Bottom + DesktopDpi.Scale(4, this));
        txtCompanyName.Size = new Size(edW, edH);
        curY = txtCompanyName.Bottom + DesktopDpi.Scale(10, this);

        lblBranchName.Location = new Point(0, curY);
        txtBranchName.Location = new Point(0, lblBranchName.Bottom + DesktopDpi.Scale(4, this));
        txtBranchName.Size = new Size(edW, edH);
    }

    private void LayoutStep5()
    {
        lblStep5Title.Location = new Point(0, DesktopDpi.Scale(4, this));
        lblStep5Desc.Location = new Point(0, DesktopDpi.Scale(34, this));
        int contentW = Math.Max(320, panelStep5.ClientSize.Width - DesktopDpi.Scale(16, this));
        lblStep5Desc.Width = contentW;

        int edW = Math.Min(DesktopDpi.Scale(420, this), contentW);
        int edH = DesktopDpi.Scale(26, this);
        int curY = lblStep5Desc.Bottom + DesktopDpi.Scale(14, this);

        lblAdminUsername.Location = new Point(0, curY);
        txtAdminUsername.Location = new Point(0, lblAdminUsername.Bottom + DesktopDpi.Scale(4, this));
        txtAdminUsername.Size = new Size(edW, edH);
        curY = txtAdminUsername.Bottom + DesktopDpi.Scale(10, this);

        lblAdminFullName.Location = new Point(0, curY);
        txtAdminFullName.Location = new Point(0, lblAdminFullName.Bottom + DesktopDpi.Scale(4, this));
        txtAdminFullName.Size = new Size(edW, edH);
        curY = txtAdminFullName.Bottom + DesktopDpi.Scale(10, this);

        lblAdminEmail.Location = new Point(0, curY);
        txtAdminEmail.Location = new Point(0, lblAdminEmail.Bottom + DesktopDpi.Scale(4, this));
        txtAdminEmail.Size = new Size(edW, edH);
        curY = txtAdminEmail.Bottom + DesktopDpi.Scale(10, this);

        lblAdminPassword.Location = new Point(0, curY);
        txtAdminPassword.Location = new Point(0, lblAdminPassword.Bottom + DesktopDpi.Scale(4, this));
        txtAdminPassword.Size = new Size(edW, edH);
        curY = txtAdminPassword.Bottom + DesktopDpi.Scale(10, this);

        lblAdminConfirmPassword.Location = new Point(0, curY);
        txtAdminConfirmPassword.Location = new Point(0, lblAdminConfirmPassword.Bottom + DesktopDpi.Scale(4, this));
        txtAdminConfirmPassword.Size = new Size(edW, edH);
        curY = txtAdminConfirmPassword.Bottom + DesktopDpi.Scale(10, this);

        lblPasswordStrength.Location = new Point(0, curY);
        curY = lblPasswordStrength.Bottom + DesktopDpi.Scale(4, this);

        progressPasswordStrength.Location = new Point(0, curY);
        progressPasswordStrength.Size = new Size(edW, DesktopDpi.Scale(12, this));
        curY = progressPasswordStrength.Bottom + DesktopDpi.Scale(8, this);

        lblPasswordPolicy.Location = new Point(0, curY);
    }

    private void LayoutStep6()
    {
        lblStep6Title.Location = new Point(0, DesktopDpi.Scale(4, this));
        lblStep6Desc.Location = new Point(0, DesktopDpi.Scale(34, this));
        int contentW = Math.Max(320, panelStep6.ClientSize.Width - DesktopDpi.Scale(16, this));
        lblStep6Desc.Width = contentW;

        int edW = Math.Min(DesktopDpi.Scale(500, this), contentW);
        int edH = DesktopDpi.Scale(26, this);
        int curY = lblStep6Desc.Bottom + DesktopDpi.Scale(14, this);

        lblTimeZone.Location = new Point(0, curY);
        cmbTimeZone.Location = new Point(0, lblTimeZone.Bottom + DesktopDpi.Scale(4, this));
        cmbTimeZone.Size = new Size(edW, edH);
        curY = cmbTimeZone.Bottom + DesktopDpi.Scale(10, this);

        int halfW = (edW - DesktopDpi.Scale(16, this)) / 2;
        lblDateFormat.Location = new Point(0, curY);
        lblTimeFormat.Location = new Point(halfW + DesktopDpi.Scale(16, this), curY);
        curY = lblDateFormat.Bottom + DesktopDpi.Scale(4, this);

        cmbDateFormat.Location = new Point(0, curY);
        cmbDateFormat.Size = new Size(halfW, edH);
        cmbTimeFormat.Location = new Point(halfW + DesktopDpi.Scale(16, this), curY);
        cmbTimeFormat.Size = new Size(halfW, edH);
        curY = cmbDateFormat.Bottom + DesktopDpi.Scale(10, this);

        lblCurrency.Location = new Point(0, curY);
        cmbCurrency.Location = new Point(0, lblCurrency.Bottom + DesktopDpi.Scale(4, this));
        cmbCurrency.Size = new Size(edW, edH);
        curY = cmbCurrency.Bottom + DesktopDpi.Scale(10, this);

        lblTerminalName.Location = new Point(0, curY);
        txtTerminalName.Location = new Point(0, lblTerminalName.Bottom + DesktopDpi.Scale(4, this));
        txtTerminalName.Size = new Size(edW, edH);
        curY = txtTerminalName.Bottom + DesktopDpi.Scale(14, this);

        lblSamplePreview.Location = new Point(0, curY);
    }

    private void LayoutStep7()
    {
        lblStep7Title.Location = new Point(0, DesktopDpi.Scale(4, this));
        lblStep7Desc.Location = new Point(0, DesktopDpi.Scale(34, this));
        int contentW = Math.Max(320, panelStep7.ClientSize.Width - DesktopDpi.Scale(16, this));
        lblStep7Desc.Width = contentW;

        int edW = Math.Min(DesktopDpi.Scale(500, this), contentW);
        int edH = DesktopDpi.Scale(26, this);
        int curY = lblStep7Desc.Bottom + DesktopDpi.Scale(14, this);

        lblHardwareId.Location = new Point(0, curY);
        curY = lblHardwareId.Bottom + DesktopDpi.Scale(4, this);

        int copyBtnW = DesktopDpi.Scale(130, this);
        int hwW = Math.Max(180, edW - copyBtnW - DesktopDpi.Scale(10, this));
        txtHardwareId.Location = new Point(0, curY);
        txtHardwareId.Size = new Size(hwW, edH);
        btnCopyHardwareId.Location = new Point(txtHardwareId.Right + DesktopDpi.Scale(10, this), curY);
        btnCopyHardwareId.Size = new Size(copyBtnW, edH);
        curY = txtHardwareId.Bottom + DesktopDpi.Scale(16, this);

        btnImportLicense.Location = new Point(0, curY);
        btnImportLicense.Size = new Size(DesktopDpi.Scale(180, this), DesktopDpi.Scale(32, this));
        curY = btnImportLicense.Bottom + DesktopDpi.Scale(14, this);

        lblLicenseStatus.Location = new Point(0, curY);
        curY = lblLicenseStatus.Bottom + DesktopDpi.Scale(6, this);

        lblLicensedTo.Location = new Point(0, curY);
        curY = lblLicensedTo.Bottom + DesktopDpi.Scale(14, this);

        chkEvaluationMode.Location = new Point(0, curY);
        chkEvaluationMode.Width = edW;
    }

    private void LayoutStep8()
    {
        lblStep8Title.Location = new Point(0, DesktopDpi.Scale(4, this));
        lblStep8Desc.Location = new Point(0, DesktopDpi.Scale(34, this));
        int contentW = Math.Max(320, panelStep8.ClientSize.Width - DesktopDpi.Scale(16, this));
        lblStep8Desc.Width = contentW;

        int curY = lblStep8Desc.Bottom + DesktopDpi.Scale(12, this);
        int memoH = Math.Max(DesktopDpi.Scale(240, this), panelStep8.ClientSize.Height - curY - DesktopDpi.Scale(50, this));
        memoSummary.Location = new Point(0, curY);
        memoSummary.Size = new Size(contentW, memoH);
        curY = memoSummary.Bottom + DesktopDpi.Scale(12, this);

        lblFinishNotice.Location = new Point(0, curY);
        lblFinishNotice.Width = contentW;
    }

    #endregion
}
