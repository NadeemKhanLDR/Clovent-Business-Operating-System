using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.Drawing.Printing;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraPrinting;

namespace Clovent.Desktop.Forms.Base;

/// <summary>
/// Professional reporting, printing, and preview service for CBOS GridControls.
/// Enforces consistent headers/footers, landscape/portrait orientation, A4 paper fit,
/// and selective column printing for overly wide datasets.
/// </summary>
public static class GridReportingPrintService
{
    /// <summary>
    /// Displays a high-quality print preview dialog with custom header, footer,
    /// auto-fit, and optional printable column filtering.
    /// </summary>
    public static void ShowPreview(
        GridControl grid,
        GridView view,
        string reportTitle,
        string periodOrAsOfText,
        bool landscape = true,
        string[]? visiblePrintColumns = null,
        IWin32Window? owner = null)
    {
        using var ps = new PrintingSystem();
        using var link = CreateConfiguredLink(ps, grid, view, reportTitle, periodOrAsOfText, landscape);

        ExecuteWithPrintableColumns(view, visiblePrintColumns, () =>
        {
            link.CreateDocument();
            link.ShowPreviewDialog(owner);
        });
    }

    /// <summary>
    /// Convenience overload using grid.MainView as the GridView.
    /// </summary>
    public static void ShowPreview(
        GridControl grid,
        string reportTitle,
        string periodOrAsOfText,
        IWin32Window? owner = null,
        bool landscape = true,
        string[]? visiblePrintColumns = null)
    {
        if (grid.MainView is GridView gv)
        {
            ShowPreview(grid, gv, reportTitle, periodOrAsOfText, landscape, visiblePrintColumns, owner);
        }
    }

    /// <summary>
    /// Opens the print dialog for direct printing.
    /// </summary>
    public static void Print(
        GridControl grid,
        GridView view,
        string reportTitle,
        string periodOrAsOfText,
        bool landscape = true,
        string[]? visiblePrintColumns = null)
    {
        using var ps = new PrintingSystem();
        using var link = CreateConfiguredLink(ps, grid, view, reportTitle, periodOrAsOfText, landscape);

        ExecuteWithPrintableColumns(view, visiblePrintColumns, () =>
        {
            link.CreateDocument();
            link.PrintDlg();
        });
    }

    /// <summary>
    /// Convenience overload using grid.MainView as the GridView.
    /// </summary>
    public static void Print(
        GridControl grid,
        string reportTitle,
        string periodOrAsOfText,
        IWin32Window? owner = null,
        bool landscape = true,
        string[]? visiblePrintColumns = null)
    {
        if (grid.MainView is GridView gv)
        {
            Print(grid, gv, reportTitle, periodOrAsOfText, landscape, visiblePrintColumns);
        }
    }

    /// <summary>
    /// Exports the report to PDF with professional styling to a specified path.
    /// </summary>
    public static void ExportToPdf(
        GridControl grid,
        GridView view,
        string reportTitle,
        string periodOrAsOfText,
        string filePath,
        bool landscape = true,
        string[]? visiblePrintColumns = null)
    {
        using var ps = new PrintingSystem();
        using var link = CreateConfiguredLink(ps, grid, view, reportTitle, periodOrAsOfText, landscape);

        ExecuteWithPrintableColumns(view, visiblePrintColumns, () =>
        {
            link.CreateDocument();
            link.ExportToPdf(filePath);
        });
    }

    /// <summary>
    /// Prompts user with SaveFileDialog and exports to PDF with professional styling.
    /// </summary>
    public static void ExportToPdf(
        GridControl grid,
        string reportTitle,
        string periodOrAsOfText,
        IWin32Window? owner = null,
        bool landscape = true,
        string[]? visiblePrintColumns = null)
    {
        if (grid.MainView is not GridView gv) return;

        var cleanName = string.Join("_", reportTitle.Split(System.IO.Path.GetInvalidFileNameChars())) + ".pdf";
        using var dialog = new SaveFileDialog
        {
            Filter = "PDF files (*.pdf)|*.pdf",
            FileName = cleanName
        };

        if (dialog.ShowDialog(owner) == DialogResult.OK)
        {
            ExportToPdf(grid, gv, reportTitle, periodOrAsOfText, dialog.FileName, landscape, visiblePrintColumns);
            XtraMessageBox.Show(owner, $"{reportTitle} exported to PDF successfully.", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private static PrintableComponentLink CreateConfiguredLink(
        PrintingSystem ps,
        GridControl grid,
        GridView view,
        string reportTitle,
        string periodOrAsOfText,
        bool landscape)
    {
        var link = new PrintableComponentLink(ps)
        {
            Component = grid,
            Landscape = landscape,
            PaperKind = DXPaperKind.A4,
            Margins = new System.Drawing.Printing.Margins(30, 30, 45, 45)
        };

        if (link.PageHeaderFooter is PageHeaderFooter phf)
        {
            phf.Header.Content.Clear();
            var headerRight = string.IsNullOrWhiteSpace(periodOrAsOfText)
                ? $"Generated: {BusinessDateTimeService.Instance.FormatDateTime(DateTimeOffset.UtcNow)}"
                : $"{periodOrAsOfText}\r\nGenerated: {BusinessDateTimeService.Instance.FormatDateTime(DateTimeOffset.UtcNow)}";

            phf.Header.Content.AddRange([
                $"CLOVENT BUSINESS OPERATING SYSTEM\r\n{reportTitle.ToUpperInvariant()}",
                string.Empty,
                headerRight
            ]);
            phf.Header.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            phf.Footer.Content.Clear();
            phf.Footer.Content.AddRange([
                periodOrAsOfText,
                string.Empty,
                "Page [Page # of Pages #]"
            ]);
            phf.Footer.Font = new Font("Segoe UI", 8.5F);
        }

        // Apply grid print options
        view.OptionsPrint.AutoWidth = true;
        view.OptionsPrint.EnableAppearanceEvenRow = true;
        view.OptionsPrint.PrintHeader = true;
        view.OptionsPrint.PrintFooter = true;
        view.OptionsPrint.AllowMultilineHeaders = true;

        view.AppearancePrint.HeaderPanel.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        view.AppearancePrint.HeaderPanel.TextOptions.WordWrap = WordWrap.Wrap;
        view.AppearancePrint.Row.Font = new Font("Segoe UI", 8F);
        view.AppearancePrint.FooterPanel.Font = new Font("Segoe UI", 8F, FontStyle.Bold);

        link.PrintingSystem.Document.AutoFitToPagesWidth = 1;

        return link;
    }

    private static void ExecuteWithPrintableColumns(
        GridView view,
        string[]? visiblePrintColumns,
        Action action)
    {
        if (visiblePrintColumns == null || visiblePrintColumns.Length == 0)
        {
            action();
            return;
        }

        var set = new HashSet<string>(visiblePrintColumns, StringComparer.OrdinalIgnoreCase);
        var savedPrintable = new Dictionary<GridColumn, DefaultBoolean>();

        foreach (GridColumn col in view.Columns)
        {
            savedPrintable[col] = col.OptionsColumn.Printable;
            col.OptionsColumn.Printable = set.Contains(col.FieldName)
                ? DefaultBoolean.True
                : DefaultBoolean.False;
        }

        try
        {
            action();
        }
        finally
        {
            foreach (var (col, printable) in savedPrintable)
            {
                col.OptionsColumn.Printable = printable;
            }
        }
    }
}
