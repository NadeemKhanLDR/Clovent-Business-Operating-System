using Clovent.Desktop.Forms.Base.Appearance;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;

namespace Clovent.Desktop.Forms.Restaurant.Appearance;

partial class AppearanceSettingsView
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    private readonly SimpleButton _newButton = new() { Text = "+  New Rule" };
    private readonly SimpleButton _editButton = new() { Text = "✎  Edit" };
    private readonly SimpleButton _deleteButton = new() { Text = "✕  Delete" };
    private readonly GridControl _grid = new() { Dock = DockStyle.Fill };
    private readonly GridView _gridView = new();

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;
        Name = "AppearanceSettingsView";

        BuildLayout();
        WireEvents();

        AppearanceManager.Changed += AppearanceManager_Changed;

        Load += AppearanceSettingsView_Load;
    }

    #endregion

    private void BuildLayout()
    {
        _gridView.OptionsBehavior.Editable = false;
        _gridView.OptionsSelection.MultiSelect = false;
        _gridView.OptionsView.ShowGroupPanel = false;
        _gridView.OptionsView.ColumnAutoWidth = true;
        _gridView.RowHeight = 32;
        _gridView.Columns.AddVisible("ScopeDescription", "Applies To");
        _gridView.Columns.AddVisible("FontFamily", "Font Family");
        _gridView.Columns.AddVisible("FontSize", "Size");
        _gridView.Columns.AddVisible("ForeColorHex", "Text Color");
        _gridView.Columns.AddVisible("BackColorHex", "Background");
        _gridView.FocusedRowChanged += GridView_FocusedRowChanged;
        _gridView.DoubleClick += GridView_DoubleClick;

        _grid.MainView = _gridView;
        _grid.ViewCollection.Add(_gridView);

        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(8) };
        foreach (var button in new[] { _newButton, _editButton, _deleteButton })
        {
            button.AutoSize = true;
            button.MinimumSize = new Size(90, 40);
            button.Margin = new Padding(0, 0, 8, 0);
            toolbar.Controls.Add(button);
        }

        var note = new LabelControl
        {
            Text = "Changes apply immediately to every open Restaurant screen - no restart needed. More specific rules (a named control) win over broader ones (a whole module).",
            Dock = DockStyle.Top,
            AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical,
            Padding = new Padding(8, 0, 8, 8),
        };
        note.Appearance.ForeColor = Color.Gray;
        note.Appearance.Options.UseForeColor = true;

        Controls.Add(_grid);
        Controls.Add(note);
        Controls.Add(toolbar);
    }

    private void WireEvents()
    {
        _newButton.Click += NewButton_Click;
        _editButton.Click += EditButton_Click;
        _deleteButton.Click += DeleteButton_Click;
    }
}
