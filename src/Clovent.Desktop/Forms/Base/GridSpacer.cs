namespace Clovent.Desktop.Forms.Base;

/// <summary>
/// A thin docked-top spacer separating a filter/selector row from the grid
/// beneath it (~10 logical px, DPI-scaled once the handle exists) - the
/// "selector row touches the grid directly" defect from the Dining Areas
/// screenshot. Shared so every selector-over-grid screen gets the same gap.
/// </summary>
public sealed class GridSpacer : Control
{
    private const int LogicalHeight = 10;

    public GridSpacer()
    {
        Dock = DockStyle.Top;
        Height = LogicalHeight;
        TabStop = false;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Height = DesktopDpi.Scale(LogicalHeight, this);
    }
}
