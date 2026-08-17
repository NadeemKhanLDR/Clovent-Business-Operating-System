using DevExpress.Utils;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;

namespace Clovent.Desktop.Forms.Base;

/// <summary>
/// Turns a plain "Active"/"Inactive" text column into a colored pill/badge -
/// tinted background, bold centered text - via <see cref="GridView.RowCellStyle"/>,
/// the standard DevExpress way to recolor a cell without a custom-draw
/// handler. Every Restaurant grid (Menu Items, Sales Summary) that shows a
/// Status column uses this so the badge look is identical everywhere,
/// instead of each screen picking its own colors.
/// </summary>
public static class StatusBadgeStyler
{
    private static readonly Color ActiveBack = Color.FromArgb(223, 240, 216);
    private static readonly Color ActiveFore = Color.FromArgb(39, 174, 96);
    private static readonly Color InactiveBack = Color.FromArgb(250, 224, 224);
    private static readonly Color InactiveFore = Color.FromArgb(192, 57, 43);

    /// <summary>
    /// Wires <paramref name="view"/>'s <see cref="GridView.RowCellStyle"/> so
    /// every cell in <paramref name="column"/> is tinted green ("Active"-like
    /// values) or red, based on <paramref name="isPositive"/> applied to the
    /// cell's own text.
    /// </summary>
    public static void Apply(GridView view, GridColumn column, Func<string, bool> isPositive)
    {
        view.RowCellStyle += (_, e) =>
        {
            if (e.Column != column || e.CellValue is not string value)
            {
                return;
            }

            var positive = isPositive(value);
            e.Appearance.BackColor = positive ? ActiveBack : InactiveBack;
            e.Appearance.ForeColor = positive ? ActiveFore : InactiveFore;
            e.Appearance.Options.UseBackColor = true;
            e.Appearance.Options.UseForeColor = true;
            e.Appearance.Font = new Font(e.Appearance.Font, FontStyle.Bold);
            e.Appearance.Options.UseFont = true;
            e.Appearance.TextOptions.HAlignment = HorzAlignment.Center;
            e.Appearance.Options.UseTextOptions = true;
        };
    }
}
