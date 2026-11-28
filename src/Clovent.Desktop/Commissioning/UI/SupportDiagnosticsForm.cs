using System.Windows.Forms;
using Clovent.Platform.Configuration;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Commissioning.UI;

/// <summary>
/// System and Support Information dialog showing non-sensitive diagnostics.
/// Implements Requirement 38. Safe for export and clipboard sharing with technical support engineers.
/// </summary>
public partial class SupportDiagnosticsForm : XtraForm
{
    private readonly SystemDiagnosticsService _diagnosticsService = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SupportDiagnosticsForm"/> class.
    /// </summary>
    public SupportDiagnosticsForm()
    {
        InitializeComponent();

        if (DesignMode || System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime)
        {
            return;
        }

        Load += SupportDiagnosticsForm_Load;
        btnCopy.Click += BtnCopy_Click;
        btnSave.Click += BtnSave_Click;
        btnRefresh.Click += BtnRefresh_Click;
    }

    private async void SupportDiagnosticsForm_Load(object? sender, EventArgs e)
    {
        await RefreshDiagnosticsAsync();
    }

    private async void BtnRefresh_Click(object? sender, EventArgs e)
    {
        await RefreshDiagnosticsAsync();
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(memoDiagnostics.Text))
            {
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Title = "Export System Support Diagnostics",
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                FileName = $"cbos-diagnostics-{DateTime.UtcNow:yyyyMMdd-HHmmss}.txt"
            };

            if (sfd.ShowDialog(this) == DialogResult.OK)
            {
                AtomicFileWriter.WriteStringAtomic(sfd.FileName, memoDiagnostics.Text);
                XtraMessageBox.Show(
                    this,
                    $"Diagnostics report exported to:\n{sfd.FileName}",
                    "Export Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show(
                this,
                $"Unable to export diagnostics report: {ex.Message}",
                "Export Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
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
        _ = RefreshDiagnosticsAsync();
    }

    /// <summary>
    /// Asynchronously compiles and displays all non-sensitive system diagnostics.
    /// </summary>
    public async Task RefreshDiagnosticsAsync()
    {
        btnRefresh.Enabled = false;
        try
        {
            var report = await _diagnosticsService.BuildDiagnosticsReportAsync(DeviceDpi);
            memoDiagnostics.Text = report;
            memoDiagnostics.SelectionStart = 0;
            memoDiagnostics.SelectionLength = 0;
        }
        finally
        {
            btnRefresh.Enabled = true;
        }
    }
}
