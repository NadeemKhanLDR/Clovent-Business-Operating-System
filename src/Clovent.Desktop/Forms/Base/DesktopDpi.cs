namespace Clovent.Desktop.Forms.Base;

/// <summary>
/// Scales fixed 96-DPI-logical pixel constants by a control's actual DPI.
/// This app deliberately does not use WinForms <c>AutoScaleMode</c> (see
/// <c>BaseForm.Designer.cs</c>'s remarks), so DevExpress skin fonts grow
/// with DPI while hard-coded pixel widths do not - the exact combination
/// behind every clipped-button/clipped-search-box screenshot in the
/// Restaurant UI audit. Every fixed pixel constant that must track the
/// skin's font size goes through here instead.
/// </summary>
public static class DesktopDpi
{
    /// <summary>Scales <paramref name="logicalPixels"/> (a 96-DPI value) to <paramref name="reference"/>'s actual DPI.</summary>
    public static int Scale(int logicalPixels, Control reference)
    {
        int dpi = reference?.DeviceDpi > 0 ? reference.DeviceDpi : 96;
        return Scale(logicalPixels, dpi);
    }

    /// <summary>Scales <paramref name="logicalPixels"/> (a 96-DPI value) to an explicit <paramref name="dpi"/>.</summary>
    public static int Scale(int logicalPixels, int dpi) =>
        (int)Math.Round(logicalPixels * (dpi > 0 ? dpi : 96) / 96.0);
}
