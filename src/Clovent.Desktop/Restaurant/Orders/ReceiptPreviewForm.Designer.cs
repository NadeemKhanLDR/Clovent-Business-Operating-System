using DevExpress.XtraEditors;

namespace Clovent.Desktop.Restaurant.Orders;

partial class ReceiptPreviewForm
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    private readonly PrintDialog _printDialog = new();
    private MemoEdit _textEdit;
    private FlowLayoutPanel _buttonPanel;
    private SimpleButton _closeButton;
    private SimpleButton _printButton;

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
        _textEdit = new MemoEdit();
        _buttonPanel = new FlowLayoutPanel();
        _closeButton = new SimpleButton();
        _printButton = new SimpleButton();
        ((System.ComponentModel.ISupportInitialize)_textEdit.Properties).BeginInit();
        _buttonPanel.SuspendLayout();
        SuspendLayout();
        // 
        // _textEdit
        // 
        _textEdit.Dock = DockStyle.Fill;
        _textEdit.Location = new Point(0, 0);
        _textEdit.Margin = new Padding(8, 8, 8, 8);
        _textEdit.Name = "_textEdit";
        _textEdit.Properties.Appearance.Font = new Font("Courier New", 9F);
        _textEdit.Properties.Appearance.Options.UseFont = true;
        _textEdit.Properties.ReadOnly = true;
        _textEdit.Properties.WordWrap = false;
        _textEdit.Size = new Size(416, 433);
        _textEdit.TabIndex = 0;
        // 
        // _buttonPanel
        // 
        _buttonPanel.Controls.Add(_closeButton);
        _buttonPanel.Controls.Add(_printButton);
        _buttonPanel.Dock = DockStyle.Bottom;
        _buttonPanel.FlowDirection = FlowDirection.RightToLeft;
        _buttonPanel.Location = new Point(0, 433);
        _buttonPanel.Name = "_buttonPanel";
        _buttonPanel.Size = new Size(416, 48);
        _buttonPanel.TabIndex = 1;
        // 
        // _closeButton
        // 
        _closeButton.DialogResult = DialogResult.OK;
        _closeButton.Location = new Point(226, 3);
        _closeButton.Name = "_closeButton";
        _closeButton.Size = new Size(187, 57);
        _closeButton.TabIndex = 0;
        _closeButton.Text = "Close";
        // 
        // _printButton
        // 
        _printButton.Location = new Point(33, 3);
        _printButton.Name = "_printButton";
        _printButton.Size = new Size(187, 57);
        _printButton.TabIndex = 1;
        _printButton.Text = "Print";
        _printButton.Click += PrintButton_Click;
        // 
        // ReceiptPreviewForm
        // 
        AcceptButton = _closeButton;
        ClientSize = new Size(416, 481);
        Controls.Add(_textEdit);
        Controls.Add(_buttonPanel);
        MinimizeBox = false;
        MinimumSize = new Size(360, 420);
        Name = "ReceiptPreviewForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Receipt Preview";
        FormClosed += ReceiptPreviewForm_FormClosed;
        ((System.ComponentModel.ISupportInitialize)_textEdit.Properties).EndInit();
        _buttonPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion
}
