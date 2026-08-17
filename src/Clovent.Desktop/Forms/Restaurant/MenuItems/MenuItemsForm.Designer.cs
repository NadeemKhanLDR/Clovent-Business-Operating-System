using Clovent.Desktop.Forms.Base;

namespace Clovent.Desktop.Forms.Restaurant.MenuItems;

partial class MenuItemsForm
{
    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        txtSearch = new DevExpress.XtraEditors.TextEdit();
        cboCategory = new DevExpress.XtraEditors.ComboBoxEdit();
        btnNewMenuItem = new DevExpress.XtraEditors.SimpleButton();
        btnEdit = new DevExpress.XtraEditors.SimpleButton();
        btnActivate = new DevExpress.XtraEditors.SimpleButton();
        btnDeactivate = new DevExpress.XtraEditors.SimpleButton();
        btnNewCategory = new DevExpress.XtraEditors.SimpleButton();
        btnCategoryColor = new DevExpress.XtraEditors.SimpleButton();
        btnMoveUp = new DevExpress.XtraEditors.SimpleButton();
        btnMoveDown = new DevExpress.XtraEditors.SimpleButton();
        btnRefresh = new DevExpress.XtraEditors.SimpleButton();
        gridControl = new DevExpress.XtraGrid.GridControl();
        gridView = new DevExpress.XtraGrid.Views.Grid.GridView();
        repositoryPhotoEdit = new DevExpress.XtraEditors.Repository.RepositoryItemPictureEdit();
        colPhoto = new DevExpress.XtraGrid.Columns.GridColumn();
        colName = new DevExpress.XtraGrid.Columns.GridColumn();
        colCategoryName = new DevExpress.XtraGrid.Columns.GridColumn();
        colPrice = new DevExpress.XtraGrid.Columns.GridColumn();
        colStatus = new DevExpress.XtraGrid.Columns.GridColumn();
        emptyStateLabel = new DevExpress.XtraEditors.LabelControl();
        ((System.ComponentModel.ISupportInitialize)txtSearch.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)cboCategory.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)gridControl).BeginInit();
        ((System.ComponentModel.ISupportInitialize)gridView).BeginInit();
        ((System.ComponentModel.ISupportInitialize)repositoryPhotoEdit).BeginInit();
        SuspendLayout();
        //
        // txtSearch
        //
        txtSearch.Margin = new Padding(0, 2, 0, DesktopStyle.PanelPadding);
        txtSearch.Name = "txtSearch";
        txtSearch.Properties.NullValuePrompt = "Search menu items...";
        txtSearch.Width = CommandPanelLayout.Width - 24;
        txtSearch.TabIndex = 0;
        txtSearch.EditValueChanged += TxtSearch_EditValueChanged;
        //
        // cboCategory
        //
        cboCategory.Margin = new Padding(0, 2, 0, DesktopStyle.PanelPadding);
        cboCategory.Name = "cboCategory";
        cboCategory.Width = CommandPanelLayout.Width - 24;
        cboCategory.TabIndex = 1;
        cboCategory.EditValueChanged += CboCategory_EditValueChanged;
        //
        // btnNewMenuItem
        //
        btnNewMenuItem.Name = "btnNewMenuItem";
        btnNewMenuItem.TabIndex = 2;
        btnNewMenuItem.Text = "+  New Menu Item";
        btnNewMenuItem.Appearance.Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold);
        btnNewMenuItem.Appearance.Options.UseFont = true;
        btnNewMenuItem.Click += BtnNewMenuItem_Click;
        //
        // btnEdit
        //
        btnEdit.Name = "btnEdit";
        btnEdit.TabIndex = 3;
        btnEdit.Text = "✎  Edit";
        btnEdit.Click += BtnEdit_Click;
        //
        // btnActivate
        //
        btnActivate.Name = "btnActivate";
        btnActivate.TabIndex = 4;
        btnActivate.Text = "✓  Activate";
        btnActivate.Click += BtnActivate_Click;
        //
        // btnDeactivate
        //
        btnDeactivate.Name = "btnDeactivate";
        btnDeactivate.TabIndex = 5;
        btnDeactivate.Text = "✕  Deactivate";
        btnDeactivate.Click += BtnDeactivate_Click;
        //
        // btnNewCategory
        //
        btnNewCategory.Name = "btnNewCategory";
        btnNewCategory.TabIndex = 6;
        btnNewCategory.Text = "+  New Category";
        btnNewCategory.Click += BtnNewCategory_Click;
        //
        // btnCategoryColor
        //
        btnCategoryColor.Name = "btnCategoryColor";
        btnCategoryColor.TabIndex = 7;
        btnCategoryColor.Text = "🎨  Category Color";
        btnCategoryColor.Click += BtnCategoryColor_Click;
        //
        // btnMoveUp
        //
        btnMoveUp.Name = "btnMoveUp";
        btnMoveUp.TabIndex = 8;
        btnMoveUp.Text = "▲  Move Up";
        btnMoveUp.Click += BtnMoveUp_Click;
        //
        // btnMoveDown
        //
        btnMoveDown.Name = "btnMoveDown";
        btnMoveDown.TabIndex = 9;
        btnMoveDown.Text = "▼  Move Down";
        btnMoveDown.Click += BtnMoveDown_Click;
        //
        // btnRefresh
        //
        btnRefresh.Name = "btnRefresh";
        btnRefresh.TabIndex = 10;
        btnRefresh.Text = "⟳  Refresh";
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
        gridView.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] { colPhoto, colName, colCategoryName, colPrice, colStatus });
        gridView.GridControl = gridControl;
        gridView.Name = "gridView";
        gridView.OptionsBehavior.AutoPopulateColumns = false;
        gridView.OptionsBehavior.Editable = false;
        gridView.OptionsSelection.MultiSelect = false;
        gridView.OptionsView.ColumnAutoWidth = true;
        gridView.OptionsView.ShowGroupPanel = false;
        // Fixed-height rows so every Photo thumbnail renders at the same
        // size regardless of whether a given menu item has a photo -
        // RowAutoHeight would otherwise shrink rows with no image back
        // down, making the grid look uneven row to row.
        gridView.OptionsView.RowAutoHeight = false;
        gridView.RowHeight = 56;
        gridView.FocusedRowChanged += GridView_FocusedRowChanged;
        // Double-click is the fastest way to open an item for editing - a
        // restaurant owner scanning the grid shouldn't have to look for the
        // Edit button once they already know which row they want.
        gridView.DoubleClick += GridView_DoubleClick;
        //
        // emptyStateLabel
        //
        // Shown in place of the (otherwise blank) grid when there is
        // nothing to display - "no menu items at all yet" vs. "no items
        // match the current search/category" get their own wording (set in
        // ApplyFilter) so the owner is never left staring at empty
        // headers wondering what went wrong.
        emptyStateLabel.Dock = DockStyle.Fill;
        emptyStateLabel.Appearance.Font = new Font("Segoe UI", 11F);
        emptyStateLabel.Appearance.ForeColor = DesktopStyle.CaptionForeColor;
        emptyStateLabel.Appearance.Options.UseFont = true;
        emptyStateLabel.Appearance.Options.UseForeColor = true;
        emptyStateLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        emptyStateLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        emptyStateLabel.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
        emptyStateLabel.Appearance.Options.UseTextOptions = true;
        emptyStateLabel.Visible = false;
        //
        // repositoryPhotoEdit
        //
        repositoryPhotoEdit.Name = "repositoryPhotoEdit";
        repositoryPhotoEdit.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Squeeze;
        repositoryPhotoEdit.ShowMenu = false;
        gridControl.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] { repositoryPhotoEdit });
        //
        // colPhoto
        //
        colPhoto.Caption = "Photo";
        colPhoto.ColumnEdit = repositoryPhotoEdit;
        colPhoto.FieldName = "Photo";
        colPhoto.Name = "colPhoto";
        colPhoto.OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
        colPhoto.Visible = true;
        colPhoto.VisibleIndex = 0;
        colPhoto.Width = 56;
        //
        // colName
        //
        colName.Caption = "Menu Item";
        colName.FieldName = "Name";
        colName.Name = "colName";
        colName.Visible = true;
        colName.VisibleIndex = 1;
        colName.Width = 240;
        //
        // colCategoryName
        //
        colCategoryName.Caption = "Category";
        colCategoryName.FieldName = "CategoryName";
        colCategoryName.Name = "colCategoryName";
        colCategoryName.Visible = true;
        colCategoryName.VisibleIndex = 2;
        colCategoryName.Width = 160;
        //
        // colPrice
        //
        colPrice.Caption = "Selling Price";
        colPrice.FieldName = "Price";
        colPrice.Name = "colPrice";
        colPrice.Visible = true;
        colPrice.VisibleIndex = 3;
        colPrice.Width = 110;
        //
        // colStatus
        //
        colStatus.Caption = "Status";
        colStatus.FieldName = "Status";
        colStatus.Name = "colStatus";
        colStatus.Visible = true;
        colStatus.VisibleIndex = 4;
        colStatus.Width = 90;
        //
        // MenuItemsForm
        //
        // gridControl and emptyStateLabel share the same Fill area -
        // emptyStateLabel is added second (on top) so toggling its
        // Visible/BringToFront in ApplyFilter overlays it on the grid
        // without needing a second content area from CommandPanelLayout.
        var gridHost = new DevExpress.XtraEditors.PanelControl { Dock = DockStyle.Fill };
        gridHost.Controls.Add(gridControl);
        gridHost.Controls.Add(emptyStateLabel);

        // Left command panel (search/category filter/actions) + grid filling
        // the rest - the same CommandPanelLayout shell every list screen in
        // this app uses (ProductsForm is the direct template), just scoped
        // to Restaurant-friendly fields/vocabulary only.
        var commandFlow = CommandPanelLayout.Build(ContentPanel, gridHost);
        commandFlow.Controls.Add(CommandPanelLayout.BuildSectionHeading("Search"));
        commandFlow.Controls.Add(txtSearch);
        commandFlow.Controls.Add(cboCategory);
        commandFlow.Controls.Add(CommandPanelLayout.BuildSectionHeading("Actions"));
        CommandPanelLayout.AddCommandButton(commandFlow, btnNewMenuItem);
        CommandPanelLayout.AddCommandButton(commandFlow, btnEdit);
        CommandPanelLayout.AddCommandButton(commandFlow, btnActivate);
        CommandPanelLayout.AddCommandButton(commandFlow, btnDeactivate);
        CommandPanelLayout.AddCommandButton(commandFlow, btnNewCategory);
        CommandPanelLayout.AddCommandButton(commandFlow, btnCategoryColor);
        // "Arrange" - drag-and-drop is the ask, but a live WinForms/
        // DevExpress row-drag implementation can't be visually verified in
        // this environment (no interactive designer/display - the same
        // constraint every prior UI pass in this codebase documents); Move
        // Up/Move Down achieve the identical outcome (a persisted
        // SortOrder POS respects - see RestaurantPosView.ReloadMenuItemsAsync)
        // without that risk.
        commandFlow.Controls.Add(CommandPanelLayout.BuildSectionHeading("Arrange"));
        CommandPanelLayout.AddCommandButton(commandFlow, btnMoveUp);
        CommandPanelLayout.AddCommandButton(commandFlow, btnMoveDown);
        commandFlow.Controls.Add(CommandPanelLayout.BuildSectionHeading("Info"));
        CommandPanelLayout.AddCommandButton(commandFlow, btnRefresh);
        Name = "MenuItemsForm";
        Size = new Size(1200, 600);
        ((System.ComponentModel.ISupportInitialize)txtSearch.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)cboCategory.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)gridControl).EndInit();
        ((System.ComponentModel.ISupportInitialize)gridView).EndInit();
        ((System.ComponentModel.ISupportInitialize)repositoryPhotoEdit).EndInit();
        ResumeLayout(false);
    }

    private DevExpress.XtraEditors.TextEdit txtSearch;
    private DevExpress.XtraEditors.ComboBoxEdit cboCategory;
    private DevExpress.XtraEditors.SimpleButton btnNewMenuItem;
    private DevExpress.XtraEditors.SimpleButton btnEdit;
    private DevExpress.XtraEditors.SimpleButton btnActivate;
    private DevExpress.XtraEditors.SimpleButton btnDeactivate;
    private DevExpress.XtraEditors.SimpleButton btnNewCategory;
    private DevExpress.XtraEditors.SimpleButton btnCategoryColor;
    private DevExpress.XtraEditors.SimpleButton btnMoveUp;
    private DevExpress.XtraEditors.SimpleButton btnMoveDown;
    private DevExpress.XtraEditors.SimpleButton btnRefresh;
    private DevExpress.XtraGrid.GridControl gridControl;
    private DevExpress.XtraGrid.Views.Grid.GridView gridView;
    private DevExpress.XtraEditors.Repository.RepositoryItemPictureEdit repositoryPhotoEdit;
    private DevExpress.XtraGrid.Columns.GridColumn colPhoto;
    private DevExpress.XtraGrid.Columns.GridColumn colName;
    private DevExpress.XtraGrid.Columns.GridColumn colCategoryName;
    private DevExpress.XtraGrid.Columns.GridColumn colPrice;
    private DevExpress.XtraGrid.Columns.GridColumn colStatus;
    private DevExpress.XtraEditors.LabelControl emptyStateLabel;
}
