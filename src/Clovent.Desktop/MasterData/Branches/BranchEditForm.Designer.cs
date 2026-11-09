using DevExpress.XtraEditors;

namespace Clovent.Desktop.MasterData.Branches;

partial class BranchEditForm
{
    /// <summary>Required designer variable.</summary>
    private TableLayoutPanel _branchLayout;
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
        _branchLayout = new TableLayoutPanel();
        _branchLayout.Name = "_branchLayout";
        _branchLayout.Dock = DockStyle.Fill;
        _branchLayout.AutoSize = true;
        _branchLayout.Padding = new Padding(12);
        _branchLayout.ColumnCount = 2;
        _branchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _branchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _branchLayout.RowCount = 6;
        _branchLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _branchLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _branchLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _branchLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _branchLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _branchLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
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
        //
        // label1
        //
        label1.Dock = DockStyle.Left;
        label1.Location = new Point(15, 15);
        label1.Name = "label1";
        label1.Padding = new Padding(0, 4, 0, 4);
        label1.Size = new Size(31, 21);
        label1.TabIndex = 0;
        label1.Text = "Name:";
        //
        // _nameEdit
        //
        _nameEdit.Dock = DockStyle.Top;
        _nameEdit.Location = new Point(82, 15);
        _nameEdit.Name = "_nameEdit";
        _nameEdit.Size = new Size(383, 20);
        _nameEdit.TabIndex = 1;
        //
        // label2
        //
        label2.Dock = DockStyle.Left;
        label2.Location = new Point(15, 42);
        label2.Name = "label2";
        label2.Padding = new Padding(0, 4, 0, 4);
        label2.Size = new Size(34, 21);
        label2.TabIndex = 2;
        label2.Text = "Street:";
        //
        // _streetEdit
        //
        _streetEdit.Dock = DockStyle.Top;
        _streetEdit.Location = new Point(82, 42);
        _streetEdit.Name = "_streetEdit";
        _streetEdit.Size = new Size(383, 20);
        _streetEdit.TabIndex = 3;
        //
        // label3
        //
        label3.Dock = DockStyle.Left;
        label3.Location = new Point(15, 69);
        label3.Name = "label3";
        label3.Padding = new Padding(0, 4, 0, 4);
        label3.Size = new Size(23, 21);
        label3.TabIndex = 4;
        label3.Text = "City:";
        //
        // _cityEdit
        //
        _cityEdit.Dock = DockStyle.Top;
        _cityEdit.Location = new Point(82, 69);
        _cityEdit.Name = "_cityEdit";
        _cityEdit.Size = new Size(383, 20);
        _cityEdit.TabIndex = 5;
        //
        // label4
        //
        label4.Dock = DockStyle.Left;
        label4.Location = new Point(15, 96);
        label4.Name = "label4";
        label4.Padding = new Padding(0, 4, 0, 4);
        label4.Size = new Size(30, 21);
        label4.TabIndex = 6;
        label4.Text = "State:";
        //
        // _stateEdit
        //
        _stateEdit.Dock = DockStyle.Top;
        _stateEdit.Location = new Point(82, 96);
        _stateEdit.Name = "_stateEdit";
        _stateEdit.Size = new Size(383, 20);
        _stateEdit.TabIndex = 7;
        //
        // label5
        //
        label5.Dock = DockStyle.Left;
        label5.Location = new Point(15, 123);
        label5.Name = "label5";
        label5.Padding = new Padding(0, 4, 0, 4);
        label5.Size = new Size(61, 21);
        label5.TabIndex = 8;
        label5.Text = "Postal Code:";
        //
        // _postalCodeEdit
        //
        _postalCodeEdit.Dock = DockStyle.Top;
        _postalCodeEdit.Location = new Point(82, 123);
        _postalCodeEdit.Name = "_postalCodeEdit";
        _postalCodeEdit.Size = new Size(383, 20);
        _postalCodeEdit.TabIndex = 9;
        //
        // label6
        //
        label6.Dock = DockStyle.Left;
        label6.Location = new Point(15, 150);
        label6.Name = "label6";
        label6.Padding = new Padding(0, 4, 0, 4);
        label6.Size = new Size(43, 21);
        label6.TabIndex = 10;
        label6.Text = "Country:";
        //
        // _countryEdit
        //
        _countryEdit.Dock = DockStyle.Top;
        _countryEdit.Location = new Point(82, 150);
        _countryEdit.Name = "_countryEdit";
        _countryEdit.Size = new Size(383, 20);
        _countryEdit.TabIndex = 11;
        //
        // BranchEditForm
        //
        ClientSize = new Size(480, 360);
        Name = "BranchEditForm";
        ((System.ComponentModel.ISupportInitialize)_nameEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_streetEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_cityEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_stateEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_postalCodeEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_countryEdit.Properties).EndInit();
        _branchLayout.Controls.Add(label1, 0, 0);
        _branchLayout.Controls.Add(_nameEdit, 1, 0);
        _branchLayout.Controls.Add(label2, 0, 1);
        _branchLayout.Controls.Add(_streetEdit, 1, 1);
        _branchLayout.Controls.Add(label3, 0, 2);
        _branchLayout.Controls.Add(_cityEdit, 1, 2);
        _branchLayout.Controls.Add(label4, 0, 3);
        _branchLayout.Controls.Add(_stateEdit, 1, 3);
        _branchLayout.Controls.Add(label5, 0, 4);
        _branchLayout.Controls.Add(_postalCodeEdit, 1, 4);
        _branchLayout.Controls.Add(label6, 0, 5);
        _branchLayout.Controls.Add(_countryEdit, 1, 5);
        Controls.Add(_branchLayout);
        Controls.SetChildIndex(_branchLayout, 0);
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
