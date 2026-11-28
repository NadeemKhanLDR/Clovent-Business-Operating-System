using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.Commissioning.Database;
using Clovent.Desktop.Configuration;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Commissioning.UI;

/// <summary>
/// Administrative dialog for creating, verifying, scheduling, and restoring SQL Server backups.
/// </summary>
public sealed class DatabaseBackupRestoreForm : XtraForm
{
    private readonly IDatabaseBackupService _backupService;
    private readonly BackupHistoryStore _historyStore;
    private readonly BackupConfigurationStore _configStore;
    private readonly string _connectionString;

    private TextEdit _txtBackupDir = null!;
    private CheckEdit _chkScheduled = null!;
    private SpinEdit _spinIntervalHours = null!;
    private TextEdit _txtRestoreFile = null!;
    private TextEdit _txtTargetDatabase = null!;
    private CheckEdit _chkSafetyBackup = null!;
    private MemoEdit _memoOutput = null!;
    private SimpleButton _btnBackupNow = null!;
    private SimpleButton _btnVerify = null!;
    private SimpleButton _btnRestore = null!;

    public DatabaseBackupRestoreForm(
        IDatabaseBackupService? backupService = null,
        BackupHistoryStore? historyStore = null,
        BackupConfigurationStore? configStore = null,
        string? connectionString = null)
    {
        _backupService = backupService ?? new DatabaseBackupService();
        _historyStore = historyStore ?? new BackupHistoryStore();
        _configStore = configStore ?? new BackupConfigurationStore();
        _connectionString = string.IsNullOrWhiteSpace(connectionString)
            ? DatabaseSecretStore.ResolveConnectionString()
            : connectionString;

        InitializeUi();
        LoadConfigurationAndHistory();
    }

    private void InitializeUi()
    {
        Text = "Database Backup & Restore";
        ClientSize = new Size(860, 620);
        MinimumSize = new Size(760, 540);
        StartPosition = FormStartPosition.CenterParent;
        Padding = new Padding(16);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // 1. Backup Destination & Schedule
        var grpConfig = new GroupControl { Text = "Backup Destination & Schedule", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
        var cfgPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2, AutoSize = true };
        cfgPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        cfgPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        cfgPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        cfgPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        cfgPanel.Controls.Add(new LabelControl { Text = "Backup Folder:", Anchor = AnchorStyles.Left }, 0, 0);
        _txtBackupDir = new TextEdit { Dock = DockStyle.Fill };
        _txtBackupDir.Properties.NullValuePrompt = "Leave blank for SQL Server default / %ProgramData%\\Clovent\\Backups";
        cfgPanel.Controls.Add(_txtBackupDir, 1, 0);

        var btnBrowseDir = new SimpleButton { Text = "Browse...", Width = 90 };
        btnBrowseDir.Click += (_, _) =>
        {
            using var fbd = new FolderBrowserDialog();
            if (!string.IsNullOrWhiteSpace(_txtBackupDir.Text)) fbd.SelectedPath = _txtBackupDir.Text;
            if (fbd.ShowDialog(this) == DialogResult.OK) _txtBackupDir.Text = fbd.SelectedPath;
        };
        cfgPanel.Controls.Add(btnBrowseDir, 2, 0);

        var btnSaveCfg = new SimpleButton { Text = "Save Settings", Width = 110 };
        btnSaveCfg.Click += (_, _) => SaveSettings();
        cfgPanel.Controls.Add(btnSaveCfg, 3, 0);

        _chkScheduled = new CheckEdit { Text = "Enable Scheduled Background Backups Every (Hours):" };
        cfgPanel.Controls.Add(_chkScheduled, 0, 1);
        cfgPanel.SetColumnSpan(_chkScheduled, 2);

        _spinIntervalHours = new SpinEdit { Width = 80 };
        _spinIntervalHours.Properties.MinValue = 1;
        _spinIntervalHours.Properties.MaxValue = 168;
        _spinIntervalHours.Properties.IsFloatValue = false;
        cfgPanel.Controls.Add(_spinIntervalHours, 2, 1);

        _btnBackupNow = new SimpleButton { Text = "Create Backup Now", Width = 140 };
        _btnBackupNow.Click += async (_, _) => await CreateBackupAsync();
        cfgPanel.Controls.Add(_btnBackupNow, 3, 1);

        grpConfig.Controls.Add(cfgPanel);
        layout.Controls.Add(grpConfig, 0, 0);

        // 2. Verify & Restore
        var grpRestore = new GroupControl { Text = "Verify & Restore Database", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
        var rstPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2, AutoSize = true };
        rstPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        rstPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        rstPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        rstPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        rstPanel.Controls.Add(new LabelControl { Text = "Backup File (.bak):", Anchor = AnchorStyles.Left }, 0, 0);
        _txtRestoreFile = new TextEdit { Dock = DockStyle.Fill };
        rstPanel.Controls.Add(_txtRestoreFile, 1, 0);

        var btnBrowseBak = new SimpleButton { Text = "Browse...", Width = 90 };
        btnBrowseBak.Click += (_, _) =>
        {
            using var ofd = new OpenFileDialog { Filter = "SQL Backup Files (*.bak)|*.bak|All Files (*.*)|*.*" };
            if (ofd.ShowDialog(this) == DialogResult.OK) _txtRestoreFile.Text = ofd.FileName;
        };
        rstPanel.Controls.Add(btnBrowseBak, 2, 0);

        _btnVerify = new SimpleButton { Text = "Verify Integrity", Width = 110 };
        _btnVerify.Click += async (_, _) => await VerifyBackupAsync();
        rstPanel.Controls.Add(_btnVerify, 3, 0);

        rstPanel.Controls.Add(new LabelControl { Text = "Target Database:", Anchor = AnchorStyles.Left }, 0, 1);
        _txtTargetDatabase = new TextEdit { Dock = DockStyle.Fill };
        rstPanel.Controls.Add(_txtTargetDatabase, 1, 1);

        _chkSafetyBackup = new CheckEdit { Text = "Pre-restore safety backup", Checked = true };
        rstPanel.Controls.Add(_chkSafetyBackup, 2, 1);

        _btnRestore = new SimpleButton { Text = "Restore Database", Width = 120 };
        _btnRestore.Click += async (_, _) => await RestoreDatabaseAsync();
        rstPanel.Controls.Add(_btnRestore, 3, 1);

        grpRestore.Controls.Add(rstPanel);
        layout.Controls.Add(grpRestore, 0, 1);

        // 3. History & Log Output
        var lblHist = new LabelControl { Text = "Backup, Verification & Restore History / Activity Log:", Padding = new Padding(0, 6, 0, 2) };
        layout.Controls.Add(lblHist, 0, 2);

        _memoOutput = new MemoEdit { Dock = DockStyle.Fill };
        _memoOutput.Properties.ReadOnly = true;
        _memoOutput.Properties.Appearance.Font = new Font("Consolas", 9F);
        _memoOutput.Properties.Appearance.Options.UseFont = true;
        _memoOutput.Properties.ScrollBars = ScrollBars.Both;
        _memoOutput.Properties.WordWrap = false;
        layout.Controls.Add(_memoOutput, 0, 3);

        Controls.Add(layout);
    }

    private void LoadConfigurationAndHistory()
    {
        var cfg = _configStore.Load();
        _txtBackupDir.Text = cfg.BackupDirectory ?? string.Empty;
        _chkScheduled.Checked = cfg.ScheduledBackupEnabled;
        _spinIntervalHours.Value = Math.Max(1, cfg.ScheduledBackupIntervalHours);

        try
        {
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_connectionString);
            _txtTargetDatabase.Text = string.IsNullOrWhiteSpace(builder.InitialCatalog)
                ? DatabaseConnectionSettings.DefaultDatabaseName
                : builder.InitialCatalog;
        }
        catch
        {
            _txtTargetDatabase.Text = DatabaseConnectionSettings.DefaultDatabaseName;
        }

        RefreshHistoryView();
    }

    private void SaveSettings()
    {
        var cfg = _configStore.Load();
        cfg.BackupDirectory = string.IsNullOrWhiteSpace(_txtBackupDir.Text) ? null : _txtBackupDir.Text.Trim();
        cfg.ScheduledBackupEnabled = _chkScheduled.Checked;
        cfg.ScheduledBackupIntervalHours = Convert.ToInt32(_spinIntervalHours.Value);
        _configStore.Save(cfg);

        XtraMessageBox.Show(this, "Backup configuration saved.", "Settings Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task CreateBackupAsync()
    {
        SetBusy(true);
        try
        {
            var dir = string.IsNullOrWhiteSpace(_txtBackupDir.Text) ? null : _txtBackupDir.Text.Trim();
            var (success, path, msg) = await _backupService.CreateBackupAsync(_connectionString, dir).ConfigureAwait(true);
            if (success && string.IsNullOrWhiteSpace(_txtRestoreFile.Text))
            {
                _txtRestoreFile.Text = path;
            }

            RefreshHistoryView();
            XtraMessageBox.Show(this, msg, success ? "Backup Succeeded" : "Backup Failed",
                MessageBoxButtons.OK, success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task VerifyBackupAsync()
    {
        var path = _txtRestoreFile.Text.Trim();
        if (string.IsNullOrWhiteSpace(path))
        {
            XtraMessageBox.Show(this, "Please select a .bak file to verify.", "Backup File Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SetBusy(true);
        try
        {
            var res = await _backupService.VerifyBackupAsync(_connectionString, path).ConfigureAwait(true);
            RefreshHistoryView();
            var details = res.Success
                ? $"{res.Message}\n\nSource Database: {res.DatabaseName ?? "Unknown"}\nServer: {res.ServerName ?? "Unknown"}\nFinish Date: {res.BackupFinishDateUtc?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Unknown"}\nCOPY_ONLY: {res.IsCopyOnly}"
                : res.Message;

            XtraMessageBox.Show(this, details, res.Success ? "Backup Verified" : "Verification Failed",
                MessageBoxButtons.OK, res.Success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task RestoreDatabaseAsync()
    {
        var path = _txtRestoreFile.Text.Trim();
        var targetDb = _txtTargetDatabase.Text.Trim();
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(targetDb))
        {
            XtraMessageBox.Show(this, "Please specify both a backup file and a target database name.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var confirm = XtraInputBox.Show(
            $"WARNING: Restoring '{targetDb}' from '{Path.GetFileName(path)}' will replace all data in '{targetDb}'.\n\nType the target database name '{targetDb}' to confirm:",
            "Confirm Database Restore",
            string.Empty);

        if (!string.Equals(confirm?.Trim(), targetDb, StringComparison.OrdinalIgnoreCase))
        {
            XtraMessageBox.Show(this, "Restore cancelled: confirmation text did not match target database name.", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SetBusy(true);
        try
        {
            var res = await _backupService.RestoreDatabaseAsync(
                _connectionString,
                path,
                targetDb,
                _chkSafetyBackup.Checked).ConfigureAwait(true);

            RefreshHistoryView();
            XtraMessageBox.Show(this, res.Message, res.Success ? "Restore Succeeded" : "Restore Failed",
                MessageBoxButtons.OK, res.Success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void RefreshHistoryView()
    {
        var entries = _historyStore.LoadAll();
        var lines = entries.Select(e =>
            $"[{e.TimestampUtc:yyyy-MM-dd HH:mm:ss} UTC] {e.Operation,-7} | {(e.Success ? "OK  " : "FAIL")} | Verified:{(e.Verified ? "Yes" : "No "),-3} | DB:{e.DatabaseName} | {e.BackupPath} | {e.Message}");
        _memoOutput.Text = entries.Count == 0
            ? "No backup, verification, or restore operations recorded yet."
            : string.Join(Environment.NewLine, lines);
    }

    private void SetBusy(bool busy)
    {
        _btnBackupNow.Enabled = !busy;
        _btnVerify.Enabled = !busy;
        _btnRestore.Enabled = !busy;
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }
}
