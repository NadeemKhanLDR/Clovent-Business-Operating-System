namespace Clovent.Desktop.Startup;

partial class ErrorDialogForm
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
        _messageLabel = new DevExpress.XtraEditors.LabelControl();
        _detailsMemo = new DevExpress.XtraEditors.MemoEdit();
        _buttonPanel = new DevExpress.XtraEditors.PanelControl();
        _detailsToggle = new DevExpress.XtraEditors.SimpleButton();
        _copyButton = new DevExpress.XtraEditors.SimpleButton();
        _closeButton = new DevExpress.XtraEditors.SimpleButton();
        ((System.ComponentModel.ISupportInitialize)_detailsMemo.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_buttonPanel).BeginInit();
        _buttonPanel.SuspendLayout();
        SuspendLayout();
        //
        // _messageLabel
        //
        _messageLabel.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical;
        _messageLabel.Dock = DockStyle.Top;
        _messageLabel.Name = "_messageLabel";
        _messageLabel.Padding = new Padding(16, 16, 16, 8);
        //
        // _detailsMemo
        //
        _detailsMemo.Dock = DockStyle.Fill;
        _detailsMemo.Name = "_detailsMemo";
        _detailsMemo.Properties.ReadOnly = true;
        _detailsMemo.Properties.ScrollBars = ScrollBars.Vertical;
        _detailsMemo.Visible = false;
        //
        // _buttonPanel
        //
        _buttonPanel.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
        _buttonPanel.Controls.Add(_closeButton);
        _buttonPanel.Controls.Add(_copyButton);
        _buttonPanel.Controls.Add(_detailsToggle);
        _buttonPanel.Dock = DockStyle.Bottom;
        _buttonPanel.Height = 44;
        _buttonPanel.Name = "_buttonPanel";
        //
        // _detailsToggle
        //
        _detailsToggle.Dock = DockStyle.Left;
        _detailsToggle.Name = "_detailsToggle";
        _detailsToggle.Text = "Show Details";
        _detailsToggle.Width = 120;
        _detailsToggle.Click += DetailsToggle_Click;
        //
        // _copyButton
        //
        _copyButton.Dock = DockStyle.Left;
        _copyButton.Name = "_copyButton";
        _copyButton.Text = "Copy Details";
        _copyButton.Width = 120;
        _copyButton.Click += CopyButton_Click;
        //
        // _closeButton
        //
        _closeButton.Dock = DockStyle.Right;
        _closeButton.DialogResult = DialogResult.OK;
        _closeButton.Name = "_closeButton";
        _closeButton.Text = "Close";
        _closeButton.Width = 100;
        _closeButton.Click += CloseButton_Click;
        //
        // ErrorDialogForm
        //
        Text = "An error occurred";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 560;
        Height = 200;
        MinimumSize = new Size(420, 160);
        MaximizeBox = false;
        MinimizeBox = false;
        Controls.Add(_detailsMemo);
        Controls.Add(_buttonPanel);
        Controls.Add(_messageLabel);
        AcceptButton = _closeButton;
        CancelButton = _closeButton;
        ((System.ComponentModel.ISupportInitialize)_detailsMemo.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_buttonPanel).EndInit();
        _buttonPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private DevExpress.XtraEditors.LabelControl _messageLabel;
    private DevExpress.XtraEditors.MemoEdit _detailsMemo;
    private DevExpress.XtraEditors.PanelControl _buttonPanel;
    private DevExpress.XtraEditors.SimpleButton _detailsToggle;
    private DevExpress.XtraEditors.SimpleButton _copyButton;
    private DevExpress.XtraEditors.SimpleButton _closeButton;
}
