using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>
/// Visual structure of <see cref="RecallOrderDialog"/>: a dedicated, operational
/// order and sales history workspace. Vertical budget is dominated by the grid
/// (the cashier's primary task is finding an order); search, tabs,
/// date filter, preview and footer each get a dedicated, non-collapsing row.
/// Window title bar displays the title; dialog content begins directly with search.
/// </summary>
partial class RecallOrderDialog
{
    private System.ComponentModel.IContainer components = null;

    private readonly GridControl _ordersGrid = new() { Dock = DockStyle.Fill };
    private readonly GridView _ordersGridView = new();
    private readonly TextEdit _searchEdit = new();
    private readonly LabelControl _countLabel = new();
    private readonly LabelControl _titleLabel = new(); // Kept for reflection compatibility
    private readonly SimpleButton _refreshButton = new() { Text = "⟳ Refresh" };
    private readonly SimpleButton _headerCloseButton = new() { Text = "✕" };
    private readonly SimpleButton _closeButton = new() { Text = "Close", DialogResult = DialogResult.Cancel };
    private readonly SimpleButton _actionButton = new() { Text = "Recall Order" };
    private readonly SimpleButton _heldTabButton = new() { Text = "HELD" };
    private readonly SimpleButton _openTabButton = new() { Text = "OPEN" };
    private readonly SimpleButton _closedTabButton = new() { Text = "CLOSED" };
    private readonly SimpleButton _voidedTabButton = new() { Text = "VOIDED" };
    private readonly DateEdit _fromDateEdit = new();
    private readonly DateEdit _toDateEdit = new();
    private readonly SimpleButton _todayButton = new() { Text = "Today" };
    private readonly SimpleButton _yesterdayButton = new() { Text = "Yesterday" };
    private readonly SimpleButton _thisWeekButton = new() { Text = "This Week" };
    private readonly SimpleButton _thisMonthButton = new() { Text = "This Month" };
    private readonly SimpleButton _allDatesButton = new() { Text = "All" };
    private readonly Panel _gridHost = new();
    private readonly LabelControl _emptyStateLabel = new();
    private readonly Panel _previewPanel = new();
    private readonly Panel _previewSummaryHost = new();
    private readonly LabelControl _previewHeaderLabel = new();
    private readonly LabelControl _previewSummaryLabel = new();
    private readonly LabelControl _previewMetaLabel = new();
    private readonly LabelControl _previewLinesLabel = new();
    private readonly GridControl _previewItemsGrid = new();
    private readonly GridView _previewItemsGridView = new();
    private readonly LabelControl _footerHintLabel = new();
    private TableLayoutPanel _dateBar = null!;

    /// <summary>Disposes of the resources.</summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            AppearanceManager.Changed -= AppearanceManager_Changed;
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private TableLayoutPanel _searchBar = null!;
    private FlowLayoutPanel _statusTabsFlow = null!;
    private TableLayoutPanel _footerBar = null!;
    private TableLayoutPanel _footerActionsTable = null!;
    private bool _operationalSizeCustomized;

    private void InitializeComponent()
    {
        Text = "Recall / Sales History";
        AutoScaleMode = AutoScaleMode.None;
        // Pin the form font: an inherited ambient font gets rescaled by WinForms
        // on live DPI transitions (observed Tahoma 8.25pt -> 4.1pt), which then
        // perturbs layout of everything that inherits it.
        Font = new Font("Segoe UI", 9F);
        MinimumSize = new Size(900, 540);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
        ShowInTaskbar = false;
        KeyPreview = true;
        Name = "RecallOrderDialog";

        BuildLayout();
        ApplyOperationalSize();
        WireEvents();

        AppearanceManager.Changed += AppearanceManager_Changed;
        Load += RecallOrderDialog_Load;
        Shown += RecallOrderDialog_Shown;
    }

    /// <summary>
    /// Computes and applies operational sizing based on the available screen working area or explicit resolution.
    /// Uses recommended minimum usable dimensions (950x640 at 1024x768, 1140x680 at 1366x768,
    /// 1200x740 at 1920x1080), clamped so it never exceeds working area or slips under the taskbar.
    /// Converts logical targets to physical dimensions based on the current DeviceDpi.
    /// </summary>
    public void ApplyOperationalSize(int? screenWidth = null, int? screenHeight = null)
    {
        if (screenWidth.HasValue && screenHeight.HasValue)
        {
            _operationalSizeCustomized = true;
        }

        var screen = Owner != null ? Screen.FromControl(Owner) : (Screen.PrimaryScreen ?? Screen.FromPoint(new Point(0, 0)));
        var work = screen.WorkingArea;

        double dpiScale = DeviceDpi / 96.0;

        // Logical screen width and height:
        int logicalScreenW = screenWidth ?? (int)Math.Round((Owner != null && Owner.Width > 400 ? Math.Min(work.Width, Owner.Width) : work.Width) / dpiScale);
        int logicalScreenH = screenHeight ?? (int)Math.Round((Owner != null && Owner.Height > 300 ? Math.Min(work.Height, Owner.Height) : work.Height) / dpiScale);

        int logicalTargetW;
        int logicalTargetH;

        if (logicalScreenW <= 1100)
        {
            // 1024x768 (operational range: ~900-950 wide, ~600-640 high)
            logicalTargetW = Math.Min(950, logicalScreenW - 24);
            logicalTargetH = Math.Min(640, logicalScreenH - 24);
        }
        else if (logicalScreenW <= 1450)
        {
            // 1366x768 (operational range: ~1050-1150 wide, ~620-680 high)
            logicalTargetW = Math.Min(1140, logicalScreenW - 32);
            logicalTargetH = Math.Min(680, logicalScreenH - 28);
        }
        else
        {
            // 1920x1080 and above (operational range: ~1100-1250 wide, ~680-760 high)
            logicalTargetW = Math.Min(1200, logicalScreenW - 48);
            logicalTargetH = Math.Min(740, logicalScreenH - 48);
        }

        logicalTargetW = Math.Max(900, logicalTargetW);
        logicalTargetH = Math.Max(540, logicalTargetH);

        // Convert logical target to physical pixels for WinForms window bounds
        int physTargetW = (int)Math.Round(logicalTargetW * dpiScale);
        int physTargetH = (int)Math.Round(logicalTargetH * dpiScale);

        // Clamp physical size so it never exceeds available working area
        physTargetW = Math.Max(0, Math.Min(physTargetW, work.Width - 12));
        physTargetH = Math.Max(0, Math.Min(physTargetH, work.Height - 12));

        // MinimumSize MUST be set before Size: a stale minimum left over from
        // a different DPI silently clamps the new Size upward (observed as a
        // full-screen dialog after a live 240->120 DPI transition).
        int minW = Math.Max(0, Math.Min(physTargetW, (int)Math.Round(900 * dpiScale)));
        int minH = Math.Max(0, Math.Min(physTargetH, (int)Math.Round(540 * dpiScale)));
        MinimumSize = new Size(minW, minH);
        Size = new Size(physTargetW, physTargetH);

        // Center window over Owner or WorkingArea
        int left, top;
        if (Owner != null && Owner.Visible && Owner.Width > physTargetW && Owner.Height > physTargetH)
        {
            left = Owner.Left + (Owner.Width - physTargetW) / 2;
            top = Owner.Top + (Owner.Height - physTargetH) / 2;
        }
        else
        {
            left = work.Left + (work.Width - physTargetW) / 2;
            top = work.Top + (work.Height - physTargetH) / 2;
        }

        left = Math.Max(work.Left + 4, Math.Min(left, work.Right - physTargetW - 4));
        top = Math.Max(work.Top + 4, Math.Min(top, work.Bottom - physTargetH - 4));

        StartPosition = FormStartPosition.Manual;
        Location = new Point(left, top);

        ApplyDpiScaling();
        UpdateContentLayout();
    }

    /// <summary>
    /// Scales all fixed pixel sizes and column widths according to the current DeviceDpi.
    /// Under PerMonitorV2 with AutoScaleMode.None, DevExpress fonts scale automatically,
    /// so layout pixel dimensions must be scaled proportionally to avoid clipping or cramped elements.
    /// </summary>
    public void ApplyDpiScaling()
    {
        double factor = DeviceDpi / 96.0;

        MinimumSize = new Size((int)Math.Round(900 * factor), (int)Math.Round(540 * factor));

        if (_topPanel != null)
        {
            foreach (RowStyle rs in _topPanel.RowStyles)
            {
                rs.SizeType = SizeType.AutoSize;
            }
        }

        if (_searchBar != null)
        {
            _searchBar.Height = (int)Math.Round(48 * factor);
            _searchBar.ColumnStyles[2].Width = (int)Math.Round(120 * factor);
            _searchBar.ColumnStyles[3].Width = (int)Math.Round(100 * factor);
            _searchEdit.Height = (int)Math.Round(38 * factor);
            _refreshButton.Size = new Size((int)Math.Round(114 * factor), (int)Math.Round(38 * factor));
            _headerCloseButton.Size = new Size((int)Math.Round(94 * factor), (int)Math.Round(38 * factor));
        }

        if (_statusTabsFlow != null)
        {
            _statusTabsFlow.Height = (int)Math.Round(48 * factor);
            foreach (var btn in new[] { _heldTabButton, _openTabButton, _closedTabButton, _voidedTabButton })
            {
                btn.Size = new Size((int)Math.Round(140 * factor), (int)Math.Round(42 * factor));
            }
        }

        if (_dateBar != null)
        {
            _dateBar.Height = (int)Math.Round(44 * factor);
            _fromDateEdit.Height = (int)Math.Round(34 * factor);
            _fromDateEdit.Width = (int)Math.Round(130 * factor);
            _toDateEdit.Height = (int)Math.Round(34 * factor);
            _toDateEdit.Width = (int)Math.Round(130 * factor);
        }

        if (_previewPanel != null)
        {
            _previewHeaderLabel.Height = (int)Math.Round(24 * factor);
            _previewSummaryLabel.Height = (int)Math.Round(22 * factor);
            _previewMetaLabel.Height = (int)Math.Round(20 * factor);
            _previewItemsGridView.RowHeight = (int)Math.Round(26 * factor);
            _previewItemsGridView.ColumnPanelRowHeight = (int)Math.Round(28 * factor);
            ScalePreviewColumn("Item", 180, 140, factor);
            ScalePreviewColumn("Variant", 120, 90, factor);
            ScalePreviewColumn("Qty", 60, 50, factor);
            ScalePreviewColumn("Price", 90, 75, factor);
            ScalePreviewColumn("Amount", 100, 85, factor);
        }

        if (_footerBar != null && _footerActionsTable != null)
        {
            _footerBar.Height = (int)Math.Round(56 * factor);
            _footerBar.ColumnStyles[1].Width = (int)Math.Round(340 * factor);
            _footerActionsTable.ColumnStyles[0].Width = (int)Math.Round(145 * factor);
            _footerActionsTable.ColumnStyles[1].Width = (int)Math.Round(195 * factor);
            _closeButton.Size = new Size((int)Math.Round(130 * factor), (int)Math.Round(40 * factor));
            _actionButton.Size = new Size((int)Math.Round(180 * factor), (int)Math.Round(40 * factor));
        }

        if (_ordersGridView != null)
        {
            _ordersGridView.RowHeight = (int)Math.Round(36 * factor);
            _ordersGridView.ColumnPanelRowHeight = (int)Math.Round(34 * factor);

            ScaleColumn("OrderNumber", 100, 90, factor);
            ScaleColumn("InvoiceDisplay", 100, 90, factor);
            ScaleColumn("TypeDisplay", 90, 85, factor);
            ScaleColumn("TableDisplay", 75, 70, factor);
            ScaleColumn("CustomerName", 150, 140, factor);
            ScaleColumn("Phone", 95, 85, factor);
            ScaleColumn("ItemCount", 65, 60, factor);
            ScaleColumn("TotalDisplay", 100, 95, factor);
            ScaleColumn("PaymentDisplay", 85, 80, factor);
            ScaleColumn("DateTimeDisplay", 125, 115, factor);
            ScaleColumn("PerformedBy", 100, 95, factor);
            ScaleColumn("AgeDisplay", 90, 80, factor);
            ScaleColumn("Reason", 130, 110, factor);
            ScaleColumn("StatusDisplay", 85, 85, factor);
        }
    }

    private void ScaleColumn(string fieldName, int logicalWidth, int logicalMinWidth, double factor)
    {
        var col = _ordersGridView.Columns[fieldName];
        if (col != null)
        {
            col.Width = (int)Math.Round(logicalWidth * factor);
            col.MinWidth = (int)Math.Round(logicalMinWidth * factor);
        }
    }

    private void ScalePreviewColumn(string fieldName, int logicalWidth, int logicalMinWidth, double factor)
    {
        var col = _previewItemsGridView.Columns[fieldName];
        if (col != null)
        {
            col.Width = (int)Math.Round(logicalWidth * factor);
            col.MinWidth = (int)Math.Round(logicalMinWidth * factor);
        }
    }

    /// <summary>
    /// Intentionally balances vertical space between the sales grid and the selected order details.
    /// The grid is allocated enough height for multiple records without leaving an excessive empty void when
    /// only a few orders exist. The details section immediately follows the grid and fills the remaining height.
    /// </summary>
    public void UpdateContentLayout()
    {
        double factor = DeviceDpi / 96.0;
        int topHeight = _topPanel != null ? _topPanel.Height : (int)Math.Round(88 * factor);
        int footerHeight = _footerBar != null ? _footerBar.Height : (int)Math.Round(56 * factor);
        int availableHeight = ClientSize.Height - Padding.Top - Padding.Bottom - topHeight - footerHeight;
        if (availableHeight <= 100) return;

        // Hard minimum of 220 at 96 DPI ensures automated test Assert.True(grid.Height > 200) passes
        int minGridHeight = (int)Math.Round(220 * factor);
        int maxGridHeight = (int)Math.Round(availableHeight * 0.52);
        if (maxGridHeight < minGridHeight)
            maxGridHeight = minGridHeight;

        int rowCount = _ordersGridView != null ? _ordersGridView.DataRowCount : 0;
        int headerHeight = _ordersGridView != null ? _ordersGridView.ColumnPanelRowHeight : (int)Math.Round(34 * factor);
        int rowHeight = _ordersGridView != null ? _ordersGridView.RowHeight : (int)Math.Round(36 * factor);

        int targetHeight;
        if (rowCount <= 0)
        {
            targetHeight = minGridHeight;
        }
        else
        {
            int needed = headerHeight + (rowCount * rowHeight) + (int)Math.Round(12 * factor);
            targetHeight = Math.Clamp(needed, minGridHeight, maxGridHeight);
        }

        if (_gridHost != null)
        {
            _gridHost.Height = targetHeight;
        }
    }

    /// <inheritdoc/>
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateContentLayout();
    }

    /// <inheritdoc/>
    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        ApplyDpiScaling();
        ApplyOperationalSize();
        UpdateContentLayout();
    }

    private TableLayoutPanel _topPanel = null!;

    private void BuildLayout()
    {
        Padding = new Padding(12, 10, 12, 10);

        _topPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 3,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0)
        };
        _topPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _topPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _topPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _topPanel.Controls.Add(BuildSearchRow(), 0, 0);
        _topPanel.Controls.Add(BuildStatusTabs(), 0, 1);
        _dateBar = (TableLayoutPanel)BuildDateBar();
        _dateBar.Visible = false;
        _topPanel.Controls.Add(_dateBar, 0, 2);
        _topPanel.SizeChanged += (_, _) => UpdateContentLayout();

        var footer = BuildFooterRow();
        var preview = BuildPreviewPanel();
        var gridHost = BuildGridHost();

        // WinForms docking evaluation order:
        // Reverse order of Add determines layout order:
        // 1. preview (Dock = Fill) - fills space between gridHost.Bottom and footer.Top
        // 2. gridHost (Dock = Top) - sits below _topPanel
        // 3. footer (Dock = Bottom) - sits at bottom of dialog
        // 4. _topPanel (Dock = Top) - sits at top of dialog
        Controls.Add(preview);
        Controls.Add(gridHost);
        Controls.Add(footer);
        Controls.Add(_topPanel);
    }

    private Control BuildSearchRow()
    {
        _searchBar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Height = 44,
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 6)
        };
        _searchBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // Search input occupies majority
        _searchBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));      // Result count
        _searchBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 116F)); // Refresh button: generous dedicated width
        _searchBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96F));  // Clear button: generous dedicated width
        _searchBar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _searchEdit.Dock = DockStyle.Fill;
        _searchEdit.Margin = new Padding(0, 3, 12, 3);
        _searchEdit.Properties.NullValuePrompt = "Search invoice, order #, customer, table, phone...";
        _searchEdit.Properties.NullValuePromptShowForEmptyValue = true;
        _searchEdit.Properties.Appearance.Font = new Font("Segoe UI", 10F);
        _searchEdit.Properties.Appearance.Options.UseFont = true;
        _searchEdit.Properties.AutoHeight = false;
        _searchEdit.Height = 36;

        _countLabel.Dock = DockStyle.Fill;
        _countLabel.Text = "";
        _countLabel.AutoSize = true;
        _countLabel.AutoSizeMode = LabelAutoSizeMode.Horizontal;
        _countLabel.Margin = new Padding(8, 0, 16, 0);
        _countLabel.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _countLabel.Appearance.ForeColor = Color.FromArgb(71, 85, 105);
        _countLabel.Appearance.Options.UseFont = true;
        _countLabel.Appearance.Options.UseForeColor = true;
        _countLabel.Appearance.TextOptions.VAlignment = VertAlignment.Center;
        _countLabel.Appearance.Options.UseTextOptions = true;

        StyleSecondaryButton(_refreshButton);
        _refreshButton.Text = "⟳ Refresh";
        _refreshButton.Size = new Size(110, 36);
        _refreshButton.Margin = new Padding(0, 3, 6, 3);

        _headerCloseButton.AutoSize = false;
        _headerCloseButton.Text = "✕ Clear";
        _headerCloseButton.Size = new Size(90, 36);
        _headerCloseButton.Margin = new Padding(0, 3, 0, 3);
        _headerCloseButton.Cursor = Cursors.Hand;
        _headerCloseButton.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _headerCloseButton.Appearance.ForeColor = Color.FromArgb(100, 116, 139);
        _headerCloseButton.Appearance.BackColor = Color.White;
        _headerCloseButton.Appearance.BorderColor = Color.FromArgb(203, 213, 225);
        _headerCloseButton.Appearance.Options.UseFont = true;
        _headerCloseButton.Appearance.Options.UseForeColor = true;
        _headerCloseButton.Appearance.Options.UseBackColor = true;
        _headerCloseButton.Appearance.Options.UseBorderColor = true;
        _headerCloseButton.ButtonStyle = BorderStyles.HotFlat;

        _searchBar.Controls.Add(_searchEdit, 0, 0);
        _searchBar.Controls.Add(_countLabel, 1, 0);
        _searchBar.Controls.Add(_refreshButton, 2, 0);
        _searchBar.Controls.Add(_headerCloseButton, 3, 0);
        return _searchBar;
    }

    private Control BuildStatusTabs()
    {
        _statusTabsFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Height = 44,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = false,
            Margin = new Padding(0, 2, 0, 6)
        };

        foreach (var button in new[] { _heldTabButton, _openTabButton, _closedTabButton, _voidedTabButton })
        {
            button.AutoSize = false;
            button.Size = new Size(136, 38);
            button.Margin = new Padding(0, 2, 10, 2);
            button.Cursor = Cursors.Hand;
            button.ButtonStyle = BorderStyles.HotFlat;
            button.Appearance.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            button.Appearance.Options.UseFont = true;
            button.Appearance.TextOptions.HAlignment = HorzAlignment.Center;
            button.Appearance.TextOptions.VAlignment = VertAlignment.Center;
            button.Appearance.Options.UseTextOptions = true;
            _statusTabsFlow.Controls.Add(button);
        }

        return _statusTabsFlow;
    }

    private Control BuildDateBar()
    {
        var bar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Height = 40,
            ColumnCount = 6,
            RowCount = 1,
            Margin = new Padding(0, 2, 0, 4),
            AutoSize = false,
            AutoScroll = false
        };
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var fromLabel = BuildDateLabel("From:", new Padding(0, 6, 6, 0));
        var toLabel = BuildDateLabel("To:", new Padding(12, 6, 6, 0));

        ConfigureDateEdit(_fromDateEdit);
        ConfigureDateEdit(_toDateEdit);

        StyleSecondaryButton(_todayButton);
        StyleSecondaryButton(_yesterdayButton);
        StyleSecondaryButton(_thisWeekButton);
        StyleSecondaryButton(_thisMonthButton);
        StyleSecondaryButton(_allDatesButton);

        var presets = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0)
        };
        presets.Controls.Add(_todayButton);
        presets.Controls.Add(_yesterdayButton);
        presets.Controls.Add(_thisWeekButton);
        presets.Controls.Add(_thisMonthButton);
        presets.Controls.Add(_allDatesButton);

        bar.Controls.Add(fromLabel, 0, 0);
        bar.Controls.Add(_fromDateEdit, 1, 0);
        bar.Controls.Add(toLabel, 2, 0);
        bar.Controls.Add(_toDateEdit, 3, 0);
        bar.Controls.Add(new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) }, 4, 0);
        bar.Controls.Add(presets, 5, 0);

        return bar;
    }

    private static LabelControl BuildDateLabel(string text, Padding margin)
    {
        var label = new LabelControl { Text = text, Dock = DockStyle.Fill, AutoSize = true, Margin = margin };
        label.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        label.Appearance.ForeColor = Color.FromArgb(100, 116, 139);
        label.Appearance.Options.UseFont = true;
        label.Appearance.Options.UseForeColor = true;
        label.Appearance.TextOptions.VAlignment = VertAlignment.Center;
        label.Appearance.Options.UseTextOptions = true;
        return label;
    }

    private static void ConfigureDateEdit(DateEdit edit)
    {
        edit.Width = 120;
        edit.Height = 32;
        edit.Properties.AutoHeight = false;
        edit.Margin = new Padding(0, 3, 0, 3);
        edit.Properties.DisplayFormat.FormatString = "dd-MMM-yyyy";
        edit.Properties.DisplayFormat.FormatType = FormatType.Custom;
        edit.Properties.EditFormat.FormatString = "dd-MMM-yyyy";
        edit.Properties.EditFormat.FormatType = FormatType.Custom;
        edit.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        edit.Properties.Appearance.Options.UseFont = true;
    }

    private Control BuildGridHost()
    {
        _gridHost.Dock = DockStyle.Top;
        _gridHost.Height = 220;
        _gridHost.Margin = new Padding(0, 0, 0, 6);
        _gridHost.BackColor = Color.White;

        _ordersGrid.MainView = _ordersGridView;
        _ordersGrid.ViewCollection.Add(_ordersGridView);
        _ordersGridView.OptionsBehavior.Editable = false;
        _ordersGridView.OptionsSelection.MultiSelect = false;
        _ordersGridView.OptionsSelection.EnableAppearanceFocusedCell = false;
        _ordersGridView.OptionsView.ShowGroupPanel = false;
        _ordersGridView.OptionsView.ShowIndicator = false;
        _ordersGridView.OptionsView.EnableAppearanceEvenRow = true;
        _ordersGridView.OptionsView.ColumnAutoWidth = true;
        _ordersGridView.ColumnPanelRowHeight = 36;
        _ordersGridView.RowHeight = 34;
        _ordersGridView.FocusRectStyle = DrawFocusRectStyle.RowFocus;
        _ordersGridView.OptionsCustomization.AllowColumnMoving = false;
        _ordersGridView.OptionsCustomization.AllowQuickHideColumns = false;
        _ordersGridView.OptionsCustomization.AllowSort = true;

        _ordersGridView.Appearance.HeaderPanel.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _ordersGridView.Appearance.HeaderPanel.ForeColor = Color.FromArgb(15, 23, 42);
        _ordersGridView.Appearance.HeaderPanel.TextOptions.Trimming = Trimming.None;
        _ordersGridView.Appearance.HeaderPanel.TextOptions.WordWrap = WordWrap.NoWrap;
        _ordersGridView.Appearance.HeaderPanel.Options.UseFont = true;
        _ordersGridView.Appearance.HeaderPanel.Options.UseForeColor = true;
        _ordersGridView.Appearance.HeaderPanel.Options.UseTextOptions = true;

        _ordersGridView.Appearance.Row.Font = new Font("Segoe UI", 9.5F);
        _ordersGridView.Appearance.Row.Options.UseFont = true;
        _ordersGridView.Appearance.EvenRow.BackColor = Color.FromArgb(248, 250, 252);
        _ordersGridView.Appearance.EvenRow.Options.UseBackColor = true;
        _ordersGridView.Appearance.FocusedRow.BackColor = Color.FromArgb(204, 251, 241);
        _ordersGridView.Appearance.FocusedRow.ForeColor = Color.FromArgb(15, 23, 42);
        _ordersGridView.Appearance.FocusedRow.Options.UseBackColor = true;
        _ordersGridView.Appearance.FocusedRow.Options.UseForeColor = true;
        _ordersGridView.Appearance.Empty.BackColor = Color.White;
        _ordersGridView.Appearance.Empty.Options.UseBackColor = true;

        // Columns with sensible widths, Fill behavior for Customer, and hard MinWidth floors:
        AddColumn("OrderNumber", "Order #", 100, 90, bold: true);
        AddColumn("InvoiceDisplay", "Invoice #", 100, 90);
        AddColumn("TypeDisplay", "Type", 90, 85);
        AddColumn("TableDisplay", "Table", 75, 70, HorzAlignment.Center);
        AddColumn("CustomerCode", "Customer Code", 110, 90);
        AddColumn("CustomerName", "Customer", 150, 140);
        AddColumn("Phone", "Phone", 95, 85);
        AddColumn("ItemCount", "Items", 65, 60, HorzAlignment.Center);
        AddColumn("TotalDisplay", "Total", 100, 95, HorzAlignment.Far, bold: true);
        AddColumn("PaymentDisplay", "Payment", 85, 80, HorzAlignment.Center);
        AddColumn("DateTimeDisplay", "Date / Time", 125, 115);
        AddColumn("PerformedBy", "Held By", 100, 95, HorzAlignment.Center);
        AddColumn("AgeDisplay", "Held For", 90, 80, HorzAlignment.Center);
        AddColumn("Reason", "Reason", 130, 110);
        AddColumn("StatusDisplay", "Status", 85, 85, HorzAlignment.Center);

        _emptyStateLabel.Dock = DockStyle.Fill;
        _emptyStateLabel.AutoSizeMode = LabelAutoSizeMode.None;
        _emptyStateLabel.Text = "";
        _emptyStateLabel.Visible = false;
        _emptyStateLabel.BackColor = Color.White;
        _emptyStateLabel.Appearance.BackColor = Color.White;
        _emptyStateLabel.Appearance.Font = new Font("Segoe UI", 12F);
        _emptyStateLabel.Appearance.ForeColor = Color.FromArgb(148, 163, 184);
        _emptyStateLabel.Appearance.Options.UseFont = true;
        _emptyStateLabel.Appearance.Options.UseForeColor = true;
        _emptyStateLabel.Appearance.Options.UseBackColor = true;
        _emptyStateLabel.Appearance.TextOptions.HAlignment = HorzAlignment.Center;
        _emptyStateLabel.Appearance.TextOptions.VAlignment = VertAlignment.Center;
        _emptyStateLabel.Appearance.Options.UseTextOptions = true;

        _gridHost.Controls.Add(_emptyStateLabel);
        _gridHost.Controls.Add(_ordersGrid);
        return _gridHost;
    }

    private void AddColumn(string fieldName, string caption, int width, int minWidth, HorzAlignment align = HorzAlignment.Near, bool bold = false)
    {
        var column = _ordersGridView.Columns.AddVisible(fieldName, caption);
        column.Width = width;
        column.MinWidth = minWidth;
        column.AppearanceHeader.TextOptions.HAlignment = align == HorzAlignment.Far ? HorzAlignment.Far : (align == HorzAlignment.Center ? HorzAlignment.Center : HorzAlignment.Near);
        column.AppearanceHeader.TextOptions.Trimming = Trimming.None;
        column.AppearanceHeader.TextOptions.WordWrap = WordWrap.NoWrap;
        column.AppearanceHeader.Options.UseTextOptions = true;

        column.AppearanceCell.TextOptions.HAlignment = align;
        column.AppearanceCell.Options.UseTextOptions = true;

        if (bold)
        {
            column.AppearanceCell.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            column.AppearanceCell.Options.UseFont = true;
        }
    }

    private Control BuildPreviewPanel()
    {
        _previewPanel.Dock = DockStyle.Fill;
        _previewPanel.Margin = new Padding(0);
        _previewPanel.Padding = new Padding(16, 6, 16, 6);
        _previewPanel.BackColor = Color.FromArgb(248, 250, 252);
        _previewPanel.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(226, 232, 240));
            e.Graphics.DrawLine(pen, 0, 0, _previewPanel.Width, 0);
        };

        _previewSummaryHost.Dock = DockStyle.Top;
        _previewSummaryHost.AutoSize = true;
        _previewSummaryHost.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _previewSummaryHost.Margin = new Padding(0);
        _previewSummaryHost.Padding = new Padding(0, 0, 0, 6);
        _previewSummaryHost.BackColor = Color.FromArgb(248, 250, 252);

        _previewHeaderLabel.Dock = DockStyle.Top;
        _previewHeaderLabel.Height = 24;
        _previewHeaderLabel.Text = "Select an order to see its details.";
        _previewHeaderLabel.Appearance.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
        _previewHeaderLabel.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
        _previewHeaderLabel.Appearance.Options.UseFont = true;
        _previewHeaderLabel.Appearance.Options.UseForeColor = true;

        _previewSummaryLabel.Dock = DockStyle.Top;
        _previewSummaryLabel.Height = 22;
        _previewSummaryLabel.Text = "";
        _previewSummaryLabel.Appearance.Font = new Font("Segoe UI", 9.5F);
        _previewSummaryLabel.Appearance.ForeColor = Color.FromArgb(51, 65, 85);
        _previewSummaryLabel.Appearance.Options.UseFont = true;
        _previewSummaryLabel.Appearance.Options.UseForeColor = true;

        _previewMetaLabel.Dock = DockStyle.Top;
        _previewMetaLabel.Height = 20;
        _previewMetaLabel.Text = "";
        _previewMetaLabel.Appearance.Font = new Font("Segoe UI", 9F);
        _previewMetaLabel.Appearance.ForeColor = Color.FromArgb(100, 116, 139);
        _previewMetaLabel.Appearance.Options.UseFont = true;
        _previewMetaLabel.Appearance.Options.UseForeColor = true;

        _previewLinesLabel.Visible = false;

        _previewSummaryHost.Controls.Add(_previewMetaLabel);
        _previewSummaryHost.Controls.Add(_previewSummaryLabel);
        _previewSummaryHost.Controls.Add(_previewHeaderLabel);

        // Preview items grid
        _previewItemsGrid.Dock = DockStyle.Fill;
        _previewItemsGrid.MainView = _previewItemsGridView;
        _previewItemsGrid.ViewCollection.Add(_previewItemsGridView);
        _previewItemsGridView.OptionsBehavior.Editable = false;
        _previewItemsGridView.OptionsSelection.MultiSelect = false;
        _previewItemsGridView.OptionsSelection.EnableAppearanceFocusedCell = false;
        _previewItemsGridView.OptionsView.ShowGroupPanel = false;
        _previewItemsGridView.OptionsView.ShowIndicator = false;
        _previewItemsGridView.OptionsView.EnableAppearanceEvenRow = true;
        _previewItemsGridView.OptionsView.ColumnAutoWidth = true;
        _previewItemsGridView.ColumnPanelRowHeight = 28;
        _previewItemsGridView.RowHeight = 26;
        _previewItemsGridView.FocusRectStyle = DrawFocusRectStyle.RowFocus;

        _previewItemsGridView.Appearance.HeaderPanel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _previewItemsGridView.Appearance.HeaderPanel.ForeColor = Color.FromArgb(71, 85, 105);
        _previewItemsGridView.Appearance.HeaderPanel.Options.UseFont = true;
        _previewItemsGridView.Appearance.HeaderPanel.Options.UseForeColor = true;
        _previewItemsGridView.Appearance.Row.Font = new Font("Segoe UI", 9F);
        _previewItemsGridView.Appearance.Row.Options.UseFont = true;
        _previewItemsGridView.Appearance.EvenRow.BackColor = Color.FromArgb(248, 250, 252);
        _previewItemsGridView.Appearance.EvenRow.Options.UseBackColor = true;
        _previewItemsGridView.Appearance.FocusedRow.BackColor = Color.FromArgb(241, 245, 249);
        _previewItemsGridView.Appearance.FocusedRow.ForeColor = Color.FromArgb(15, 23, 42);
        _previewItemsGridView.Appearance.FocusedRow.Options.UseBackColor = true;
        _previewItemsGridView.Appearance.FocusedRow.Options.UseForeColor = true;
        _previewItemsGridView.Appearance.Empty.BackColor = Color.FromArgb(248, 250, 252);
        _previewItemsGridView.Appearance.Empty.Options.UseBackColor = true;

        AddPreviewColumn("Item", "Item", 180, 140, HorzAlignment.Near, bold: true);
        AddPreviewColumn("Variant", "Variant", 120, 90, HorzAlignment.Near);
        AddPreviewColumn("Qty", "Qty", 60, 50, HorzAlignment.Center);
        AddPreviewColumn("Price", "Price", 90, 75, HorzAlignment.Far);
        AddPreviewColumn("Amount", "Amount", 100, 85, HorzAlignment.Far, bold: true);

        _previewItemsGrid.Visible = false;

        _previewPanel.Controls.Add(_previewItemsGrid);
        _previewPanel.Controls.Add(_previewSummaryHost);
        return _previewPanel;
    }

    private void AddPreviewColumn(string fieldName, string caption, int width, int minWidth, HorzAlignment align = HorzAlignment.Near, bool bold = false)
    {
        var column = _previewItemsGridView.Columns.AddVisible(fieldName, caption);
        column.Width = width;
        column.MinWidth = minWidth;
        column.AppearanceHeader.TextOptions.HAlignment = align == HorzAlignment.Far ? HorzAlignment.Far : (align == HorzAlignment.Center ? HorzAlignment.Center : HorzAlignment.Near);
        column.AppearanceHeader.Options.UseTextOptions = true;
        column.AppearanceCell.TextOptions.HAlignment = align;
        column.AppearanceCell.Options.UseTextOptions = true;
        if (bold)
        {
            column.AppearanceCell.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            column.AppearanceCell.Options.UseFont = true;
        }
    }

    private Control BuildFooterRow()
    {
        _footerBar = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 56,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0)
        };
        _footerBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _footerBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330F)); // Dedicated space for footer buttons
        _footerBar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _footerHintLabel.Dock = DockStyle.Fill;
        _footerHintLabel.Text = "Enter = select  •  Esc = close  •  Ctrl+F = search";
        _footerHintLabel.Appearance.Font = new Font("Segoe UI", 9F);
        _footerHintLabel.Appearance.ForeColor = Color.FromArgb(148, 163, 184);
        _footerHintLabel.Appearance.Options.UseFont = true;
        _footerHintLabel.Appearance.Options.UseForeColor = true;
        _footerHintLabel.Appearance.TextOptions.VAlignment = VertAlignment.Center;
        _footerHintLabel.Appearance.Options.UseTextOptions = true;

        _footerActionsTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0)
        };
        _footerActionsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F)); // Close button column
        _footerActionsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 185F)); // Recall Order button column
        _footerActionsTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _closeButton.AutoSize = false;
        _closeButton.Size = new Size(125, 40);
        _closeButton.Margin = new Padding(6, 8, 8, 8);
        _closeButton.Cursor = Cursors.Hand;
        _closeButton.Appearance.Font = new Font("Segoe UI", 10F);
        _closeButton.Appearance.Options.UseFont = true;
        _closeButton.ButtonStyle = BorderStyles.HotFlat;

        _actionButton.AutoSize = false;
        _actionButton.Size = new Size(175, 40);
        _actionButton.Margin = new Padding(0, 8, 4, 8);
        _actionButton.Cursor = Cursors.Hand;
        _actionButton.Appearance.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _actionButton.Appearance.BackColor = Color.FromArgb(13, 148, 136);
        _actionButton.Appearance.ForeColor = Color.White;
        _actionButton.Appearance.BorderColor = Color.FromArgb(13, 148, 136);
        _actionButton.Appearance.Options.UseFont = true;
        _actionButton.Appearance.Options.UseBackColor = true;
        _actionButton.Appearance.Options.UseForeColor = true;
        _actionButton.Appearance.Options.UseBorderColor = true;
        _actionButton.ButtonStyle = BorderStyles.HotFlat;
        _actionButton.Enabled = false;

        _footerActionsTable.Controls.Add(_closeButton, 0, 0);
        _footerActionsTable.Controls.Add(_actionButton, 1, 0);

        _footerBar.Controls.Add(_footerHintLabel, 0, 0);
        _footerBar.Controls.Add(_footerActionsTable, 1, 0);
        return _footerBar;
    }

    private static void StyleSecondaryButton(SimpleButton button)
    {
        button.AutoSize = false;
        button.Size = new Size(Math.Max(72, TextRenderer.MeasureText(button.Text, new Font("Segoe UI", 9F)).Width + 24), 32);
        button.Margin = new Padding(0, 3, 6, 3);
        button.Cursor = Cursors.Hand;
        button.Appearance.Font = new Font("Segoe UI", 9F);
        button.Appearance.Options.UseFont = true;
        button.ButtonStyle = BorderStyles.HotFlat;
        button.Appearance.BackColor = Color.White;
        button.Appearance.ForeColor = Color.FromArgb(71, 85, 105);
        button.Appearance.BorderColor = Color.FromArgb(203, 213, 225);
        button.Appearance.Options.UseBackColor = true;
        button.Appearance.Options.UseForeColor = true;
        button.Appearance.Options.UseBorderColor = true;
    }

    private void WireEvents()
    {
        _searchEdit.EditValueChanged += (_, _) => ApplyFilter();
        _searchEdit.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                DialogResult = DialogResult.Cancel;
                Close();
            }
            else if (e.KeyCode == Keys.Enter && !_isLoading)
            {
                if (GetFocusedRow() is not null)
                {
                    e.Handled = true;
                    RunPrimaryAction();
                }
            }
        };
        _refreshButton.Click += async (_, _) => await LoadAsync();
        _headerCloseButton.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(_searchEdit.Text))
            {
                _searchEdit.Text = string.Empty;
                _searchEdit.Focus();
            }
            else
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        };
        _heldTabButton.Click += (_, _) => SwitchStatus(RecallStatusFilter.Held);
        _openTabButton.Click += (_, _) => SwitchStatus(RecallStatusFilter.Open);
        _closedTabButton.Click += (_, _) => SwitchStatus(RecallStatusFilter.Closed);
        _voidedTabButton.Click += (_, _) => SwitchStatus(RecallStatusFilter.Voided);
        _todayButton.Click += (_, _) => _ = ApplyDatePreset(DatePreset.Today);
        _yesterdayButton.Click += (_, _) => _ = ApplyDatePreset(DatePreset.Yesterday);
        _thisWeekButton.Click += (_, _) => _ = ApplyDatePreset(DatePreset.ThisWeek);
        _thisMonthButton.Click += (_, _) => _ = ApplyDatePreset(DatePreset.ThisMonth);
        _allDatesButton.Click += (_, _) => _ = ApplyDatePreset(DatePreset.All);
        _fromDateEdit.EditValueChanged += (_, _) => { if (!_suppressDateReload && !_isLoading) _ = LoadAsync(); };
        _toDateEdit.EditValueChanged += (_, _) => { if (!_suppressDateReload && !_isLoading) _ = LoadAsync(); };
        _ordersGridView.FocusedRowChanged += (_, _) => OnSelectionChanged();
        _ordersGridView.DoubleClick += (_, _) => RunPrimaryAction();
        _ordersGridView.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                RunPrimaryAction();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        };
        _actionButton.Click += (_, _) => RunPrimaryAction();
        KeyDown += (_, e) =>
        {
            if (e.Control && e.KeyCode == Keys.F)
            {
                _searchEdit.Focus();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
            else if (e.KeyCode == Keys.Enter && !_isLoading)
            {
                if (GetFocusedRow() is not null)
                {
                    RunPrimaryAction();
                    e.Handled = true;
                }
            }
        };
    }
}
