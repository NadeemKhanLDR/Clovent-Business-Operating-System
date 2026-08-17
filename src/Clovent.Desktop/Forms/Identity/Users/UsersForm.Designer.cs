using Clovent.Desktop.Forms.Base;

namespace Clovent.Desktop.Forms.Identity.Users;

partial class UsersForm
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
        btnResetPassword = new DevExpress.XtraEditors.SimpleButton();
        btnUnlock = new DevExpress.XtraEditors.SimpleButton();
        btnRefresh = new DevExpress.XtraEditors.SimpleButton();
        gridControl = new DevExpress.XtraGrid.GridControl();
        gridView = new DevExpress.XtraGrid.Views.Grid.GridView();
        colUserName = new DevExpress.XtraGrid.Columns.GridColumn();
        colDisplayName = new DevExpress.XtraGrid.Columns.GridColumn();
        colEmail = new DevExpress.XtraGrid.Columns.GridColumn();
        colStatus = new DevExpress.XtraGrid.Columns.GridColumn();
        colCompanyName = new DevExpress.XtraGrid.Columns.GridColumn();
        colBranchName = new DevExpress.XtraGrid.Columns.GridColumn();
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
        // Width only, not a fixed Height - this DevExpress skin's TextEdit
        // renders taller than a hand-guessed Height under it (confirmed via
        // screenshot: the box's real rendered bottom edge overlapped the
        // "Actions" heading FlowLayoutPanel had already positioned based on
        // the shorter declared Size), the same skin-vs-declared-size
        // mismatch as LoginForm's fields. Leaving Height unset lets the
        // control report its own real size to the FlowLayoutPanel instead.
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
        // btnResetPassword
        //
        btnResetPassword.Name = "btnResetPassword";
        btnResetPassword.TabIndex = 5;
        btnResetPassword.Text = "Reset Password";
        btnResetPassword.Click += BtnResetPassword_Click;
        //
        // btnUnlock
        //
        btnUnlock.Name = "btnUnlock";
        btnUnlock.TabIndex = 6;
        btnUnlock.Text = "Unlock";
        btnUnlock.Click += BtnUnlock_Click;
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
        gridView.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] { colUserName, colDisplayName, colEmail, colStatus, colCompanyName, colBranchName });
        gridView.GridControl = gridControl;
        gridView.Name = "gridView";
        gridView.OptionsBehavior.AutoPopulateColumns = false;
        gridView.OptionsBehavior.Editable = false;
        gridView.OptionsSelection.MultiSelect = false;
        gridView.OptionsView.ColumnAutoWidth = true;
        gridView.OptionsView.ShowGroupPanel = false;
        gridView.FocusedRowChanged += GridView_FocusedRowChanged;
        //
        // colUserName
        //
        colUserName.Caption = "Username";
        colUserName.FieldName = "UserName";
        colUserName.Name = "colUserName";
        colUserName.Visible = true;
        colUserName.VisibleIndex = 0;
        colUserName.Width = 130;
        //
        // colDisplayName
        //
        colDisplayName.Caption = "Display Name";
        colDisplayName.FieldName = "DisplayName";
        colDisplayName.Name = "colDisplayName";
        colDisplayName.Visible = true;
        colDisplayName.VisibleIndex = 1;
        colDisplayName.Width = 180;
        //
        // colEmail
        //
        colEmail.Caption = "Email";
        colEmail.FieldName = "Email";
        colEmail.Name = "colEmail";
        colEmail.Visible = true;
        colEmail.VisibleIndex = 2;
        colEmail.Width = 200;
        //
        // colStatus
        //
        colStatus.Caption = "Status";
        colStatus.FieldName = "Status";
        colStatus.Name = "colStatus";
        colStatus.Visible = true;
        colStatus.VisibleIndex = 3;
        colStatus.Width = 100;
        //
        // colCompanyName
        //
        colCompanyName.Caption = "Company";
        colCompanyName.FieldName = "CompanyName";
        colCompanyName.Name = "colCompanyName";
        colCompanyName.Visible = true;
        colCompanyName.VisibleIndex = 4;
        colCompanyName.Width = 150;
        //
        // colBranchName
        //
        colBranchName.Caption = "Branch";
        colBranchName.FieldName = "BranchName";
        colBranchName.Name = "colBranchName";
        colBranchName.Visible = true;
        colBranchName.VisibleIndex = 5;
        colBranchName.Width = 150;
        //
        // UsersForm
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
        CommandPanelLayout.AddCommandButton(commandFlow, btnResetPassword);
        CommandPanelLayout.AddCommandButton(commandFlow, btnUnlock);
        CommandPanelLayout.AddCommandButton(commandFlow, btnRefresh);
        Name = "UsersForm";
        // Deliberately no AutoScaleMode/AutoScaleDimensions - see BaseForm.Designer.cs's remarks.
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
    private DevExpress.XtraEditors.SimpleButton btnResetPassword;
    private DevExpress.XtraEditors.SimpleButton btnUnlock;
    private DevExpress.XtraEditors.SimpleButton btnRefresh;
    private DevExpress.XtraGrid.GridControl gridControl;
    private DevExpress.XtraGrid.Views.Grid.GridView gridView;
    private DevExpress.XtraGrid.Columns.GridColumn colUserName;
    private DevExpress.XtraGrid.Columns.GridColumn colDisplayName;
    private DevExpress.XtraGrid.Columns.GridColumn colEmail;
    private DevExpress.XtraGrid.Columns.GridColumn colStatus;
    private DevExpress.XtraGrid.Columns.GridColumn colCompanyName;
    private DevExpress.XtraGrid.Columns.GridColumn colBranchName;
}
