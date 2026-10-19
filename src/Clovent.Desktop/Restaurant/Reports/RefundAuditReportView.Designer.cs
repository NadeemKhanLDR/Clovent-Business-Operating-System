using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;

namespace Clovent.Desktop.Restaurant.Reports;

partial class RefundAuditReportView
{
    private System.ComponentModel.IContainer components = null;

    private readonly TableLayoutPanel _rootLayout = new();
    private readonly PanelControl _filterPanel = new();
    private readonly FlowLayoutPanel _filterFlow = new();
    private readonly TableLayoutPanel _warehousePair = new();
    private readonly LabelControl _warehouseLabel = new();
    private readonly ComboBoxEdit _warehouseCombo = new();
    private readonly TableLayoutPanel _periodPair = new();
    private readonly LabelControl _periodLabel = new();
    private readonly ComboBoxEdit _periodCombo = new();
    private readonly TableLayoutPanel _fromPair = new();
    private readonly LabelControl _fromLabel = new();
    private readonly DateEdit _fromDateEdit = new();
    private readonly TableLayoutPanel _toPair = new();
    private readonly LabelControl _toLabel = new();
    private readonly DateEdit _toDateEdit = new();
    private readonly SimpleButton _refreshButton = new();

    private readonly PanelControl _kpiPanel = new();
    private readonly FlowLayoutPanel _kpiFlow = new();
    private readonly LabelControl _kpiCount = new();
    private readonly LabelControl _kpiGrossRefunded = new();
    private readonly LabelControl _kpiTaxReversed = new();
    private readonly LabelControl _kpiNetRefunded = new();
    private readonly LabelControl _kpiCashPayouts = new();
    private readonly LabelControl _kpiCardRefunds = new();

    private readonly GridControl _gridControl = new();
    private readonly GridView _gridView = new();

    /// <summary>Standard Designer-generated disposal of components.</summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            AppearanceManager.Changed -= AppearanceManager_Changed;
            _gate?.Dispose();
            _scope?.Dispose();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        ((System.ComponentModel.ISupportInitialize)_warehouseCombo.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_periodCombo.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_fromDateEdit.Properties.CalendarTimeProperties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_fromDateEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_toDateEdit.Properties.CalendarTimeProperties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_toDateEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_filterPanel).BeginInit();
        _filterPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_kpiPanel).BeginInit();
        _kpiPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_gridControl).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_gridView).BeginInit();
        SuspendLayout();

        Dock = DockStyle.Fill;
        Name = "RefundAuditReportView";

        BuildLayout();
        WireEvents();

        ((System.ComponentModel.ISupportInitialize)_warehouseCombo.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_periodCombo.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_fromDateEdit.Properties.CalendarTimeProperties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_fromDateEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_toDateEdit.Properties.CalendarTimeProperties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_toDateEdit.Properties).EndInit();
        _filterPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_filterPanel).EndInit();
        _kpiPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_kpiPanel).EndInit();
        ((System.ComponentModel.ISupportInitialize)_gridControl).EndInit();
        ((System.ComponentModel.ISupportInitialize)_gridView).EndInit();
        ResumeLayout(false);

        AppearanceManager.Changed += AppearanceManager_Changed;
        Load += RefundAuditReportView_Load;
    }

    private void BuildLayout()
    {
        _rootLayout.Dock = DockStyle.Fill;
        _rootLayout.ColumnCount = 1;
        _rootLayout.RowCount = 3;
        _rootLayout.Padding = new Padding(12);
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        // 1. Filter Strip
        _filterPanel.Dock = DockStyle.Fill;
        _filterPanel.AutoSize = true;
        _filterPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _filterPanel.Padding = new Padding(10);
        _filterPanel.Margin = new Padding(0, 0, 0, 10);

        _filterFlow.Dock = DockStyle.Top;
        _filterFlow.AutoSize = true;
        _filterFlow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _filterFlow.FlowDirection = FlowDirection.LeftToRight;
        _filterFlow.WrapContents = true;

        SetupFilterPair(_warehousePair, _warehouseLabel, "Warehouse:", _warehouseCombo, 260);
        _warehouseCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _warehouseCombo.Properties.DropDownRows = 8;

        SetupFilterPair(_periodPair, _periodLabel, "Period:", _periodCombo, 140);
        _periodCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _periodCombo.Properties.DropDownRows = 8;

        SetupDatePair(_fromPair, _fromLabel, "From:", _fromDateEdit, 130);
        SetupDatePair(_toPair, _toLabel, "To:", _toDateEdit, 130);

        _refreshButton.Text = "Generate Report";
        _refreshButton.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _refreshButton.Appearance.Options.UseFont = true;
        _refreshButton.Anchor = AnchorStyles.Left;
        _refreshButton.Margin = new Padding(4, 0, 0, 4);
        _refreshButton.AutoSize = true;
        _refreshButton.MinimumSize = new Size(140, DesktopStyle.ToolbarControlHeight);
        _refreshButton.Size = new Size(140, DesktopStyle.ToolbarControlHeight);

        _filterFlow.Controls.Add(_warehousePair);
        _filterFlow.Controls.Add(_periodPair);
        _filterFlow.Controls.Add(_fromPair);
        _filterFlow.Controls.Add(_toPair);
        _filterFlow.Controls.Add(_refreshButton);

        _filterPanel.Controls.Add(_filterFlow);
        _rootLayout.Controls.Add(_filterPanel, 0, 0);

        // 2. KPI Summary Strip
        _kpiPanel.Dock = DockStyle.Fill;
        _kpiPanel.AutoSize = true;
        _kpiPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _kpiPanel.Padding = new Padding(10);
        _kpiPanel.Margin = new Padding(0, 0, 0, 10);

        _kpiFlow.Dock = DockStyle.Top;
        _kpiFlow.AutoSize = true;
        _kpiFlow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _kpiFlow.FlowDirection = FlowDirection.LeftToRight;
        _kpiFlow.WrapContents = true;

        void StyleKpi(LabelControl lbl, string text, Color color)
        {
            lbl.Text = text;
            lbl.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lbl.Appearance.ForeColor = color;
            lbl.Appearance.Options.UseFont = true;
            lbl.Appearance.Options.UseForeColor = true;
            lbl.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            lbl.Appearance.Options.UseTextOptions = true;
            lbl.AutoSizeMode = LabelAutoSizeMode.Horizontal;
            lbl.Margin = new Padding(0, 4, 20, 4);
            lbl.Padding = Padding.Empty;
        }

        StyleKpi(_kpiCount, "Refunds: 0", Color.Black);
        StyleKpi(_kpiGrossRefunded, "Gross: 0.00", Color.Black);
        StyleKpi(_kpiTaxReversed, "Tax Reversed: 0.00", Color.SteelBlue);
        StyleKpi(_kpiNetRefunded, "Total Refunded: 0.00", Color.IndianRed);
        StyleKpi(_kpiCashPayouts, "Cash Payouts: 0.00", Color.DarkGreen);
        StyleKpi(_kpiCardRefunds, "Acct Credits: 0.00 | Card: 0.00", Color.DarkSlateBlue);

        _kpiFlow.Controls.Add(_kpiCount);
        _kpiFlow.Controls.Add(_kpiGrossRefunded);
        _kpiFlow.Controls.Add(_kpiTaxReversed);
        _kpiFlow.Controls.Add(_kpiNetRefunded);
        _kpiFlow.Controls.Add(_kpiCashPayouts);
        _kpiFlow.Controls.Add(_kpiCardRefunds);

        _kpiPanel.Controls.Add(_kpiFlow);
        _rootLayout.Controls.Add(_kpiPanel, 0, 1);

        // 3. Grid
        _gridControl.Dock = DockStyle.Fill;
        _gridControl.MainView = _gridView;
        _gridControl.ViewCollection.Add(_gridView);
        _gridView.OptionsView.ShowGroupPanel = false;
        _gridView.OptionsView.ShowIndicator = false;
        _gridView.OptionsBehavior.Editable = false;
        _gridView.RowHeight = 28;

        _rootLayout.Controls.Add(_gridControl, 0, 2);
        Controls.Add(_rootLayout);

        _filterPanel.Resize += (s, e) => UpdatePanelHeights();
        _kpiPanel.Resize += (s, e) => UpdatePanelHeights();
        _filterFlow.Layout += (s, e) => UpdatePanelHeights();
        _kpiFlow.Layout += (s, e) => UpdatePanelHeights();
        Resize += (s, e) => UpdatePanelHeights();

        ScaleLayoutAtRuntime();
    }

    private static void SetupFilterPair(TableLayoutPanel pair, LabelControl label, string caption, BaseEdit editor, int width)
    {
        pair.AutoSize = true;
        pair.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        pair.ColumnCount = 2;
        pair.RowCount = 1;
        pair.Margin = new Padding(0, 0, 12, 4);
        pair.Padding = Padding.Empty;
        pair.Anchor = AnchorStyles.Left;
        pair.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        pair.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        pair.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        label.Text = caption;
        label.AutoSizeMode = LabelAutoSizeMode.Horizontal;
        label.Anchor = AnchorStyles.Left;
        label.Margin = new Padding(0, 0, 6, 0);
        label.Padding = Padding.Empty;
        label.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        label.Appearance.Options.UseTextOptions = true;

        editor.Anchor = AnchorStyles.Left;
        editor.Margin = Padding.Empty;
        editor.Properties.AutoHeight = false;
        editor.Properties.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        editor.Properties.Appearance.Options.UseTextOptions = true;
        editor.MinimumSize = new Size(width, DesktopStyle.ToolbarControlHeight);
        editor.Size = editor.MinimumSize;

        pair.Controls.Add(label, 0, 0);
        pair.Controls.Add(editor, 1, 0);
    }

    private static void SetupDatePair(TableLayoutPanel pair, LabelControl label, string caption, DateEdit dateEdit, int width)
    {
        SetupFilterPair(pair, label, caption, dateEdit, width);

        dateEdit.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
        dateEdit.Properties.DisplayFormat.FormatString = "dd-MMM-yyyy";
        dateEdit.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
        dateEdit.Properties.EditFormat.FormatString = "dd-MMM-yyyy";
        dateEdit.Properties.Mask.EditMask = "dd-MMM-yyyy";
    }

    /// <summary>Applies DPI-aware runtime scaling to filter controls, buttons, and summary totals.</summary>
    public void ScaleLayoutAtRuntime()
    {
        if (DesignModeHelper.IsInDesignMode) return;

        int editorH = DesktopDpi.Scale(DesktopStyle.ToolbarControlHeight, this);
        int warehouseW = DesktopDpi.Scale(260, this);
        int periodW = DesktopDpi.Scale(140, this);
        int dateW = DesktopDpi.Scale(130, this);

        _warehouseCombo.MinimumSize = new Size(warehouseW, editorH);
        _warehouseCombo.Size = _warehouseCombo.MinimumSize;

        _periodCombo.MinimumSize = new Size(periodW, editorH);
        _periodCombo.Size = _periodCombo.MinimumSize;

        _fromDateEdit.MinimumSize = new Size(dateW, editorH);
        _fromDateEdit.Size = _fromDateEdit.MinimumSize;

        _toDateEdit.MinimumSize = new Size(dateW, editorH);
        _toDateEdit.Size = _toDateEdit.MinimumSize;

        var bestBtn = _refreshButton.CalcBestSize();
        int btnW = Math.Max(DesktopDpi.Scale(140, this), bestBtn.Width + DesktopDpi.Scale(20, this));
        int btnH = Math.Max(editorH, bestBtn.Height);
        _refreshButton.MinimumSize = new Size(btnW, btnH);
        _refreshButton.Size = _refreshButton.MinimumSize;
        _refreshButton.Padding = new Padding(DesktopDpi.Scale(10, this), 0, DesktopDpi.Scale(10, this), 0);

        int pairMarginR = DesktopDpi.Scale(12, this);
        int pairMarginB = DesktopDpi.Scale(4, this);
        _warehousePair.Margin = new Padding(0, 0, pairMarginR, pairMarginB);
        _periodPair.Margin = new Padding(0, 0, pairMarginR, pairMarginB);
        _fromPair.Margin = new Padding(0, 0, pairMarginR, pairMarginB);
        _toPair.Margin = new Padding(0, 0, pairMarginR, pairMarginB);
        _refreshButton.Margin = new Padding(DesktopDpi.Scale(4, this), 0, 0, pairMarginB);

        int kpiSpacing = DesktopDpi.Scale(20, this);
        int kpiVSpacing = DesktopDpi.Scale(4, this);
        var kpis = new[] { _kpiCount, _kpiGrossRefunded, _kpiTaxReversed, _kpiNetRefunded, _kpiCashPayouts, _kpiCardRefunds };
        foreach (var lbl in kpis)
        {
            lbl.Margin = new Padding(0, kpiVSpacing, kpiSpacing, kpiVSpacing);
        }

        _rootLayout.Padding = new Padding(DesktopDpi.Scale(12, this));
        _filterPanel.Padding = new Padding(DesktopDpi.Scale(10, this));
        _filterPanel.Margin = new Padding(0, 0, 0, DesktopDpi.Scale(10, this));
        _kpiPanel.Padding = new Padding(DesktopDpi.Scale(10, this));
        _kpiPanel.Margin = new Padding(0, 0, 0, DesktopDpi.Scale(10, this));

        _gridView.RowHeight = DesktopDpi.Scale(28, this);
        _gridView.ColumnPanelRowHeight = DesktopDpi.Scale(30, this);

        UpdatePanelHeights();
    }

    private void UpdatePanelHeights()
    {
        if (IsDisposed) return;

        int filterClientW = _filterPanel.ClientSize.Width > 0 ? _filterPanel.ClientSize.Width : _filterPanel.Width;
        if (filterClientW > 0)
        {
            int filterReqH = _filterFlow.GetPreferredSize(new Size(filterClientW, 0)).Height;
            int filterTotalH = filterReqH + _filterPanel.Padding.Vertical + 4;
            if (_filterPanel.MinimumSize.Height != filterTotalH)
            {
                _filterPanel.MinimumSize = new Size(0, filterTotalH);
                _filterPanel.Height = filterTotalH;
            }
        }

        int kpiClientW = _kpiPanel.ClientSize.Width > 0 ? _kpiPanel.ClientSize.Width : _kpiPanel.Width;
        if (kpiClientW > 0)
        {
            int kpiReqH = _kpiFlow.GetPreferredSize(new Size(kpiClientW, 0)).Height;
            int kpiTotalH = kpiReqH + _kpiPanel.Padding.Vertical + 4;
            if (_kpiPanel.MinimumSize.Height != kpiTotalH)
            {
                _kpiPanel.MinimumSize = new Size(0, kpiTotalH);
                _kpiPanel.Height = kpiTotalH;
            }
        }
    }
}
