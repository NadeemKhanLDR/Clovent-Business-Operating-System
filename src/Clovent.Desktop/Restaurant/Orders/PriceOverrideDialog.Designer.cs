using DevExpress.XtraEditors;

namespace Clovent.Desktop.Restaurant.Orders;

partial class PriceOverrideDialog
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
        label2 = new System.Windows.Forms.Label();
        label3 = new System.Windows.Forms.Label();
        _currentPriceLabel = new LabelControl();
        _newPriceEdit = new SpinEdit();
        _reasonEdit = new MemoEdit();
        ((System.ComponentModel.ISupportInitialize)_newPriceEdit.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_reasonEdit.Properties).BeginInit();
        SuspendLayout();
        _contentPanel.SuspendLayout();
        _contentPanel.RowCount = 3;
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
        _contentPanel.Controls.Add(_currentPriceLabel, 0, 0);
        _contentPanel.SetColumnSpan(_currentPriceLabel, 2);
        // _currentPriceLabel
        _currentPriceLabel.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label2, 0, 1);
        _contentPanel.Controls.Add(_newPriceEdit, 1, 1);
        // label2
        label2.AutoSize = true;
        label2.Dock = System.Windows.Forms.DockStyle.Left;
        label2.Text = "New Price:";
        label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label2.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _newPriceEdit
        _newPriceEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.Controls.Add(label3, 0, 2);
        _contentPanel.Controls.Add(_reasonEdit, 1, 2);
        // label3
        label3.AutoSize = true;
        label3.Dock = System.Windows.Forms.DockStyle.Left;
        label3.Text = "Reason:";
        label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        label3.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        // _reasonEdit
        _reasonEdit.Dock = System.Windows.Forms.DockStyle.Top;
        _contentPanel.ResumeLayout(false);
        _contentPanel.PerformLayout();
        //
        // _currentPriceLabel
        //
        _currentPriceLabel.Name = "_currentPriceLabel";
        _currentPriceLabel.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _currentPriceLabel.Appearance.Options.UseFont = true;
        //
        // _newPriceEdit
        //
        _newPriceEdit.Name = "_newPriceEdit";
        _newPriceEdit.Properties.MinValue = 0;
        _newPriceEdit.Properties.MaxValue = 1_000_000;
        _newPriceEdit.Properties.Increment = 0.01m;
        //
        // _reasonEdit
        //
        _reasonEdit.Name = "_reasonEdit";
        _reasonEdit.Height = 60;
        //
        // PriceOverrideDialog
        //



        ((System.ComponentModel.ISupportInitialize)_newPriceEdit.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_reasonEdit.Properties).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private LabelControl _currentPriceLabel;
    private SpinEdit _newPriceEdit;
    private MemoEdit _reasonEdit;

    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.Label label3;
}
