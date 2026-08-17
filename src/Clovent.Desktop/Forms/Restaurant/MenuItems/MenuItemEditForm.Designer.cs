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
        label1 = new System.Windows.Forms.Label();
        label2 = new System.Windows.Forms.Label();
        label3 = new System.Windows.Forms.Label();
        label5 = new System.Windows.Forms.Label();
        ((System.ComponentModel.ISupportInitialize)_nameEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_categoryCombo.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_priceEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_activeEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_pictureEdit.Properties).BeginInit();
        SuspendLayout();
        _contentPanel.SuspendLayout();
        _contentPanel.RowCount = 5;
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.Controls.Add(label1, 0, 0);
        _contentPanel.Controls.Add(_nameEdit, 1, 0);
        // label1
        label1.AutoSize = true;
        label1.Dock = System.Windows.Forms.DockStyle.Left;
        label1.Text = "Item Name:";
        label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label1.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _nameEdit
        _nameEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label2, 0, 1);
        _contentPanel.Controls.Add(_categoryCombo, 1, 1);
        // label2
        label2.AutoSize = true;
        label2.Dock = System.Windows.Forms.DockStyle.Left;
        label2.Text = "Category:";
        label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label2.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _categoryCombo
        _categoryCombo.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label3, 0, 2);
        _contentPanel.Controls.Add(_priceEdit, 1, 2);
        // label3
        label3.AutoSize = true;
        label3.Dock = System.Windows.Forms.DockStyle.Left;
        label3.Text = "Selling Price:";
        label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label3.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _priceEdit
        _priceEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(_activeEdit, 0, 3);
        _contentPanel.SetColumnSpan(_activeEdit, 2);
        // _activeEdit
        _activeEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label5, 0, 4);
        _contentPanel.Controls.Add(_imageButtons, 1, 4);
        // label5
        label5.AutoSize = true;
        label5.Dock = System.Windows.Forms.DockStyle.Left;
        label5.Text = "Photo:";
        label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label5.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _imageButtons
        _imageButtons.Height = PhotoBoxSize + 8;
        _imageButtons.Dock = System.Windows.Forms.DockStyle.Fill;
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
        _chooseImageButton.Click += ChooseImageButton_Click;
        //
        // _clearImageButton
        //
        _clearImageButton.Name = "_clearImageButton";
        _clearImageButton.Click += ClearImageButton_Click;
        //
        // MenuItemEditForm
        //
        EnableSaveAndNew();

        var headingLabel = new LabelControl
        {
            Text = "Menu Item",
            Dock = DockStyle.Top,
            Height = 32,
            AutoSizeMode = LabelAutoSizeMode.None,
        };
        headingLabel.Appearance.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        headingLabel.Appearance.Options.UseFont = true;
        headingLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        headingLabel.Appearance.Options.UseTextOptions = true;
        Controls.Add(headingLabel);

        // _noPhotoLabel sits directly on top of _pictureEdit (same Size,
        // same Location) inside this fixed, un-docked host - a FlowLayoutPanel
        // can't overlap two children, so the photo box needs its own plain
        // Panel rather than joining the surrounding FlowLayoutPanel directly.
        var photoBox = new PanelControl { Size = new Size(PhotoBoxSize, PhotoBoxSize), BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder };
        photoBox.Controls.Add(_noPhotoLabel);
        photoBox.Controls.Add(_pictureEdit);

        _imageButtons = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false };
        _imageButtons.Controls.Add(photoBox);
        var buttonColumn = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Margin = new Padding(8, 0, 0, 0) };
        buttonColumn.Controls.Add(_chooseImageButton);
        buttonColumn.Controls.Add(_clearImageButton);
        _imageButtons.Controls.Add(buttonColumn);





        ((System.ComponentModel.ISupportInitialize)_nameEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_categoryCombo.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_priceEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_activeEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_pictureEdit.Properties).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.Label label3;
    private System.Windows.Forms.Label label5;
}
