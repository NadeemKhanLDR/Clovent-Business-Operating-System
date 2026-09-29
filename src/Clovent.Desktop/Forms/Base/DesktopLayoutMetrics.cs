using System;
using System.Drawing;
using System.Windows.Forms;

namespace Clovent.Desktop.Forms.Base;

/// <summary>
/// Centralized layout metrics and logical pixel constants for CBOS desktop forms and dialogs.
/// All constants are expressed in standard 96-DPI logical pixels and scaled via <see cref="DesktopDpi"/>.
/// </summary>
public static class DesktopLayoutMetrics
{
    /// <summary>Outer padding around dialog content panels (16 logical pixels).</summary>
    public const int DialogPadding = 16;

    /// <summary>Vertical spacing between distinct visual sections or card groups (16 logical pixels).</summary>
    public const int SectionSpacing = 16;

    /// <summary>Vertical spacing between adjacent editor rows (8 logical pixels).</summary>
    public const int RowSpacing = 8;

    /// <summary>Standard baseline height for single-line text, combo, date, and spin editors (28 logical pixels).</summary>
    public const int EditorHeight = 28;

    /// <summary>Standard minimum width for primary/secondary dialog action buttons (85 logical pixels).</summary>
    public const int ButtonMinWidth = 85;

    /// <summary>Standard minimum width for prominent dialog action buttons such as 'Record Bulk Collection' or 'Create Customer' (110 logical pixels).</summary>
    public const int ProminentButtonMinWidth = 110;

    /// <summary>Standard height for dialog action buttons (30 logical pixels).</summary>
    public const int ButtonHeight = 30;

    /// <summary>Vertical breathing room between content and footer button panels (12 logical pixels).</summary>
    public const int FooterSpacing = 12;

    /// <summary>Standard minimum width for left-aligned form field labels (140 logical pixels).</summary>
    public const int StandardLabelWidth = 140;

    /// <summary>Standard height for multi-line notes / memo editors (110 logical pixels).</summary>
    public const int StandardMemoHeight = 110;

    /// <summary>Scales <paramref name="logicalPixels"/> using the reference control's current DPI.</summary>
    public static int Scale(int logicalPixels, Control reference) =>
        DesktopDpi.Scale(logicalPixels, reference);

    /// <summary>Creates a scaled uniform <see cref="Padding"/> from <paramref name="logicalPixels"/>.</summary>
    public static Padding CreatePadding(int logicalPixels, Control reference)
    {
        int scaled = DesktopDpi.Scale(logicalPixels, reference);
        return new Padding(scaled);
    }

    /// <summary>Creates a scaled rectangular <see cref="Padding"/> from logical pixel values.</summary>
    public static Padding CreatePadding(int left, int top, int right, int bottom, Control reference) =>
        new(
            DesktopDpi.Scale(left, reference),
            DesktopDpi.Scale(top, reference),
            DesktopDpi.Scale(right, reference),
            DesktopDpi.Scale(bottom, reference));
}
