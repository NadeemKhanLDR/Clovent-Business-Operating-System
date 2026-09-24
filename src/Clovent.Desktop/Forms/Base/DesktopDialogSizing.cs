using System;
using System.Drawing;
using System.Windows.Forms;

namespace Clovent.Desktop.Forms.Base;

/// <summary>
/// Reusable DPI-aware modal dialog sizing and placement helper for CBOS desktop forms.
/// Ensures dialogs start at comfortable operational dimensions, enforce minimum bounds,
/// clamp safely within the active monitor's working area, and center over their parent.
/// </summary>
public static class DesktopDialogSizing
{
    /// <summary>
    /// Applies high-DPI scaling, minimum size enforcement, working area clamping,
    /// and parent centering to <paramref name="form"/>.
    /// </summary>
    /// <param name="form">The dialog form being configured.</param>
    /// <param name="preferredLogicalWidth">Preferred width in 96-DPI logical pixels.</param>
    /// <param name="preferredLogicalHeight">Preferred height in 96-DPI logical pixels.</param>
    /// <param name="minimumLogicalWidth">Minimum width in 96-DPI logical pixels.</param>
    /// <param name="minimumLogicalHeight">Minimum height in 96-DPI logical pixels.</param>
    /// <param name="owner">Optional owner control or form for DPI context.</param>
    /// <param name="resizable">Whether to set <see cref="FormBorderStyle.Sizable"/> with maximize enabled.</param>
    public static void Apply(
        Form form,
        int preferredLogicalWidth,
        int preferredLogicalHeight,
        int minimumLogicalWidth,
        int minimumLogicalHeight,
        Control? owner = null,
        bool resizable = false)
    {
        ArgumentNullException.ThrowIfNull(form);

        var reference = owner ?? form;
        var scale = (int px) => DesktopDpi.Scale(px, reference);

        var targetW = scale(preferredLogicalWidth);
        var targetH = scale(preferredLogicalHeight);
        var minW = scale(minimumLogicalWidth);
        var minH = scale(minimumLogicalHeight);

        // Retrieve active screen working area
        var screen = Screen.FromControl(reference);
        var workArea = screen.WorkingArea;

        // Clamp to 96% of working area to guarantee borders, title bar, and taskbar stay accessible
        int maxAllowedW = Math.Max(minW, (int)(workArea.Width * 0.96));
        int maxAllowedH = Math.Max(minH, (int)(workArea.Height * 0.96));

        int finalW = Math.Clamp(targetW, minW, maxAllowedW);
        int finalH = Math.Clamp(targetH, minH, maxAllowedH);

        form.StartPosition = FormStartPosition.CenterParent;
        form.MinimizeBox = false;
        form.ShowInTaskbar = false;

        if (resizable)
        {
            form.FormBorderStyle = FormBorderStyle.Sizable;
            form.MaximizeBox = true;
        }
        else
        {
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.MaximizeBox = false;
        }

        form.MinimumSize = new Size(Math.Min(minW, maxAllowedW), Math.Min(minH, maxAllowedH));
        form.ClientSize = new Size(finalW, finalH);

        // Guarantee dialog is always centered over owner or active screen, never stranded in top-left
        form.Load += (s, e) => CenterOnOwnerOrScreen(form, owner);
    }

    /// <summary>
    /// Centers <paramref name="form"/> precisely over <paramref name="owner"/> or the active monitor's
    /// working area, clamping within screen bounds so title bar and buttons are always visible.
    /// </summary>
    public static void CenterOnOwnerOrScreen(Form form, Control? owner = null)
    {
        ArgumentNullException.ThrowIfNull(form);

        Rectangle parentBounds;
        if (owner != null && owner.Visible && owner.IsHandleCreated)
        {
            parentBounds = owner.RectangleToScreen(owner.ClientRectangle);
        }
        else if (form.Owner != null && form.Owner.Visible && form.Owner.IsHandleCreated)
        {
            parentBounds = form.Owner.RectangleToScreen(form.Owner.ClientRectangle);
        }
        else
        {
            var screenForForm = Screen.FromControl(form) ?? Screen.PrimaryScreen;
            parentBounds = screenForForm?.WorkingArea ?? new Rectangle(0, 0, 1024, 768);
        }

        var screen = Screen.FromRectangle(parentBounds) ?? Screen.PrimaryScreen;
        var workArea = screen?.WorkingArea ?? parentBounds;

        int x = parentBounds.Left + (parentBounds.Width - form.Width) / 2;
        int y = parentBounds.Top + (parentBounds.Height - form.Height) / 2;

        x = Math.Clamp(x, workArea.Left, Math.Max(workArea.Left, workArea.Right - form.Width));
        y = Math.Clamp(y, workArea.Top, Math.Max(workArea.Top, workArea.Bottom - form.Height));

        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(x, y);
    }
}
