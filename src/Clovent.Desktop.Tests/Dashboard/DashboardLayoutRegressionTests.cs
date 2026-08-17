using System.Drawing;
using System.Reflection;
using Clovent.Desktop.Forms.Dashboard;
using DevExpress.XtraEditors;
using Xunit;

namespace Clovent.Desktop.Tests.Dashboard;

/// <summary>
/// Layout regression guard for the Dashboard's Business-Context strip
/// (the row of "Current Organization / Current Company / Current Branch /
/// Current Fiscal Year / Current User" captions and their bold values).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DashboardView"/>'s <c>tlpContext</c> previously used two
/// fixed <c>RowStyle(SizeType.Absolute, 24F/32F)</c> rows. Under
/// <c>ApplicationHighDpiMode=PerMonitorV2</c> the DevExpress
/// <c>LabelControl</c> fonts scale with the monitor, but absolute pixel
/// row heights do not, so at &gt;=125% DPI the caption/value text grew
/// taller than its cell and was overpainted by the next row's label - and
/// at higher DPI by the KPI card region below it. The visible symptom was
/// "the captions are clipped behind the KPI cards."
/// </para>
/// <para>
/// These tests force a real WinForms layout (instantiating the actual
/// <see cref="DashboardView"/> via its parameterless Designer constructor,
/// setting a real <c>Size</c>/<c>Handle</c>, calling
/// <see cref="Control.PerformLayout"/>) and assert, at three font scales
/// (100/150/250%), that each context-row label's preferred height fits
/// inside its allocated row - i.e. the rows scale with the font. They also
/// assert the KPI card grid (<c>tlpKpi</c>) never overlaps the context
/// strip (<c>tlpContext</c>).
/// </para>
/// </remarks>
public class DashboardLayoutRegressionTests
{
    [Theory]
    [InlineData(1.0f, "100%")]
    [InlineData(1.5f, "150%")]
    [InlineData(2.5f, "250%")]
    public void ContextStrip_Rows_Grow_With_Font_And_Never_Overflow(float fontScale, string label)
    {
        using var view = new DashboardView();
        if (fontScale != 1.0f)
        {
            ScaleAppearanceFonts(view, fontScale);
        }

        ForceLayout(view, new Size(1600, 1000));

        var tlpContext = Field<TableLayoutPanel>(view, "tlpContext");
        var tlpKpi = Field<TableLayoutPanel>(view, "tlpKpi");
        var rowHeights = tlpContext.GetRowHeights();

        // No vertical overlap between the context strip and the KPI grid.
        var ctxScreen = tlpContext.Parent!.RectangleToScreen(tlpContext.Bounds);
        var kpiScreen = tlpKpi.Parent!.RectangleToScreen(tlpKpi.Bounds);
        Assert.True(
            kpiScreen.Top >= ctxScreen.Bottom,
            $"{label}: KPI grid (top={kpiScreen.Top}) overlaps context strip (bottom={ctxScreen.Bottom}).");

        // Every caption/value label must fit inside its own row height, so it
        // never overflows into the neighbouring row and gets overpainted.
        AssertLabelFitsRow(view, tlpContext, rowHeights, 0, "lblOrganizationCaption", label);
        AssertLabelFitsRow(view, tlpContext, rowHeights, 0, "lblCompanyCaption", label);
        AssertLabelFitsRow(view, tlpContext, rowHeights, 0, "lblBranchCaption", label);
        AssertLabelFitsRow(view, tlpContext, rowHeights, 0, "lblFiscalYearCaption", label);
        AssertLabelFitsRow(view, tlpContext, rowHeights, 0, "lblCurrentUserCaption", label);
        AssertLabelFitsRow(view, tlpContext, rowHeights, 1, "lblOrganizationValue", label);
        AssertLabelFitsRow(view, tlpContext, rowHeights, 1, "lblCompanyValue", label);
        AssertLabelFitsRow(view, tlpContext, rowHeights, 1, "lblBranchValue", label);
        AssertLabelFitsRow(view, tlpContext, rowHeights, 1, "lblFiscalYearValue", label);
        AssertLabelFitsRow(view, tlpContext, rowHeights, 1, "lblCurrentUserValue", label);
    }

    private static void AssertLabelFitsRow(DashboardView view, TableLayoutPanel tlpContext, int[] rowHeights, int rowIndex, string name, string label)
    {
        var lab = Field<LabelControl>(view, name);
        var pref = lab.GetPreferredSize(new Size(tlpContext.Width, 0)).Height;
        var rowH = rowHeights[rowIndex];
        Assert.True(
            rowH >= pref,
            $"{label}: {name} needs {pref}px but row {rowIndex} is only {rowH}px (text would overflow and be overpainted).");
    }

    private static void ForceLayout(Control c, Size size)
    {
        c.Size = size;
        try { _ = c.Handle; } catch { }
        c.PerformLayout();
        Application.DoEvents();
        foreach (Control child in c.Controls)
        {
            ForceLayout(child, size);
        }
    }

    private static void ScaleAppearanceFonts(Control c, float scale)
    {
        const BindingFlags Bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (var prop in c.GetType().GetProperties(Bf))
        {
            if (prop.Name != "Appearance" || !prop.CanRead)
            {
                continue;
            }

            if (prop.GetValue(c) is { } app)
            {
                var fontProp = app.GetType().GetProperty("Font");
                if (fontProp?.GetValue(app) is Font f)
                {
                    fontProp.SetValue(app, new Font(f.FontFamily, f.SizeInPoints * scale, f.Style));
                }
            }
        }

        foreach (Control child in c.Controls)
        {
            ScaleAppearanceFonts(child, scale);
        }
    }

    private static T Field<T>(object obj, string name)
    {
        const BindingFlags Bf = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        return (T)obj.GetType().GetField(name, Bf)!.GetValue(obj)!;
    }
}
