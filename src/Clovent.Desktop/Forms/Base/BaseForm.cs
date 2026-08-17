using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Forms.Base;

/// <summary>
/// Base class for every business screen hosted as a document tab in
/// <c>Forms.Shell.MainForm</c>'s <c>DocumentManager</c>/<c>TabbedView</c>.
/// Inherits <see cref="XtraUserControl"/>, not <c>XtraForm</c> or
/// <see cref="Form"/> - DevExpress's non-MDI <c>TabbedView.AddDocument</c>
/// hosts a <see cref="Control"/> inside a tab; hosting an actual child
/// <see cref="Form"/> there would be the MDI-child pattern this application
/// does not use. Subclasses put their own controls inside
/// <see cref="ContentPanel"/> and their own toolbar buttons inside
/// <see cref="ToolbarFlow"/> (a self-wrapping <see cref="FlowLayoutPanel"/>,
/// never <see cref="ToolbarPanel"/> directly) - all declared in
/// <c>BaseForm.Designer.cs</c>, the reserved layout every subclass builds
/// on top of. <see cref="BusyOverlayPanel"/>/<see cref="BusyProgressBar"/>/
/// <see cref="BusyStatusLabel"/> back <see cref="SetBusy"/>/
/// <see cref="RunBusyAsync"/> and are not meant to be touched directly by
/// subclasses.
/// </summary>
/// <remarks>
/// <b>Visual Studio Designer compatibility.</b> Declared as a plain (not
/// <c>abstract</c>) class with a <c>protected</c> parameterless
/// constructor - never intended to be instantiated directly (every real
/// screen is a subclass), but the WinForms Designer must be able to
/// construct a class's immediate base type when opening a derived form's
/// designer surface, and it cannot do that if the base is <c>abstract</c>.
/// </remarks>
public partial class BaseForm : XtraUserControl
{
    /// <summary>Builds the reserved toolbar/content/busy-overlay layout every subclass shares.</summary>
    protected BaseForm()
    {
        InitializeComponent();

        // ToolbarFlow_SizeChanged (below) only fires when ToolbarFlow's own
        // size actually changes, which never happens for a screen (e.g.
        // DashboardView) that never adds anything to it - a subclass's own
        // InitializeComponent (which populates ToolbarFlow) runs *after*
        // this constructor returns, so ToolbarPanel is stuck at whatever
        // literal height BaseForm.Designer.cs gave it at the moment this
        // constructor ran, permanently, for any screen with an empty
        // toolbar. Confirmed via a live screenshot: Dashboard reserved a
        // large empty band below the document tabs for a toolbar it never
        // uses. Load fires once, after the full derived-class construction
        // chain (including the subclass's own InitializeComponent) has
        // finished, so by then ToolbarFlow's real, final child count is
        // known either way.
        Load += (_, _) => UpdateToolbarPanelSize();
    }

    /// <summary>Standard Designer-generated disposal of <c>components</c> (<c>BaseForm.Designer.cs</c>) - most subclasses declare their own shadowing <c>components</c> field and dispose it themselves, but this keeps <see cref="BaseForm"/> correct on its own.</summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// A permission/feature key identifying this screen, for documentation
    /// and future enforcement at the document-open level. Today's
    /// authorization gating happens entirely at the Ribbon-button-visibility
    /// layer (<c>Navigation.NavigationMenuBuilder</c>), unchanged by this
    /// property - it is an extension point, not yet consulted anywhere.
    /// </summary>
    public virtual string? PermissionKey => null;

    private bool _isDirty;

    /// <summary>Whether this screen has unsaved changes. Drives the default <see cref="ConfirmClose"/> behavior.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
    public bool IsDirty
    {
        get => _isDirty;
        protected set
        {
            if (_isDirty == value)
            {
                return;
            }

            _isDirty = value;
            DirtyChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Raised when <see cref="IsDirty"/> changes.</summary>
    public event EventHandler? DirtyChanged;

    private string _statusText = string.Empty;

    /// <summary>Free-text status shown in the Shell's status bar while this document is the active tab.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
    public string StatusText
    {
        get => _statusText;
        protected set
        {
            if (_statusText == value)
            {
                return;
            }

            _statusText = value;
            StatusTextChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Raised when <see cref="StatusText"/> changes.</summary>
    public event EventHandler? StatusTextChanged;

    /// <summary>Shows or hides the busy overlay, disabling <see cref="ToolbarPanel"/>/<see cref="ContentPanel"/> while busy.</summary>
    protected void SetBusy(bool busy, string? message = null)
    {
        BusyStatusLabel.Text = message ?? "Working...";
        BusyOverlayPanel.Visible = busy;
        if (busy)
        {
            BusyOverlayPanel.BringToFront();
        }

        ToolbarPanel.Enabled = !busy;
        ContentPanel.Enabled = !busy;
    }

    /// <summary>Runs <paramref name="action"/> with the busy overlay shown, always hiding it again afterward.</summary>
    protected async Task RunBusyAsync(Func<Task> action, string? message = null)
    {
        SetBusy(true, message);
        try
        {
            await action();
        }
        finally
        {
            SetBusy(false);
        }
    }

    /// <summary>Keeps <see cref="ToolbarPanel"/>'s height matched to its <see cref="ToolbarFlow"/>'s actual content height - the same technique <c>MasterData.MasterDataListView.BuildLayout()</c> uses, since a fixed toolbar height can clip a wrapped second row or a tall-DPI button.</summary>
    private void ToolbarFlow_SizeChanged(object? sender, EventArgs e) => UpdateToolbarPanelSize();

    /// <summary>
    /// Sizes <see cref="ToolbarPanel"/> to <see cref="ToolbarFlow"/>'s real
    /// content, and collapses it to nothing when the toolbar is empty (e.g.
    /// <c>DashboardView</c>, which has no toolbar buttons at all) instead of
    /// reserving space for a toolbar that will never draw anything - see
    /// this class's constructor for why both the <c>SizeChanged</c> path
    /// and an explicit <c>Load</c>-time call are both needed.
    /// </summary>
    private void UpdateToolbarPanelSize()
    {
        var hasContent = ToolbarFlow.Controls.Count > 0;
        ToolbarPanel.Visible = hasContent;
        ToolbarPanel.Height = hasContent ? ToolbarFlow.Height + ToolbarFlow.Padding.Vertical : 0;
    }

    /// <summary>Recenters the busy overlay's spinner/label whenever the overlay resizes - <c>Anchor=None</c> alone does not keep a control centered, it only means the control never tracks an edge.</summary>
    private void BusyOverlayPanel_Resize(object? sender, EventArgs e)
    {
        BusyProgressBar.Location = new Point(
            (BusyOverlayPanel.ClientSize.Width - BusyProgressBar.Width) / 2,
            (BusyOverlayPanel.ClientSize.Height - BusyProgressBar.Height) / 2 - 16);

        BusyStatusLabel.Location = new Point(
            (BusyOverlayPanel.ClientSize.Width - BusyStatusLabel.Width) / 2,
            BusyProgressBar.Bottom + 8);
    }

    /// <summary>
    /// Saves this screen's pending changes. Called by <c>MainForm</c> on
    /// Ctrl+S. No-op by default - most list/search screens save immediately
    /// per-command via <c>IMediator</c> and never go dirty, so the default
    /// is correct for them; override where a screen actually buffers edits.
    /// </summary>
    public virtual Task<bool> SaveAsync() => Task.FromResult(true);

    /// <summary>
    /// Reloads this screen's data. Called once by <c>MainForm</c> right
    /// after the document is first created (replacing the old
    /// <c>Load += async (_,_) => ...</c> pattern), and again on F5. No-op by
    /// default.
    /// </summary>
    public virtual Task RefreshAsync() => Task.CompletedTask;

    /// <summary>
    /// Whether this document may close now. Called by <c>MainForm</c> from
    /// <c>TabbedView.DocumentClosing</c>; returning <see langword="false"/>
    /// cancels the close. Default: prompt to save if <see cref="IsDirty"/>.
    /// The prompt (and, on "Yes", the save) runs synchronously because
    /// <c>DocumentClosing</c> is itself a synchronous WinForms event - the
    /// same accepted sync-over-async tradeoff <c>Program.cs</c> already
    /// takes during startup (<c>BuildAndInitializeAsync().GetAwaiter().GetResult()</c>).
    /// </summary>
    public virtual bool ConfirmClose()
    {
        if (!IsDirty)
        {
            return true;
        }

        var result = XtraMessageBox.Show(
            this,
            "Save changes before closing?",
            "Unsaved Changes",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Warning);

        return result switch
        {
            DialogResult.Yes => SaveAsync().GetAwaiter().GetResult(),
            DialogResult.No => true,
            _ => false,
        };
    }
}
