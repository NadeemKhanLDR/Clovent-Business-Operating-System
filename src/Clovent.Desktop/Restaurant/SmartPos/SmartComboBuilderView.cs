using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Restaurant.Application.SmartCombos;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Restaurant.SmartPos;

/// <summary>
/// Professional Back Office Smart Combo Builder view.
/// Analyzes completed historical sales for frequently bought together variant combinations,
/// displays explainable KPI metrics, and allows managers to review and convert opportunities into POS Quick Order deals.
/// </summary>
public sealed class SmartComboBuilderView : XtraUserControl
{
    public sealed record PeriodOption(string Display, int Days);

    private readonly IServiceScopeFactory _scopes;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly LookUpEdit _location = new();
    private readonly LookUpEdit _period = new();
    private readonly SpinEdit _days = new();
    private readonly SimpleButton _analyze = new();
    private readonly SimpleButton _preview = new();
    private readonly LabelControl _lblStatusMessage = new();
    private readonly MarqueeProgressBarControl _busy = new();

    // Header Controls
    private readonly LabelControl _lblTitle = new();
    private readonly LabelControl _lblSubtitle = new();

    // KPI Card Value Labels
    private readonly LabelControl _lblKpiOpportunities = new();
    private readonly LabelControl _lblKpiEligibleSales = new();
    private readonly LabelControl _lblKpiConvertedDeals = new();
    private readonly LabelControl _lblKpiAvgAttachRate = new();

    // Grid & Empty State
    private readonly GridControl _grid = new();
    private readonly GridView _view = new();
    private readonly PanelControl _emptyPanel = new();
    private readonly LabelControl _lblEmptyTitle = new();
    private readonly LabelControl _lblEmptyDescription = new();
    private readonly SimpleButton _btnRetry = new();

    private Control _headerControl = null!;
    private Control _filterCard = null!;
    private Control _kpiCards = null!;
    private Control _resultsHeader = null!;
    private Control _resultsContent = null!;
    private LabelControl _lblPeriod = null!;
    private LabelControl _lblLocation = null!;

    private ComboAnalysis? _analysis;
    private bool _working;

    public SmartComboBuilderView(IServiceScopeFactory scopes, SmartComboOptions options)
    {
        _scopes = scopes;
        Dock = DockStyle.Fill;
        Name = nameof(SmartComboBuilderView);

        InitializeComponent(options);

#if DEBUG
        HookLifecycleTelemetry();
        SmartPosLayoutTelemetry.LogSmartComboEvent(this, "Constructor End", _lblPeriod, _period, _lblLocation, _location, _analyze, _filterCard, _kpiCards, _grid);
#endif
    }

#if DEBUG
    private void HookLifecycleTelemetry()
    {
        HandleCreated += (_, _) => SmartPosLayoutTelemetry.LogSmartComboEvent(this, "HandleCreated", _lblPeriod, _period, _lblLocation, _location, _analyze, _filterCard, _kpiCards, _grid);
        ParentChanged += (_, _) => SmartPosLayoutTelemetry.LogSmartComboEvent(this, "ParentChanged", _lblPeriod, _period, _lblLocation, _location, _analyze, _filterCard, _kpiCards, _grid);
        VisibleChanged += (_, _) => SmartPosLayoutTelemetry.LogSmartComboEvent(this, "VisibleChanged", _lblPeriod, _period, _lblLocation, _location, _analyze, _filterCard, _kpiCards, _grid);
        SizeChanged += (_, _) => SmartPosLayoutTelemetry.LogSmartComboEvent(this, "SizeChanged", _lblPeriod, _period, _lblLocation, _location, _analyze, _filterCard, _kpiCards, _grid);
    }
#endif

    private void InitializeComponent(SmartComboOptions options)
    {
        // -------------------------------------------------------------
        // Period & Location Controls Setup
        // -------------------------------------------------------------
        _days.Properties.MinValue = 1;
        _days.Properties.MaxValue = 366;
        _days.Properties.IsFloatValue = false;
        _days.Value = options.PeriodDays;

        var periodOptions = new List<PeriodOption>
        {
            new("Last 7 Days", 7),
            new("Last 14 Days", 14),
            new("Last 30 Days", 30),
            new("Last 60 Days", 60),
            new("Last 90 Days", 90)
        };

        _period.Properties.DataSource = periodOptions;
        _period.Properties.DisplayMember = nameof(PeriodOption.Display);
        _period.Properties.ValueMember = nameof(PeriodOption.Days);
        _period.Properties.Columns.Clear();
        _period.Properties.Columns.Add(new LookUpColumnInfo(nameof(PeriodOption.Display), "Period"));
        _period.Properties.ShowHeader = false;
        _period.Properties.ShowFooter = false;
        _period.Properties.NullText = "Select period";
        SmartPosControlSizing.ConfigureEditor(_period, 180, 36);
        _period.EditValue = periodOptions.Any(p => p.Days == options.PeriodDays) ? options.PeriodDays : 30;

        _period.EditValueChanged += (_, _) =>
        {
            if (_period.EditValue is int days)
            {
                _days.Value = days;
            }
            InvalidateAnalysis();
        };

        _location.Properties.DisplayMember = nameof(ComboLocation.Name);
        _location.Properties.ValueMember = nameof(ComboLocation.Id);
        _location.Properties.Columns.Clear();
        _location.Properties.Columns.Add(new LookUpColumnInfo(nameof(ComboLocation.Name), "Location"));
        _location.Properties.ShowHeader = false;
        _location.Properties.ShowFooter = false;
        _location.Properties.NullText = "Select permitted location";
        SmartPosControlSizing.ConfigureEditor(_location, 300, 36);
        _location.EditValueChanged += (_, _) => InvalidateAnalysis();

        // -------------------------------------------------------------
        // Action Buttons Setup
        // -------------------------------------------------------------
        _analyze.Text = "Analyze Sales";
        SmartPosControlSizing.ConfigureButton(_analyze, 160, 36);
        _analyze.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _analyze.Appearance.BackColor = Color.FromArgb(13, 148, 136); // Teal-600
        _analyze.Appearance.ForeColor = Color.White;
        _analyze.Appearance.Options.UseFont = true;
        _analyze.Appearance.Options.UseBackColor = true;
        _analyze.Appearance.Options.UseForeColor = true;
        _analyze.Cursor = Cursors.Hand;
        _analyze.Click += async (_, _) => await RunAsync(AnalyzeAsync);

        _preview.Text = "Preview";
        SmartPosControlSizing.ConfigureButton(_preview, 120, 34);
        _preview.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _preview.Appearance.Options.UseFont = true;
        _preview.Enabled = false;
        _preview.Cursor = Cursors.Hand;
        _preview.Click += async (_, _) => await RunAsync(PreviewAsync);

        // -------------------------------------------------------------
        // Status & Busy Indicator Setup
        // -------------------------------------------------------------
        _lblStatusMessage.Text = "Ready to analyze completed sales.";
        _lblStatusMessage.AutoSizeMode = LabelAutoSizeMode.Default;
        _lblStatusMessage.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
        _lblStatusMessage.Appearance.ForeColor = Color.FromArgb(100, 116, 139);
        _lblStatusMessage.Appearance.Options.UseFont = true;
        _lblStatusMessage.Appearance.Options.UseForeColor = true;

        _busy.Height = 3;
        _busy.Width = 320;
        _busy.Visible = false;

        // -------------------------------------------------------------
        // Root Layout: Auto-Sized Top Sections + Filling Results Region
        // -------------------------------------------------------------
        var rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 5,
            ColumnCount = 1,
            Padding = new Padding(16),
            Margin = new Padding(0)
        };
        rootLayout.ColumnStyles.Clear();
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        rootLayout.RowStyles.Clear();
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // Row 0: Page Header
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // Row 1: Filter Bar Card
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // Row 2: KPI Cards Bar
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // Row 3: Results Header
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));     // Row 4: Results Content (Grid / Empty State)

        // Add 5 Vertical Regions
        _headerControl = BuildPageHeader();
        _filterCard = BuildFilterCard();
        _kpiCards = BuildKpiCards();
        _resultsHeader = BuildResultsHeader();
        _resultsContent = BuildResultsContent();

        rootLayout.Controls.Add(_headerControl, 0, 0);
        rootLayout.Controls.Add(_filterCard, 0, 1);
        rootLayout.Controls.Add(_kpiCards, 0, 2);
        rootLayout.Controls.Add(_resultsHeader, 0, 3);
        rootLayout.Controls.Add(_resultsContent, 0, 4);

        Controls.Add(rootLayout);

        // -------------------------------------------------------------
        // View Events & Lifecycle
        // -------------------------------------------------------------
        _view.FocusedRowChanged += (_, _) => UpdatePreviewButtonState();
        _view.RowCountChanged += (_, _) => UpdatePreviewButtonState();
        _view.DoubleClick += async (_, _) => await RunAsync(PreviewAsync);

        Load += async (_, _) => await RunAsync(async () =>
        {
            AppearanceManager.Apply(this, "Restaurant", nameof(SmartComboBuilderView));
            // Re-apply DPI-aware sizing after theme font application
            SmartPosControlSizing.ConfigureButton(_analyze, 160, 36);
            SmartPosControlSizing.ConfigureButton(_preview, 120, 34);

#if DEBUG
            SmartPosLayoutTelemetry.LogSmartComboEvent(this, "Load", _lblPeriod, _period, _lblLocation, _location, _analyze, _filterCard, _kpiCards, _grid);
            BeginInvoke(async () =>
            {
                await Task.Delay(100);
                SmartPosLayoutTelemetry.LogSmartComboEvent(this, "BeginInvoke 100ms after load", _lblPeriod, _period, _lblLocation, _location, _analyze, _filterCard, _kpiCards, _grid);
            });
#endif

            using var scope = _scopes.CreateScope();
            var locations = await scope.ServiceProvider.GetRequiredService<ISmartComboAccess>().LocationsAsync(_lifetime.Token);
            _location.Properties.DataSource = locations;
            if (locations.Count > 0)
            {
                _location.EditValue = locations[0].Id;
            }
            UpdateKpiCards(null);
            ShowInitialState();
        });
    }

    /// <summary>
    /// Row 0: Clean auto-sized page header.
    /// </summary>
    private Control BuildPageHeader()
    {
        var header = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, 12),
            Padding = new Padding(0)
        };

        _lblTitle.Text = "SMART COMBO BUILDER";
        _lblTitle.AutoSizeMode = LabelAutoSizeMode.Default;
        _lblTitle.Appearance.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        _lblTitle.Appearance.ForeColor = Color.FromArgb(15, 23, 42); // Slate-900
        _lblTitle.Appearance.Options.UseFont = true;
        _lblTitle.Appearance.Options.UseForeColor = true;
        _lblTitle.Margin = new Padding(0, 0, 0, 4);



        _lblSubtitle.Text = "Discover profitable product combinations from completed restaurant sales.";
        _lblSubtitle.AutoSizeMode = LabelAutoSizeMode.Default;
        _lblSubtitle.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        _lblSubtitle.Appearance.ForeColor = Color.FromArgb(71, 85, 105); // Slate-600
        _lblSubtitle.Appearance.Options.UseFont = true;
        _lblSubtitle.Appearance.Options.UseForeColor = true;
        _lblSubtitle.Margin = new Padding(0);

        header.Controls.Add(_lblTitle);
        header.Controls.Add(_lblSubtitle);

        return header;
    }

    /// <summary>
    /// Row 1: Rebuilt Filter Panel using single TableLayoutPanel (3 AutoSize rows x 4 columns).
    /// Row 0: Labels (AutoSize)
    /// Row 1: Editors / Action (AutoSize)
    /// Row 2: Status (AutoSize)
    /// Col 0: Period (AutoSize)
    /// Col 1: Location (AutoSize)
    /// Col 2: Analyze Button (AutoSize)
    /// Col 3: Spacer (Percent 100%)
    /// </summary>
    private Control BuildFilterCard()
    {
        var filterPanel = new PanelControl
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BorderStyle = BorderStyles.Simple,
            Padding = new Padding(16, 12, 16, 12),
            Margin = new Padding(0, 0, 0, 12)
        };
        filterPanel.Appearance.BackColor = Color.White;
        filterPanel.Appearance.BorderColor = Color.FromArgb(226, 232, 240);
        filterPanel.Appearance.Options.UseBackColor = true;
        filterPanel.Appearance.Options.UseBorderColor = true;

        var filterLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 4,
            RowCount = 3,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        filterLayout.ColumnStyles.Clear();
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));      // Col 0: Period
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));      // Col 1: Location
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));      // Col 2: Analyze Button
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // Col 3: Space absorber

        filterLayout.RowStyles.Clear();
        filterLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));            // Row 0: Labels
        filterLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));            // Row 1: Controls
        filterLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));            // Row 2: Status

        var lblPeriod = new LabelControl
        {
            Text = "Analysis Period",
            AutoSizeMode = LabelAutoSizeMode.Default,
            Margin = new Padding(0, 0, DesktopDpi.Scale(16, this), DesktopDpi.Scale(4, this))
        };
        lblPeriod.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblPeriod.Appearance.ForeColor = Color.FromArgb(71, 85, 105);
        lblPeriod.Appearance.Options.UseFont = true;
        lblPeriod.Appearance.Options.UseForeColor = true;

        var lblLocation = new LabelControl
        {
            Text = "Location / Warehouse",
            AutoSizeMode = LabelAutoSizeMode.Default,
            Margin = new Padding(0, 0, DesktopDpi.Scale(16, this), DesktopDpi.Scale(4, this))
        };
        lblLocation.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblLocation.Appearance.ForeColor = Color.FromArgb(71, 85, 105);
        lblLocation.Appearance.Options.UseFont = true;
        lblLocation.Appearance.Options.UseForeColor = true;

        _lblPeriod = lblPeriod;
        _lblLocation = lblLocation;

        var lblSpacer = new LabelControl
        {
            Text = " ",
            AutoSizeMode = LabelAutoSizeMode.Default,
            Margin = new Padding(0, 0, 0, DesktopDpi.Scale(4, this))
        };

        // Configure controls with explicit sizing and margins
        SmartPosControlSizing.ConfigureEditor(_period, 180, 36);
        _period.Margin = new Padding(0, 0, DesktopDpi.Scale(16, this), 0);
        _period.Dock = DockStyle.None;

        SmartPosControlSizing.ConfigureEditor(_location, 280, 36);
        _location.Margin = new Padding(0, 0, DesktopDpi.Scale(16, this), 0);
        _location.Dock = DockStyle.None;

        _analyze.Text = "Analyze Sales";
        SmartPosControlSizing.ConfigureButton(_analyze, 160, 36);
        _analyze.Margin = new Padding(0);
        _analyze.Dock = DockStyle.None;

        // Add Labels to Row 0
        filterLayout.Controls.Add(lblPeriod, 0, 0);
        filterLayout.Controls.Add(lblLocation, 1, 0);
        filterLayout.Controls.Add(lblSpacer, 2, 0);

        // Add Controls to Row 1
        filterLayout.Controls.Add(_period, 0, 1);
        filterLayout.Controls.Add(_location, 1, 1);
        filterLayout.Controls.Add(_analyze, 2, 1);

        // Status Row (Row 2, spans all 4 columns)
        var statusBox = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 8, 0, 0),
            Padding = new Padding(0)
        };
        _lblStatusMessage.Margin = new Padding(0, 0, 0, 4);
        _busy.Margin = new Padding(0);

        statusBox.Controls.Add(_lblStatusMessage);
        statusBox.Controls.Add(_busy);

        filterLayout.Controls.Add(statusBox, 0, 2);
        filterLayout.SetColumnSpan(statusBox, 4);

        filterPanel.Controls.Add(filterLayout);
        _filterCard = filterPanel;
        return filterPanel;
    }

    /// <summary>
    /// Row 2: Four simple, auto-sized KPI cards with guaranteed minimum height.
    /// </summary>
    private Control BuildKpiCards()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 1,
            ColumnCount = 4,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, 12),
            Padding = new Padding(0)
        };
        panel.ColumnStyles.Clear();
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

        panel.RowStyles.Clear();
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        panel.Controls.Add(CreateKpiCard("COMBO OPPORTUNITIES", _lblKpiOpportunities, Color.FromArgb(13, 148, 136), "New combinations", 0), 0, 0);
        panel.Controls.Add(CreateKpiCard("ELIGIBLE SALES", _lblKpiEligibleSales, Color.FromArgb(15, 23, 42), "Completed orders analyzed", 1), 1, 0);
        panel.Controls.Add(CreateKpiCard("CONVERTED DEALS", _lblKpiConvertedDeals, Color.FromArgb(99, 102, 241), "Created from opportunities", 2), 2, 0);
        panel.Controls.Add(CreateKpiCard("AVG. ATTACH RATE", _lblKpiAvgAttachRate, Color.FromArgb(217, 119, 6), "Across opportunities", 3), 3, 0);

        _kpiCards = panel;
        return panel;
    }

    private static PanelControl CreateKpiCard(string caption, LabelControl valueLabel, Color valueColor, string supportingText, int colIndex)
    {
        var card = new PanelControl
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(0, 95),
            BorderStyle = BorderStyles.Simple,
            Padding = new Padding(16, 12, 16, 12),
            Margin = new Padding(colIndex == 0 ? 0 : 4, 0, colIndex == 3 ? 0 : 4, 0)
        };
        card.Appearance.BackColor = Color.White;
        card.Appearance.BorderColor = Color.FromArgb(226, 232, 240);
        card.Appearance.Options.UseBackColor = true;
        card.Appearance.Options.UseBorderColor = true;

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        content.ColumnStyles.Clear();
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        content.RowStyles.Clear();
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var lblCaption = new LabelControl
        {
            Text = caption,
            AutoSizeMode = LabelAutoSizeMode.Default,
            Margin = new Padding(0, 0, 0, 4)
        };
        lblCaption.Appearance.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
        lblCaption.Appearance.ForeColor = Color.FromArgb(100, 116, 139);
        lblCaption.Appearance.Options.UseFont = true;
        lblCaption.Appearance.Options.UseForeColor = true;

        valueLabel.Text = caption.Contains("ATTACH") ? "—" : "0";
        valueLabel.AutoSizeMode = LabelAutoSizeMode.Default;
        valueLabel.Appearance.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
        valueLabel.Appearance.ForeColor = valueColor;
        valueLabel.Appearance.Options.UseFont = true;
        valueLabel.Appearance.Options.UseForeColor = true;
        valueLabel.Margin = new Padding(0, 0, 0, 4);

        var lblSub = new LabelControl
        {
            Text = supportingText,
            AutoSizeMode = LabelAutoSizeMode.Default,
            Margin = new Padding(0)
        };
        lblSub.Appearance.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular);
        lblSub.Appearance.ForeColor = Color.FromArgb(148, 163, 184);
        lblSub.Appearance.Options.UseFont = true;
        lblSub.Appearance.Options.UseForeColor = true;

        content.Controls.Add(lblCaption, 0, 0);
        content.Controls.Add(valueLabel, 0, 1);
        content.Controls.Add(lblSub, 0, 2);

        card.Controls.Add(content);
        return card;
    }

    /// <summary>
    /// Row 3: Auto-sized results section header.
    /// </summary>
    private Control BuildResultsHeader()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 1,
            ColumnCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, 8),
            Padding = new Padding(0, 4, 0, 4)
        };
        panel.ColumnStyles.Clear();
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.RowStyles.Clear();
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var titleBox = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        var lblHeader = new LabelControl
        {
            Text = "COMBO OPPORTUNITIES",
            AutoSizeMode = LabelAutoSizeMode.Default,
            Margin = new Padding(0, 0, 0, 2)
        };
        lblHeader.Appearance.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblHeader.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
        lblHeader.Appearance.Options.UseFont = true;
        lblHeader.Appearance.Options.UseForeColor = true;

        var lblDesc = new LabelControl
        {
            Text = "Review combinations discovered from completed sales.",
            AutoSizeMode = LabelAutoSizeMode.Default,
            Margin = new Padding(0)
        };
        lblDesc.Appearance.Font = new Font("Segoe UI", 8.75F, FontStyle.Regular);
        lblDesc.Appearance.ForeColor = Color.FromArgb(100, 116, 139);
        lblDesc.Appearance.Options.UseFont = true;
        lblDesc.Appearance.Options.UseForeColor = true;

        titleBox.Controls.Add(lblHeader);
        titleBox.Controls.Add(lblDesc);

        _preview.Anchor = AnchorStyles.Right;
        _preview.Margin = new Padding(12, 0, 0, 0);

        panel.Controls.Add(titleBox, 0, 0);
        panel.Controls.Add(_preview, 1, 0);

        return panel;
    }

    /// <summary>
    /// Row 4: Results host container consuming all remaining height (Percent 100%).
    /// </summary>
    private Control BuildResultsContent()
    {
        var container = new PanelControl
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyles.Simple,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };
        container.Appearance.BackColor = Color.White;
        container.Appearance.BorderColor = Color.FromArgb(226, 232, 240);
        container.Appearance.Options.UseBackColor = true;
        container.Appearance.Options.UseBorderColor = true;

        // Configure GridControl & GridView
        _grid.Dock = DockStyle.Fill;
        _grid.MainView = _view;
        _grid.ViewCollection.Add(_view);

        _view.OptionsBehavior.Editable = false;
        _view.OptionsBehavior.ReadOnly = true;
        _view.OptionsView.ShowGroupPanel = false;
        _view.OptionsView.ShowIndicator = false;
        _view.OptionsView.ColumnAutoWidth = false; // Enable horizontal scrolling when viewport width is constrained
        _view.OptionsView.EnableAppearanceEvenRow = true;
        _view.RowHeight = DesktopDpi.Scale(32, this);
        _view.ColumnPanelRowHeight = DesktopDpi.Scale(36, this);

        _view.Appearance.EvenRow.BackColor = Color.FromArgb(248, 250, 252);
        _view.Appearance.FocusedRow.BackColor = Color.FromArgb(224, 242, 254);
        _view.Appearance.FocusedRow.ForeColor = Color.FromArgb(15, 23, 42);
        _view.Appearance.HeaderPanel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _view.Appearance.HeaderPanel.ForeColor = Color.FromArgb(51, 65, 85);
        _view.Appearance.Row.Font = new Font("Segoe UI", 9F);

        // Columns definition with generous minimum widths
        _view.Columns.Clear();

        void AddColumn(string fieldName, string caption, int width, int minWidth, bool fixedWidth, HorzAlignment hAlign)
        {
            var col = _view.Columns.AddVisible(fieldName, caption);
            col.Width = DesktopDpi.Scale(width, this);
            col.MinWidth = DesktopDpi.Scale(minWidth, this);
            col.OptionsColumn.FixedWidth = fixedWidth;
            col.AppearanceHeader.TextOptions.HAlignment = hAlign;
            col.AppearanceCell.TextOptions.HAlignment = hAlign;
        }

        AddColumn(nameof(ComboRow.Combo), "Suggested Combo", 240, 180, false, HorzAlignment.Near);
        AddColumn(nameof(ComboRow.Items), "Items Composition", 380, 300, false, HorzAlignment.Near);
        AddColumn(nameof(ComboRow.BoughtTogether), "Bought Together", 125, 115, false, HorzAlignment.Far);
        AddColumn(nameof(ComboRow.Support), "Support", 95, 90, false, HorzAlignment.Far);
        AddColumn(nameof(ComboRow.AttachRate), "Attach Rate", 115, 105, false, HorzAlignment.Far);
        AddColumn(nameof(ComboRow.Lift), "Lift", 85, 75, false, HorzAlignment.Far);
        AddColumn(nameof(ComboRow.NormalPrice), "Normal Price", 125, 115, false, HorzAlignment.Far);
        AddColumn(nameof(ComboRow.SuggestedPrice), "Suggested Price", 150, 135, false, HorzAlignment.Far);
        AddColumn(nameof(ComboRow.Discount), "Discount", 100, 90, false, HorzAlignment.Far);
        AddColumn(nameof(ComboRow.EstimatedMargin), "Est. Margin", 125, 115, false, HorzAlignment.Far);
        AddColumn(nameof(ComboRow.Status), "Status", 95, 90, false, HorzAlignment.Center);

        _grid.SizeChanged += (_, _) => UpdateGridAutoWidth();
        UpdateGridAutoWidth();

        // Rich ToolTip on Items column
        var toolTipController = new ToolTipController();
        _grid.ToolTipController = toolTipController;
        toolTipController.GetActiveObjectInfo += (_, e) =>
        {
            if (e.SelectedControl != _grid) return;
            var hitInfo = _view.CalcHitInfo(e.ControlMousePosition);
            if (!hitInfo.InRowCell) return;

            if (hitInfo.Column.FieldName == nameof(ComboRow.Items) && _view.GetRow(hitInfo.RowHandle) is ComboRow row)
            {
                e.Info = new ToolTipControlInfo(
                    e.ControlMousePosition,
                    $"Combo Composition:\n{row.Items}\n\nNormal Total: {row.NormalPrice}\nSuggested Deal: {row.SuggestedPrice}");
            }
        };

        // Configure Empty State Panel
        BuildEmptyStatePanel();

        container.Controls.Add(_grid);
        container.Controls.Add(_emptyPanel);

        return container;
    }

    private void UpdateGridAutoWidth()
    {
        // On wide screens, stretch all columns to fill 100% of the grid area.
        // On narrower screens, preserve minimum readable widths with horizontal scrollbar.
        int threshold = DesktopDpi.Scale(1100, this);
        _view.OptionsView.ColumnAutoWidth = _grid.ClientSize.Width >= threshold;
    }

    /// <summary>
    /// Configures the full-fill empty state panel centered via a 3x3 layout table.
    /// </summary>
    private void BuildEmptyStatePanel()
    {
        _emptyPanel.Dock = DockStyle.Fill;
        _emptyPanel.BorderStyle = BorderStyles.NoBorder;
        _emptyPanel.Appearance.BackColor = Color.FromArgb(248, 250, 252);
        _emptyPanel.Appearance.Options.UseBackColor = true;

        var centerTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 3,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        centerTable.ColumnStyles.Clear();
        centerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        centerTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        centerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        centerTable.RowStyles.Clear();
        centerTable.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        centerTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        centerTable.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        var contentBox = new TableLayoutPanel
        {
            ColumnCount = 1,
            RowCount = 3,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(540, 120),
            MaximumSize = new Size(620, 0),
            Margin = new Padding(0),
            Padding = new Padding(24)
        };
        contentBox.ColumnStyles.Clear();
        contentBox.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        contentBox.RowStyles.Clear();
        contentBox.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contentBox.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contentBox.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _lblEmptyTitle.Text = "Ready to analyze sales";
        _lblEmptyTitle.AutoSizeMode = LabelAutoSizeMode.Default;
        _lblEmptyTitle.Appearance.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        _lblEmptyTitle.Appearance.ForeColor = Color.FromArgb(30, 41, 59);
        _lblEmptyTitle.Appearance.TextOptions.HAlignment = HorzAlignment.Center;
        _lblEmptyTitle.Appearance.Options.UseFont = true;
        _lblEmptyTitle.Appearance.Options.UseForeColor = true;
        _lblEmptyTitle.Appearance.Options.UseTextOptions = true;
        _lblEmptyTitle.Dock = DockStyle.Top;
        _lblEmptyTitle.Margin = new Padding(0, 0, 0, 8);

        _lblEmptyDescription.Text = "Choose an analysis period and location, then select Analyze Sales to discover combinations customers frequently purchase together.";
        _lblEmptyDescription.AutoSizeMode = LabelAutoSizeMode.Vertical;
        _lblEmptyDescription.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        _lblEmptyDescription.Appearance.ForeColor = Color.FromArgb(100, 116, 139);
        _lblEmptyDescription.Appearance.TextOptions.HAlignment = HorzAlignment.Center;
        _lblEmptyDescription.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
        _lblEmptyDescription.Appearance.Options.UseFont = true;
        _lblEmptyDescription.Appearance.Options.UseForeColor = true;
        _lblEmptyDescription.Appearance.Options.UseTextOptions = true;
        _lblEmptyDescription.Dock = DockStyle.Top;
        _lblEmptyDescription.Margin = new Padding(0);

        _btnRetry.Text = "Retry Analysis";
        _btnRetry.Size = new Size(130, 32);
        _btnRetry.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _btnRetry.Appearance.BackColor = Color.FromArgb(13, 148, 136);
        _btnRetry.Appearance.ForeColor = Color.White;
        _btnRetry.Appearance.Options.UseFont = true;
        _btnRetry.Appearance.Options.UseBackColor = true;
        _btnRetry.Appearance.Options.UseForeColor = true;
        _btnRetry.Cursor = Cursors.Hand;
        _btnRetry.Visible = false;
        _btnRetry.Anchor = AnchorStyles.None;
        _btnRetry.Margin = new Padding(0, 14, 0, 0);
        _btnRetry.Click += async (_, _) => await RunAsync(AnalyzeAsync);

        contentBox.Controls.Add(_lblEmptyTitle, 0, 0);
        contentBox.Controls.Add(_lblEmptyDescription, 0, 1);
        contentBox.Controls.Add(_btnRetry, 0, 2);

        centerTable.Controls.Add(contentBox, 1, 1);
        _emptyPanel.Controls.Add(centerTable);
    }

    private void ShowInitialState()
    {
        _emptyPanel.BringToFront();
        _emptyPanel.Visible = true;
        _grid.Visible = false;
        _lblEmptyTitle.Text = "Ready to analyze sales";
        _lblEmptyDescription.Text = "Choose an analysis period and location, then select Analyze Sales\nto discover combinations customers frequently purchase together.";
        _btnRetry.Visible = false;
    }

    private void InvalidateAnalysis()
    {
        _analysis = null;
        _grid.DataSource = null;
        UpdatePreviewButtonState();
        UpdateKpiCards(null);
        ShowInitialState();
        _lblStatusMessage.Text = "Ready to analyze completed sales.";
    }

    private void UpdatePreviewButtonState()
    {
        var row = _view.GetFocusedRow() as ComboRow;
        _preview.Enabled = !_working && row != null && _analysis?.Opportunities.Count > 0;
    }

    private void UpdateKpiCards(ComboAnalysis? analysis)
    {
        if (analysis == null)
        {
            _lblKpiOpportunities.Text = "0";
            _lblKpiEligibleSales.Text = "0";
            _lblKpiConvertedDeals.Text = "0";
            _lblKpiAvgAttachRate.Text = "—";
            return;
        }

        _lblKpiOpportunities.Text = analysis.Opportunities.Count.ToString("N0");
        _lblKpiEligibleSales.Text = analysis.EligibleOrders.ToString("N0");
        _lblKpiConvertedDeals.Text = analysis.ConvertedCount.ToString("N0");
        _lblKpiAvgAttachRate.Text = analysis.Opportunities.Count > 0
            ? analysis.Opportunities.Average(x => x.AttachRate).ToString("P1")
            : "—";
    }

    private async Task RunAsync(Func<Task> action)
    {
        if (_working || IsDisposed) return;
        _working = true;
        _busy.Visible = true;
        _analyze.Enabled = _preview.Enabled = _location.Enabled = _period.Enabled = _days.Enabled = false;
        _lblStatusMessage.Text = "Analyzing completed sales...";

        try
        {
            await action();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            if (!IsDisposed)
            {
                XtraMessageBox.Show(this, ex.Message, "Smart Combo Builder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _emptyPanel.BringToFront();
                _emptyPanel.Visible = true;
                _grid.Visible = false;
                _lblEmptyTitle.Text = "Unable to analyze sales";
                _lblEmptyDescription.Text = $"The analysis could not be completed.\n{ex.Message}\n\nPlease check connection or try again.";
                _btnRetry.Visible = true;
                _lblStatusMessage.Text = "Analysis failed.";
            }
        }
        finally
        {
            _working = false;
            if (!IsDisposed)
            {
                _busy.Visible = false;
                _analyze.Enabled = _location.Enabled = _period.Enabled = _days.Enabled = true;
                UpdatePreviewButtonState();
            }
        }
    }

    private async Task AnalyzeAsync()
    {
        if (_location.EditValue is not Guid warehouse)
            throw new InvalidOperationException("Select a permitted location.");

        int days = _period.EditValue is int p ? p : (int)_days.Value;
        using var scope = _scopes.CreateScope();
        var currency = await scope.ServiceProvider.GetRequiredService<ISmartComboAccess>().CurrencyAsync(warehouse, _lifetime.Token);
        CurrencyDisplay.Configure(currency.Symbol, currency.DecimalPlaces);

        var result = await scope.ServiceProvider.GetRequiredService<IMediator>().Send(
            new AnalyzeSmartCombosQuery(warehouse, days), _lifetime.Token);

        if (IsDisposed) return;

        _analysis = result;
        UpdateKpiCards(result);

        if (result.Opportunities.Count > 0)
        {
            _grid.DataSource = result.Opportunities.Select(x => new ComboRow(x)).ToList();
            _emptyPanel.Visible = false;
            _grid.BringToFront();
            _grid.Visible = true;
            _view.FocusedRowHandle = 0;
            UpdateGridAutoWidth();
            UpdatePreviewButtonState();
            _lblStatusMessage.Text = $"Analysis complete — {result.EligibleOrders} eligible orders analyzed, {result.Opportunities.Count} opportunities discovered.";
        }
        else
        {
            _grid.DataSource = Array.Empty<ComboRow>();
            _emptyPanel.BringToFront();
            _emptyPanel.Visible = true;
            _grid.Visible = false;
            UpdatePreviewButtonState();

            var locName = _location.Text;
            if (string.IsNullOrWhiteSpace(locName)) locName = "selected location";

            _lblEmptyTitle.Text = "No combo opportunities found";
            _lblEmptyDescription.Text =
                $"No combinations met the current thresholds for {locName}\nduring the selected period.\n\n" +
                $"{result.EligibleOrders} eligible completed orders were analyzed.";
            _btnRetry.Visible = false;
            _lblStatusMessage.Text = $"Analysis complete — {result.EligibleOrders} eligible orders analyzed, no qualifying combinations found.";
        }
    }

    private async Task PreviewAsync()
    {
        if (_analysis == null || _view.GetFocusedRow() is not ComboRow row) return;
        var opportunity = _analysis.Opportunities.SingleOrDefault(x => x.Signature == row.Signature);
        if (opportunity == null) return;

        using var scope = _scopes.CreateScope();
        var access = scope.ServiceProvider.GetRequiredService<ISmartComboAccess>();
        bool create = true, dismiss = true;
        try { await access.RequireAsync("create", _analysis.WarehouseId, _lifetime.Token); } catch (UnauthorizedAccessException) { create = false; }
        try { await access.RequireAsync("dismiss", _analysis.WarehouseId, _lifetime.Token); } catch (UnauthorizedAccessException) { dismiss = false; }

        using var dialog = new SmartComboPreviewDialog(opportunity, _analysis, create, dismiss);
        var result = dialog.ShowDialog(this);
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        int days = _period.EditValue is int p ? p : (int)_days.Value;
        if (result == DialogResult.Yes)
        {
            await mediator.Send(new ConvertSmartComboCommand(_analysis.WarehouseId, days, opportunity.Signature, dialog.ComboName, dialog.DealPrice), _lifetime.Token);
            XtraMessageBox.Show(this, "Deal created. Open Restaurant POS → Quick Orders at this location.", "Smart Combo Builder");
        }
        else if (result == DialogResult.No)
        {
            await mediator.Send(new DismissSmartComboCommand(_analysis.WarehouseId, days, opportunity.Signature, dialog.Reason), _lifetime.Token);
        }
        else
        {
            return;
        }

        await AnalyzeAsync();
    }

#if DEBUG
    /// <summary>
    /// Programmatic layout diagnostics helper to inspect bounds and detect overlaps.
    /// </summary>
    public string InspectLayoutDiagnostics()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== SMART COMBO BUILDER LAYOUT DIAGNOSTICS ===");

        void CheckControl(Control c, string name)
        {
            var p = c.Parent;
            var client = p != null ? p.ClientRectangle : Rectangle.Empty;
            sb.AppendLine($"{name}: Bounds={c.Bounds}, Visible={c.Visible}, MinSize={c.MinimumSize}, ParentClient={client}");

            if (p != null && c.Visible && client.Width > 0 && client.Height > 0)
            {
                if (c.Right > client.Right + 4)
                    sb.AppendLine($"  WARNING: {name} Right ({c.Right}) exceeds Parent Client Right ({client.Right})");
                if (c.Bottom > client.Bottom + 4)
                    sb.AppendLine($"  WARNING: {name} Bottom ({c.Bottom}) exceeds Parent Client Bottom ({client.Bottom})");
            }
        }

        CheckControl(_lblTitle, nameof(_lblTitle));
        CheckControl(_lblSubtitle, nameof(_lblSubtitle));
        CheckControl(_period, nameof(_period));
        CheckControl(_location, nameof(_location));
        CheckControl(_analyze, nameof(_analyze));
        CheckControl(_lblStatusMessage, nameof(_lblStatusMessage));
        CheckControl(_lblKpiOpportunities, nameof(_lblKpiOpportunities));
        CheckControl(_lblKpiEligibleSales, nameof(_lblKpiEligibleSales));
        CheckControl(_lblKpiConvertedDeals, nameof(_lblKpiConvertedDeals));
        CheckControl(_lblKpiAvgAttachRate, nameof(_lblKpiAvgAttachRate));
        CheckControl(_preview, nameof(_preview));
        CheckControl(_grid, nameof(_grid));
        CheckControl(_emptyPanel, nameof(_emptyPanel));
        CheckControl(_lblEmptyTitle, nameof(_lblEmptyTitle));
        CheckControl(_lblEmptyDescription, nameof(_lblEmptyDescription));

        return sb.ToString();
    }
#endif

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _lifetime.Cancel();
            _lifetime.Dispose();
        }
        base.Dispose(disposing);
    }

    public sealed class ComboRow(ComboOpportunity opportunity)
    {
        internal string Signature => opportunity.Signature;
        public string Combo => opportunity.Name;
        public string Items => string.Join(" + ", opportunity.Items.Select(x => x.DisplayName));
        public int BoughtTogether => opportunity.Frequency;
        public string Support => opportunity.Support.ToString("P2");
        public string AttachRate => opportunity.AttachRate.ToString("P2");
        public string Lift => opportunity.Lift.ToString("N2");
        public string NormalPrice => CurrencyDisplay.FormatPlain(opportunity.NormalPrice);
        public string SuggestedPrice => CurrencyDisplay.FormatPlain(opportunity.SuggestedPrice);
        public string Discount => opportunity.DiscountRate.ToString("P2");
        public string EstimatedMargin => opportunity.Margin?.ToString("P2") ?? "Cost unavailable";
        public string Status => "New";
    }
}
