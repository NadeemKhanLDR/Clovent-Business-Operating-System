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
    private readonly LabelControl _warehouseLabel = new();
    private readonly ComboBoxEdit _warehouseCombo = new();
    private readonly LabelControl _periodLabel = new();
    private readonly ComboBoxEdit _periodCombo = new();
    private readonly LabelControl _fromLabel = new();
    private readonly DateEdit _fromDateEdit = new();
    private readonly LabelControl _toLabel = new();
    private readonly DateEdit _toDateEdit = new();
    private readonly SimpleButton _refreshButton = new();

    private readonly PanelControl _kpiPanel = new();
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
            _gate.Dispose();
            _scope.Dispose();
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
        _filterPanel.Padding = new Padding(10);
        _filterPanel.Margin = new Padding(0, 0, 0, 10);

        var filterFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };

        _warehouseLabel.Text = "Warehouse:";
        _warehouseLabel.Padding = new Padding(0, 6, 6, 0);
        _warehouseCombo.Width = 160;
        _warehouseCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;

        _periodLabel.Text = "Period:";
        _periodLabel.Padding = new Padding(12, 6, 6, 0);
        _periodCombo.Width = 120;
        _periodCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;

        _fromLabel.Text = "From:";
        _fromLabel.Padding = new Padding(12, 6, 6, 0);
        _fromDateEdit.Width = 110;

        _toLabel.Text = "To:";
        _toLabel.Padding = new Padding(12, 6, 6, 0);
        _toDateEdit.Width = 110;

        _refreshButton.Text = "Generate Report";
        _refreshButton.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _refreshButton.Appearance.Options.UseFont = true;
        _refreshButton.Margin = new Padding(16, 0, 0, 0);
        _refreshButton.Size = new Size(130, 30);

        filterFlow.Controls.Add(_warehouseLabel);
        filterFlow.Controls.Add(_warehouseCombo);
        filterFlow.Controls.Add(_periodLabel);
        filterFlow.Controls.Add(_periodCombo);
        filterFlow.Controls.Add(_fromLabel);
        filterFlow.Controls.Add(_fromDateEdit);
        filterFlow.Controls.Add(_toLabel);
        filterFlow.Controls.Add(_toDateEdit);
        filterFlow.Controls.Add(_refreshButton);

        _filterPanel.Controls.Add(filterFlow);
        _rootLayout.Controls.Add(_filterPanel, 0, 0);

        // 2. KPI Summary Strip
        _kpiPanel.Dock = DockStyle.Fill;
        _kpiPanel.AutoSize = true;
        _kpiPanel.Padding = new Padding(10);
        _kpiPanel.Margin = new Padding(0, 0, 0, 10);

        var kpiFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };

        void StyleKpi(LabelControl lbl, string text, Color color)
        {
            lbl.Text = text;
            lbl.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lbl.Appearance.ForeColor = color;
            lbl.Appearance.Options.UseFont = true;
            lbl.Appearance.Options.UseForeColor = true;
            lbl.Padding = new Padding(0, 0, 24, 6);
        }

        StyleKpi(_kpiCount, "Refunds: 0", Color.Black);
        StyleKpi(_kpiGrossRefunded, "Gross: 0.00", Color.Black);
        StyleKpi(_kpiTaxReversed, "Tax Reversed: 0.00", Color.SteelBlue);
        StyleKpi(_kpiNetRefunded, "Total Refunded: 0.00", Color.IndianRed);
        StyleKpi(_kpiCashPayouts, "Cash Payouts: 0.00", Color.DarkGreen);
        StyleKpi(_kpiCardRefunds, "Card Refunds: 0.00", Color.DarkSlateBlue);

        kpiFlow.Controls.Add(_kpiCount);
        kpiFlow.Controls.Add(_kpiGrossRefunded);
        kpiFlow.Controls.Add(_kpiTaxReversed);
        kpiFlow.Controls.Add(_kpiNetRefunded);
        kpiFlow.Controls.Add(_kpiCashPayouts);
        kpiFlow.Controls.Add(_kpiCardRefunds);

        _kpiPanel.Controls.Add(kpiFlow);
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
    }
}
