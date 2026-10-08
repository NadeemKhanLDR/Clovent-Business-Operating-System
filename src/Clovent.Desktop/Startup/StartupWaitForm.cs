using System;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraWaitForm;
using Clovent.Desktop.Forms.Base;

namespace Clovent.Desktop.Startup;

/// <summary>
/// Application startup and module transition wait form.
/// Replaces DevExpress's fixed 246px-wide DemoWaitForm with a high-DPI-aware,
/// properly scaled surface sized to accommodate full titles (such as
/// "Clovent Business Operating System") without truncation.
/// </summary>
public partial class StartupWaitForm : WaitForm
{
    /// <summary>Builds the startup wait form.</summary>
    public StartupWaitForm()
    {
        InitializeComponent();
        progressPanel.AutoHeight = true;
    }

    /// <inheritdoc/>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (DesignModeHelper.IsInDesignMode)
        {
            return;
        }

        ApplyDpiScaling();
    }

    private void ApplyDpiScaling()
    {
        int scaledWidth = DesktopDpi.Scale(460, this);
        int scaledHeight = DesktopDpi.Scale(84, this);
        MinimumSize = new Size(scaledWidth, scaledHeight);
        ClientSize = new Size(scaledWidth, scaledHeight);
        progressPanel.ImageHorzOffset = DesktopDpi.Scale(16, this);
        AdjustWidthForText();
    }

    /// <inheritdoc/>
    public override void SetCaption(string caption)
    {
        base.SetCaption(caption);
        progressPanel.Caption = caption;
        AdjustWidthForText();
    }

    /// <inheritdoc/>
    public override void SetDescription(string description)
    {
        base.SetDescription(description);
        progressPanel.Description = description;
        AdjustWidthForText();
    }

    /// <inheritdoc/>
    public override void ProcessCommand(Enum cmd, object arg)
    {
        base.ProcessCommand(cmd, arg);
    }

    private void AdjustWidthForText()
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        var captionFont = progressPanel.AppearanceCaption.Font ?? Font;
        var descFont = progressPanel.AppearanceDescription.Font ?? Font;

        int captionW = TextRenderer.MeasureText(progressPanel.Caption ?? string.Empty, captionFont).Width;
        int descW = TextRenderer.MeasureText(progressPanel.Description ?? string.Empty, descFont).Width;
        int maxTextWidth = Math.Max(captionW, descW);

        // Account for ring indicator (36px), spacing, and padding
        int requiredWidth = maxTextWidth + DesktopDpi.Scale(100, this);
        int currentMin = DesktopDpi.Scale(460, this);
        int targetWidth = Math.Max(currentMin, requiredWidth);

        if (ClientSize.Width < targetWidth)
        {
            ClientSize = new Size(targetWidth, ClientSize.Height);
        }
    }
}
