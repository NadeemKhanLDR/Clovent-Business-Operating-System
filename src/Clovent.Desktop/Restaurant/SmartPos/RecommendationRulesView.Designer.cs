using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;

namespace Clovent.Desktop.Restaurant.SmartPos;

partial class RecommendationRulesView
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
    private LabelControl headerLabel;
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
        headerLabel = new LabelControl();
        actionsPanel = new FlowLayoutPanel();
        bottomPanel = new TableLayoutPanel();

        ((System.ComponentModel.ISupportInitialize)_gridControl).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_gridView).BeginInit();
        SuspendLayout();

        Dock = DockStyle.Fill;
        Name = "RecommendationRulesView";

        // Root Layout
        root.Dock = DockStyle.Fill;
        root.ColumnCount = 1;
        root.RowCount = 3;
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));       // Header & Buttons (responsive, never clipped)
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // Grid
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));  // Bottom Summary

        // --- TOP HEADER & BUTTONS ---
        topPanel.Dock = DockStyle.Fill;
        topPanel.ColumnCount = 2;
        topPanel.RowCount = 1;
        topPanel.Padding = new Padding(12, 6, 12, 6);
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        headerLabel.Text = "RECOMMENDATION RULES";
        headerLabel.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        headerLabel.AutoSizeMode = LabelAutoSizeMode.Horizontal;
        headerLabel.AutoSize = true;
        headerLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        headerLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Near;
        headerLabel.Padding = new Padding(0, 4, 12, 4);

        actionsPanel.Dock = DockStyle.Fill;
        actionsPanel.FlowDirection = FlowDirection.RightToLeft;
        actionsPanel.Padding = new Padding(0, 2, 0, 0);

        _btnOrderHealth.Text = "Order Health...";
        _btnOrderHealth.MinimumSize = new Size(110, 32);
        _btnOrderHealth.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        actionsPanel.Controls.Add(_btnOrderHealth);

        _btnToggleStatus.Text = "Deactivate";
        _btnToggleStatus.MinimumSize = new Size(100, 32);
        _btnToggleStatus.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        actionsPanel.Controls.Add(_btnToggleStatus);

        _btnEdit.Text = "Edit";
        _btnEdit.MinimumSize = new Size(80, 32);
        _btnEdit.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        actionsPanel.Controls.Add(_btnEdit);

        _refreshButton.Text = "Refresh";
        _refreshButton.MinimumSize = new Size(80, 32);
        _refreshButton.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        actionsPanel.Controls.Add(_refreshButton);

        _newButton.Text = "+ New Rule";
        _newButton.MinimumSize = new Size(110, 32);
        _newButton.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        actionsPanel.Controls.Add(_newButton);

        topPanel.Controls.Add(headerLabel, 0, 0);
        topPanel.Controls.Add(actionsPanel, 1, 0);

        // --- MAIN GRID ---
        _gridControl.Dock = DockStyle.Fill;
        _gridControl.MainView = _gridView;
        _gridControl.ViewCollection.Add(_gridView);

        _gridView.OptionsBehavior.Editable = false;
        _gridView.OptionsSelection.MultiSelect = false;
        _gridView.OptionsView.ShowGroupPanel = false;
        _gridView.OptionsView.ColumnAutoWidth = true;
        _gridView.RowHeight = 30;
        _gridView.Appearance.Row.Font = new Font("Segoe UI", 9.5F);
        _gridView.Appearance.Row.Options.UseFont = true;

        AddColumn("TriggerProduct", "Trigger Product", 200);
        AddColumn("RecommendedProduct", "Recommended Product", 220);
        AddColumn("Priority", "Priority", 80, DevExpress.Utils.HorzAlignment.Far);
        AddColumn("StatusText", "Status", 80, DevExpress.Utils.HorzAlignment.Center);
        AddColumn("TimeWindow", "Time Window", 110, DevExpress.Utils.HorzAlignment.Center);
        AddColumn("Days", "Days", 160);
        AddColumn("Notes", "Notes", 180);

        _gridView.FocusedRowChanged += GridView_FocusedRowChanged;
        _gridView.RowCellClick += GridView_RowCellClick;

        // --- BOTTOM SUMMARY ---
        bottomPanel.Dock = DockStyle.Fill;
        bottomPanel.ColumnCount = 1;
        bottomPanel.RowCount = 1;
        bottomPanel.Padding = new Padding(12, 6, 12, 6);

        _lblSummary.Dock = DockStyle.Fill;
        _lblSummary.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _lblSummary.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Near;
        _lblSummary.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        bottomPanel.Controls.Add(_lblSummary, 0, 0);

        // Wire Button Events
        _newButton.Click += NewButton_Click;
        _btnEdit.Click += BtnEdit_Click;
        _btnToggleStatus.Click += BtnToggleStatus_Click;
        _btnOrderHealth.Click += BtnOrderHealth_Click;
        _refreshButton.Click += RefreshButton_Click;

        Load += RecommendationRulesView_Load;

        // Assemble Root
        root.Controls.Add(topPanel, 0, 0);
        root.Controls.Add(_gridControl, 0, 1);
        root.Controls.Add(bottomPanel, 0, 2);
        Controls.Add(root);

        ((System.ComponentModel.ISupportInitialize)_gridControl).EndInit();
        ((System.ComponentModel.ISupportInitialize)_gridView).EndInit();

        ResumeLayout(false);
    }

    private void AddColumn(string fieldName, string caption, int width, DevExpress.Utils.HorzAlignment alignment = DevExpress.Utils.HorzAlignment.Near)
    {
        var column = _gridView.Columns.AddVisible(fieldName, caption);
        column.Width = width;
        column.AppearanceHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        column.AppearanceHeader.Options.UseFont = true;
        column.AppearanceCell.TextOptions.HAlignment = alignment;
        column.AppearanceCell.Options.UseTextOptions = true;
    }
}
