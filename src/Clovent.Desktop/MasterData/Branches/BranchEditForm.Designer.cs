using DevExpress.XtraEditors;

namespace Clovent.Desktop.MasterData.Branches;

partial class BranchEditForm
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

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
        label1 = new LabelControl();
        _nameEdit = new TextEdit();
        label2 = new LabelControl();
        _streetEdit = new TextEdit();
        label3 = new LabelControl();
        _cityEdit = new TextEdit();
        label4 = new LabelControl();
        _stateEdit = new TextEdit();
        label5 = new LabelControl();
        _postalCodeEdit = new TextEdit();
        label6 = new LabelControl();
        _countryEdit = new TextEdit();
        ((System.ComponentModel.ISupportInitialize)_nameEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_streetEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_cityEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_stateEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_postalCodeEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_countryEdit.Properties).BeginInit();
        SuspendLayout();
        _contentPanel.SuspendLayout();
        _contentPanel.RowCount = 6;
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.Controls.Add(label1, 0, 0);
        _contentPanel.Controls.Add(_nameEdit, 1, 0);
        _contentPanel.Controls.Add(label2, 0, 1);
        _contentPanel.Controls.Add(_streetEdit, 1, 1);
        _contentPanel.Controls.Add(label3, 0, 2);
        _contentPanel.Controls.Add(_cityEdit, 1, 2);
        _contentPanel.Controls.Add(label4, 0, 3);
        _contentPanel.Controls.Add(_stateEdit, 1, 3);
        _contentPanel.Controls.Add(label5, 0, 4);
        _contentPanel.Controls.Add(_postalCodeEdit, 1, 4);
        _contentPanel.Controls.Add(label6, 0, 5);
        _contentPanel.Controls.Add(_countryEdit, 1, 5);
        //
        // labels
        //
        label1.Text = "Name:";
        label2.Text = "Street:";
        label3.Text = "City:";
        label4.Text = "State:";
        label5.Text = "Postal Code:";
        label6.Text = "Country:";
        label1.AutoSize = true;
        label1.Dock = System.Windows.Forms.DockStyle.Left;
        label1.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        label2.AutoSize = true;
        label2.Dock = System.Windows.Forms.DockStyle.Left;
        label2.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        label3.AutoSize = true;
        label3.Dock = System.Windows.Forms.DockStyle.Left;
        label3.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        label4.AutoSize = true;
        label4.Dock = System.Windows.Forms.DockStyle.Left;
        label4.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        label5.AutoSize = true;
        label5.Dock = System.Windows.Forms.DockStyle.Left;
        label5.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        label6.AutoSize = true;
        label6.Dock = System.Windows.Forms.DockStyle.Left;
        label6.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        //
        // editors
        //
        _nameEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _nameEdit.Width = 260;
        _streetEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _streetEdit.Width = 260;
        _cityEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _cityEdit.Width = 260;
        _stateEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _stateEdit.Width = 260;
        _postalCodeEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _postalCodeEdit.Width = 260;
        _countryEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _countryEdit.Width = 260;
        _contentPanel.ResumeLayout(false);
        _contentPanel.PerformLayout();
        //
        // BranchEditForm
        //
        ClientSize = new System.Drawing.Size(480, 360);
        Name = "BranchEditForm";
        ((System.ComponentModel.ISupportInitialize)_nameEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_streetEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_cityEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_stateEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_postalCodeEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_countryEdit.Properties).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private TextEdit _nameEdit;
    private TextEdit _streetEdit;
    private TextEdit _cityEdit;
    private TextEdit _stateEdit;
    private TextEdit _postalCodeEdit;
    private TextEdit _countryEdit;

    private LabelControl label1;
    private LabelControl label2;
    private LabelControl label3;
    private LabelControl label4;
    private LabelControl label5;
    private LabelControl label6;
}
