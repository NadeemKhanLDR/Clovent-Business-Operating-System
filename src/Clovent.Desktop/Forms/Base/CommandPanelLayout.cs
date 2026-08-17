using DevExpress.XtraEditors;

namespace Clovent.Desktop.Forms.Base;

/// <summary>
/// Builds the "left command panel (search/actions/info) + content fills the
/// rest" shell every list-style screen in this app now uses - the
/// professional-ERP "navigation pane + content" shape (Outlook, SSMS,
/// DevExpress's own XAF demos), replacing the older toolbar-above-grid
/// strip. Shared by <c>MasterData.MasterDataListView{TDto}</c> (inline, not
/// this helper - it predates this extraction and is already verified) and
/// every hand-authored <c>BaseForm</c> list screen (Users/Roles/Products) so
/// the tricky <see cref="SplitContainer.SplitterDistance"/> initialization
/// workaround below is written and tested once, not per screen.
/// </summary>
public static class CommandPanelLayout
{
    /// <summary>Left command-panel width - wide enough for this DevExpress skin's real button chrome (see <see cref="AddCommandButton"/>) even at the smallest supported resolution, where it still leaves the content a large majority of the width.</summary>
    public const int Width = 240;

    /// <summary>
    /// Creates the <see cref="SplitContainer"/> shell, adds it to <paramref name="host"/>,
    /// docks <paramref name="content"/> (typically a <c>GridControl</c>) into
    /// its right panel, and returns the left panel's <see cref="FlowLayoutPanel"/>
    /// for the caller to add its own Search/Actions/Info controls to (via
    /// <see cref="BuildSectionHeading"/>/<see cref="AddCommandButton"/>).
    /// </summary>
    public static FlowLayoutPanel Build(Control host, Control content)
    {
        // FixedPanel=Panel1 means only Panel2 (the content) grows when the
        // window/screen resizes, so it always gets the majority of the
        // space at every supported resolution. Panel1MinSize/Panel2MinSize
        // are deliberately small (not CommandPanelLayout.Width, the actual
        // desired command-panel width - that's applied via SplitterDistance
        // below): SplitContainer enforces "Panel1MinSize <= SplitterDistance
        // <= Width - Panel2MinSize" from its own internal layout code on
        // every resize, not just when this class sets SplitterDistance, and
        // a live crash (confirmed via screenshot, MasterDataListView's
        // original implementation) showed a width transiently narrower than
        // a large min-size sum during a document's own layout is enough to
        // violate that constraint and crash the whole app with an unhandled
        // ArgumentOutOfRangeException raised from inside SplitContainer's
        // own code, not this class's - so a try/catch around this class's
        // own SplitterDistance assignment alone cannot fully guard it.
        // Small minimums here can't realistically be violated even by a
        // mid-layout transient width.
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            FixedPanel = FixedPanel.Panel1,
            Panel1MinSize = 40,
            Panel2MinSize = 40,
            SplitterWidth = 6,
        };

        var commandPanel = new PanelControl { Dock = DockStyle.Fill, Padding = new Padding(12, 8, 12, 8) };
        // AutoScroll as a safety net for screens with many extra action
        // buttons at a small window height - confirmed via screenshot to
        // actually produce a working scrollbar for a FlowLayoutPanel (unlike
        // DevExpress's PanelControl.AutoScroll, which does not - see
        // DashboardView.Designer.cs's scrollHost comment for that surprise).
        var commandFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
        };
        commandPanel.Controls.Add(commandFlow);

        split.Panel1.Controls.Add(commandPanel);
        split.Panel2.Controls.Add(content);
        host.Controls.Add(split);

        // SplitterDistance can't be set synchronously here: this control
        // tree is still width-0 at this point - nothing has sized it yet,
        // since whatever ultimately hosts it (a document tab, a screen's
        // ContentPanel) hasn't laid it out. Resize fires whenever the
        // container's size actually changes, including the first real
        // layout pass, so retrying from it (guarded to run only once,
        // and only once a valid width is available) reliably catches
        // the point where the constraint above can actually be satisfied.
        var splitterDistanceSet = false;
        split.Resize += (_, _) =>
        {
            if (splitterDistanceSet || split.Width < split.Panel1MinSize + split.Panel2MinSize)
            {
                return;
            }

            try
            {
                split.SplitterDistance = Width;
                splitterDistanceSet = true;
            }
            catch (ArgumentOutOfRangeException)
            {
                // Last-resort safety net: SplitContainer's own internal
                // layout can still call its SplitterDistance setter with a
                // stale width from paths this class doesn't control (e.g.
                // mid-resize during DevExpress's own document layout).
                // Leaving splitterDistanceSet false lets a later, valid
                // resize retry this - worst case the splitter briefly sits
                // at its framework default instead of Width, a cosmetic
                // gap, not the crash this replaces.
            }
        };

        return commandFlow;
    }

    /// <summary>A left-command-panel section label ("Search"/"Actions"/"Info"), styled like Dashboard's list-column captions for a consistent look across the app.</summary>
    public static LabelControl BuildSectionHeading(string text) => new()
    {
        Text = text,
        Appearance = { Font = DesktopStyle.SectionHeadingFont, Options = { UseFont = true } },
        // LabelControl's default AutoSizeMode ("Default") recomputes and
        // overrides an explicitly-assigned Height regardless of whether
        // AutoSize is set - confirmed via two live screenshots where
        // neither a bare Height=32 nor AutoSize=true stopped "Actions" from
        // overlapping the search box above it (the same root cause
        // LoginForm's tagline label hit earlier). AutoSizeMode.None is what
        // actually makes Height stick.
        AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None,
        Height = 32,
        Margin = new Padding(0, 10, 0, 6),
    };

    /// <summary>
    /// Adds one command button to the vertical left-panel stack. AutoSize +
    /// MinimumSize (not a fixed Size) so the button's real caption always
    /// fits regardless of DevExpress skin/DPI - a fixed width was
    /// confirmed, via live screenshots in an earlier pass, to clip captions
    /// like "Reset Password" under this app's active skin.
    /// </summary>
    public static void AddCommandButton(FlowLayoutPanel commandFlow, SimpleButton button)
    {
        button.AutoSize = true;
        button.MinimumSize = new Size(Width - 24, DesktopStyle.ToolbarControlHeight);
        button.Margin = new Padding(0, 0, 0, DesktopStyle.ControlGap / 2);
        commandFlow.Controls.Add(button);
    }
}
