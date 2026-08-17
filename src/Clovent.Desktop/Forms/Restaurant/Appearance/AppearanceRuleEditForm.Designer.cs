using Clovent.Desktop.Forms.Base.Appearance;
using DevExpress.XtraEditors;
using System.Drawing;
using System.Windows.Forms;

namespace Clovent.Desktop.Forms.Restaurant.Appearance;

partial class AppearanceRuleEditForm
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    private ComboBoxEdit _scopeTypeCombo;
    private ComboBoxEdit _scopeKeyCombo;

    private CheckEdit _overrideFontCheck;
    private ComboBoxEdit _fontFamilyCombo;
    private SpinEdit _fontSizeEdit;
    private CheckEdit _boldCheck;
    private CheckEdit _italicCheck;
    private CheckEdit _underlineCheck;
    private CheckEdit _strikeoutCheck;

    private CheckEdit _overrideForeColorCheck;
    private ColorEdit _foreColorEdit;

    private CheckEdit _overrideBackColorCheck;
    private ColorEdit _backColorEdit;

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
        label4 = new System.Windows.Forms.Label();
        label5 = new System.Windows.Forms.Label();
        label6 = new System.Windows.Forms.Label();
        _stylePanel = new System.Windows.Forms.TableLayoutPanel();
        label8 = new System.Windows.Forms.Label();
        label10 = new System.Windows.Forms.Label();
        _scopeTypeCombo = new ComboBoxEdit();
        _scopeKeyCombo = new ComboBoxEdit();
        _overrideFontCheck = new CheckEdit();
        _fontFamilyCombo = new ComboBoxEdit();
        _fontSizeEdit = new SpinEdit();
        _boldCheck = new CheckEdit();
        _italicCheck = new CheckEdit();
        _underlineCheck = new CheckEdit();
        _strikeoutCheck = new CheckEdit();
        _overrideForeColorCheck = new CheckEdit();
        _foreColorEdit = new ColorEdit();
        _overrideBackColorCheck = new CheckEdit();
        _backColorEdit = new ColorEdit();

        ((System.ComponentModel.ISupportInitialize)_scopeTypeCombo.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_scopeKeyCombo.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_overrideFontCheck.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_fontFamilyCombo.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_fontSizeEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_boldCheck.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_italicCheck.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_underlineCheck.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_strikeoutCheck.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_overrideForeColorCheck.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_foreColorEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_overrideBackColorCheck.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_backColorEdit.Properties).BeginInit();
        SuspendLayout();
        _contentPanel.SuspendLayout();
        _contentPanel.RowCount = 10;
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.Controls.Add(label1, 0, 0);
        _contentPanel.Controls.Add(_scopeTypeCombo, 1, 0);
        // label1
        label1.AutoSize = true;
        label1.Dock = System.Windows.Forms.DockStyle.Left;
        label1.Text = "Applies To:";
        label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label1.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _scopeTypeCombo
        _scopeTypeCombo.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label2, 0, 1);
        _contentPanel.Controls.Add(_scopeKeyCombo, 1, 1);
        // label2
        label2.AutoSize = true;
        label2.Dock = System.Windows.Forms.DockStyle.Left;
        label2.Text = "Scope Key:";
        label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label2.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _scopeKeyCombo
        _scopeKeyCombo.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(_overrideFontCheck, 0, 2);
        _contentPanel.SetColumnSpan(_overrideFontCheck, 2);
        // _overrideFontCheck
        _overrideFontCheck.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label4, 0, 3);
        _contentPanel.Controls.Add(_fontFamilyCombo, 1, 3);
        // label4
        label4.AutoSize = true;
        label4.Dock = System.Windows.Forms.DockStyle.Left;
        label4.Text = "Font Family:";
        label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label4.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _fontFamilyCombo
        _fontFamilyCombo.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label5, 0, 4);
        _contentPanel.Controls.Add(_fontSizeEdit, 1, 4);
        // label5
        label5.AutoSize = true;
        label5.Dock = System.Windows.Forms.DockStyle.Left;
        label5.Text = "Font Size:";
        label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label5.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _fontSizeEdit
        _fontSizeEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label6, 0, 5);
        _contentPanel.Controls.Add(_stylePanel, 1, 5);
        // _stylePanel
        _stylePanel.Dock = System.Windows.Forms.DockStyle.Fill;
        _stylePanel.ColumnCount = 4;
        _stylePanel.RowCount = 1;
        _stylePanel.Margin = new System.Windows.Forms.Padding(0);
        _stylePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
        _stylePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
        _stylePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
        _stylePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
        _stylePanel.Controls.Add(_boldCheck, 0, 0);
        _stylePanel.Controls.Add(_italicCheck, 1, 0);
        _stylePanel.Controls.Add(_underlineCheck, 2, 0);
        _stylePanel.Controls.Add(_strikeoutCheck, 3, 0);

        // label6
        label6.AutoSize = true;
        label6.Dock = System.Windows.Forms.DockStyle.Left;
        label6.Text = "Style:";
        label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label6.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // BuildStyleRow()
        
        _contentPanel.Controls.Add(_overrideForeColorCheck, 0, 6);
        _contentPanel.SetColumnSpan(_overrideForeColorCheck, 2);
        // _overrideForeColorCheck
        _overrideForeColorCheck.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label8, 0, 7);
        _contentPanel.Controls.Add(_foreColorEdit, 1, 7);
        // label8
        label8.AutoSize = true;
        label8.Dock = System.Windows.Forms.DockStyle.Left;
        label8.Text = "Text Color:";
        label8.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label8.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _foreColorEdit
        _foreColorEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(_overrideBackColorCheck, 0, 8);
        _contentPanel.SetColumnSpan(_overrideBackColorCheck, 2);
        // _overrideBackColorCheck
        _overrideBackColorCheck.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label10, 0, 9);
        _contentPanel.Controls.Add(_backColorEdit, 1, 9);
        // label10
        label10.AutoSize = true;
        label10.Dock = System.Windows.Forms.DockStyle.Left;
        label10.Text = "Background Color:";
        label10.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label10.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _backColorEdit
        _backColorEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.ResumeLayout(false);
        _contentPanel.PerformLayout();
        //
        // _scopeTypeCombo
        //
        _scopeTypeCombo.Name = "_scopeTypeCombo";
        _scopeTypeCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _scopeTypeCombo.EditValueChanged += ScopeTypeCombo_EditValueChanged;
        //
        // _scopeKeyCombo
        //
        _scopeKeyCombo.Name = "_scopeKeyCombo";
        //
        // _overrideFontCheck
        //
        _overrideFontCheck.Name = "_overrideFontCheck";
        _overrideFontCheck.Text = "Override font";
        _overrideFontCheck.CheckedChanged += OverrideFontCheck_CheckedChanged;
        //
        // _fontFamilyCombo
        //
        _fontFamilyCombo.Name = "_fontFamilyCombo";
        //
        // _fontSizeEdit
        //
        _fontSizeEdit.Name = "_fontSizeEdit";
        _fontSizeEdit.Properties.MinValue = 6;
        _fontSizeEdit.Properties.MaxValue = 72;
        _fontSizeEdit.Value = 10;
        //
        // _boldCheck
        //
        _boldCheck.Name = "_boldCheck";
        _boldCheck.Text = "Bold";
        //
        // _italicCheck
        //
        _italicCheck.Name = "_italicCheck";
        _italicCheck.Text = "Italic";
        //
        // _underlineCheck
        //
        _underlineCheck.Name = "_underlineCheck";
        _underlineCheck.Text = "Underline";
        //
        // _strikeoutCheck
        //
        _strikeoutCheck.Name = "_strikeoutCheck";
        _strikeoutCheck.Text = "Strikeout";
        //
        // _overrideForeColorCheck
        //
        _overrideForeColorCheck.Name = "_overrideForeColorCheck";
        _overrideForeColorCheck.Text = "Override text color";
        _overrideForeColorCheck.CheckedChanged += OverrideForeColorCheck_CheckedChanged;
        //
        // _foreColorEdit
        //
        _foreColorEdit.Name = "_foreColorEdit";
        _foreColorEdit.Color = Color.Black;
        //
        // _overrideBackColorCheck
        //
        _overrideBackColorCheck.Name = "_overrideBackColorCheck";
        _overrideBackColorCheck.Text = "Override background color";
        _overrideBackColorCheck.CheckedChanged += OverrideBackColorCheck_CheckedChanged;
        //
        // _backColorEdit
        //
        _backColorEdit.Name = "_backColorEdit";
        _backColorEdit.Color = Color.White;
        //
        // AppearanceRuleEditForm
        //
        ((System.ComponentModel.ISupportInitialize)_scopeTypeCombo.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_scopeKeyCombo.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_overrideFontCheck.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_fontFamilyCombo.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_fontSizeEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_boldCheck.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_italicCheck.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_underlineCheck.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_strikeoutCheck.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_overrideForeColorCheck.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_foreColorEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_overrideBackColorCheck.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_backColorEdit.Properties).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.Label label4;
    private System.Windows.Forms.Label label5;
    private System.Windows.Forms.Label label6;
    private System.Windows.Forms.TableLayoutPanel _stylePanel;
    private System.Windows.Forms.Label label8;
    private System.Windows.Forms.Label label10;
}
