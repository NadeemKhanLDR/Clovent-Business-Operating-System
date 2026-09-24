using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;

namespace Clovent.Desktop.Restaurant.SmartPos;

partial class QuickOrderTemplatesView
{
    private System.ComponentModel.IContainer components = null;

    private GridControl _gridControl;
    private GridView _gridView;
    private SimpleButton _newButton;
    private SimpleButton _btnEdit;
    private SimpleButton _btnToggleStatus;
    private SimpleButton _btnOrderHealth;
    private SimpleButton _refreshButton;
    private LabelControl _lblSummary;

    private TableLayoutPanel root;
    private TableLayoutPanel topPanel;
    private FlowLayoutPanel titleBox;
    private LabelControl headerLabel;
    private LabelControl subHeaderLabel;
    private FlowLayoutPanel actionsPanel;
    private TableLayoutPanel bottomPanel;

    private void InitializeComponent()
    {
        _gridControl = new GridControl();
        _gridView = new GridView();
        _newButton = new SimpleButton();
        _btnEdit = new SimpleButton();
        _btnToggleStatus = new SimpleButton();
        _btnOrderHealth = new SimpleButton();
        _refreshButton = new SimpleButton();
        _lblSummary = new LabelControl();

        root = new TableLayoutPanel();
        topPanel = new TableLayoutPanel();
        titleBox = new FlowLayoutPanel();
        headerLabel = new LabelControl();
        subHeaderLabel = new LabelControl();
        actionsPanel = new FlowLayoutPanel();
        bottomPanel = new TableLayoutPanel();

        ((System.ComponentModel.ISupportInitialize)_gridControl).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_gridView).BeginInit();
        SuspendLayout();

        Dock = DockStyle.Fill;
        Name = "QuickOrderTemplatesView";
        Padding = new Padding(24, 18, 24, 18);

        // Root Layout
        root.Dock = DockStyle.Fill;
        root.ColumnCount = 1;
        root.RowCount = 3;
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));  // Header & Buttons
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Grid
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));  // Bottom Summary

        // --- TOP HEADER & BUTTONS ---
        topPanel.Dock = DockStyle.Fill;
        topPanel.ColumnCount = 2;
        topPanel.RowCount = 1;
        topPanel.Padding = new Padding(0, 0, 0, 10);
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        // Title and Subtitle Box
        titleBox.Dock = DockStyle.Fill;
        titleBox.FlowDirection = FlowDirection.TopDown;
        titleBox.WrapContents = false;
        titleBox.Margin = new Padding(0);

        headerLabel.Text = "QUICK ORDER TEMPLATES";
        headerLabel.Font = new Font("Segoe UI", 13.5F, FontStyle.Bold);
        headerLabel.ForeColor = Color.FromArgb(15, 23, 42);
        headerLabel.AutoSizeMode = LabelAutoSizeMode.Default;
        headerLabel.Margin = new Padding(0, 0, 0, 3);

        subHeaderLabel.Text = "Create reusable deals and meal combinations for faster Restaurant POS ordering.";
        subHeaderLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        subHeaderLabel.ForeColor = Color.FromArgb(100, 116, 139);
        subHeaderLabel.AutoSizeMode = LabelAutoSizeMode.Default;

        titleBox.Controls.Add(headerLabel);
        titleBox.Controls.Add(subHeaderLabel);

        // Actions Panel (Right-aligned)
        actionsPanel.Dock = DockStyle.Fill;
        actionsPanel.FlowDirection = FlowDirection.RightToLeft;
        actionsPanel.WrapContents = false;
        actionsPanel.Margin = new Padding(0);
        actionsPanel.Padding = new Padding(0, 4, 0, 0);

        _btnOrderHealth.Text = "Order Health...";
        _btnOrderHealth.Size = new Size(110, 32);
        _btnOrderHealth.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        _btnOrderHealth.Cursor = Cursors.Hand;
        _btnOrderHealth.Margin = new Padding(6, 0, 0, 0);

        _refreshButton.Text = "Refresh";
        _refreshButton.Size = new Size(80, 32);
        _refreshButton.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        _refreshButton.Cursor = Cursors.Hand;
        _refreshButton.Margin = new Padding(16, 0, 0, 0); // Visual gap before status actions

        _btnToggleStatus.Text = "Deactivate";
        _btnToggleStatus.Size = new Size(95, 32);
        _btnToggleStatus.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        _btnToggleStatus.Cursor = Cursors.Hand;
        _btnToggleStatus.Margin = new Padding(6, 0, 0, 0);
        _btnToggleStatus.Enabled = false;

        _btnEdit.Text = "Edit";
        _btnEdit.Size = new Size(80, 32);
        _btnEdit.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        _btnEdit.Cursor = Cursors.Hand;
        _btnEdit.Margin = new Padding(6, 0, 0, 0);
        _btnEdit.Enabled = false;

        _newButton.Text = "+ New Template";
        _newButton.Size = new Size(130, 32);
        _newButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _newButton.Appearance.BackColor = Color.FromArgb(13, 148, 136); // Teal-600
        _newButton.Appearance.ForeColor = Color.White;
        _newButton.Appearance.Options.UseBackColor = true;
        _newButton.Appearance.Options.UseForeColor = true;
        _newButton.Appearance.Options.UseFont = true;
        _newButton.Cursor = Cursors.Hand;
        _newButton.Margin = new Padding(0);

        actionsPanel.Controls.Add(_btnOrderHealth);
        actionsPanel.Controls.Add(_refreshButton);
        actionsPanel.Controls.Add(_btnToggleStatus);
        actionsPanel.Controls.Add(_btnEdit);
        actionsPanel.Controls.Add(_newButton);

        topPanel.Controls.Add(titleBox, 0, 0);
        topPanel.Controls.Add(actionsPanel, 1, 0);

        // --- MAIN GRID ---
        _gridControl.Dock = DockStyle.Fill;
        _gridControl.MainView = _gridView;
        _gridControl.ViewCollection.Add(_gridView);

        _gridView.OptionsBehavior.Editable = false;
        _gridView.OptionsBehavior.ReadOnly = true;
        _gridView.OptionsSelection.MultiSelect = false;
        _gridView.OptionsView.ShowGroupPanel = false;
        _gridView.OptionsView.ShowIndicator = false;
        _gridView.OptionsView.ColumnAutoWidth = true;
        _gridView.OptionsView.EnableAppearanceEvenRow = true;
        _gridView.RowHeight = 32;
        _gridView.ColumnPanelRowHeight = 36;
        _gridView.Appearance.Row.Font = new Font("Segoe UI", 9F);
        _gridView.Appearance.HeaderPanel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _gridView.Appearance.HeaderPanel.ForeColor = Color.FromArgb(51, 65, 85);
        _gridView.Appearance.EvenRow.BackColor = Color.FromArgb(248, 250, 252);
        _gridView.Appearance.FocusedRow.BackColor = Color.FromArgb(224, 242, 254);
        _gridView.Appearance.FocusedRow.ForeColor = Color.FromArgb(15, 23, 42);

        AddColumn("Name", "Template Name", 220, 160, DevExpress.Utils.HorzAlignment.Near);
        AddColumn("Description", "Description", 300, 200, DevExpress.Utils.HorzAlignment.Near);
        AddColumn("DisplayOrder", "Order", 70, 50, DevExpress.Utils.HorzAlignment.Far);
        AddColumn("ItemCount", "Items", 70, 50, DevExpress.Utils.HorzAlignment.Center);
        AddColumn("TotalDisplay", "Total", 120, 90, DevExpress.Utils.HorzAlignment.Far);
        AddColumn("StatusText", "Status", 90, 70, DevExpress.Utils.HorzAlignment.Center);

        _gridView.FocusedRowChanged += GridView_FocusedRowChanged;
        _gridView.DoubleClick += GridView_DoubleClick;

        // --- BOTTOM SUMMARY ---
        bottomPanel.Dock = DockStyle.Fill;
        bottomPanel.ColumnCount = 1;
        bottomPanel.RowCount = 1;
        bottomPanel.Padding = new Padding(6, 8, 6, 4);

        _lblSummary.Dock = DockStyle.Fill;
        _lblSummary.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _lblSummary.ForeColor = Color.FromArgb(71, 85, 105);
        _lblSummary.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Near;
        _lblSummary.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        bottomPanel.Controls.Add(_lblSummary, 0, 0);

        // Wire Button Events
        _newButton.Click += NewButton_Click;
        _btnEdit.Click += BtnEdit_Click;
        _btnToggleStatus.Click += BtnToggleStatus_Click;
        _btnOrderHealth.Click += BtnOrderHealth_Click;
        _refreshButton.Click += RefreshButton_Click;

        Load += QuickOrderTemplatesView_Load;

        // Assemble Root
        root.Controls.Add(topPanel, 0, 0);
        root.Controls.Add(_gridControl, 0, 1);
        root.Controls.Add(bottomPanel, 0, 2);
        Controls.Add(root);

        ((System.ComponentModel.ISupportInitialize)_gridControl).EndInit();
        ((System.ComponentModel.ISupportInitialize)_gridView).EndInit();

        ResumeLayout(false);
    }

    private void AddColumn(string fieldName, string caption, int width, int minWidth, DevExpress.Utils.HorzAlignment alignment)
    {
        var column = _gridView.Columns.AddVisible(fieldName, caption);
        column.Width = width;
        column.MinWidth = minWidth;
        column.AppearanceHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        column.AppearanceHeader.Options.UseFont = true;
        column.AppearanceHeader.TextOptions.HAlignment = alignment;
        column.AppearanceCell.TextOptions.HAlignment = alignment;
        column.AppearanceCell.Options.UseTextOptions = true;
    }
}
