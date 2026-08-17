using System.ComponentModel;
using Clovent.Desktop.Forms.Base;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;

namespace Clovent.Desktop.MasterData;

/// <summary>
/// One extra, entity-specific command button on a <see cref="MasterDataListView{TDto}"/>
/// toolbar (e.g. "Receive", "Issue", "Apply", "Complete") beyond the fixed
/// New/Edit/Activate/Deactivate set every screen already has - added for
/// Milestone 14 ("Product Catalog &amp; Inventory Foundation") screens whose
/// workflow needs more than a status toggle (<c>WarehouseStock</c>'s
/// Receive/Issue/Reserve/Release, <c>StockAdjustment</c>'s Apply,
/// <c>StockTransfer</c>'s Complete).
/// </summary>
/// <typeparam name="TDto">The row DTO type, matching the hosting <see cref="MasterDataListView{TDto}"/>.</typeparam>
/// <param name="Caption">The button's caption.</param>
/// <param name="Handler">Invoked with the focused row when clicked.</param>
/// <param name="IsEnabledFor">Whether the button should be enabled for the focused row (beyond the base "a row is focused" check) - e.g. only when a transfer is still Pending. Defaults to always-enabled.</param>
/// <param name="FeatureOperation">The operation name passed to <see cref="MasterDataListView{TDto}.CanUseFeatureAsync"/> to gate this button (e.g. <c>"receive"</c>, <c>"apply"</c>) - <see langword="null"/> to leave it ungated beyond the base New/Edit/Activate/Deactivate set.</param>
public sealed record MasterDataListAction<TDto>(string Caption, Func<TDto, Task> Handler, Func<TDto, bool>? IsEnabledFor = null, string? FeatureOperation = null);

/// <summary>
/// Shared grid+toolbar chrome for every master-data management screen
/// (Organization, Company, Branch, Department, Warehouse, Terminal, Fiscal
/// Year, Currency) - the "reusable grid+toolbar+edit-dialog pattern" this
/// milestone's Desktop deliverable calls for, so the chrome (search box,
/// New/Edit/Activate/Deactivate/Refresh buttons, grid wiring) is written
/// once rather than 8 times. A screen supplies its own load/search/CRUD
/// behavior via delegate properties and its own entity-specific edit
/// dialog (built on <see cref="MasterDataEditFormBase"/>) - this control
/// never itself constructs a dialog, since the fields differ per entity.
/// </summary>
/// <typeparam name="TDto">The Application-layer DTO record this screen lists (e.g. <c>OrganizationDto</c>).</typeparam>
[System.ComponentModel.DesignerCategory("Code")]
public sealed class MasterDataListView<TDto> : XtraUserControl
    where TDto : class
{
    /// <summary>Designer-only constructor - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public MasterDataListView() : this(System.Array.Empty<MasterDataColumn>(), null)
    {
    }
    private readonly GridControl _gridControl = new() { Dock = DockStyle.Fill };
    private readonly GridView _gridView = new();
    private readonly TextEdit _searchBox = new();
    private readonly SimpleButton _newButton = new() { Text = "New" };
    private readonly SimpleButton _editButton = new() { Text = "Edit" };
    private readonly SimpleButton _activateButton = new() { Text = "Activate" };
    private readonly SimpleButton _deactivateButton = new() { Text = "Deactivate" };
    private readonly SimpleButton _refreshButton = new() { Text = "Refresh" };
    private readonly SimpleButton _exportButton = new() { Text = "Export CSV", Visible = false };
    private readonly SimpleButton _importButton = new() { Text = "Import CSV", Visible = false };
    private readonly LabelControl _statusLabel = new() { Text = string.Empty };
    private readonly IReadOnlyList<MasterDataListAction<TDto>> _extraActions;
    private readonly List<SimpleButton> _extraActionButtons = [];

    private IReadOnlyList<TDto> _allItems = [];

    /// <summary>Loads the full, unfiltered set of rows. Required before <see cref="RefreshAsync"/> is called.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
    public Func<CancellationToken, Task<IReadOnlyList<TDto>>>? LoadItemsAsync { get; set; }

    /// <summary>Extracts the free-text search haystack for a row (e.g. its name). Required for the search box to filter.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
    public Func<TDto, string>? SearchTextSelector { get; set; }

    /// <summary>Extracts a row's lifecycle status string ("Active"/"Inactive") - gates whether Activate or Deactivate is enabled for the focused row.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
    public Func<TDto, string>? StatusSelector { get; set; }

    /// <summary>
    /// Checks whether the current user may perform <c>create</c>/<c>edit</c>/<c>activate</c>/<c>deactivate</c>
    /// on this screen - backed by <c>IFeatureAuthorizationPolicy</c> in practice.
    /// Every button defaults to enabled if this is left unset.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
    public Func<string, Task<bool>>? CanUseFeatureAsync { get; set; }

    /// <summary>Invoked when "New" is clicked. Should show the entity's own create dialog and return once done (whether saved or cancelled).</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
    public Func<Task>? OnNew { get; set; }

    /// <summary>Invoked when "Edit" is clicked with the focused row.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
    public Func<TDto, Task>? OnEdit { get; set; }

    /// <summary>Invoked when "Activate" is clicked with the focused row.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
    public Func<TDto, Task>? OnActivate { get; set; }

    /// <summary>Invoked when "Deactivate" is clicked with the focused row.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
    public Func<TDto, Task>? OnDeactivate { get; set; }

    private Func<IReadOnlyList<TDto>, Task>? _onExportCsv;
    private Func<Task>? _onImportCsv;

    /// <summary>
    /// Invoked when "Export CSV" is clicked, given every currently-visible
    /// (search-filtered) row. The "Export CSV" button only appears once this
    /// is set - screens without a natural tabular export (e.g. append-only
    /// ledgers) simply never set it.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
    public Func<IReadOnlyList<TDto>, Task>? OnExportCsv
    {
        get => _onExportCsv;
        set
        {
            _onExportCsv = value;
            _exportButton.Visible = value is not null;
        }
    }

    /// <summary>
    /// Invoked when "Import CSV" is clicked. Should show its own file picker
    /// and return once done (whether imported or cancelled); the caller
    /// refreshes the grid afterward. The "Import CSV" button only appears
    /// once this is set.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
    public Func<Task>? OnImportCsv
    {
        get => _onImportCsv;
        set
        {
            _onImportCsv = value;
            _importButton.Visible = value is not null;
        }
    }

    /// <summary>
    /// Overrides the "Activate" button's caption - e.g. Fiscal Year has no
    /// "Activate" concept (closing is one-way), so its screen leaves
    /// <see cref="OnActivate"/> unset and never shows this button as
    /// enabled; every other screen keeps the default "Activate" caption.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
    public string ActivateButtonText
    {
        get => _activateButton.Text;
        set => _activateButton.Text = value;
    }

    /// <summary>Overrides the "Deactivate" button's caption - e.g. "Close" for Fiscal Year.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
    public string DeactivateButtonText
    {
        get => _deactivateButton.Text;
        set => _deactivateButton.Text = value;
    }

    /// <summary>Builds the list view for the given columns.</summary>
    /// <param name="columns">The grid's columns.</param>
    /// <param name="extraActions">Entity-specific command buttons beyond New/Edit/Activate/Deactivate/Refresh, added in order after Refresh.</param>
    public MasterDataListView(IReadOnlyList<MasterDataColumn> columns, IReadOnlyList<MasterDataListAction<TDto>>? extraActions = null)
    {
        _extraActions = extraActions ?? [];

        Dock = DockStyle.Fill;

        _gridControl.MainView = _gridView;
        _gridControl.ViewCollection.Add(_gridView);
        _gridView.OptionsBehavior.Editable = false;
        _gridView.OptionsSelection.MultiSelect = false;
        _gridView.OptionsView.ShowGroupPanel = false;
        _gridView.OptionsView.ColumnAutoWidth = true;

        foreach (var column in columns)
        {
            var gridColumn = _gridView.Columns.AddVisible(column.FieldName, column.Caption);
            if (column.Width is { } width)
            {
                gridColumn.Width = width;
            }
        }

        _gridView.FocusedRowChanged += (_, _) => UpdateButtonStates();

        _searchBox.Properties.NullValuePrompt = "Search...";
        _searchBox.EditValueChanged += (_, _) => ApplyFilter();

        _newButton.Click += async (_, _) =>
        {
            if (OnNew is { } handler)
            {
                await handler();
                await RefreshAsync();
            }
        };

        _editButton.Click += async (_, _) =>
        {
            if (GetFocusedItem() is { } item && OnEdit is { } handler)
            {
                await handler(item);
                await RefreshAsync();
            }
        };

        _activateButton.Click += async (_, _) =>
        {
            if (GetFocusedItem() is { } item && OnActivate is { } handler)
            {
                await handler(item);
                await RefreshAsync();
            }
        };

        _deactivateButton.Click += async (_, _) =>
        {
            if (GetFocusedItem() is { } item && OnDeactivate is { } handler)
            {
                await handler(item);
                await RefreshAsync();
            }
        };

        _refreshButton.Click += async (_, _) => await RefreshAsync();

        _exportButton.Click += async (_, _) =>
        {
            if (OnExportCsv is { } handler)
            {
                await handler(MasterDataFilter.Apply(_allItems, _searchBox.Text, SearchTextSelector));
            }
        };

        _importButton.Click += async (_, _) =>
        {
            if (OnImportCsv is { } handler)
            {
                await handler();
                await RefreshAsync();
            }
        };

        foreach (var action in _extraActions)
        {
            var button = new SimpleButton { Text = action.Caption };
            button.Click += async (_, _) =>
            {
                if (GetFocusedItem() is { } item)
                {
                    await action.Handler(item);
                    await RefreshAsync();
                }
            };
            _extraActionButtons.Add(button);
        }

        BuildLayout();
    }

    /// <summary>Reloads every row via <see cref="LoadItemsAsync"/>, re-applies the current search filter, and re-evaluates feature permissions.</summary>
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (LoadItemsAsync is null)
        {
            return;
        }

        SetBusy(true);
        try
        {
            _allItems = await LoadItemsAsync(cancellationToken);
            ApplyFilter();
            await UpdateFeaturePermissionsAsync();
            UpdateButtonStates();
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ApplyFilter()
    {
        var visible = MasterDataFilter.Apply(_allItems, _searchBox.Text, SearchTextSelector);

        _gridControl.DataSource = visible.ToList();
        _statusLabel.Text = $"{visible.Count} of {_allItems.Count} record(s)";
        UpdateButtonStates();
    }

    private async Task UpdateFeaturePermissionsAsync()
    {
        if (CanUseFeatureAsync is null)
        {
            return;
        }

        _newButton.Enabled = await CanUseFeatureAsync("create");
        var canEdit = await CanUseFeatureAsync("edit");
        var canActivate = await CanUseFeatureAsync("activate");
        var canDeactivate = await CanUseFeatureAsync("deactivate");

        _editButton.Tag = canEdit;
        _activateButton.Tag = canActivate;
        _deactivateButton.Tag = canDeactivate;

        for (var i = 0; i < _extraActions.Count; i++)
        {
            _extraActionButtons[i].Tag = _extraActions[i].FeatureOperation is { } operation
                ? await CanUseFeatureAsync(operation)
                : (bool?)null;
        }
    }

    private void UpdateButtonStates()
    {
        var focused = GetFocusedItem();
        var hasFocusedRow = focused is not null;
        var status = focused is not null && StatusSelector is not null ? StatusSelector(focused) : null;

        var permittedEdit = _editButton.Tag as bool?;
        var permittedActivate = _activateButton.Tag as bool?;
        var permittedDeactivate = _deactivateButton.Tag as bool?;

        _editButton.Enabled = MasterDataFilter.CanEdit(hasFocusedRow, permittedEdit, OnEdit is not null);
        _activateButton.Enabled = MasterDataFilter.CanActivate(hasFocusedRow, permittedActivate, status, OnActivate is not null);
        _deactivateButton.Enabled = MasterDataFilter.CanDeactivate(hasFocusedRow, permittedDeactivate, status, OnDeactivate is not null);

        for (var i = 0; i < _extraActions.Count; i++)
        {
            var isEnabledFor = _extraActions[i].IsEnabledFor;
            var permitted = _extraActionButtons[i].Tag as bool?;
            var stateAllows = isEnabledFor is null || (focused is not null && isEnabledFor(focused));
            _extraActionButtons[i].Enabled = hasFocusedRow && (permitted ?? true) && stateAllows;
        }
    }

    private TDto? GetFocusedItem() => _gridView.GetFocusedRow() as TDto;

    private void SetBusy(bool isBusy)
    {
        _refreshButton.Enabled = !isBusy;
        Cursor = isBusy ? Cursors.WaitCursor : Cursors.Default;
    }

    /// <summary>
    /// Left command-panel width. Fixed on container resize (see
    /// <see cref="BuildLayout"/>'s <c>FixedPanel</c> setting) so the grid -
    /// never the command panel - absorbs all extra width when the window
    /// grows; wide enough for this DevExpress skin's real button chrome
    /// (see <see cref="AddCommandButton"/>'s own headroom comment) even at
    /// the smallest supported resolution (1280x720), where it still leaves
    /// the grid a large majority of the width.
    /// </summary>
    private const int CommandPanelWidth = 240;

    private void BuildLayout()
    {
        // SplitContainer, not a Dock=Top toolbar band: a left command panel
        // (search, actions, context info) plus the grid filling the rest,
        // matching the professional-ERP "navigation pane + content" shape
        // (Outlook, SSMS, DevExpress's own XAF demos) instead of a web-style
        // toolbar-above-grid strip. FixedPanel=Panel1 means only Panel2 (the
        // grid) grows when the window/screen resizes, so the grid always
        // gets the majority of the space at every supported resolution -
        // Panel1MinSize/Panel2MinSize below are a hard floor under that.
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            FixedPanel = FixedPanel.Panel1,
            // Deliberately small, not the actual desired command-panel
            // width (that's CommandPanelWidth, applied via SplitterDistance
            // below) - SplitContainer enforces "Panel1MinSize <=
            // SplitterDistance <= Width - Panel2MinSize" from its own
            // internal layout code on every resize, not just when this
            // class sets SplitterDistance, and a live crash confirmed that
            // internal call is not something a try/catch around this
            // class's own assignment can intercept. A width transiently
            // narrower than 200+320px during DevExpress's own document
            // layout (before this control reaches its final, much wider
            // steady-state size) was enough to violate that constraint and
            // crash the whole app. Small minimums here can't realistically
            // be violated even by a mid-layout transient width.
            Panel1MinSize = 40,
            Panel2MinSize = 40,
            SplitterWidth = 6,
        };

        var commandPanel = new PanelControl { Dock = DockStyle.Fill, Padding = new Padding(12, 8, 12, 8) };
        // AutoScroll as a safety net, not the expected path: at the smallest
        // supported resolution (1280x720) with several extra action buttons
        // (e.g. WarehouseStock's Receive/Issue/Reserve/Release), the stacked
        // command list could in principle exceed the visible height - a
        // scrollbar keeps every action reachable instead of the old
        // horizontal toolbar's failure mode (buttons overflowing off the
        // right edge with no way to reach them at all).
        var commandFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
        };

        commandFlow.Controls.Add(BuildSectionHeading("Search"));
        _searchBox.Properties.NullValuePrompt = "Search...";
        _searchBox.AutoSize = false;
        _searchBox.Width = CommandPanelWidth - commandPanel.Padding.Horizontal - 4;
        _searchBox.Margin = new Padding(0, 2, 0, DesktopStyle.PanelPadding);
        commandFlow.Controls.Add(_searchBox);

        commandFlow.Controls.Add(BuildSectionHeading("Actions"));
        AddCommandButton(commandFlow, _newButton);
        AddCommandButton(commandFlow, _editButton);
        AddCommandButton(commandFlow, _activateButton);
        AddCommandButton(commandFlow, _deactivateButton);
        AddCommandButton(commandFlow, _refreshButton);
        AddCommandButton(commandFlow, _exportButton);
        AddCommandButton(commandFlow, _importButton);
        foreach (var button in _extraActionButtons)
        {
            AddCommandButton(commandFlow, button);
        }

        var infoHeading = BuildSectionHeading("Info");
        infoHeading.Margin = new Padding(0, DesktopStyle.PanelPadding, 0, 4);
        commandFlow.Controls.Add(infoHeading);
        _statusLabel.Appearance.Font = DesktopStyle.CaptionFont;
        _statusLabel.Appearance.ForeColor = DesktopStyle.CaptionForeColor;
        _statusLabel.Appearance.Options.UseFont = true;
        _statusLabel.Appearance.Options.UseForeColor = true;
        _statusLabel.Margin = new Padding(0, 2, 0, 0);
        commandFlow.Controls.Add(_statusLabel);

        commandPanel.Controls.Add(commandFlow);

        split.Panel1.Controls.Add(commandPanel);
        split.Panel2.Controls.Add(_gridControl);

        Controls.Add(split);
        // SplitterDistance can't be set here (or anywhere else during
        // construction): this control itself is still width-0 at this
        // point - nothing has sized it yet, since whatever ultimately hosts
        // it (a document tab, a screen's ContentPanel) hasn't laid it out -
        // so SplitContainer's own "Panel1MinSize <= SplitterDistance <=
        // Width - Panel2MinSize" invariant is violated against that zero
        // width, throwing ArgumentOutOfRangeException. Confirmed via a live
        // crash dialog on first navigating to a MasterData screen - and
        // confirmed a second time that Load alone isn't a reliable enough
        // signal either (it can still fire before DevExpress's TabbedView
        // finishes sizing a freshly-created document, reproducing the same
        // crash). Retrying from split.Resize as well, guarded to run only
        // once real width is available, catches whichever event actually
        // ends up carrying the real size first.
        var splitterDistanceSet = false;
        void EnsureSplitterDistance()
        {
            if (splitterDistanceSet || split.Width < split.Panel1MinSize + split.Panel2MinSize)
            {
                return;
            }

            try
            {
                split.SplitterDistance = CommandPanelWidth;
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
                // at its framework default instead of CommandPanelWidth,
                // which is a cosmetic gap, not the crash this replaces.
            }
        }

        Load += (_, _) => EnsureSplitterDistance();
        split.Resize += (_, _) => EnsureSplitterDistance();
    }

    /// <summary>A left-command-panel section label ("Search"/"Actions"/"Info"), styled like Dashboard's list-column captions for a consistent look across the app.</summary>
    private static LabelControl BuildSectionHeading(string text) => new()
    {
        Text = text,
        Appearance = { Font = DesktopStyle.SectionHeadingFont, Options = { UseFont = true } },
        Margin = new Padding(0, 4, 0, 4),
    };

    /// <summary>
    /// Adds one command button to the vertical left-panel stack. AutoSize +
    /// MinimumSize (not a fixed Size) so the button's real caption always
    /// fits regardless of DevExpress skin/DPI - a fixed width was
    /// confirmed, via live screenshots in an earlier pass, to clip captions
    /// like "Reset Password" under this app's active skin.
    /// </summary>
    private void AddCommandButton(FlowLayoutPanel commandFlow, SimpleButton button)
    {
        button.AutoSize = true;
        button.MinimumSize = new Size(CommandPanelWidth - 24, DesktopStyle.ToolbarControlHeight);
        button.Margin = new Padding(0, 0, 0, DesktopStyle.ControlGap / 2);
        commandFlow.Controls.Add(button);
    }
}
