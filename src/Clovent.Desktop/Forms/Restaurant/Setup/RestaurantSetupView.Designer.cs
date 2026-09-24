using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Forms.Restaurant.Setup;

partial class RestaurantSetupView
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    private readonly TextEdit _prefixEdit = new();
    private readonly SpinEdit _startingNumberEdit = new() { Properties = { MinValue = 1, MaxValue = 999_999_999, Mask = { EditMask = "N0" } } };
    private readonly LabelControl _previewLabel = new();

    private readonly ComboBoxEdit _languageCombo = new();

    private readonly ComboBoxEdit _itemsPerRowCombo = new();
    private readonly RadioGroup _activeOrdersRadioGroup = new();
    private readonly ComboBoxEdit _defaultPaymentMethodCombo = new();

    private readonly SimpleButton _saveSettingsButton = new() { Text = "Save Settings" };
    private readonly LabelControl _statusLabel = new();

    private PanelControl _headerPanel;
    private XtraScrollableControl _scrollContainer;
    private TableLayoutPanel _contentLayout;
    private GroupControl _numberingCard;
    private GroupControl _languageCard;
    private GroupControl _posCard;
    private TableLayoutPanel _actionsPanel;

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        ((System.ComponentModel.ISupportInitialize)_startingNumberEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_languageCombo.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_itemsPerRowCombo.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_activeOrdersRadioGroup.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_defaultPaymentMethodCombo.Properties).BeginInit();
        SuspendLayout();

        Dock = DockStyle.Fill;
        Name = "RestaurantSetupView";

        //
        // Header Panel
        //
        _headerPanel = new PanelControl
        {
            Dock = DockStyle.Top,
            BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder,
            Height = 54,
            Padding = new Padding(24, 10, 24, 4)
        };

        var headerTitleBox = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(0)
        };

        var titleLabel = new LabelControl
        {
            Text = "RESTAURANT SETUP",
            Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Margin = new Padding(0, 0, 0, 3)
        };

        var subTitleLabel = new LabelControl
        {
            Text = "Configure restaurant order numbering, display language, and POS behavior.",
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            ForeColor = Color.FromArgb(100, 116, 139),
            Margin = new Padding(0)
        };

        headerTitleBox.Controls.Add(titleLabel);
        headerTitleBox.Controls.Add(subTitleLabel);
        _headerPanel.Controls.Add(headerTitleBox);

        //
        // Scroll Container
        //
        _scrollContainer = new XtraScrollableControl
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(24, 4, 24, 12)
        };

        _contentLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 4,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        _contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _contentLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _contentLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _contentLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _contentLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        //
        // Card 1: Order Numbering
        //
        _prefixEdit.Name = "_prefixEdit";
        _prefixEdit.Width = 200;
        _prefixEdit.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _prefixEdit.Properties.Appearance.Options.UseFont = true;
        _prefixEdit.EditValueChanged += PrefixEdit_EditValueChanged;

        _startingNumberEdit.Name = "_startingNumberEdit";
        _startingNumberEdit.Width = 200;
        _startingNumberEdit.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _startingNumberEdit.Properties.Appearance.Options.UseFont = true;
        _startingNumberEdit.EditValueChanged += StartingNumberEdit_EditValueChanged;

        _previewLabel.Name = "_previewLabel";
        _previewLabel.Appearance.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _previewLabel.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
        _previewLabel.Appearance.Options.UseFont = true;
        _previewLabel.Appearance.Options.UseForeColor = true;
        _previewLabel.Margin = new Padding(0, 4, 0, 4);

        _numberingCard = BuildCard("ORDER NUMBERING");
        var numberingForm = BuildFieldTable();
        AddRow(numberingForm, "Order Number Prefix:", _prefixEdit);
        AddRow(numberingForm, "Starting Number:", _startingNumberEdit);
        AddRow(numberingForm, "Next Order Number:", _previewLabel);

        var numberingNote = BuildNote("Changes apply to new orders only. Existing order numbers are unchanged.");
        AddNoteRow(numberingForm, numberingNote);

        _numberingCard.Controls.Add(numberingForm);

        //
        // Card 2: Display Language
        //
        _languageCombo.Name = "_languageCombo";
        _languageCombo.Width = 240;
        _languageCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _languageCombo.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _languageCombo.Properties.Appearance.Options.UseFont = true;
        _languageCombo.Properties.AppearanceDropDown.Font = new Font("Segoe UI", 9.5F);
        _languageCombo.Properties.AppearanceDropDown.Options.UseFont = true;

        _languageCard = BuildCard("DISPLAY");
        var languageForm = BuildFieldTable();
        AddRow(languageForm, "Language:", _languageCombo);

        var languageNote = BuildNote("Language changes apply to supported screens upon reopening.");
        AddNoteRow(languageForm, languageNote);

        _languageCard.Controls.Add(languageForm);

        //
        // Card 3: POS Layout Settings
        //
        _itemsPerRowCombo.Name = "_itemsPerRowCombo";
        _itemsPerRowCombo.Width = 120;
        _itemsPerRowCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _itemsPerRowCombo.Properties.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _itemsPerRowCombo.Properties.Appearance.Options.UseFont = true;
        _itemsPerRowCombo.Properties.AppearanceDropDown.Font = new Font("Segoe UI", 9.5F);
        _itemsPerRowCombo.Properties.AppearanceDropDown.Options.UseFont = true;
        for (int i = 4; i <= 8; i++)
        {
            _itemsPerRowCombo.Properties.Items.Add(i);
        }

        _activeOrdersRadioGroup.Name = "_activeOrdersRadioGroup";
        _activeOrdersRadioGroup.Width = 220;
        _activeOrdersRadioGroup.Height = 26;
        _activeOrdersRadioGroup.MaximumSize = new Size(0, 26);
        _activeOrdersRadioGroup.BackColor = Color.Transparent;
        _activeOrdersRadioGroup.Properties.Appearance.BackColor = Color.Transparent;
        _activeOrdersRadioGroup.Properties.Appearance.Options.UseBackColor = true;
        _activeOrdersRadioGroup.Properties.AppearanceFocused.BackColor = Color.Transparent;
        _activeOrdersRadioGroup.Properties.AppearanceFocused.Options.UseBackColor = true;
        _activeOrdersRadioGroup.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _activeOrdersRadioGroup.Properties.Appearance.Options.UseFont = true;
        _activeOrdersRadioGroup.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
        _activeOrdersRadioGroup.Properties.Padding = new Padding(0);
        _activeOrdersRadioGroup.Properties.Columns = 2;
        _activeOrdersRadioGroup.Properties.Items.Add(new DevExpress.XtraEditors.Controls.RadioGroupItem(false, "Show"));
        _activeOrdersRadioGroup.Properties.Items.Add(new DevExpress.XtraEditors.Controls.RadioGroupItem(true, "Hide"));

        _defaultPaymentMethodCombo.Name = "_defaultPaymentMethodCombo";
        _defaultPaymentMethodCombo.Width = 240;
        _defaultPaymentMethodCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _defaultPaymentMethodCombo.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _defaultPaymentMethodCombo.Properties.Appearance.Options.UseFont = true;
        _defaultPaymentMethodCombo.Properties.AppearanceDropDown.Font = new Font("Segoe UI", 9.5F);
        _defaultPaymentMethodCombo.Properties.AppearanceDropDown.Options.UseFont = true;

        _posCard = BuildCard("POS LAYOUT");
        var posForm = BuildFieldTable();
        AddRow(posForm, "Items Per Row:", _itemsPerRowCombo);
        AddRow(posForm, "Active Orders:", _activeOrdersRadioGroup, topPaddingAdjustment: 4);
        AddRow(posForm, "Default Payment Method:", _defaultPaymentMethodCombo);

        var posNote = BuildNote("Controls menu density, Active Orders visibility, and the default tender method.");
        AddNoteRow(posForm, posNote);

        _posCard.Controls.Add(posForm);

        //
        // Footer Actions & Status
        //
        _actionsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 10, 0, 10),
            Padding = new Padding(0)
        };
        _actionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _actionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _actionsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _statusLabel.Name = "_statusLabel";
        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _statusLabel.Appearance.ForeColor = Color.FromArgb(13, 148, 136); // Teal
        _statusLabel.Appearance.Options.UseFont = true;
        _statusLabel.Appearance.Options.UseForeColor = true;
        _statusLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _statusLabel.Text = string.Empty;

        _saveSettingsButton.Name = "_saveSettingsButton";
        _saveSettingsButton.Text = "Save Settings";
        _saveSettingsButton.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _saveSettingsButton.Appearance.BackColor = Color.FromArgb(13, 148, 136); // Teal-600
        _saveSettingsButton.Appearance.ForeColor = Color.White;
        _saveSettingsButton.Appearance.Options.UseFont = true;
        _saveSettingsButton.Appearance.Options.UseBackColor = true;
        _saveSettingsButton.Appearance.Options.UseForeColor = true;
        _saveSettingsButton.Size = new Size(150, 38);
        _saveSettingsButton.Cursor = Cursors.Hand;
        _saveSettingsButton.Click += SaveSettingsButton_Click;

        _actionsPanel.Controls.Add(_statusLabel, 0, 0);
        _actionsPanel.Controls.Add(_saveSettingsButton, 1, 0);

        //
        // Assemble Content
        //
        _numberingCard.Margin = new Padding(0, 0, 0, 10);
        _languageCard.Margin = new Padding(0, 0, 0, 10);
        _posCard.Margin = new Padding(0, 0, 0, 10);

        _contentLayout.Controls.Add(_numberingCard, 0, 0);
        _contentLayout.Controls.Add(_languageCard, 0, 1);
        _contentLayout.Controls.Add(_posCard, 0, 2);
        _contentLayout.Controls.Add(_actionsPanel, 0, 3);

        _scrollContainer.Controls.Add(_contentLayout);

        Controls.Add(_scrollContainer);
        Controls.Add(_headerPanel);

        AppearanceManager.Changed += AppearanceManager_Changed;
        Load += RestaurantSetupView_Load;

        ((System.ComponentModel.ISupportInitialize)_prefixEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_startingNumberEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_languageCombo.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_itemsPerRowCombo.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_activeOrdersRadioGroup.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_defaultPaymentMethodCombo.Properties).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private static GroupControl BuildCard(string title) => new()
    {
        Text = title,
        Dock = DockStyle.Top,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Padding = new Padding(18, 10, 18, 12)
    };

    private static TableLayoutPanel BuildFieldTable()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(0, 4, 0, 0),
            Margin = new Padding(0)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F)); // Uniform label column across all cards
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        return panel;
    }

    private static LabelControl BuildNote(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        AutoSizeMode = LabelAutoSizeMode.Vertical,
        Font = new Font("Segoe UI", 9F, FontStyle.Regular),
        ForeColor = Color.FromArgb(100, 116, 139),
        Padding = new Padding(0, 4, 0, 4)
    };

    private static void AddRow(TableLayoutPanel panel, string labelText, Control editor, int topPaddingAdjustment = 0)
    {
        panel.RowCount += 1;
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var lbl = new LabelControl
        {
            Text = labelText,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(51, 65, 85),
            Padding = new Padding(0, 4 + topPaddingAdjustment, 12, 4),
            Dock = DockStyle.Fill
        };
        lbl.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        panel.Controls.Add(lbl, 0, panel.RowCount - 1);

        editor.Anchor = AnchorStyles.Left;
        editor.Margin = new Padding(0, 4 + topPaddingAdjustment, 0, 4);
        panel.Controls.Add(editor, 1, panel.RowCount - 1);
    }

    private static void AddNoteRow(TableLayoutPanel panel, Control note)
    {
        panel.RowCount += 1;
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        note.Margin = new Padding(0, 10, 0, 4);
        panel.Controls.Add(note, 0, panel.RowCount - 1);
        panel.SetColumnSpan(note, 2);
    }
}

