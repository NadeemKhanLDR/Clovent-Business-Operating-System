using DevExpress.XtraEditors;

namespace Clovent.Desktop.MasterData.Settings;

partial class BusinessSettingsManagementView
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    private OrganizationHierarchySelector _selector;
    private readonly ComboBoxEdit _currencyCombo = new();
    private readonly ComboBoxEdit _languageCombo = new();
    private readonly LookUpEdit _timeZoneCombo = new();
    private readonly ComboBoxEdit _fiscalYearCombo = new();
    private readonly ComboBoxEdit _dateFormatCombo = new();
    private readonly LabelControl _exampleLabel = new();
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

        _timeZoneCombo.Properties.ValueMember = "TimeZoneEntryId";
        _timeZoneCombo.Properties.DisplayMember = "DisplayName";
        _timeZoneCombo.Properties.SearchMode = DevExpress.XtraEditors.Controls.SearchMode.AutoFilter;
        _timeZoneCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;
        _timeZoneCombo.Properties.ImmediatePopup = true;
        _timeZoneCombo.Properties.NullText = "Search timezone...";
        _timeZoneCombo.Properties.Columns.Clear();
        _timeZoneCombo.Properties.Columns.Add(new DevExpress.XtraEditors.Controls.LookUpColumnInfo("DisplayName", "Time Zone"));
        _timeZoneCombo.Properties.ShowHeader = false;

        _dateFormatCombo.Properties.Items.Clear();
        _dateFormatCombo.Properties.Items.AddRange(new[]
        {
            "dd/MM/yyyy HH:mm",
            "dd/MM/yyyy hh:mm tt",
            "MM/dd/yyyy HH:mm",
            "MM/dd/yyyy hh:mm tt",
            "yyyy-MM-dd HH:mm"
        });
        _dateFormatCombo.TextChanged += (s, e) => UpdateExampleLabel();

        BuildLayout();

        Load += BusinessSettingsManagementView_Load;
    }

    private void BuildLayout()
    {
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(12),
            AutoSize = true
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 0: selector
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 1: section title
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 2: fields form
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 3: button row
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Row 4: spacer

        // Row 0: Selector
        _selector.Dock = DockStyle.Fill;
        _selector.Margin = new Padding(0, 0, 0, 12);
        mainLayout.Controls.Add(_selector, 0, 0);

        // Row 1: Title
        var titleLabel = new LabelControl
        {
            Text = "Business Settings",
            Font = new Font(Font.FontFamily, 12F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 12)
        };
        mainLayout.Controls.Add(titleLabel, 0, 1);

        // Row 2: Fields Form
        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 0,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 12)
        };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(180, this))); // Consistent labels width
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // Expanding editors column

        AddRow(form, "Default Currency:", _currencyCombo);
        AddRow(form, "Default Language:", _languageCombo);
        AddRow(form, "Default Time Zone:", _timeZoneCombo);
        AddRow(form, "Default Fiscal Year:", _fiscalYearCombo);
        AddRow(form, "Date & Time Format:", _dateFormatCombo);
        AddRow(form, "Example:", _exampleLabel);

        mainLayout.Controls.Add(form, 0, 2);

        // Row 3: Buttons
        var buttonRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 12, 0, 0)
        };
        buttonRow.Controls.Add(_saveButton);
        buttonRow.Controls.Add(_statusLabel);
        _statusLabel.Padding = new Padding(12, 6, 0, 0); // vertically align with save button

        mainLayout.Controls.Add(buttonRow, 0, 3);

        foreach (var combo in new[] { _currencyCombo, _languageCombo, _fiscalYearCombo })
        {
            combo.Dock = DockStyle.Left;
            combo.Width = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(450, this);
            combo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        }

        _timeZoneCombo.Dock = DockStyle.Left;
        _timeZoneCombo.Width = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(450, this);

        _dateFormatCombo.Dock = DockStyle.Left;
        _dateFormatCombo.Width = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(450, this);
        _dateFormatCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;

        _exampleLabel.Dock = DockStyle.Left;
        _exampleLabel.Padding = new Padding(0, 6, 0, 0);
        _exampleLabel.Font = new Font(this.Font, FontStyle.Italic);

        _saveButton.Size = new Size(Clovent.Desktop.Forms.Base.DesktopDpi.Scale(100, this), Clovent.Desktop.Forms.Base.DesktopDpi.Scale(32, this));

        Controls.Add(mainLayout);
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
