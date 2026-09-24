using System;
using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using DevExpress.Utils;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Restaurant.SmartPos;

/// <summary>
/// Small presentation helper ensuring consistent, touch-friendly, DPI-aware control dimensions
/// for Restaurant Smart POS Back Office screens and dialogs.
/// </summary>
public static class SmartPosControlSizing
{
    public const int StandardEditorHeight = 34;
    public const int StandardButtonHeight = 34;
    public const int LargeButtonHeight = 38;

    /// <summary>
    /// Configures a DevExpress editor with explicit height and AutoHeight disabled
    /// so that TableLayoutPanel / FlowLayoutPanel sizing calculations do not collapse it.
    /// Scaled by control DPI.
    /// </summary>
    public static void ConfigureEditor(BaseEdit editor, int minWidth, int height = StandardEditorHeight)
    {
        int scaledW = DesktopDpi.Scale(minWidth, editor);
        int scaledH = DesktopDpi.Scale(height, editor);
        editor.Properties.AutoHeight = false;
        editor.Size = new Size(scaledW, scaledH);
        editor.MinimumSize = new Size(scaledW, scaledH);
        editor.Properties.Appearance.TextOptions.VAlignment = VertAlignment.Center;
    }

    /// <summary>
    /// Configures a DevExpress SimpleButton with guaranteed minimum dimensions
    /// and ensures button width is at least as wide as the rendered text + padding.
    /// Scaled by control DPI so that button text is never vertically or horizontally clipped.
    /// </summary>
    public static void ConfigureButton(SimpleButton button, int minWidth, int height = StandardButtonHeight)
    {
        button.AutoSize = false;
        int scaledMinW = DesktopDpi.Scale(minWidth, button);
        int scaledH = DesktopDpi.Scale(height, button);
        var bestSize = button.CalcBestSize();
        int finalWidth = Math.Max(scaledMinW, bestSize.Width + DesktopDpi.Scale(16, button));
        int finalHeight = Math.Max(scaledH, bestSize.Height);
        button.Size = new Size(finalWidth, finalHeight);
        button.MinimumSize = new Size(finalWidth, finalHeight);
    }
}
