using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;

namespace Clovent.Desktop.Restaurant.Reports;

partial class TaxSummaryReportView
{
    private FlowLayoutPanel _filterFlow;
    private FlowLayoutPanel _kpiFlow;
    private DevExpress.XtraGrid.Columns.GridColumn colAuth;
    private DevExpress.XtraGrid.Columns.GridColumn colCode;
    private DevExpress.XtraGrid.Columns.GridColumn colRate;
    private DevExpress.XtraGrid.Columns.GridColumn colClass;
    private DevExpress.XtraGrid.Columns.GridColumn colBase;
    private DevExpress.XtraGrid.Columns.GridColumn colCharged;
    private DevExpress.XtraGrid.Columns.GridColumn colRef;
    private DevExpress.XtraGrid.Columns.GridColumn colNet;
    private System.ComponentModel.IContainer components = null;

    private TableLayoutPanel _rootLayout;
    private PanelControl _filterPanel;
    private LabelControl _warehouseLabel;
    private ComboBoxEdit _warehouseCombo;
    private LabelControl _periodLabel;
    private ComboBoxEdit _periodCombo;
    private LabelControl _fromLabel;
    private DateEdit _fromDateEdit;
    private LabelControl _toLabel;
    private DateEdit _toDateEdit;
    private SimpleButton _refreshButton;

    private PanelControl _kpiPanel;
    private LabelControl _kpiGrossSales;
    private LabelControl _kpiTaxableBase;
    private LabelControl _kpiTaxCharged;
    private LabelControl _kpiTaxRefunded;
    private LabelControl _kpiNetTax;
    private LabelControl _kpiNetSales;

    private GridControl _gridControl;
    private GridView _gridView;

    /// <summary>Standard Designer-generated disposal of components.</summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            AppearanceManager.Changed -= AppearanceManager_Changed;
            _gate.Drain(TimeSpan.FromMilliseconds(500));
            _scope?.Dispose();
            _gate.Dispose();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        _rootLayout = new TableLayoutPanel();
        _filterPanel = new PanelControl();
        _filterFlow = new FlowLayoutPanel();
        _warehouseLabel = new LabelControl();
        _warehouseCombo = new ComboBoxEdit();
        _periodLabel = new LabelControl();
        _periodCombo = new ComboBoxEdit();
        _fromLabel = new LabelControl();
        _fromDateEdit = new DateEdit();
        _toLabel = new LabelControl();
        _toDateEdit = new DateEdit();
        _refreshButton = new SimpleButton();
        _kpiPanel = new PanelControl();
        _kpiFlow = new FlowLayoutPanel();
        _kpiGrossSales = new LabelControl();
        _kpiTaxableBase = new LabelControl();
        _kpiTaxCharged = new LabelControl();
        _kpiTaxRefunded = new LabelControl();
        _kpiNetTax = new LabelControl();
        _kpiNetSales = new LabelControl();
        _gridControl = new GridControl();
        _gridView = new GridView();
        colAuth = new DevExpress.XtraGrid.Columns.GridColumn();
        colCode = new DevExpress.XtraGrid.Columns.GridColumn();
        colRate = new DevExpress.XtraGrid.Columns.GridColumn();
        colClass = new DevExpress.XtraGrid.Columns.GridColumn();
        colBase = new DevExpress.XtraGrid.Columns.GridColumn();
        colCharged = new DevExpress.XtraGrid.Columns.GridColumn();
        colRef = new DevExpress.XtraGrid.Columns.GridColumn();
        colNet = new DevExpress.XtraGrid.Columns.GridColumn();
        _rootLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_filterPanel).BeginInit();
        _filterPanel.SuspendLayout();
        _filterFlow.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_warehouseCombo.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_periodCombo.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_fromDateEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_fromDateEdit.Properties.CalendarTimeProperties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_toDateEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_toDateEdit.Properties.CalendarTimeProperties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_kpiPanel).BeginInit();
        _kpiPanel.SuspendLayout();
        _kpiFlow.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_gridControl).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_gridView).BeginInit();
        SuspendLayout();
        //
        // _rootLayout
        //
        _rootLayout.ColumnCount = 1;
        _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _rootLayout.Controls.Add(_filterPanel, 0, 0);
        _rootLayout.Controls.Add(_kpiPanel, 0, 1);
        _rootLayout.Controls.Add(_gridControl, 0, 2);
        _rootLayout.Dock = DockStyle.Fill;
        _rootLayout.Location = new Point(0, 0);
        _rootLayout.Name = "_rootLayout";
        _rootLayout.Padding = new Padding(12);
        _rootLayout.RowCount = 3;
        _rootLayout.RowStyles.Add(new RowStyle());
        _rootLayout.RowStyles.Add(new RowStyle());
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _rootLayout.Size = new Size(1141, 732);
        _rootLayout.TabIndex = 0;
        //
        // _filterPanel
        //
        _filterPanel.AutoSize = true;
        _filterPanel.Controls.Add(_filterFlow);
        _filterPanel.Dock = DockStyle.Fill;
        _filterPanel.Location = new Point(12, 12);
        _filterPanel.Margin = new Padding(0, 0, 0, 10);
        _filterPanel.MinimumSize = new Size(0, 56);
        _filterPanel.Name = "_filterPanel";
        _filterPanel.Padding = new Padding(10, 8, 10, 8);
        _filterPanel.Size = new Size(1117, 58);
        _filterPanel.TabIndex = 0;
        //
        // _filterFlow
        //
        _filterFlow.AutoSize = true;
        _filterFlow.Controls.Add(_warehouseLabel);
        _filterFlow.Controls.Add(_warehouseCombo);
        _filterFlow.Controls.Add(_periodLabel);
        _filterFlow.Controls.Add(_periodCombo);
        _filterFlow.Controls.Add(_fromLabel);
        _filterFlow.Controls.Add(_fromDateEdit);
        _filterFlow.Controls.Add(_toLabel);
        _filterFlow.Controls.Add(_toDateEdit);
        _filterFlow.Controls.Add(_refreshButton);
        _filterFlow.Dock = DockStyle.Top;
        _filterFlow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _filterFlow.Location = new Point(12, 10);
        _filterFlow.Name = "_filterFlow";
        _filterFlow.Size = new Size(1093, 38);
        _filterFlow.TabIndex = 0;
        //
        // _warehouseLabel
        //
        _warehouseLabel.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _warehouseLabel.Appearance.Options.UseFont = true;
        _warehouseLabel.Location = new Point(3, 3);
        _warehouseLabel.Name = "_warehouseLabel";
        _warehouseLabel.Padding = new Padding(0, 7, 6, 0);
        _warehouseLabel.Size = new Size(79, 24);
        _warehouseLabel.TabIndex = 0;
        _warehouseLabel.Text = "Warehouse:";
        //
        // _warehouseCombo
        //
        _warehouseCombo.Location = new Point(88, 3);
        _warehouseCombo.Name = "_warehouseCombo";
        _warehouseCombo.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _warehouseCombo.Properties.Appearance.Options.UseFont = true;
        _warehouseCombo.Properties.AutoHeight = false;
        _warehouseCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _warehouseCombo.Size = new Size(220, 32);
        _warehouseCombo.TabIndex = 1;
        //
        // _periodLabel
        //
        _periodLabel.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _periodLabel.Appearance.Options.UseFont = true;
        _periodLabel.Location = new Point(314, 3);
        _periodLabel.Name = "_periodLabel";
        _periodLabel.Padding = new Padding(12, 7, 6, 0);
        _periodLabel.Size = new Size(62, 24);
        _periodLabel.TabIndex = 2;
        _periodLabel.Text = "Period:";
        //
        // _periodCombo
        //
        _periodCombo.Location = new Point(382, 3);
        _periodCombo.Name = "_periodCombo";
        _periodCombo.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _periodCombo.Properties.Appearance.Options.UseFont = true;
        _periodCombo.Properties.AutoHeight = false;
        _periodCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _periodCombo.Size = new Size(140, 32);
        _periodCombo.TabIndex = 3;
        //
        // _fromLabel
        //
        _fromLabel.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _fromLabel.Appearance.Options.UseFont = true;
        _fromLabel.Location = new Point(528, 3);
        _fromLabel.Name = "_fromLabel";
        _fromLabel.Padding = new Padding(12, 7, 6, 0);
        _fromLabel.Size = new Size(54, 24);
        _fromLabel.TabIndex = 4;
        _fromLabel.Text = "From:";
        //
        // _fromDateEdit
        //
        _fromDateEdit.EditValue = new DateTime(2026, 11, 9, 0, 0, 0, 0);
        _fromDateEdit.Location = new Point(588, 3);
        _fromDateEdit.Name = "_fromDateEdit";
        _fromDateEdit.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _fromDateEdit.Properties.Appearance.Options.UseFont = true;
        _fromDateEdit.Properties.AutoHeight = false;
        _fromDateEdit.Size = new Size(135, 32);
        _fromDateEdit.TabIndex = 5;
        //
        // _toLabel
        //
        _toLabel.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _toLabel.Appearance.Options.UseFont = true;
        _toLabel.Location = new Point(729, 3);
        _toLabel.Name = "_toLabel";
        _toLabel.Padding = new Padding(12, 7, 6, 0);
        _toLabel.Size = new Size(38, 24);
        _toLabel.TabIndex = 6;
        _toLabel.Text = "To:";
        //
        // _toDateEdit
        //
        _toDateEdit.EditValue = new DateTime(2026, 11, 9, 0, 0, 0, 0);
        _toDateEdit.Location = new Point(773, 3);
        _toDateEdit.Name = "_toDateEdit";
        _toDateEdit.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _toDateEdit.Properties.Appearance.Options.UseFont = true;
        _toDateEdit.Properties.AutoHeight = false;
        _toDateEdit.Size = new Size(135, 32);
        _toDateEdit.TabIndex = 7;
        //
        // _refreshButton
        //
        _refreshButton.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _refreshButton.Appearance.Options.UseFont = true;
        _refreshButton.AutoSize = true;
        _refreshButton.Location = new Point(927, 0);
        _refreshButton.Margin = new Padding(16, 0, 0, 0);
        _refreshButton.MinimumSize = new Size(140, 32);
        _refreshButton.Name = "_refreshButton";
        _refreshButton.Size = new Size(140, 32);
        _refreshButton.TabIndex = 8;
        _refreshButton.Text = "Generate Report";
        //
        // _kpiPanel
        //
        _kpiPanel.AutoSize = true;
        _kpiPanel.Controls.Add(_kpiFlow);
        _kpiPanel.Dock = DockStyle.Fill;
        _kpiPanel.Location = new Point(12, 80);
        _kpiPanel.Margin = new Padding(0, 0, 0, 10);
        _kpiPanel.MinimumSize = new Size(0, 42);
        _kpiPanel.Name = "_kpiPanel";
        _kpiPanel.Padding = new Padding(10);
        _kpiPanel.Size = new Size(1117, 53);
        _kpiPanel.TabIndex = 1;
        //
        // _kpiFlow
        //
        _kpiFlow.AutoSize = true;
        _kpiFlow.Controls.Add(_kpiGrossSales);
        _kpiFlow.Controls.Add(_kpiTaxableBase);
        _kpiFlow.Controls.Add(_kpiTaxCharged);
        _kpiFlow.Controls.Add(_kpiTaxRefunded);
        _kpiFlow.Controls.Add(_kpiNetTax);
        _kpiFlow.Controls.Add(_kpiNetSales);
        _kpiFlow.Dock = DockStyle.Top;
        _kpiFlow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _kpiFlow.Location = new Point(12, 12);
        _kpiFlow.Name = "_kpiFlow";
        _kpiFlow.Size = new Size(1093, 29);
        _kpiFlow.TabIndex = 0;
        //
        // _kpiGrossSales
        //
        _kpiGrossSales.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _kpiGrossSales.Appearance.ForeColor = Color.Black;
        _kpiGrossSales.Appearance.Options.UseFont = true;
        _kpiGrossSales.Appearance.Options.UseForeColor = true;
        _kpiGrossSales.Location = new Point(3, 3);
        _kpiGrossSales.Name = "_kpiGrossSales";
        _kpiGrossSales.Padding = new Padding(0, 0, 24, 6);
        _kpiGrossSales.Size = new Size(126, 23);
        _kpiGrossSales.TabIndex = 0;
        _kpiGrossSales.Text = "Gross Sales: 0.00";
        //
        // _kpiTaxableBase
        //
        _kpiTaxableBase.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _kpiTaxableBase.Appearance.ForeColor = Color.Black;
        _kpiTaxableBase.Appearance.Options.UseFont = true;
        _kpiTaxableBase.Appearance.Options.UseForeColor = true;
        _kpiTaxableBase.Location = new Point(135, 3);
        _kpiTaxableBase.Name = "_kpiTaxableBase";
        _kpiTaxableBase.Padding = new Padding(0, 0, 24, 6);
        _kpiTaxableBase.Size = new Size(137, 23);
        _kpiTaxableBase.TabIndex = 1;
        _kpiTaxableBase.Text = "Taxable Base: 0.00";
        //
        // _kpiTaxCharged
        //
        _kpiTaxCharged.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _kpiTaxCharged.Appearance.ForeColor = Color.SteelBlue;
        _kpiTaxCharged.Appearance.Options.UseFont = true;
        _kpiTaxCharged.Appearance.Options.UseForeColor = true;
        _kpiTaxCharged.Location = new Point(278, 3);
        _kpiTaxCharged.Name = "_kpiTaxCharged";
        _kpiTaxCharged.Padding = new Padding(0, 0, 24, 6);
        _kpiTaxCharged.Size = new Size(134, 23);
        _kpiTaxCharged.TabIndex = 2;
        _kpiTaxCharged.Text = "Tax Charged: 0.00";
        //
        // _kpiTaxRefunded
        //
        _kpiTaxRefunded.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _kpiTaxRefunded.Appearance.ForeColor = Color.IndianRed;
        _kpiTaxRefunded.Appearance.Options.UseFont = true;
        _kpiTaxRefunded.Appearance.Options.UseForeColor = true;
        _kpiTaxRefunded.Location = new Point(418, 3);
        _kpiTaxRefunded.Name = "_kpiTaxRefunded";
        _kpiTaxRefunded.Padding = new Padding(0, 0, 24, 6);
        _kpiTaxRefunded.Size = new Size(142, 23);
        _kpiTaxRefunded.TabIndex = 3;
        _kpiTaxRefunded.Text = "Tax Refunded: 0.00";
        //
        // _kpiNetTax
        //
        _kpiNetTax.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _kpiNetTax.Appearance.ForeColor = Color.DarkGreen;
        _kpiNetTax.Appearance.Options.UseFont = true;
        _kpiNetTax.Appearance.Options.UseForeColor = true;
        _kpiNetTax.Location = new Point(566, 3);
        _kpiNetTax.Name = "_kpiNetTax";
        _kpiNetTax.Padding = new Padding(0, 0, 24, 6);
        _kpiNetTax.Size = new Size(157, 23);
        _kpiNetTax.TabIndex = 4;
        _kpiNetTax.Text = "Net Tax Payable: 0.00";
        //
        // _kpiNetSales
        //
        _kpiNetSales.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _kpiNetSales.Appearance.ForeColor = Color.Black;
        _kpiNetSales.Appearance.Options.UseFont = true;
        _kpiNetSales.Appearance.Options.UseForeColor = true;
        _kpiNetSales.Location = new Point(729, 3);
        _kpiNetSales.Name = "_kpiNetSales";
        _kpiNetSales.Padding = new Padding(0, 0, 24, 6);
        _kpiNetSales.Size = new Size(114, 23);
        _kpiNetSales.TabIndex = 5;
        _kpiNetSales.Text = "Net Sales: 0.00";
        //
        // _gridControl
        //
        _gridControl.Dock = DockStyle.Fill;
        _gridControl.Location = new Point(15, 146);
        _gridControl.MainView = _gridView;
        _gridControl.Name = "_gridControl";
        _gridControl.Size = new Size(1111, 571);
        _gridControl.TabIndex = 2;
        _gridControl.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { _gridView });
        //
        // _gridView
        //
        _gridView.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] { colAuth, colCode, colRate, colClass, colBase, colCharged, colRef, colNet });
        _gridView.GridControl = _gridControl;
        _gridView.Name = "_gridView";
        _gridView.OptionsBehavior.Editable = false;
        _gridView.OptionsView.ShowGroupPanel = false;
        _gridView.OptionsView.ShowIndicator = false;
        _gridView.RowHeight = 28;
        //
        // colAuth
        //
        colAuth.Caption = "Authority";
        colAuth.FieldName = "Authority";
        colAuth.Name = "colAuth";
        colAuth.Visible = true;
        colAuth.VisibleIndex = 0;
        colAuth.Width = 90;
        //
        // colCode
        //
        colCode.Caption = "Tax Code";
        colCode.FieldName = "TaxCode";
        colCode.Name = "colCode";
        colCode.Visible = true;
        colCode.VisibleIndex = 1;
        colCode.Width = 120;
        //
        // colRate
        //
        colRate.Caption = "Rate %";
        colRate.DisplayFormat.FormatString = "0.##'%'";
        colRate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
        colRate.FieldName = "RatePercentage";
        colRate.Name = "colRate";
        colRate.Visible = true;
        colRate.VisibleIndex = 2;
        colRate.Width = 80;
        //
        // colClass
        //
        colClass.Caption = "Classification";
        colClass.FieldName = "TaxClassification";
        colClass.Name = "colClass";
        colClass.Visible = true;
        colClass.VisibleIndex = 3;
        colClass.Width = 110;
        //
        // colBase
        //
        colBase.Caption = "Taxable Base";
        colBase.DisplayFormat.FormatString = "F2";
        colBase.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Custom;
        colBase.FieldName = "TaxableBase";
        colBase.Name = "colBase";
        colBase.Visible = true;
        colBase.VisibleIndex = 4;
        colBase.Width = 110;
        //
        // colCharged
        //
        colCharged.Caption = "Tax Charged";
        colCharged.DisplayFormat.FormatString = "F2";
        colCharged.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Custom;
        colCharged.FieldName = "TaxCharged";
        colCharged.Name = "colCharged";
        colCharged.Visible = true;
        colCharged.VisibleIndex = 5;
        colCharged.Width = 110;
        //
        // colRef
        //
        colRef.Caption = "Tax Refunded";
        colRef.DisplayFormat.FormatString = "F2";
        colRef.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Custom;
        colRef.FieldName = "TaxRefunded";
        colRef.Name = "colRef";
        colRef.Visible = true;
        colRef.VisibleIndex = 6;
        colRef.Width = 110;
        //
        // colNet
        //
        colNet.Caption = "Net Tax Payable";
        colNet.DisplayFormat.FormatString = "F2";
        colNet.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Custom;
        colNet.FieldName = "NetTax";
        colNet.Name = "colNet";
        colNet.Visible = true;
        colNet.VisibleIndex = 7;
        colNet.Width = 120;
        //
        // TaxSummaryReportView
        //
        Controls.Add(_rootLayout);
        Name = "TaxSummaryReportView";
        Size = new Size(1141, 732);
        Load += TaxSummaryReportView_Load;
        _rootLayout.ResumeLayout(false);
        _rootLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_filterPanel).EndInit();
        _filterPanel.ResumeLayout(false);
        _filterPanel.PerformLayout();
        _filterFlow.ResumeLayout(false);
        _filterFlow.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_warehouseCombo.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_periodCombo.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_fromDateEdit.Properties.CalendarTimeProperties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_fromDateEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_toDateEdit.Properties.CalendarTimeProperties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_toDateEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_kpiPanel).EndInit();
        _kpiPanel.ResumeLayout(false);
        _kpiPanel.PerformLayout();
        _kpiFlow.ResumeLayout(false);
        _kpiFlow.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_gridControl).EndInit();
        ((System.ComponentModel.ISupportInitialize)_gridView).EndInit();
        ResumeLayout(false);
    }

}
