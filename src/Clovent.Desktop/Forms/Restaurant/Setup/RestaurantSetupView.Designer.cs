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

    private readonly ComboBoxEdit _itemsPerRowCombo = new();
    private readonly RadioGroup _activeOrdersRadioGroup = new();
    private readonly ComboBoxEdit _defaultPaymentMethodCombo = new();
    private readonly SimpleButton _savePosButton = new() { Text = "Save POS Settings" };
    private readonly LabelControl _posStatusLabel = new();

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

        //
        // _prefixEdit / _startingNumberEdit / _saveButton / _statusLabel / _previewLabel
        //
        _prefixEdit.Name = "_prefixEdit";
        _prefixEdit.Width = 150;
        _prefixEdit.Properties.Appearance.Font = new Font("Segoe UI", 10F);
        _prefixEdit.Properties.Appearance.Options.UseFont = true;
        _prefixEdit.EditValueChanged += PrefixEdit_EditValueChanged;
        _startingNumberEdit.Name = "_startingNumberEdit";
        _startingNumberEdit.Width = 150;
        _startingNumberEdit.Properties.Appearance.Font = new Font("Segoe UI", 10F);
        _startingNumberEdit.Properties.Appearance.Options.UseFont = true;
        _startingNumberEdit.EditValueChanged += StartingNumberEdit_EditValueChanged;
        _saveButton.AutoSize = true;
        _saveButton.Name = "_saveButton";
        _saveButton.Click += SaveButton_Click;
        Clovent.Desktop.Forms.Base.DesktopIcons.Apply(_saveButton, Clovent.Desktop.Forms.Base.DesktopIcons.Save);
        _statusLabel.Name = "_statusLabel";
        _previewLabel.Name = "_previewLabel";
        _previewLabel.Appearance.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _previewLabel.Appearance.Options.UseFont = true;
        _previewLabel.Margin = new Padding(0, 8, 0, 4);

        var numberingCard = BuildCard("Order Numbering");
        var numberingCardLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0),
            Padding = new Padding(0),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        numberingCardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        numberingCardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        numberingCardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        numberingCardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var numberingForm = BuildFieldTable();
        AddRow(numberingForm, "Order Number Prefix:", _prefixEdit);
        AddRow(numberingForm, "Starting Number:", _startingNumberEdit);
        AddRow(numberingForm, "Next order number:", _previewLabel);

        var numberingButtonRow = BuildButtonRow();
        _statusLabel.Margin = new Padding(12, 8, 12, 0);
        numberingButtonRow.Controls.Add(_statusLabel);
        numberingButtonRow.Controls.Add(_saveButton);

        var numberingNote = BuildNote("Changing these only affects orders created from now on - existing order numbers are never renumbered.");
        numberingNote.Dock = DockStyle.Fill;

        numberingForm.Dock = DockStyle.Fill;
        numberingButtonRow.Dock = DockStyle.Fill;

        numberingCardLayout.Controls.Add(numberingForm, 0, 0);
        numberingCardLayout.Controls.Add(numberingButtonRow, 0, 1);
        numberingCardLayout.Controls.Add(numberingNote, 0, 2);
        numberingCard.Controls.Add(numberingCardLayout);

        //
        // _languageCombo / _saveLanguageButton / _languageStatusLabel
        //
        _languageCombo.Name = "_languageCombo";
        _languageCombo.Width = 260;
        _languageCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _languageCombo.Properties.Appearance.Font = new Font("Segoe UI", 10F);
        _languageCombo.Properties.Appearance.Options.UseFont = true;
        _languageCombo.Properties.AppearanceDropDown.Font = new Font("Segoe UI", 10F);
        _languageCombo.Properties.AppearanceDropDown.Options.UseFont = true;
        _saveLanguageButton.AutoSize = true;
        _saveLanguageButton.Name = "_saveLanguageButton";
        _saveLanguageButton.Click += SaveLanguageButton_Click;
        Clovent.Desktop.Forms.Base.DesktopIcons.Apply(_saveLanguageButton, Clovent.Desktop.Forms.Base.DesktopIcons.Save);
        _languageStatusLabel.Name = "_languageStatusLabel";

        var languageCard = BuildCard("Display Language");
        var languageCardLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0),
            Padding = new Padding(0),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        languageCardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        languageCardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        languageCardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        languageCardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var languageForm = BuildFieldTable();
        AddRow(languageForm, "Language:", _languageCombo);

        var languageButtonRow = BuildButtonRow();
        _languageStatusLabel.Margin = new Padding(12, 8, 12, 0);
        languageButtonRow.Controls.Add(_languageStatusLabel);
        languageButtonRow.Controls.Add(_saveLanguageButton);

        var languageNote = BuildNote("Applies immediately to this window; already-open POS/Menu Items/Setup tabs pick it up the next time they're reopened. Layout stays left-to-right - this is a text translation, not a right-to-left redesign.");
        languageNote.Dock = DockStyle.Fill;

        languageForm.Dock = DockStyle.Fill;
        languageButtonRow.Dock = DockStyle.Fill;

        languageCardLayout.Controls.Add(languageForm, 0, 0);
        languageCardLayout.Controls.Add(languageButtonRow, 0, 1);
        languageCardLayout.Controls.Add(languageNote, 0, 2);
        languageCard.Controls.Add(languageCardLayout);

        //
        // POS Layout Settings Card
        //
        _itemsPerRowCombo.Name = "_itemsPerRowCombo";
        _itemsPerRowCombo.Width = 120;
        _itemsPerRowCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _itemsPerRowCombo.Properties.AutoHeight = true;
        _itemsPerRowCombo.Properties.Appearance.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _itemsPerRowCombo.Properties.Appearance.Options.UseFont = true;
        _itemsPerRowCombo.Properties.AppearanceDropDown.Font = new Font("Segoe UI", 10F);
        _itemsPerRowCombo.Properties.AppearanceDropDown.Options.UseFont = true;
        for (int i = 4; i <= 8; i++)
        {
            _itemsPerRowCombo.Properties.Items.Add(i);
        }

        _activeOrdersRadioGroup.Name = "_activeOrdersRadioGroup";
        _activeOrdersRadioGroup.Width = 280;
        _activeOrdersRadioGroup.Height = 32;
        _activeOrdersRadioGroup.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _activeOrdersRadioGroup.Properties.Appearance.Options.UseFont = true;
        _activeOrdersRadioGroup.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
        _activeOrdersRadioGroup.Properties.Items.Add(new DevExpress.XtraEditors.Controls.RadioGroupItem(false, "Show Active Orders"));
        _activeOrdersRadioGroup.Properties.Items.Add(new DevExpress.XtraEditors.Controls.RadioGroupItem(true, "Hide Active Orders"));

        _defaultPaymentMethodCombo.Name = "_defaultPaymentMethodCombo";
        _defaultPaymentMethodCombo.Width = 180;
        _defaultPaymentMethodCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _defaultPaymentMethodCombo.Properties.AutoHeight = true;
        _defaultPaymentMethodCombo.Properties.Appearance.Font = new Font("Segoe UI", 10F);
        _defaultPaymentMethodCombo.Properties.Appearance.Options.UseFont = true;
        _defaultPaymentMethodCombo.Properties.AppearanceDropDown.Font = new Font("Segoe UI", 10F);
        _defaultPaymentMethodCombo.Properties.AppearanceDropDown.Options.UseFont = true;

        _savePosButton.AutoSize = true;
        _savePosButton.Name = "_savePosButton";
        _savePosButton.Click += SavePosButton_Click;
        Clovent.Desktop.Forms.Base.DesktopIcons.Apply(_savePosButton, Clovent.Desktop.Forms.Base.DesktopIcons.Save);
        _posStatusLabel.Name = "_posStatusLabel";

        var posCard = BuildCard("POS Layout Settings");
        var posCardLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Margin = new Padding(0),
            Padding = new Padding(0),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        posCardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        posCardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        posCardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        posCardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        posCardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var posForm = BuildFieldTable();
        AddRow(posForm, "Items Per Row:", _itemsPerRowCombo);
        AddRow(posForm, "Active Orders:", _activeOrdersRadioGroup);
        AddRow(posForm, "Default Payment:", _defaultPaymentMethodCombo);

        var posButtonRow = BuildButtonRow();
        _posStatusLabel.Margin = new Padding(12, 8, 12, 0);
        posButtonRow.Controls.Add(_posStatusLabel);
        posButtonRow.Controls.Add(_savePosButton);

        var posNote = BuildNote("Controls how many menu items are displayed in each row of the POS Grid view (4–8), whether Active Orders starts visible, and the default tender method.");
        posNote.Dock = DockStyle.Fill;
        var posInfoNote = BuildNote("The system also synchronizes Active Orders and Grid/List preferences automatically when toggled in the POS screen.");
        posInfoNote.Dock = DockStyle.Fill;

        posForm.Dock = DockStyle.Fill;
        posButtonRow.Dock = DockStyle.Fill;

        posCardLayout.Controls.Add(posForm, 0, 0);
        posCardLayout.Controls.Add(posButtonRow, 0, 1);
        posCardLayout.Controls.Add(posNote, 0, 2);
        posCardLayout.Controls.Add(posInfoNote, 0, 3);
        posCard.Controls.Add(posCardLayout);

        //
        // RestaurantSetupView
        //
        var layoutContainer = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(16),
            Name = "layoutContainer"
        };
        layoutContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        layoutContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        layoutContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layoutContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        numberingCard.Dock = DockStyle.Fill;
        numberingCard.Margin = new Padding(0, 0, 8, 8);
        languageCard.Dock = DockStyle.Fill;
        languageCard.Margin = new Padding(8, 0, 0, 8);
        posCard.Dock = DockStyle.Fill;
        posCard.Margin = new Padding(8, 8, 0, 0);

        layoutContainer.Controls.Add(numberingCard, 0, 0);
        layoutContainer.SetRowSpan(numberingCard, 2);
        layoutContainer.Controls.Add(languageCard, 1, 0);
        layoutContainer.Controls.Add(posCard, 1, 1);

        Controls.Add(layoutContainer);
        Name = "RestaurantSetupView";
        AutoScroll = true;

        AppearanceManager.Changed += AppearanceManager_Changed;
        Load += RestaurantSetupView_Load;

        ((System.ComponentModel.ISupportInitialize)_startingNumberEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_languageCombo.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_itemsPerRowCombo.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_activeOrdersRadioGroup.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_defaultPaymentMethodCombo.Properties).EndInit();
        ResumeLayout(false);
    }

    #endregion

    /// <summary>
    /// One titled settings "card": a bordered <see cref="GroupControl"/>
    /// whose caption is the section heading, docked to the top of the
    /// screen. Editors dock/stretch inside it, so nothing depends on fixed
    /// pixel widths (this app has no AutoScaleMode - see DesktopDpi).
    /// </summary>
    private static GroupControl BuildCard(string title) => new()
    {
        Text = title,
        Padding = new Padding(16, 8, 16, 12),
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
    };

    /// <summary>The label|editor grid inside a card. Editors fill their column, so both stay fully visible at every DPI.</summary>
    private static TableLayoutPanel BuildFieldTable() => new()
    {
        Dock = DockStyle.Top,
        ColumnCount = 2,
        AutoSize = true,
        Padding = new Padding(0, 4, 0, 0),
        ColumnStyles =
        {
            // AutoSize label column (not a fixed width): with no
            // AutoScaleMode in this app, a fixed label column clips its
            // captions at above-100% DPI.
            new ColumnStyle(SizeType.AutoSize),
            new ColumnStyle(SizeType.Percent, 100F),
        },
    };

    /// <summary>The card's bottom action row - buttons right-aligned (RightToLeft flow), status text to their left.</summary>
    private static FlowLayoutPanel BuildButtonRow() => new()
    {
        Dock = DockStyle.Bottom,
        FlowDirection = FlowDirection.RightToLeft,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Padding = new Padding(0, 8, 0, 0),
    };

    private static LabelControl BuildNote(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Bottom,
        AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical,
        Padding = new Padding(0, 8, 0, 0),
    };

    private static void AddRow(TableLayoutPanel panel, string label, Control editor)
    {
        panel.RowCount += 1;
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(new LabelControl { Text = label, Padding = new Padding(0, 6, 8, 0) }, 0, panel.RowCount - 1);
        editor.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        editor.Margin = new Padding(0, 4, 0, 4);
        panel.Controls.Add(editor, 1, panel.RowCount - 1);
    }
}
