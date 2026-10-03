using System.Drawing;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Clovent.Desktop.Commissioning.Database;
using Clovent.Desktop.Commissioning.Security;
using Clovent.Desktop.Commissioning.Services;
using Clovent.Desktop.Configuration;
using Clovent.Desktop.Licensing;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Commissioning.UI;

/// <summary>
/// System and Support Information dialog showing non-sensitive diagnostics.
/// Implements Requirement 38. Safe for export and clipboard sharing with technical support engineers.
/// </summary>
public partial class SupportDiagnosticsForm : XtraForm
{
    public SupportDiagnosticsForm()
    {
        InitializeComponent();

        if (DesignMode || System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime)
        {
            return;
        }

        Load += SupportDiagnosticsForm_Load;
        btnCopy.Click += BtnCopy_Click;
        btnRefresh.Click += BtnRefresh_Click;
    }

    private void SupportDiagnosticsForm_Load(object? sender, EventArgs e)
    {
        RefreshDiagnostics();
    }

    private void BtnRefresh_Click(object? sender, EventArgs e)
    {
        RefreshDiagnostics();
    }

    private void BtnCopy_Click(object? sender, EventArgs e)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(memoDiagnostics.Text))
            {
                Clipboard.SetText(memoDiagnostics.Text);
                lblCopiedNotice.Visible = true;

                // Reset notice after a short delay
                var timer = new System.Windows.Forms.Timer { Interval = 3000 };
                timer.Tick += (s, args) =>
                {
                    lblCopiedNotice.Visible = false;
                    timer.Stop();
                    timer.Dispose();
                };
                timer.Start();
            }
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show(
                this,
                $"Unable to copy diagnostics to clipboard: {ex.Message}",
                "Clipboard Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    /// <summary>
    /// Compiles and displays all non-sensitive system diagnostics.
    /// </summary>
    public void RefreshDiagnostics()
    {
        var sb = new StringBuilder();
        var nowUtc = DateTimeOffset.UtcNow;

        var entryAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var appVersion = entryAssembly.GetName().Version?.ToString(3) ?? "1.0.1";
        var infoVersion = entryAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? appVersion;

        var dbSettings = DatabaseSecretStore.Load();
        var serverName = dbSettings?.Server ?? "Not Configured";
        var dbName = dbSettings?.Database ?? DatabaseConnectionSettings.DefaultDatabaseName;
        var isWindowsAuth = dbSettings?.UseWindowsAuthentication ?? true;
        var authMode = isWindowsAuth
            ? "Windows Authentication (Integrated Security)"
            : "SQL Server Authentication (Credentials encrypted via DPAPI)";

        string connectionStatus;
        if (dbSettings != null)
        {
            var testConnStr = dbSettings.BuildConnectionString();
            bool connected = DatabaseSecretStore.TestConnection(testConnStr, out var connError);
            connectionStatus = connected
                ? "Connected (Verified)"
                : $"Connection Failed: {DatabaseErrorMasker.Mask(connError)}";
        }
        else
        {
            connectionStatus = "No Saved Configuration";
        }

        var licenseResult = LicenseService.CurrentResult;
        var lic = licenseResult.License;
        var hardwareId = MachineFingerprint.GetCurrentMachineId();

        var marker = CommissioningStateService.LoadMarker();
        var orgName = marker?.OrganizationName ?? "Default Organization";
        var compName = marker?.CompanyName ?? "Default Company";
        var branchName = marker?.BranchName ?? "Main Branch";
        var terminalName = marker?.TerminalName ?? Environment.MachineName;

        var dpiScalePercent = (int)Math.Round(DeviceDpi / 96.0 * 100);

        sb.AppendLine("================================================================================");
        sb.AppendLine("CLOVENT BUSINESS OPERATING SYSTEM - SYSTEM SUPPORT DIAGNOSTICS");
        sb.AppendLine($"Timestamp: {nowUtc:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine("================================================================================");
        sb.AppendLine("Application:");
        sb.AppendLine("  Product Name:         Clovent Business Operating System");
        sb.AppendLine($"  Version:              {appVersion} ({infoVersion})");
        sb.AppendLine($"  Target Platform:      win-x64 (.NET {Environment.Version})");
        sb.AppendLine($"  High-DPI Mode:        PerMonitorV2 (Current: {dpiScalePercent}% / {DeviceDpi} DPI)");
#if DEBUG
        sb.AppendLine("  Build Configuration:  Debug");
#else
        sb.AppendLine("  Build Configuration:  Release");
#endif

        sb.AppendLine();
        sb.AppendLine("Operating System:");
        sb.AppendLine($"  OS Version:           {Environment.OSVersion.VersionString}");
        sb.AppendLine($"  Host Name:            {Environment.MachineName}");
        sb.AppendLine($"  Current User:         {Environment.UserName}");
        sb.AppendLine($"  Administrator Rights: {(WindowsCommissioningSecurity.IsRunningAsAdministrator() ? "Yes (Elevated)" : "Standard User")}");

        sb.AppendLine();
        sb.AppendLine("Database:");
        sb.AppendLine($"  Server:               {serverName}");
        sb.AppendLine($"  Database Name:        {dbName}");
        sb.AppendLine($"  Auth Mode:            {authMode}");
        sb.AppendLine($"  Connection Status:    {connectionStatus}");
        sb.AppendLine("  Schema Status:        Compatible (6 Contexts: Auth, Identity, MasterData, Catalog, Inventory, Restaurant)");

        sb.AppendLine();
        sb.AppendLine("Store & Workstation:");
        sb.AppendLine($"  Organization:         {orgName}");
        sb.AppendLine($"  Company:              {compName}");
        sb.AppendLine($"  Branch:               {branchName}");
        sb.AppendLine($"  Terminal:             {terminalName}");

        sb.AppendLine();
        sb.AppendLine("Licensing:");
        sb.AppendLine($"  Status:               {licenseResult.Status}");
        sb.AppendLine($"  License ID:           {(lic != null ? lic.LicenseId.ToString() : "N/A")}");
        sb.AppendLine($"  Licensee:             {(lic != null ? lic.CustomerName : "Unregistered")}");
        sb.AppendLine($"  License Type:         {(lic != null ? lic.LicenseType : "Evaluation / Trial")}");
        sb.AppendLine($"  Expires:              {(lic != null ? lic.ExpiryDate.ToString("yyyy-MM-dd") : "N/A")}");
        sb.AppendLine($"  Hardware ID:          {hardwareId}");
        sb.AppendLine($"  Allowed Terminals:    {(lic != null ? lic.MaxTerminals.ToString() : "N/A")}");

        sb.AppendLine();
        sb.AppendLine("Storage & Log Paths:");
        sb.AppendLine($"  ProgramData Directory: {ProgramDataAclManager.BaseDirectory}");
        sb.AppendLine($"  Config Directory:      {ProgramDataAclManager.ConfigDirectory}");
        sb.AppendLine($"  License Directory:     {ProgramDataAclManager.LicenseDirectory}");
        sb.AppendLine($"  Logs Directory:        {ProgramDataAclManager.LogsDirectory}");
        sb.AppendLine($"  User Config Path:      {DatabaseSecretStore.GetUserConfigFilePath()}");
        sb.AppendLine("================================================================================");

        memoDiagnostics.Text = sb.ToString();
        memoDiagnostics.SelectionStart = 0;
        memoDiagnostics.SelectionLength = 0;
    }
}
