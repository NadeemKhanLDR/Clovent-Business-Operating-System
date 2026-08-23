using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;

namespace Clovent.Desktop.Forms.Restaurant.MenuItems;

partial class MenuItemEditForm
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    private readonly TextEdit _nameEdit = new();
    private readonly ComboBoxEdit _categoryCombo = new();
    private readonly SpinEdit _priceEdit = new() { Properties = { MinValue = 0, MaxValue = 1_000_000, Increment = 1m } };
    private readonly CheckEdit _activeEdit = new() { Text = "Active", Checked = true };
    private readonly TextEdit _barcode1Edit = new();
    private readonly TextEdit _barcode2Edit = new();
    private readonly TextEdit _barcode3Edit = new();
    private readonly PictureEdit _pictureEdit = new() { Properties = { SizeMode = PictureSizeMode.Zoom, ShowMenu = false }, Height = PhotoBoxSize, Width = PhotoBoxSize };
    // Sits directly on top of _pictureEdit (same bounds, in the same
    // un-docked host panel) and is only shown while there is no photo -
    // a plain empty picture box reads as "broken/loading", not "optional".
    private readonly LabelControl _noPhotoLabel = new()
    {
        Text = "No Photo",
        Size = new Size(PhotoBoxSize, PhotoBoxSize),
        Location = new Point(0, 0),
        AutoSizeMode = LabelAutoSizeMode.None,
    };
    private readonly SimpleButton _chooseImageButton = new() { Text = "Choose Photo..." };
    private readonly SimpleButton _clearImageButton = new() { Text = "Remove Photo" };
    private FlowLayoutPanel _imageButtons;
    private PanelControl _photoBox;

    /// <summary>Clean up any resources being used.</summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        label1 = new DevExpress.XtraEditors.LabelControl();
        label2 = new DevExpress.XtraEditors.LabelControl();
        label3 = new DevExpress.XtraEditors.LabelControl();
        label5 = new DevExpress.XtraEditors.LabelControl();
        
        var headingLabel = new LabelControl
        {
            Text = "Menu Item",
            Height = 40,
            AutoSizeMode = LabelAutoSizeMode.None,
        };
        _headingLabel = headingLabel;
        headingLabel.Appearance.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        headingLabel.Appearance.Options.UseFont = true;
        headingLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        headingLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        headingLabel.Appearance.Options.UseTextOptions = true;
        headingLabel.Padding = new Padding(0, 4, 0, 2);

        var subtitleLabel = new LabelControl
        {
            Text = "Edit menu item details",
            Height = 22,
            AutoSizeMode = LabelAutoSizeMode.None,
        };
        _subtitleLabel = subtitleLabel;
        subtitleLabel.Appearance.Font = new Font("Segoe UI", 9F);
        subtitleLabel.Appearance.Options.UseFont = true;
        subtitleLabel.Appearance.ForeColor = Color.Gray;
        subtitleLabel.Appearance.Options.UseForeColor = true;
        subtitleLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        subtitleLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Top;
        subtitleLabel.Appearance.Options.UseTextOptions = true;
        subtitleLabel.Padding = new Padding(0, 0, 0, 6);

        ((System.ComponentModel.ISupportInitialize)_nameEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_categoryCombo.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_priceEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_activeEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_barcode1Edit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_barcode2Edit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_barcode3Edit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_pictureEdit.Properties).BeginInit();
        SuspendLayout();
        _contentPanel.SuspendLayout();
        _contentPanel.RowCount = 2;
        _contentPanel.RowStyles.Clear();
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
        _contentPanel.ColumnCount = 2;
        _contentPanel.ColumnStyles.Clear();
        _contentPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
        _contentPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));

        var headerPanel = new TableLayoutPanel
        {
            Dock = System.Windows.Forms.DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
            Padding = new Padding(0, 0, 0, 12),
            AutoSize = true
        };
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        headerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        
        headingLabel.Dock = DockStyle.Fill;
        subtitleLabel.Dock = DockStyle.Fill;
        headerPanel.Controls.Add(headingLabel, 0, 0);
        headerPanel.Controls.Add(subtitleLabel, 0, 1);
        _contentPanel.Controls.Add(headerPanel, 0, 0);
        _contentPanel.SetColumnSpan(headerPanel, 2);

        var leftFieldsPanel = new TableLayoutPanel
        {
            Dock = System.Windows.Forms.DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 9,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        leftFieldsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        leftFieldsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        
        leftFieldsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        leftFieldsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        leftFieldsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        leftFieldsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        leftFieldsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        leftFieldsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        leftFieldsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        leftFieldsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        leftFieldsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        // Row 0: Name
        label1.Text = "Item Name *:";
        label1.Padding = new Padding(0, 6, 8, 0);
        leftFieldsPanel.Controls.Add(label1, 0, 0);
        _nameEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _nameEdit.Margin = new Padding(0, 3, 0, 3);
        _nameEdit.Width = 260;
        leftFieldsPanel.Controls.Add(_nameEdit, 1, 0);

        // Row 1: Category
        label2.Text = "Category *:";
        label2.Padding = new Padding(0, 6, 8, 0);
        leftFieldsPanel.Controls.Add(label2, 0, 1);
        _categoryCombo.Dock = System.Windows.Forms.DockStyle.Top;
        _categoryCombo.Margin = new Padding(0, 3, 0, 3);
        _categoryCombo.Width = 260;
        leftFieldsPanel.Controls.Add(_categoryCombo, 1, 1);

        // Row 2: Price
        label3.Text = "Selling Price *:";
        label3.Padding = new Padding(0, 6, 8, 0);
        leftFieldsPanel.Controls.Add(label3, 0, 2);
        _priceEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _priceEdit.Margin = new Padding(0, 3, 0, 3);
        _priceEdit.Width = 260;
        leftFieldsPanel.Controls.Add(_priceEdit, 1, 2);

        // Row 3: Active
        _activeEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _activeEdit.Margin = new Padding(0, 3, 0, 3);
        leftFieldsPanel.Controls.Add(_activeEdit, 1, 3);

        // Row 4: Barcodes Header
        labelBarcodesHeader = new LabelControl
        {
            Text = "Barcodes:",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Margin = new Padding(0, 12, 0, 4)
        };
        leftFieldsPanel.Controls.Add(labelBarcodesHeader, 0, 4);
        leftFieldsPanel.SetColumnSpan(labelBarcodesHeader, 2);

        // Row 5: Barcode 1
        labelBarcode1 = new LabelControl
        {
            Text = "Barcode 1:",
            Padding = new Padding(0, 6, 8, 0)
        };
        leftFieldsPanel.Controls.Add(labelBarcode1, 0, 5);
        _barcode1Edit.Dock = DockStyle.Top;
        _barcode1Edit.Margin = new Padding(0, 3, 0, 3);
        _barcode1Edit.Width = 260;
        leftFieldsPanel.Controls.Add(_barcode1Edit, 1, 5);

        // Row 6: Barcode 2
        labelBarcode2 = new LabelControl
        {
            Text = "Barcode 2:",
            Padding = new Padding(0, 6, 8, 0)
        };
        leftFieldsPanel.Controls.Add(labelBarcode2, 0, 6);
        _barcode2Edit.Dock = DockStyle.Top;
        _barcode2Edit.Margin = new Padding(0, 3, 0, 3);
        _barcode2Edit.Width = 260;
        leftFieldsPanel.Controls.Add(_barcode2Edit, 1, 6);

        // Row 7: Barcode 3
        labelBarcode3 = new LabelControl
        {
            Text = "Barcode 3:",
            Padding = new Padding(0, 6, 8, 0)
        };
        leftFieldsPanel.Controls.Add(labelBarcode3, 0, 7);
        _barcode3Edit.Dock = DockStyle.Top;
        _barcode3Edit.Margin = new Padding(0, 3, 0, 3);
        _barcode3Edit.Width = 260;
        leftFieldsPanel.Controls.Add(_barcode3Edit, 1, 7);

        // Photo Section Panel
        _imageButtons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Margin = new Padding(0),
            Padding = new Padding(12, 0, 0, 0),
            Dock = System.Windows.Forms.DockStyle.Fill
        };

        // Row 0: Photo Label
        label5.Text = "Photo:";
        label5.Padding = new Padding(0, 0, 0, 4);
        _imageButtons.Controls.Add(label5);

        // Row 1: Photo Box
        var photoBox = new PanelControl { Size = new Size(PhotoBoxSize, PhotoBoxSize), BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder, Margin = new Padding(0, 4, 0, 4) };
        _photoBox = photoBox;
        photoBox.Controls.Add(_noPhotoLabel);
        photoBox.Controls.Add(_pictureEdit);
        _imageButtons.Controls.Add(photoBox);

        // Row 2: Photo Buttons
        var photoButtonRow = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Margin = new Padding(0, 8, 0, 0) };
        _chooseImageButton.Margin = new Padding(0, 0, 4, 0);
        _clearImageButton.Margin = new Padding(4, 0, 0, 0);
        photoButtonRow.Controls.Add(_chooseImageButton);
        photoButtonRow.Controls.Add(_clearImageButton);
        _imageButtons.Controls.Add(photoButtonRow);

        _contentPanel.Controls.Add(leftFieldsPanel, 0, 1);
        _contentPanel.Controls.Add(_imageButtons, 1, 1);

        _contentPanel.ResumeLayout(false);
        _contentPanel.PerformLayout();
        //
        // _nameEdit
        //
        _nameEdit.Name = "_nameEdit";
        //
        // _categoryCombo
        //
        _categoryCombo.Name = "_categoryCombo";
        //
        // _priceEdit
        //
        _priceEdit.Name = "_priceEdit";
        //
        // _activeEdit
        //
        _activeEdit.Name = "_activeEdit";
        //
        // _pictureEdit
        //
        _pictureEdit.Name = "_pictureEdit";
        _pictureEdit.Location = new Point(0, 0);
        //
        // _noPhotoLabel
        //
        _noPhotoLabel.Name = "_noPhotoLabel";
        _noPhotoLabel.Appearance.ForeColor = Color.Gray;
        _noPhotoLabel.Appearance.Options.UseForeColor = true;
        _noPhotoLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        _noPhotoLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _noPhotoLabel.Appearance.Options.UseTextOptions = true;
        _noPhotoLabel.Appearance.BorderColor = Color.Gainsboro;
        _noPhotoLabel.Appearance.Options.UseBorderColor = true;
        //
        // _chooseImageButton
        //
        _chooseImageButton.Name = "_chooseImageButton";
        _chooseImageButton.AutoSize = true;
        _chooseImageButton.Click += ChooseImageButton_Click;
        //
        // _clearImageButton
        //
        _clearImageButton.Name = "_clearImageButton";
        _clearImageButton.AutoSize = true;
        _clearImageButton.Click += ClearImageButton_Click;
        //
        // MenuItemEditForm
        //
        EnableSaveAndNew();

        ((System.ComponentModel.ISupportInitialize)_nameEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_categoryCombo.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_priceEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_activeEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_barcode1Edit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_barcode2Edit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_barcode3Edit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_pictureEdit.Properties).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private DevExpress.XtraEditors.LabelControl label1;
    private DevExpress.XtraEditors.LabelControl label2;
    private DevExpress.XtraEditors.LabelControl label3;
    private DevExpress.XtraEditors.LabelControl label5;
    private DevExpress.XtraEditors.LabelControl labelBarcodesHeader;
    private DevExpress.XtraEditors.LabelControl labelBarcode1;
    private DevExpress.XtraEditors.LabelControl labelBarcode2;
    private DevExpress.XtraEditors.LabelControl labelBarcode3;
    private LabelControl _headingLabel;
    private LabelControl _subtitleLabel;
}
