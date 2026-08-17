using Clovent.Desktop.Forms.Base.Appearance;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Forms.Restaurant.Setup;

partial class RestaurantSetupView
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    private readonly TextEdit _prefixEdit = new();
    private readonly SpinEdit _startingNumberEdit = new() { Properties = { MinValue = 1, MaxValue = 999_999_999, Mask = { EditMask = "N0" } } };
    private readonly SimpleButton _saveButton = new() { Text = "Save" };
    private readonly LabelControl _statusLabel = new();
    private readonly LabelControl _previewLabel = new();

    private readonly ComboBoxEdit _languageCombo = new();
    private readonly SimpleButton _saveLanguageButton = new() { Text = "Save Language" };
    private readonly LabelControl _languageStatusLabel = new();

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        ((System.ComponentModel.ISupportInitialize)_startingNumberEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_languageCombo.Properties).BeginInit();
        SuspendLayout();

        Dock = DockStyle.Fill;

        //
        // _prefixEdit / _startingNumberEdit / _saveButton / _statusLabel / _previewLabel
        //
        _prefixEdit.Name = "_prefixEdit";
        _prefixEdit.EditValueChanged += PrefixEdit_EditValueChanged;
        _startingNumberEdit.Name = "_startingNumberEdit";
        _startingNumberEdit.EditValueChanged += StartingNumberEdit_EditValueChanged;
        _saveButton.Name = "_saveButton";
        _saveButton.Click += SaveButton_Click;
        _statusLabel.Name = "_statusLabel";
        _previewLabel.Name = "_previewLabel";

        var form = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true, Padding = new Padding(16) };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        AddRow(form, "Order Number Prefix:", _prefixEdit);
        AddRow(form, "Starting Number:", _startingNumberEdit);
        AddRow(form, "Next order number:", _previewLabel);

        _prefixEdit.Width = 200;
        _startingNumberEdit.Width = 200;
        _previewLabel.Appearance.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _previewLabel.Appearance.Options.UseFont = true;

        var buttonRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(16, 0, 0, 0) };
        buttonRow.Controls.Add(_saveButton);
        _statusLabel.Margin = new Padding(12, 8, 0, 0);
        buttonRow.Controls.Add(_statusLabel);

        var note = new LabelControl
        {
            Text = "Changing these only affects orders created from now on - existing order numbers are never renumbered.",
            Dock = DockStyle.Top,
            AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical,
            Padding = new Padding(16, 8, 16, 0),
        };
        note.Appearance.ForeColor = Color.Gray;
        note.Appearance.Options.UseForeColor = true;

        var languageHeading = new LabelControl { Text = "Display Language", Dock = DockStyle.Top, AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical, Padding = new Padding(16, 20, 16, 4) };
        languageHeading.Appearance.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        languageHeading.Appearance.Options.UseFont = true;

        //
        // _languageCombo / _saveLanguageButton / _languageStatusLabel
        //
        _languageCombo.Name = "_languageCombo";
        _languageCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _languageCombo.Width = 200;
        _saveLanguageButton.Name = "_saveLanguageButton";
        _saveLanguageButton.Click += SaveLanguageButton_Click;
        _languageStatusLabel.Name = "_languageStatusLabel";

        var languageForm = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true, Padding = new Padding(16, 0, 16, 0) };
        languageForm.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        languageForm.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        AddRow(languageForm, "Language:", _languageCombo);

        var languageButtonRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(16, 8, 0, 0) };
        languageButtonRow.Controls.Add(_saveLanguageButton);
        _languageStatusLabel.Margin = new Padding(12, 8, 0, 0);
        languageButtonRow.Controls.Add(_languageStatusLabel);

        var languageNote = new LabelControl
        {
            Text = "Applies immediately to this window; already-open POS/Menu Items/Setup tabs pick it up the next time they're reopened. Layout stays left-to-right - this is a text translation, not a right-to-left redesign.",
            Dock = DockStyle.Top,
            AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical,
            Padding = new Padding(16, 4, 16, 0),
        };
        languageNote.Appearance.ForeColor = Color.Gray;
        languageNote.Appearance.Options.UseForeColor = true;

        //
        // RestaurantSetupView
        //
        // Reverse add-order (Bottom-most content added first) - the same
        // convention this codebase's Dock-based layouts use throughout, so
        // the language section renders below the order-number section
        // rather than needing a second host panel.
        Controls.Add(languageNote);
        Controls.Add(languageButtonRow);
        Controls.Add(languageForm);
        Controls.Add(languageHeading);
        Controls.Add(buttonRow);
        Controls.Add(note);
        Controls.Add(form);
        Name = "RestaurantSetupView";

        AppearanceManager.Changed += AppearanceManager_Changed;
        Load += RestaurantSetupView_Load;

        ((System.ComponentModel.ISupportInitialize)_startingNumberEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_languageCombo.Properties).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private static void AddRow(TableLayoutPanel panel, string label, Control editor)
    {
        var rowIndex = panel.RowCount;
        panel.RowCount = rowIndex + 1;
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(new LabelControl { Text = label, Padding = new Padding(0, 6, 8, 0) }, 0, rowIndex);
        panel.Controls.Add(editor, 1, rowIndex);
    }
}
