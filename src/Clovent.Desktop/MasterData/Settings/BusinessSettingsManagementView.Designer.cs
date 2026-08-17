using DevExpress.XtraEditors;

namespace Clovent.Desktop.MasterData.Settings;

partial class BusinessSettingsManagementView
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    private OrganizationHierarchySelector _selector;
    private readonly ComboBoxEdit _currencyCombo = new();
    private readonly ComboBoxEdit _languageCombo = new();
    private readonly ComboBoxEdit _timeZoneCombo = new();
    private readonly ComboBoxEdit _fiscalYearCombo = new();
    private readonly TextEdit _dateFormatEdit = new();
    private readonly SimpleButton _saveButton = new() { Text = "Save" };
    private readonly LabelControl _statusLabel = new();

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;

        _selector = new OrganizationHierarchySelector(_mediator, showCompany: false, showBranch: false);
        _selector.SelectionChanged += Selector_SelectionChanged;

        _saveButton.Click += SaveButton_Click;

        BuildLayout();

        Load += BusinessSettingsManagementView_Load;
    }

    private void BuildLayout()
    {
        var form = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true, Padding = new Padding(12) };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        AddRow(form, "Default Currency:", _currencyCombo);
        AddRow(form, "Default Language:", _languageCombo);
        AddRow(form, "Default Time Zone:", _timeZoneCombo);
        AddRow(form, "Default Fiscal Year:", _fiscalYearCombo);
        AddRow(form, "Date Format:", _dateFormatEdit);

        var buttonRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40 };
        buttonRow.Controls.Add(_saveButton);
        buttonRow.Controls.Add(_statusLabel);

        foreach (var combo in new[] { _currencyCombo, _languageCombo, _timeZoneCombo, _fiscalYearCombo })
        {
            combo.Width = 260;
            combo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        }
        _dateFormatEdit.Width = 260;

        Controls.Add(form);
        Controls.Add(buttonRow);
        Controls.Add(_selector);
    }

    private static void AddRow(TableLayoutPanel panel, string label, Control editor)
    {
        var rowIndex = panel.RowCount;
        panel.RowCount = rowIndex + 1;
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(new LabelControl { Text = label, Padding = new Padding(0, 6, 8, 0) }, 0, rowIndex);
        panel.Controls.Add(editor, 1, rowIndex);
    }

    #endregion
}
