using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;

namespace Clovent.Desktop.Restaurant.SmartPos;

/// <summary>
/// Diagnostic layout telemetry helper for CBOS Smart POS Back Office screens.
/// Records exact geometry, parentage, DPI, and lifecycle events to qa\runtime_layout\.
/// </summary>
public static class SmartPosLayoutTelemetry
{
    private static readonly object FileLock = new();
    public static readonly string TelemetryDir = Path.Combine("D:\\Clovent Business Operating System", "qa", "runtime_layout");

    public static void EnsureDirectory()
    {
        if (!Directory.Exists(TelemetryDir))
        {
            Directory.CreateDirectory(TelemetryDir);
        }
    }

    public static void LogAppBuildIdentity()
    {
        try
        {
            EnsureDirectory();
            var path = Path.Combine(TelemetryDir, "app_build_identity.txt");
            var proc = System.Diagnostics.Process.GetCurrentProcess();
            var asm = typeof(SmartPosLayoutTelemetry).Assembly;
            var asmPath = asm.Location;
            var fileInfo = new FileInfo(asmPath);
            var fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(asmPath);

            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("CBOS RUNTIME APPLICATION BUILD IDENTITY");
            sb.AppendLine("================================================================================");
            sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            sb.AppendLine($"Process ID: {proc.Id}");
            sb.AppendLine($"Process Name: {proc.ProcessName}");
            sb.AppendLine($"Process MainModule: {proc.MainModule?.FileName}");
            sb.AppendLine($"Assembly Location: {asmPath}");
            sb.AppendLine($"Assembly LastWriteTime: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"FileVersion: {fvi.FileVersion}");
            sb.AppendLine($"ProductVersion: {fvi.ProductVersion}");
            sb.AppendLine($"AppContext.BaseDirectory: {AppContext.BaseDirectory}");
            sb.AppendLine($"CurrentDirectory: {Environment.CurrentDirectory}");
            sb.AppendLine($"OS Version: {Environment.OSVersion}");
            sb.AppendLine($"Is64BitProcess: {Environment.Is64BitProcess}");

            foreach (var s in Screen.AllScreens)
            {
                sb.AppendLine($"Screen '{s.DeviceName}': Bounds={s.Bounds}, WorkingArea={s.WorkingArea}, Primary={s.Primary}, BitsPerPixel={s.BitsPerPixel}");
            }

            lock (FileLock)
            {
                File.WriteAllText(path, sb.ToString());
            }
        }
        catch
        {
            // Best effort diagnostic
        }
    }

    public static void LogSmartComboEvent(
        Control view,
        string eventName,
        Control? periodLabel = null,
        Control? periodEdit = null,
        Control? locationLabel = null,
        Control? locationEdit = null,
        Control? analyzeBtn = null,
        Control? filterCard = null,
        Control? kpiCards = null,
        Control? grid = null)
    {
        try
        {
            EnsureDirectory();
            var path = Path.Combine(TelemetryDir, "smart_combo_REAL_runtime.txt");
            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] EVENT: {eventName}");
            sb.AppendLine($"  View: Type={view.GetType().FullName}, Name='{view.Name}', DeviceDpi={view.DeviceDpi}");
            sb.AppendLine($"  Bounds={view.Bounds}, ClientRect={view.ClientRectangle}, DisplayRect={view.DisplayRectangle}");
            sb.AppendLine($"  Font: {view.Font.Name} {view.Font.Size}pt, Style={view.Font.Style}");

            if (view is ContainerControl cc)
            {
                sb.AppendLine($"  Container: AutoScaleMode={cc.AutoScaleMode}, AutoScaleDimensions={cc.AutoScaleDimensions}, CurrentAutoScaleDimensions={cc.CurrentAutoScaleDimensions}");
            }

            var p = view.Parent;
            sb.AppendLine($"  Parent: Type={(p != null ? p.GetType().FullName : "null")}, Name='{p?.Name}', Bounds={(p != null ? p.Bounds.ToString() : "N/A")}, ClientRect={(p != null ? p.ClientRectangle.ToString() : "N/A")}");
            if (p != null)
            {
                sb.AppendLine($"  Parent Font: {p.Font.Name} {p.Font.Size}pt, Parent DeviceDpi={p.DeviceDpi}");
            }

            if (filterCard is PanelControl pc)
            {
                sb.AppendLine($"  FilterCard: Bounds={pc.Bounds}, ClientRect={pc.ClientRectangle}");
                if (pc.Controls.OfType<TableLayoutPanel>().FirstOrDefault() is { } tlp)
                {
                    sb.AppendLine($"  FilterTableLayout: Bounds={tlp.Bounds}, Rows={tlp.RowCount}, Cols={tlp.ColumnCount}");
                    for (int c = 0; c < tlp.ColumnCount; c++)
                    {
                        var cs = c < tlp.ColumnStyles.Count ? tlp.ColumnStyles[c] : null;
                        sb.AppendLine($"    Col[{c}]: SizeType={cs?.SizeType}, Width={cs?.Width}");
                    }
                    for (int r = 0; r < tlp.RowCount; r++)
                    {
                        var rs = r < tlp.RowStyles.Count ? tlp.RowStyles[r] : null;
                        sb.AppendLine($"    Row[{r}]: SizeType={rs?.SizeType}, Height={rs?.Height}");
                    }
                }
            }

            if (periodLabel != null) sb.AppendLine($"  PeriodLabel: Bounds={periodLabel.Bounds}, PreferredSize={periodLabel.PreferredSize}, Font={periodLabel.Font.Name} {periodLabel.Font.Size}pt");
            if (periodEdit != null) sb.AppendLine($"  PeriodEdit: Bounds={periodEdit.Bounds}, PreferredSize={periodEdit.PreferredSize}, Min={periodEdit.MinimumSize}, Max={periodEdit.MaximumSize}");
            if (locationLabel != null) sb.AppendLine($"  LocationLabel: Bounds={locationLabel.Bounds}, PreferredSize={locationLabel.PreferredSize}, Font={locationLabel.Font.Name} {locationLabel.Font.Size}pt");
            if (locationEdit != null) sb.AppendLine($"  LocationEdit: Bounds={locationEdit.Bounds}, PreferredSize={locationEdit.PreferredSize}, Min={locationEdit.MinimumSize}");
            if (analyzeBtn != null) sb.AppendLine($"  AnalyzeBtn: Bounds={analyzeBtn.Bounds}, PreferredSize={analyzeBtn.PreferredSize}, Min={analyzeBtn.MinimumSize}, Font={analyzeBtn.Font.Name} {analyzeBtn.Font.Size}pt");
            if (kpiCards != null) sb.AppendLine($"  KpiCards: Bounds={kpiCards.Bounds}, ClientRect={kpiCards.ClientRectangle}");
            if (grid != null) sb.AppendLine($"  ResultsGrid: Bounds={grid.Bounds}, ClientRect={grid.ClientRectangle}");

            sb.AppendLine("--------------------------------------------------------------------------------");

            lock (FileLock)
            {
                File.AppendAllText(path, sb.ToString());
            }
        }
        catch
        {
            // Best effort diagnostic
        }
    }

    public static void LogQuickOrderEvent(
        Form form,
        string eventName,
        Control? detailsPanel = null,
        Control? itemEditorPanel = null,
        Control? toolbar = null,
        Control? gridPanel = null,
        Control? footerPanel = null,
        Control? saveBtn = null,
        Control? cancelBtn = null)
    {
        try
        {
            EnsureDirectory();
            var path = Path.Combine(TelemetryDir, "quick_order_REAL_runtime.txt");
            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] EVENT: {eventName}");
            sb.AppendLine($"  Form: Type={form.GetType().FullName}, Title='{form.Text}', DeviceDpi={form.DeviceDpi}");
            sb.AppendLine($"  Form Bounds={form.Bounds}, ClientRect={form.ClientRectangle}, Size={form.Size}, ClientSize={form.ClientSize}, MinSize={form.MinimumSize}");
            sb.AppendLine($"  Font: {form.Font.Name} {form.Font.Size}pt, AutoScaleMode={form.AutoScaleMode}");

            var owner = form.Owner;
            sb.AppendLine($"  Owner: Type={(owner != null ? owner.GetType().FullName : "null")}, Name='{owner?.Name}', Bounds={(owner != null ? owner.Bounds.ToString() : "N/A")}");

            if (form.Controls.OfType<TableLayoutPanel>().FirstOrDefault() is { } mainTlp)
            {
                sb.AppendLine($"  MainTableLayout: Bounds={mainTlp.Bounds}, ClientRect={mainTlp.ClientRectangle}, Rows={mainTlp.RowCount}");
                for (int r = 0; r < mainTlp.RowCount; r++)
                {
                    var rs = r < mainTlp.RowStyles.Count ? mainTlp.RowStyles[r] : null;
                    sb.AppendLine($"    Row[{r}]: SizeType={rs?.SizeType}, Height={rs?.Height}");
                }
            }

            if (detailsPanel != null) sb.AppendLine($"  DetailsPanel: Bounds={detailsPanel.Bounds}, PreferredSize={detailsPanel.PreferredSize}");
            if (itemEditorPanel != null) sb.AppendLine($"  ItemEditorPanel: Bounds={itemEditorPanel.Bounds}, PreferredSize={itemEditorPanel.PreferredSize}");
            if (toolbar != null)
            {
                sb.AppendLine($"  Toolbar: Bounds={toolbar.Bounds}, PreferredSize={toolbar.PreferredSize}");
                foreach (Control c in toolbar.Controls)
                {
                    sb.AppendLine($"    ToolbarChild: Type={c.GetType().Name}, Text='{c.Text}', Bounds={c.Bounds}, Min={c.MinimumSize}, Font={c.Font.Name} {c.Font.Size}pt");
                }
            }
            if (gridPanel != null) sb.AppendLine($"  GridPanel: Bounds={gridPanel.Bounds}, ClientRect={gridPanel.ClientRectangle}");
            if (footerPanel != null) sb.AppendLine($"  FooterPanel: Bounds={footerPanel.Bounds}, PreferredSize={footerPanel.PreferredSize}");
            if (saveBtn != null) sb.AppendLine($"  SaveBtn: Bounds={saveBtn.Bounds}, PreferredSize={saveBtn.PreferredSize}, Min={saveBtn.MinimumSize}, Text='{saveBtn.Text}'");
            if (cancelBtn != null) sb.AppendLine($"  CancelBtn: Bounds={cancelBtn.Bounds}, PreferredSize={cancelBtn.PreferredSize}, Min={cancelBtn.MinimumSize}, Text='{cancelBtn.Text}'");

            sb.AppendLine("--------------------------------------------------------------------------------");

            lock (FileLock)
            {
                File.AppendAllText(path, sb.ToString());
            }
        }
        catch
        {
            // Best effort diagnostic
        }
    }
}
