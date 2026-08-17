using Clovent.Desktop.Forms.Base;

namespace Clovent.Desktop.Forms.Catalog.Products;

partial class ProductsForm
{
    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        txtSearch = new DevExpress.XtraEditors.TextEdit();
        btnNew = new DevExpress.XtraEditors.SimpleButton();
        btnEdit = new DevExpress.XtraEditors.SimpleButton();
        btnActivate = new DevExpress.XtraEditors.SimpleButton();
        btnDeactivate = new DevExpress.XtraEditors.SimpleButton();
        btnExportCsv = new DevExpress.XtraEditors.SimpleButton();
        btnImportCsv = new DevExpress.XtraEditors.SimpleButton();
        btnRefresh = new DevExpress.XtraEditors.SimpleButton();
        gridControl = new DevExpress.XtraGrid.GridControl();
        gridView = new DevExpress.XtraGrid.Views.Grid.GridView();
        colSku = new DevExpress.XtraGrid.Columns.GridColumn();
        colName = new DevExpress.XtraGrid.Columns.GridColumn();
        colTaxRatePercentage = new DevExpress.XtraGrid.Columns.GridColumn();
        colStatus = new DevExpress.XtraGrid.Columns.GridColumn();
        colCreatedAtUtc = new DevExpress.XtraGrid.Columns.GridColumn();
        ((System.ComponentModel.ISupportInitialize)txtSearch.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)gridControl).BeginInit();
        ((System.ComponentModel.ISupportInitialize)gridView).BeginInit();
        SuspendLayout();
        //
        // txtSearch
        //
        txtSearch.Margin = new Padding(0, 2, 0, DesktopStyle.PanelPadding);
        txtSearch.Name = "txtSearch";
        txtSearch.Properties.NullValuePrompt = "Search...";
        // Width only, not a fixed Height - see UsersForm.Designer.cs's
        // matching comment (the DevExpress skin renders TextEdit taller
        // than a hand-guessed Height, overlapping whatever FlowLayoutPanel
        // positioned next based on the shorter declared size).
        txtSearch.Width = CommandPanelLayout.Width - 24;
        txtSearch.TabIndex = 0;
        txtSearch.EditValueChanged += TxtSearch_EditValueChanged;
        //
        // btnNew
        //
        btnNew.Name = "btnNew";
        btnNew.TabIndex = 1;
        btnNew.Text = "New";
        btnNew.Click += BtnNew_Click;
        //
        // btnEdit
        //
        btnEdit.Name = "btnEdit";
        btnEdit.TabIndex = 2;
        btnEdit.Text = "Edit";
        btnEdit.Click += BtnEdit_Click;
        //
        // btnActivate
        //
        btnActivate.Name = "btnActivate";
        btnActivate.TabIndex = 3;
        btnActivate.Text = "Activate";
        btnActivate.Click += BtnActivate_Click;
        //
        // btnDeactivate
        //
        btnDeactivate.Name = "btnDeactivate";
        btnDeactivate.TabIndex = 4;
        btnDeactivate.Text = "Deactivate";
        btnDeactivate.Click += BtnDeactivate_Click;
        //
        // btnExportCsv
        //
        btnExportCsv.Name = "btnExportCsv";
        btnExportCsv.TabIndex = 5;
        btnExportCsv.Text = "Export CSV";
        btnExportCsv.Click += BtnExportCsv_Click;
        //
        // btnImportCsv
        //
        btnImportCsv.Name = "btnImportCsv";
        btnImportCsv.TabIndex = 6;
        btnImportCsv.Text = "Import CSV";
        btnImportCsv.Click += BtnImportCsv_Click;
        //
        // btnRefresh
        //
        btnRefresh.Name = "btnRefresh";
        btnRefresh.TabIndex = 7;
        btnRefresh.Text = "Refresh";
        btnRefresh.Click += BtnRefresh_Click;
        //
        // gridControl
        //
        gridControl.Dock = DockStyle.Fill;
        gridControl.Location = new Point(0, 0);
        gridControl.MainView = gridView;
        gridControl.Name = "gridControl";
        gridControl.Size = new Size(1200, 556);
        gridControl.TabIndex = 0;
        gridControl.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { gridView });
        //
        // gridView
        //
        gridView.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] { colSku, colName, colTaxRatePercentage, colStatus, colCreatedAtUtc });
        gridView.GridControl = gridControl;
        gridView.Name = "gridView";
        gridView.OptionsBehavior.AutoPopulateColumns = false;
        gridView.OptionsBehavior.Editable = false;
        gridView.OptionsSelection.MultiSelect = false;
        gridView.OptionsView.ColumnAutoWidth = true;
        gridView.OptionsView.ShowGroupPanel = false;
        gridView.FocusedRowChanged += GridView_FocusedRowChanged;
        //
        // colSku
        //
        colSku.Caption = "SKU";
        colSku.FieldName = "Sku";
        colSku.Name = "colSku";
        colSku.Visible = true;
        colSku.VisibleIndex = 0;
        colSku.Width = 120;
        //
        // colName
        //
        colName.Caption = "Name";
        colName.FieldName = "Name";
        colName.Name = "colName";
        colName.Visible = true;
        colName.VisibleIndex = 1;
        colName.Width = 220;
        //
        // colTaxRatePercentage
        //
        colTaxRatePercentage.Caption = "Tax %";
        colTaxRatePercentage.FieldName = "TaxRatePercentage";
        colTaxRatePercentage.Name = "colTaxRatePercentage";
        colTaxRatePercentage.Visible = true;
        colTaxRatePercentage.VisibleIndex = 2;
        colTaxRatePercentage.Width = 70;
        //
        // colStatus
        //
        colStatus.Caption = "Status";
        colStatus.FieldName = "Status";
        colStatus.Name = "colStatus";
        colStatus.Visible = true;
        colStatus.VisibleIndex = 3;
        colStatus.Width = 90;
        //
        // colCreatedAtUtc
        //
        colCreatedAtUtc.Caption = "Created (UTC)";
        colCreatedAtUtc.FieldName = "CreatedAtUtc";
        colCreatedAtUtc.Name = "colCreatedAtUtc";
        colCreatedAtUtc.Visible = true;
        colCreatedAtUtc.VisibleIndex = 4;
        colCreatedAtUtc.Width = 160;
        //
        // ProductsForm
        //
        // Left command panel (search/actions) + grid filling the rest -
        // see CommandPanelLayout.Build's own remarks. ToolbarFlow (BaseForm's
        // inherited horizontal toolbar band) is deliberately left empty here;
        // BaseForm collapses it to zero height when empty (see
        // BaseForm.UpdateToolbarPanelSize), so this screen has no leftover
        // toolbar band above the command-panel/grid split.
        var commandFlow = CommandPanelLayout.Build(ContentPanel, gridControl);
        commandFlow.Controls.Add(CommandPanelLayout.BuildSectionHeading("Search"));
        commandFlow.Controls.Add(txtSearch);
        commandFlow.Controls.Add(CommandPanelLayout.BuildSectionHeading("Actions"));
        CommandPanelLayout.AddCommandButton(commandFlow, btnNew);
        CommandPanelLayout.AddCommandButton(commandFlow, btnEdit);
        CommandPanelLayout.AddCommandButton(commandFlow, btnActivate);
        CommandPanelLayout.AddCommandButton(commandFlow, btnDeactivate);
        CommandPanelLayout.AddCommandButton(commandFlow, btnExportCsv);
        CommandPanelLayout.AddCommandButton(commandFlow, btnImportCsv);
        CommandPanelLayout.AddCommandButton(commandFlow, btnRefresh);
        Name = "ProductsForm";
        Size = new Size(1200, 600);
        ((System.ComponentModel.ISupportInitialize)txtSearch.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)gridControl).EndInit();
        ((System.ComponentModel.ISupportInitialize)gridView).EndInit();
        ResumeLayout(false);
    }

    private DevExpress.XtraEditors.TextEdit txtSearch;
    private DevExpress.XtraEditors.SimpleButton btnNew;
    private DevExpress.XtraEditors.SimpleButton btnEdit;
    private DevExpress.XtraEditors.SimpleButton btnActivate;
    private DevExpress.XtraEditors.SimpleButton btnDeactivate;
    private DevExpress.XtraEditors.SimpleButton btnExportCsv;
    private DevExpress.XtraEditors.SimpleButton btnImportCsv;
    private DevExpress.XtraEditors.SimpleButton btnRefresh;
    private DevExpress.XtraGrid.GridControl gridControl;
    private DevExpress.XtraGrid.Views.Grid.GridView gridView;
    private DevExpress.XtraGrid.Columns.GridColumn colSku;
    private DevExpress.XtraGrid.Columns.GridColumn colName;
    private DevExpress.XtraGrid.Columns.GridColumn colTaxRatePercentage;
    private DevExpress.XtraGrid.Columns.GridColumn colStatus;
    private DevExpress.XtraGrid.Columns.GridColumn colCreatedAtUtc;
}
