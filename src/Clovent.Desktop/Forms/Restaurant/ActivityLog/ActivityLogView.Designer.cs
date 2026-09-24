using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base.Appearance;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;

namespace Clovent.Desktop.Forms.Restaurant.ActivityLog;

partial class ActivityLogView
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        _rootLayout = new TableLayoutPanel();
        _headerPanel = new PanelControl();
        _titleLabel = new LabelControl();
        _subTitleLabel = new LabelControl();
        _toolbar = new FlowLayoutPanel();
        _searchEdit = new TextEdit();
        _refreshButton = new SimpleButton();
        _gridHost = new PanelControl();
        _grid = new GridControl();
        _gridView = new GridView();
        _emptyStateLabel = new LabelControl();
        _columnOccurredAtUtc = new GridColumn();
        _columnPerformedBy = new GridColumn();
        _columnMachineName = new GridColumn();
        _columnAction = new GridColumn();
        _columnDetails = new GridColumn();

        ((System.ComponentModel.ISupportInitialize)_headerPanel).BeginInit();
        _headerPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_searchEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_grid).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_gridView).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_gridHost).BeginInit();
        _gridHost.SuspendLayout();
        _rootLayout.SuspendLayout();
        SuspendLayout();

        //
        // _rootLayout
        //
        _rootLayout.Dock = DockStyle.Fill;
        _rootLayout.ColumnCount = 1;
        _rootLayout.RowCount = 3;
        _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _rootLayout.Margin = new Padding(0);
        _rootLayout.Padding = new Padding(0);

        //
        // _headerPanel
        //
        _headerPanel.Dock = DockStyle.Fill;
        _headerPanel.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
        _headerPanel.Padding = new Padding(24, 12, 24, 4);
        _headerPanel.Margin = new Padding(0);

        var titleBox = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(0)
        };

        _titleLabel.Text = "ACTIVITY LOG";
        _titleLabel.Font = new Font("Segoe UI", 13.5F, FontStyle.Bold);
        _titleLabel.ForeColor = Color.FromArgb(15, 23, 42);
        _titleLabel.Margin = new Padding(0, 0, 0, 3);
        _titleLabel.AutoSizeMode = LabelAutoSizeMode.Default;

        _subTitleLabel.Text = "Review user and system activity across Restaurant operations.";
        _subTitleLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        _subTitleLabel.ForeColor = Color.FromArgb(100, 116, 139);
        _subTitleLabel.Margin = new Padding(0);

        titleBox.Controls.Add(_titleLabel);
        titleBox.Controls.Add(_subTitleLabel);
        _headerPanel.Controls.Add(titleBox);

        //
        // _toolbar
        //
        _toolbar.Dock = DockStyle.Fill;
        _toolbar.AutoSize = true;
        _toolbar.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _toolbar.FlowDirection = FlowDirection.LeftToRight;
        _toolbar.Name = "_toolbar";
        _toolbar.Padding = new Padding(24, 4, 24, 8);
        _toolbar.Margin = new Padding(0);
        _toolbar.WrapContents = false;

        //
        // _searchEdit
        //
        _searchEdit.Name = "_searchEdit";
        _searchEdit.Properties.NullValuePrompt = "Search action, user, or details...";
        _searchEdit.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _searchEdit.Properties.Appearance.Options.UseFont = true;
        _searchEdit.MinimumSize = new Size(280, 30);
        _searchEdit.Width = 400;
        _searchEdit.EditValueChanged += SearchEdit_EditValueChanged;
        _searchEdit.HandleCreated += SearchEdit_HandleCreated;

        //
        // _refreshButton
        //
        _refreshButton.Name = "_refreshButton";
        _refreshButton.Text = "Refresh";
        _refreshButton.Appearance.Font = new Font("Segoe UI", 9.5F);
        _refreshButton.Appearance.Options.UseFont = true;
        _refreshButton.MinimumSize = new Size(100, 30);
        _refreshButton.Margin = new Padding(8, 0, 0, 0);
        _refreshButton.Click += RefreshButton_Click;
        Clovent.Desktop.Forms.Base.DesktopIcons.Apply(_refreshButton, Clovent.Desktop.Forms.Base.DesktopIcons.Refresh);

        _toolbar.Controls.Add(_searchEdit);
        _toolbar.Controls.Add(_refreshButton);

        //
        // _gridHost
        //
        _gridHost.Dock = DockStyle.Fill;
        _gridHost.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
        _gridHost.Padding = new Padding(24, 0, 24, 16);
        _gridHost.Margin = new Padding(0);
        _gridHost.Name = "_gridHost";

        //
        // _columnOccurredAtUtc
        //
        _columnOccurredAtUtc.Caption = "Date/Time";
        _columnOccurredAtUtc.FieldName = "OccurredAtUtc";
        _columnOccurredAtUtc.Name = "_columnOccurredAtUtc";
        _columnOccurredAtUtc.Visible = true;
        _columnOccurredAtUtc.VisibleIndex = 0;
        _columnOccurredAtUtc.Width = 160;
        _columnOccurredAtUtc.AppearanceHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _columnOccurredAtUtc.AppearanceHeader.Options.UseFont = true;

        //
        // _columnPerformedBy
        //
        _columnPerformedBy.Caption = "User";
        _columnPerformedBy.FieldName = "PerformedBy";
        _columnPerformedBy.Name = "_columnPerformedBy";
        _columnPerformedBy.Visible = true;
        _columnPerformedBy.VisibleIndex = 1;
        _columnPerformedBy.Width = 140;
        _columnPerformedBy.AppearanceHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _columnPerformedBy.AppearanceHeader.Options.UseFont = true;

        //
        // _columnMachineName
        //
        _columnMachineName.Caption = "Workstation";
        _columnMachineName.FieldName = "MachineName";
        _columnMachineName.Name = "_columnMachineName";
        _columnMachineName.Visible = true;
        _columnMachineName.VisibleIndex = 2;
        _columnMachineName.Width = 140;
        _columnMachineName.AppearanceHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _columnMachineName.AppearanceHeader.Options.UseFont = true;

        //
        // _columnAction
        //
        _columnAction.Caption = "Action";
        _columnAction.FieldName = "Action";
        _columnAction.Name = "_columnAction";
        _columnAction.Visible = true;
        _columnAction.VisibleIndex = 3;
        _columnAction.Width = 140;
        _columnAction.AppearanceHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _columnAction.AppearanceHeader.Options.UseFont = true;

        //
        // _columnDetails
        //
        _columnDetails.Caption = "Details";
        _columnDetails.FieldName = "Details";
        _columnDetails.Name = "_columnDetails";
        _columnDetails.Visible = true;
        _columnDetails.VisibleIndex = 4;
        _columnDetails.Width = 300;
        _columnDetails.AppearanceHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _columnDetails.AppearanceHeader.Options.UseFont = true;

        //
        // _gridView
        //
        _gridView.Columns.AddRange(new GridColumn[] { _columnOccurredAtUtc, _columnPerformedBy, _columnMachineName, _columnAction, _columnDetails });
        _gridView.GridControl = _grid;
        _gridView.Name = "_gridView";
        _gridView.OptionsBehavior.Editable = false;
        _gridView.OptionsSelection.MultiSelect = false;
        _gridView.OptionsView.ShowGroupPanel = false;
        _gridView.OptionsView.ColumnAutoWidth = true;
        _gridView.RowHeight = 32;
        _gridView.ColumnPanelRowHeight = 32;
        _gridView.Appearance.Row.Font = new Font("Segoe UI", 9.5F);
        _gridView.Appearance.Row.Options.UseFont = true;
        _gridView.CustomColumnDisplayText += GridView_CustomColumnDisplayText;

        //
        // _grid
        //
        _grid.Dock = DockStyle.Fill;
        _grid.MainView = _gridView;
        _grid.Name = "_grid";
        _grid.ViewCollection.Add(_gridView);

        //
        // _emptyStateLabel
        //
        _emptyStateLabel.Dock = DockStyle.Fill;
        _emptyStateLabel.Name = "_emptyStateLabel";
        _emptyStateLabel.Text = "No activity recorded yet.";
        _emptyStateLabel.Appearance.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
        _emptyStateLabel.Appearance.ForeColor = Color.FromArgb(100, 116, 139);
        _emptyStateLabel.Appearance.Options.UseFont = true;
        _emptyStateLabel.Appearance.Options.UseForeColor = true;
        _emptyStateLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        _emptyStateLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _emptyStateLabel.Visible = false;

        _gridHost.Controls.Add(_emptyStateLabel);
        _gridHost.Controls.Add(_grid);

        //
        // Assembly into _rootLayout
        //
        _rootLayout.Controls.Add(_headerPanel, 0, 0);
        _rootLayout.Controls.Add(_toolbar, 0, 1);
        _rootLayout.Controls.Add(_gridHost, 0, 2);

        //
        // ActivityLogView
        //
        Controls.Add(_rootLayout);
        Dock = DockStyle.Fill;
        Name = "ActivityLogView";
        AppearanceManager.Changed += AppearanceManager_Changed;
        Load += ActivityLogView_Load;

        ((System.ComponentModel.ISupportInitialize)_headerPanel).EndInit();
        _headerPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_searchEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_grid).EndInit();
        ((System.ComponentModel.ISupportInitialize)_gridView).EndInit();
        ((System.ComponentModel.ISupportInitialize)_gridHost).EndInit();
        _gridHost.ResumeLayout(false);
        _rootLayout.ResumeLayout(false);
        _rootLayout.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel _rootLayout;
    private PanelControl _headerPanel;
    private LabelControl _titleLabel;
    private LabelControl _subTitleLabel;
    private FlowLayoutPanel _toolbar;
    private TextEdit _searchEdit;
    private SimpleButton _refreshButton;
    private PanelControl _gridHost;
    private GridControl _grid;
    private GridView _gridView;
    private LabelControl _emptyStateLabel;
    private GridColumn _columnOccurredAtUtc;
    private GridColumn _columnPerformedBy;
    private GridColumn _columnMachineName;
    private GridColumn _columnAction;
    private GridColumn _columnDetails;
}
