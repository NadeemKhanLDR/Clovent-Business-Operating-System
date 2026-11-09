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
    private FlowLayoutPanel _filterFlow;
    private FlowLayoutPanel _kpiFlow;
    private DevExpress.XtraGrid.Columns.GridColumn colNum;
    private DevExpress.XtraGrid.Columns.GridColumn colDate;
    private DevExpress.XtraGrid.Columns.GridColumn colCashier;
    private DevExpress.XtraGrid.Columns.GridColumn colAppr;
    private DevExpress.XtraGrid.Columns.GridColumn colReason;
    private DevExpress.XtraGrid.Columns.GridColumn colSettlement;
    private DevExpress.XtraGrid.Columns.GridColumn colRef;
    private DevExpress.XtraGrid.Columns.GridColumn colSub;
    private DevExpress.XtraGrid.Columns.GridColumn colTax;
    private DevExpress.XtraGrid.Columns.GridColumn colGrand;
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
    private LabelControl _kpiCount;
    private LabelControl _kpiGrossRefunded;
    private LabelControl _kpiTaxReversed;
    private LabelControl _kpiNetRefunded;
    private LabelControl _kpiCashPayouts;
    private LabelControl _kpiCardRefunds;

    private GridControl _gridControl;
    private GridView _gridView;

    /// <summary>Standard Designer-generated disposal of components.</summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            AppearanceManager.Changed -= AppearanceManager_Changed;
            _gate.Dispose();
            _scope?.Dispose();
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
        _kpiCount = new LabelControl();
        _kpiGrossRefunded = new LabelControl();
        _kpiTaxReversed = new LabelControl();
        _kpiNetRefunded = new LabelControl();
        _kpiCashPayouts = new LabelControl();
        _kpiCardRefunds = new LabelControl();
        _gridControl = new GridControl();
        _gridView = new GridView();
        colNum = new DevExpress.XtraGrid.Columns.GridColumn();
        colDate = new DevExpress.XtraGrid.Columns.GridColumn();
        colCashier = new DevExpress.XtraGrid.Columns.GridColumn();
        colAppr = new DevExpress.XtraGrid.Columns.GridColumn();
        colReason = new DevExpress.XtraGrid.Columns.GridColumn();
        colSettlement = new DevExpress.XtraGrid.Columns.GridColumn();
        colRef = new DevExpress.XtraGrid.Columns.GridColumn();
        colSub = new DevExpress.XtraGrid.Columns.GridColumn();
        colTax = new DevExpress.XtraGrid.Columns.GridColumn();
        colGrand = new DevExpress.XtraGrid.Columns.GridColumn();
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
        _kpiFlow.Controls.Add(_kpiCount);
        _kpiFlow.Controls.Add(_kpiGrossRefunded);
        _kpiFlow.Controls.Add(_kpiTaxReversed);
        _kpiFlow.Controls.Add(_kpiNetRefunded);
        _kpiFlow.Controls.Add(_kpiCashPayouts);
        _kpiFlow.Controls.Add(_kpiCardRefunds);
        _kpiFlow.Dock = DockStyle.Top;
        _kpiFlow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _kpiFlow.Location = new Point(12, 12);
        _kpiFlow.Name = "_kpiFlow";
        _kpiFlow.Size = new Size(1093, 29);
        _kpiFlow.TabIndex = 0;
        //
        // _kpiCount
        //
        _kpiCount.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _kpiCount.Appearance.ForeColor = Color.Black;
        _kpiCount.Appearance.Options.UseFont = true;
        _kpiCount.Appearance.Options.UseForeColor = true;
        _kpiCount.Location = new Point(3, 3);
        _kpiCount.Name = "_kpiCount";
        _kpiCount.Padding = new Padding(0, 0, 24, 6);
        _kpiCount.Size = new Size(89, 23);
        _kpiCount.TabIndex = 0;
        _kpiCount.Text = "Refunds: 0";
        //
        // _kpiGrossRefunded
        //
        _kpiGrossRefunded.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _kpiGrossRefunded.Appearance.ForeColor = Color.Black;
        _kpiGrossRefunded.Appearance.Options.UseFont = true;
        _kpiGrossRefunded.Appearance.Options.UseForeColor = true;
        _kpiGrossRefunded.Location = new Point(98, 3);
        _kpiGrossRefunded.Name = "_kpiGrossRefunded";
        _kpiGrossRefunded.Padding = new Padding(0, 0, 24, 6);
        _kpiGrossRefunded.Size = new Size(91, 23);
        _kpiGrossRefunded.TabIndex = 1;
        _kpiGrossRefunded.Text = "Gross: 0.00";
        //
        // _kpiTaxReversed
        //
        _kpiTaxReversed.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _kpiTaxReversed.Appearance.ForeColor = Color.SteelBlue;
        _kpiTaxReversed.Appearance.Options.UseFont = true;
        _kpiTaxReversed.Appearance.Options.UseForeColor = true;
        _kpiTaxReversed.Location = new Point(195, 3);
        _kpiTaxReversed.Name = "_kpiTaxReversed";
        _kpiTaxReversed.Padding = new Padding(0, 0, 24, 6);
        _kpiTaxReversed.Size = new Size(138, 23);
        _kpiTaxReversed.TabIndex = 2;
        _kpiTaxReversed.Text = "Tax Reversed: 0.00";
        //
        // _kpiNetRefunded
        //
        _kpiNetRefunded.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _kpiNetRefunded.Appearance.ForeColor = Color.IndianRed;
        _kpiNetRefunded.Appearance.Options.UseFont = true;
        _kpiNetRefunded.Appearance.Options.UseForeColor = true;
        _kpiNetRefunded.Location = new Point(339, 3);
        _kpiNetRefunded.Name = "_kpiNetRefunded";
        _kpiNetRefunded.Padding = new Padding(0, 0, 24, 6);
        _kpiNetRefunded.Size = new Size(152, 23);
        _kpiNetRefunded.TabIndex = 3;
        _kpiNetRefunded.Text = "Total Refunded: 0.00";
        //
        // _kpiCashPayouts
        //
        _kpiCashPayouts.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _kpiCashPayouts.Appearance.ForeColor = Color.DarkGreen;
        _kpiCashPayouts.Appearance.Options.UseFont = true;
        _kpiCashPayouts.Appearance.Options.UseForeColor = true;
        _kpiCashPayouts.Location = new Point(497, 3);
        _kpiCashPayouts.Name = "_kpiCashPayouts";
        _kpiCashPayouts.Padding = new Padding(0, 0, 24, 6);
        _kpiCashPayouts.Size = new Size(139, 23);
        _kpiCashPayouts.TabIndex = 4;
        _kpiCashPayouts.Text = "Cash Payouts: 0.00";
        //
        // _kpiCardRefunds
        //
        _kpiCardRefunds.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _kpiCardRefunds.Appearance.ForeColor = Color.DarkSlateBlue;
        _kpiCardRefunds.Appearance.Options.UseFont = true;
        _kpiCardRefunds.Appearance.Options.UseForeColor = true;
        _kpiCardRefunds.Location = new Point(642, 3);
        _kpiCardRefunds.Name = "_kpiCardRefunds";
        _kpiCardRefunds.Padding = new Padding(0, 0, 24, 6);
        _kpiCardRefunds.Size = new Size(139, 23);
        _kpiCardRefunds.TabIndex = 5;
        _kpiCardRefunds.Text = "Card Refunds: 0.00";
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
        _gridView.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] { colNum, colDate, colCashier, colAppr, colReason, colSettlement, colRef, colSub, colTax, colGrand });
        _gridView.GridControl = _gridControl;
        _gridView.Name = "_gridView";
        _gridView.OptionsBehavior.Editable = false;
        _gridView.OptionsView.ShowGroupPanel = false;
        _gridView.OptionsView.ShowIndicator = false;
        _gridView.RowHeight = 28;
        //
        // colNum
        //
        colNum.Caption = "Credit Note #";
        colNum.FieldName = "RefundNumber";
        colNum.Name = "colNum";
        colNum.Visible = true;
        colNum.VisibleIndex = 0;
        colNum.Width = 140;
        //
        // colDate
        //
        colDate.Caption = "Date & Time";
        colDate.FieldName = "RefundedAtUtc";
        colDate.Name = "colDate";
        colDate.Visible = true;
        colDate.VisibleIndex = 1;
        colDate.Width = 150;
        //
        // colCashier
        //
        colCashier.Caption = "Cashier";
        colCashier.FieldName = "CashierName";
        colCashier.Name = "colCashier";
        colCashier.Visible = true;
        colCashier.VisibleIndex = 2;
        colCashier.Width = 110;
        //
        // colAppr
        //
        colAppr.Caption = "Approved By";
        colAppr.FieldName = "ApprovedByUserName";
        colAppr.Name = "colAppr";
        colAppr.Visible = true;
        colAppr.VisibleIndex = 3;
        colAppr.Width = 110;
        //
        // colReason
        //
        colReason.Caption = "Reason";
        colReason.FieldName = "Reason";
        colReason.Name = "colReason";
        colReason.Visible = true;
        colReason.VisibleIndex = 4;
        colReason.Width = 160;
        //
        // colSettlement
        //
        colSettlement.Caption = "Settlement";
        colSettlement.FieldName = "SettlementMethod";
        colSettlement.Name = "colSettlement";
        colSettlement.Visible = true;
        colSettlement.VisibleIndex = 5;
        colSettlement.Width = 120;
        //
        // colRef
        //
        colRef.Caption = "Reference";
        colRef.FieldName = "SettlementReference";
        colRef.Name = "colRef";
        colRef.Visible = true;
        colRef.VisibleIndex = 6;
        colRef.Width = 110;
        //
        // colSub
        //
        colSub.Caption = "Subtotal";
        colSub.DisplayFormat.FormatString = "F2";
        colSub.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Custom;
        colSub.FieldName = "SubtotalRefunded";
        colSub.Name = "colSub";
        colSub.Visible = true;
        colSub.VisibleIndex = 7;
        colSub.Width = 100;
        //
        // colTax
        //
        colTax.Caption = "Tax Reversed";
        colTax.DisplayFormat.FormatString = "F2";
        colTax.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Custom;
        colTax.FieldName = "TaxReversedTotal";
        colTax.Name = "colTax";
        colTax.Visible = true;
        colTax.VisibleIndex = 8;
        colTax.Width = 100;
        //
        // colGrand
        //
        colGrand.Caption = "Grand Total";
        colGrand.DisplayFormat.FormatString = "F2";
        colGrand.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Custom;
        colGrand.FieldName = "GrandTotalRefunded";
        colGrand.Name = "colGrand";
        colGrand.Visible = true;
        colGrand.VisibleIndex = 9;
        colGrand.Width = 110;
        //
        // RefundAuditReportView
        //
        Controls.Add(_rootLayout);
        Name = "RefundAuditReportView";
        Size = new Size(1141, 732);
        Load += RefundAuditReportView_Load;
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
