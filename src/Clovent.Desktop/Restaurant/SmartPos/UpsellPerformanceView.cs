using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Desktop.Sessions;
using Clovent.Restaurant.Application.SmartRecommendations.Dtos;
using Clovent.Restaurant.Application.SmartRecommendations.Queries;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Restaurant.SmartPos;

/// <summary>
/// Upsell Performance view: an operational reporting grid aggregating the POS's
/// suggestion analytics (offered / accepted / dismissed / conversion /
/// attributed upsell revenue) over a selectable date range using the standard
/// Report Period UX. Read-only - configuration lives in <see cref="RecommendationRulesView"/>.
/// </summary>
public sealed class UpsellPerformanceView : XtraUserControl
{
    private const string FeatureCode = "upsellperformance";

    private readonly IServiceScope _scope;
    private readonly ScreenOperationGate _gate = new();
    private readonly IMediator _mediator;
    private readonly ILogger<UpsellPerformanceView> _logger;
    private readonly ICurrentSession _currentSession;

    private PanelControl _headerPanel = null!;
    private TableLayoutPanel _toolbar = null!;
    private ComboBoxEdit _periodCombo = null!;
    private DateEdit _fromEdit = null!;
    private DateEdit _toEdit = null!;
    private SimpleButton _refreshButton = null!;
    private GridControl _gridControl = null!;
    private GridView _gridView = null!;
    private LabelControl _lblSummary = null!;

    private bool _isLoading;
    private bool _isUpdatingPeriod;

    /// <summary>Builds the screen and starts its own DI scope.</summary>
    public UpsellPerformanceView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = new SerializedMediator(_scope.ServiceProvider.GetRequiredService<IMediator>(), _gate);
        _logger = _scope.ServiceProvider.GetRequiredService<ILogger<UpsellPerformanceView>>();
        _currentSession = currentSession;

        BuildLayout();
    }

    /// <summary>Design-time-only constructor for Visual Studio Designer.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public UpsellPerformanceView()
    {
        _scope = null!;
        _mediator = null!;
        _logger = null!;
        _currentSession = null!;

        BuildLayout();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _scope?.Dispose();
            _gate?.Dispose();
        }
        base.Dispose(disposing);
    }

    private Task TryRunAsync(Func<Task> action, string actionDescription) =>
        GuardedAction.RunAsync(this, _logger, action, actionDescription);

    private void BuildLayout()
    {
        Dock = DockStyle.Fill;
        Name = "UpsellPerformanceView";

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Margin = new Padding(0)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Header
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Toolbar
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Grid
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Summary strip

        // ---- 1. Page Header ----
        _headerPanel = new PanelControl
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder,
            Padding = new Padding(16, 12, 16, 6),
            Margin = new Padding(0)
        };

        var titleBox = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0)
        };
        titleBox.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        titleBox.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        titleBox.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var titleLabel = new LabelControl
        {
            Text = "UPSELL PERFORMANCE",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Margin = new Padding(0, 0, 0, 2)
        };

        var subTitleLabel = new LabelControl
        {
            Text = "Analyze recommendation effectiveness and additional revenue.",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
            ForeColor = Color.FromArgb(100, 116, 139),
            Margin = new Padding(0)
        };

        titleBox.Controls.Add(titleLabel, 0, 0);
        titleBox.Controls.Add(subTitleLabel, 0, 1);
        _headerPanel.Controls.Add(titleBox);

        // ---- 2. Standard Report Period Toolbar ----
        _toolbar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 4,
            RowCount = 1,
            Padding = new Padding(16, 2, 16, 8),
            Margin = new Padding(0),
            BackColor = Color.White
        };
        _toolbar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var periodLabel = new LabelControl
        {
            Text = "Report Period:",
            AutoSizeMode = LabelAutoSizeMode.Horizontal,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(51, 65, 85),
            Margin = new Padding(0, 6, 6, 0)
        };

        _periodCombo = new ComboBoxEdit
        {
            Margin = new Padding(0),
            Properties = { TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor }
        };
        _periodCombo.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _periodCombo.Properties.Appearance.Options.UseFont = true;
        _periodCombo.Properties.AppearanceDropDown.Font = new Font("Segoe UI", 9.5F);
        _periodCombo.Properties.AppearanceDropDown.Options.UseFont = true;
        _periodCombo.Size = new Size(130, 30);
        foreach (var (_, name) in ReportPeriodCalculator.GetAllOptions())
        {
            _periodCombo.Properties.Items.Add(name);
        }

        var fromLabel = new LabelControl
        {
            Text = "From:",
            AutoSizeMode = LabelAutoSizeMode.Horizontal,
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = Color.Gray,
            Margin = new Padding(0, 6, 6, 0)
        };

        var dateFormat = BusinessDateTimeService.ExtractDateFormatPattern(DateTimeDisplay.FormatString);

        _fromEdit = new DateEdit
        {
            Margin = new Padding(0),
            Size = new Size(120, 30),
            Properties =
            {
                EditMask = dateFormat,
                DisplayFormat = { FormatString = dateFormat, FormatType = DevExpress.Utils.FormatType.Custom }
            }
        };
        _fromEdit.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _fromEdit.Properties.Appearance.Options.UseFont = true;

        var toLabel = new LabelControl
        {
            Text = "To:",
            AutoSizeMode = LabelAutoSizeMode.Horizontal,
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = Color.Gray,
            Margin = new Padding(0, 6, 6, 0)
        };

        _toEdit = new DateEdit
        {
            Margin = new Padding(0),
            Size = new Size(120, 30),
            Properties =
            {
                EditMask = dateFormat,
                DisplayFormat = { FormatString = dateFormat, FormatType = DevExpress.Utils.FormatType.Custom }
            }
        };
        _toEdit.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _toEdit.Properties.Appearance.Options.UseFont = true;

        // Initialize default to "Last 30 Days"
        var defaultRange = ReportPeriodCalculator.CalculateRange(ReportPeriod.Last30Days, DateOnly.FromDateTime(DateTime.Today));
        _fromEdit.EditValue = defaultRange.From.ToDateTime(TimeOnly.MinValue);
        _toEdit.EditValue = defaultRange.To.ToDateTime(TimeOnly.MinValue);
        _periodCombo.SelectedItem = "Last 30 Days";

        _periodCombo.SelectedIndexChanged += PeriodCombo_SelectedIndexChanged;
        _fromEdit.EditValueChanged += DateEdit_EditValueChanged;
        _toEdit.EditValueChanged += DateEdit_EditValueChanged;

        _refreshButton = new SimpleButton
        {
            Text = "Refresh",
            Margin = new Padding(0, 1, 0, 1),
            MinimumSize = new Size(90, 30),
            Cursor = Cursors.Hand
        };
        _refreshButton.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _refreshButton.Appearance.BackColor = Color.FromArgb(13, 148, 136); // Teal-600
        _refreshButton.Appearance.ForeColor = Color.White;
        _refreshButton.Appearance.Options.UseFont = true;
        _refreshButton.Appearance.Options.UseBackColor = true;
        _refreshButton.Appearance.Options.UseForeColor = true;
        _refreshButton.Click += RefreshButton_Click;

        var periodPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 0, 10, 0),
            Padding = new Padding(0)
        };
        periodPanel.Controls.Add(periodLabel);
        periodPanel.Controls.Add(_periodCombo);

        var fromPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 0, 10, 0),
            Padding = new Padding(0)
        };
        fromPanel.Controls.Add(fromLabel);
        fromPanel.Controls.Add(_fromEdit);

        var toPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 0, 10, 0),
            Padding = new Padding(0)
        };
        toPanel.Controls.Add(toLabel);
        toPanel.Controls.Add(_toEdit);

        _toolbar.Controls.Add(periodPanel, 0, 0);
        _toolbar.Controls.Add(fromPanel, 1, 0);
        _toolbar.Controls.Add(toPanel, 2, 0);
        _toolbar.Controls.Add(_refreshButton, 3, 0);

        // ---- 3. Grid ----
        _gridControl = new GridControl { Dock = DockStyle.Fill, Margin = new Padding(16, 0, 16, 0) };
        _gridView = new GridView(_gridControl)
        {
            OptionsBehavior = { Editable = false, ReadOnly = true },
            OptionsView = { ShowGroupPanel = false, ShowIndicator = false, ColumnAutoWidth = true }
        };
        _gridView.Appearance.Row.Font = new Font("Segoe UI", 9.5F);
        _gridView.Appearance.Row.Options.UseFont = true;
        _gridView.Appearance.HeaderPanel.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _gridView.Appearance.HeaderPanel.Options.UseFont = true;
        _gridView.CustomDrawEmptyForeground += (_, e) =>
        {
            if (_gridView.RowCount > 0) return;
            e.Handled = true;
            using var font = new Font("Segoe UI", 10F);
            TextRenderer.DrawText(e.Graphics, "No upsell recommendations recorded for the selected period.", font, e.Bounds, Color.Gray,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        };

        _gridControl.MainView = _gridView;
        _gridControl.ViewCollection.Add(_gridView);

        // ---- 4. Summary Strip ----
        var summaryContainer = new PanelControl
        {
            Dock = DockStyle.Fill,
            BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder,
            Padding = new Padding(16, 8, 16, 12),
            Margin = new Padding(0)
        };

        _lblSummary = new LabelControl
        {
            Dock = DockStyle.Fill,
            AutoSizeMode = LabelAutoSizeMode.None,
            Text = string.Empty
        };
        _lblSummary.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _lblSummary.Appearance.ForeColor = Color.FromArgb(71, 85, 105);
        _lblSummary.Appearance.Options.UseFont = true;
        _lblSummary.Appearance.Options.UseForeColor = true;
        summaryContainer.Controls.Add(_lblSummary);

        root.Controls.Add(_headerPanel, 0, 0);
        root.Controls.Add(_toolbar, 0, 1);
        root.Controls.Add(_gridControl, 0, 2);
        root.Controls.Add(summaryContainer, 0, 3);
        Controls.Add(root);

        Load += UpsellPerformanceView_Load;
    }

    private void PeriodCombo_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_isUpdatingPeriod) return;

        var periodName = _periodCombo.SelectedItem?.ToString();
        var period = ReportPeriodCalculator.ParseDisplayName(periodName);
        if (period == ReportPeriod.Custom) return;

        _isUpdatingPeriod = true;
        try
        {
            var range = ReportPeriodCalculator.CalculateRange(period, DateOnly.FromDateTime(DateTime.Today));
            _fromEdit.EditValue = range.From.ToDateTime(TimeOnly.MinValue);
            _toEdit.EditValue = range.To.ToDateTime(TimeOnly.MinValue);
        }
        finally
        {
            _isUpdatingPeriod = false;
        }
    }

    private void DateEdit_EditValueChanged(object? sender, EventArgs e)
    {
        if (_isUpdatingPeriod) return;

        if (_periodCombo.SelectedItem?.ToString() != "Custom")
        {
            _isUpdatingPeriod = true;
            try
            {
                _periodCombo.SelectedItem = "Custom";
            }
            finally
            {
                _isUpdatingPeriod = false;
            }
        }
    }

    private async void UpsellPerformanceView_Load(object? sender, EventArgs e)
    {
        if (DesignModeHelper.IsInDesignMode)
            return;

        ScaleLayoutAtRuntime();
        AppearanceManager.Apply(this, "Restaurant", nameof(UpsellPerformanceView));
        await TryRunAsync(() => RefreshAsync(), "load the upsell performance");
    }

    private void ScaleLayoutAtRuntime()
    {
        if (DesignModeHelper.IsInDesignMode) return;

        int editorH = DesktopDpi.Scale(30, this);
        _periodCombo.MinimumSize = new Size(DesktopDpi.Scale(130, this), editorH);
        _fromEdit.MinimumSize = new Size(DesktopDpi.Scale(120, this), editorH);
        _toEdit.MinimumSize = new Size(DesktopDpi.Scale(120, this), editorH);
        _refreshButton.MinimumSize = new Size(DesktopDpi.Scale(90, this), editorH);

        _gridView.RowHeight = DesktopDpi.Scale(28, this);
        _gridView.ColumnPanelRowHeight = DesktopDpi.Scale(36, this);
    }

    private async void RefreshButton_Click(object? sender, EventArgs e) =>
        await TryRunAsync(() => RefreshAsync(), "refresh the upsell performance");

    private async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (_isLoading)
        {
            return;
        }

        if (_fromEdit.EditValue is not DateTime fromDt || _toEdit.EditValue is not DateTime toDt)
        {
            return;
        }

        var range = new DateRange(DateOnly.FromDateTime(fromDt), DateOnly.FromDateTime(toDt));
        if (!range.IsValid)
        {
            XtraMessageBox.Show(this, "'To' date cannot be before 'From' date.", "Invalid Date Range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _isLoading = true;
        _refreshButton.Enabled = false;
        Cursor = Cursors.WaitCursor;
        try
        {
            var fromUtc = range.StartOfFromUtc;
            var toUtc = range.EndOfToUtcInclusive;

            var rows = await _mediator.Send(new GetUpsellPerformanceQuery(fromUtc, toUtc), cancellationToken);
            BindGrid([.. rows]);

            var from = range.From;
            var to = range.To;
            var totalOffers = rows.Sum(r => r.Offers);
            var totalAccepted = rows.Sum(r => r.Accepted);
            var totalDismissed = rows.Sum(r => r.Dismissed);
            var overallConversion = totalOffers == 0 ? 0m : Math.Round(100m * totalAccepted / totalOffers, 1);
            var totalRevenue = rows.Sum(r => r.UpsellRevenue);

            _lblSummary.Text = $"{DateTimeDisplay.FormatDate(from)} – {DateTimeDisplay.FormatDate(to)}   |   " +
                $"Offers: {totalOffers}   |   " +
                $"Accepted: {totalAccepted}   |   " +
                $"Dismissed: {totalDismissed}   |   " +
                $"Conversion: {overallConversion:0.0}%   |   " +
                $"Upsell Revenue: {CurrencyDisplay.Format(totalRevenue)}";
        }
        finally
        {
            _isLoading = false;
            _refreshButton.Enabled = true;
            Cursor = Cursors.Default;
        }
    }

    private void BindGrid(List<UpsellPerformanceDto> rows)
    {
        _gridControl.DataSource = rows.Select(r => new PerformanceGridRow(r)).ToList();
        _gridView.Columns.Clear();

        void AddColumn(string fieldName, string caption, int width, int minWidth, DevExpress.Utils.HorzAlignment alignment = DevExpress.Utils.HorzAlignment.Near)
        {
            var column = _gridView.Columns.AddVisible(fieldName, caption);
            column.Width = DesktopDpi.Scale(width, this);
            column.MinWidth = DesktopDpi.Scale(minWidth, this);
            column.OptionsColumn.AllowEdit = false;
            column.AppearanceCell.TextOptions.HAlignment = alignment;
            column.AppearanceCell.Options.UseTextOptions = true;
            column.AppearanceHeader.TextOptions.HAlignment = alignment;
            column.AppearanceHeader.Options.UseTextOptions = true;
        }

        AddColumn(nameof(PerformanceGridRow.ProductName), "Recommended Item", 280, 180);
        AddColumn(nameof(PerformanceGridRow.VariantName), "Variant", 120, 90);
        AddColumn(nameof(PerformanceGridRow.Offers), "Offers", 65, 55, DevExpress.Utils.HorzAlignment.Far);
        AddColumn(nameof(PerformanceGridRow.Accepted), "Accepted", 75, 70, DevExpress.Utils.HorzAlignment.Far);
        AddColumn(nameof(PerformanceGridRow.Dismissed), "Dismissed", 75, 70, DevExpress.Utils.HorzAlignment.Far);
        AddColumn(nameof(PerformanceGridRow.ConversionText), "Conversion %", 90, 85, DevExpress.Utils.HorzAlignment.Far);
        AddColumn(nameof(PerformanceGridRow.UpsellRevenueText), "Upsell Revenue", 110, 100, DevExpress.Utils.HorzAlignment.Far);
    }

    private sealed class PerformanceGridRow(UpsellPerformanceDto dto)
    {
        public string ProductName => dto.ProductName;
        public string VariantName => dto.VariantName;
        public int Offers => dto.Offers;
        public int Accepted => dto.Accepted;
        public int Dismissed => dto.Dismissed;
        public string ConversionText => $"{dto.ConversionPercent:0.0}%";
        public string UpsellRevenueText => CurrencyDisplay.FormatPlain(dto.UpsellRevenue);
    }
}
