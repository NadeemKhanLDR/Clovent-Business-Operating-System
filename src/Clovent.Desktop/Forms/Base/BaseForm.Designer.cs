using System.Windows.Forms;
using System.Drawing;

namespace Clovent.Desktop.Forms.Base;

partial class BaseForm
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    /// <remarks>
    /// Deliberately does NOT set <c>AutoScaleMode</c>/<c>AutoScaleDimensions</c> -
    /// combining WinForms' classic Font-based AutoScale with this app's
    /// <c>ApplicationHighDpiMode=PerMonitorV2</c> caused the Designer to
    /// recompute and permanently corrupt every literal coordinate in a
    /// derived screen's own Designer.cs the moment it was opened on a
    /// display running at a different scale than the baked-in baseline
    /// (confirmed on a 250% display). Relying on
    /// <c>ApplicationHighDpiMode=PerMonitorV2</c> alone (already declared in
    /// the csproj) plus DevExpress's own native per-monitor-DPI control
    /// rendering avoids this entirely - do not add these properties back to
    /// this file or any derived screen's Designer.cs.
    /// </remarks>
    private void InitializeComponent()
    {
        ToolbarPanel = new DevExpress.XtraEditors.PanelControl();
        ToolbarFlow = new FlowLayoutPanel();
        ContentPanel = new DevExpress.XtraEditors.PanelControl();
        BusyOverlayPanel = new DevExpress.XtraEditors.PanelControl();
        BusyProgressBar = new DevExpress.XtraEditors.ProgressBarControl();
        BusyStatusLabel = new DevExpress.XtraEditors.LabelControl();
        ((System.ComponentModel.ISupportInitialize)ToolbarPanel).BeginInit();
        ToolbarPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)ContentPanel).BeginInit();
        ((System.ComponentModel.ISupportInitialize)BusyOverlayPanel).BeginInit();
        BusyOverlayPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)BusyProgressBar.Properties).BeginInit();
        SuspendLayout();
        //
        // ToolbarPanel
        //
        ToolbarPanel.Controls.Add(ToolbarFlow);
        ToolbarPanel.Dock = DockStyle.Top;
        ToolbarPanel.Location = new Point(0, 0);
        ToolbarPanel.Name = "ToolbarPanel";
        ToolbarPanel.Dock = DockStyle.Top;
ToolbarPanel.Height = DesktopStyle.ToolbarHeight;
        ToolbarPanel.TabIndex = 0;
        //
        // ToolbarFlow
        //
        // A FlowLayoutPanel, not hand-placed Location/Size per button - the
        // same proven pattern MasterData.MasterDataListView.BuildLayout()
        // already uses (WrapContents=true so an overflowing toolbar wraps
        // to a second row on a narrow window instead of clipping buttons
        // off-screen with no way to reach them; AutoSize+SizeChanged drives
        // ToolbarPanel's own Height, since a fixed Height was previously
        // measured too short for a DevExpress SimpleButton's real rendered
        // height under some DPI/skin combinations).
        ToolbarFlow.Dock = DockStyle.Fill;
        ToolbarFlow.FlowDirection = FlowDirection.LeftToRight;
        ToolbarFlow.WrapContents = true;
        ToolbarFlow.AutoSize = true;
        ToolbarFlow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        ToolbarFlow.Padding = new Padding(DesktopStyle.ControlGap / 2);
        ToolbarFlow.Name = "ToolbarFlow";
        ToolbarFlow.TabIndex = 0;
        ToolbarFlow.SizeChanged += ToolbarFlow_SizeChanged;
        //
        // ContentPanel
        //
        ContentPanel.Dock = DockStyle.Fill;
        ContentPanel.Location = new Point(0, DesktopStyle.ToolbarHeight);
        ContentPanel.Name = "ContentPanel";
        ContentPanel.Size = new Size(1200, 556);
        ContentPanel.TabIndex = 1;
        //
        // BusyOverlayPanel
        //
        BusyOverlayPanel.Controls.Add(BusyProgressBar);
        BusyOverlayPanel.Controls.Add(BusyStatusLabel);
        BusyOverlayPanel.Dock = DockStyle.Fill;
        BusyOverlayPanel.Location = new Point(0, DesktopStyle.ToolbarHeight);
        BusyOverlayPanel.Name = "BusyOverlayPanel";
        BusyOverlayPanel.Size = new Size(1200, 556);
        BusyOverlayPanel.TabIndex = 2;
        BusyOverlayPanel.Visible = false;
        BusyOverlayPanel.Resize += BusyOverlayPanel_Resize;
        //
        // BusyProgressBar
        //
        // Anchor=None deliberately - it means "don't track any edge," not
        // "stay centered" (a common WinForms misconception). Centering is
        // instead recomputed by BusyOverlayPanel_Resize (BaseForm.cs)
        // whenever BusyOverlayPanel's size changes.
        BusyProgressBar.Anchor = AnchorStyles.None;
        BusyProgressBar.Location = new Point(500, 268);
        BusyProgressBar.Name = "BusyProgressBar";
        BusyProgressBar.Size = new Size(200, 18);
        BusyProgressBar.TabIndex = 0;
        //
        // BusyStatusLabel
        //
        BusyStatusLabel.Anchor = AnchorStyles.None;
        BusyStatusLabel.Appearance.Font = DesktopStyle.CaptionFont;
        BusyStatusLabel.Appearance.Options.UseFont = true;
        BusyStatusLabel.Appearance.Options.UseTextOptions = true;
        BusyStatusLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        BusyStatusLabel.Location = new Point(538, 292);
        BusyStatusLabel.Name = "BusyStatusLabel";
        BusyStatusLabel.Size = new Size(124, 20);
        BusyStatusLabel.TabIndex = 1;
        BusyStatusLabel.Text = "Working...";
        //
        // BaseForm
        //
        // Deliberately no AutoScaleMode/AutoScaleDimensions here - see the
        // class remarks above.
        Controls.Add(ContentPanel);
        Controls.Add(BusyOverlayPanel);
        Controls.Add(ToolbarPanel);
        Name = "BaseForm";
        Size = new Size(1200, 600);
        ((System.ComponentModel.ISupportInitialize)ToolbarPanel).EndInit();
        ToolbarPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)ContentPanel).EndInit();
        ((System.ComponentModel.ISupportInitialize)BusyProgressBar.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)BusyOverlayPanel).EndInit();
        BusyOverlayPanel.ResumeLayout(false);
        BusyOverlayPanel.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    /// <summary>Docked-top toolbar band. Subclasses never add controls directly here - use <see cref="ToolbarFlow"/>.</summary>
    protected DevExpress.XtraEditors.PanelControl ToolbarPanel;

    /// <summary>
    /// The toolbar's actual control flow - subclasses add their
    /// <c>SimpleButton</c>s/search <c>TextEdit</c> here (not to
    /// <see cref="ToolbarPanel"/> directly), so every screen gets the same
    /// wrap-on-overflow, self-sizing toolbar behavior for free.
    /// </summary>
    protected FlowLayoutPanel ToolbarFlow;

    /// <summary>Docked-fill content area. Subclasses add their own grid/controls here.</summary>
    protected DevExpress.XtraEditors.PanelControl ContentPanel;

    private DevExpress.XtraEditors.PanelControl BusyOverlayPanel;
    private DevExpress.XtraEditors.ProgressBarControl BusyProgressBar;
    private DevExpress.XtraEditors.LabelControl BusyStatusLabel;
}
