using DevExpress.XtraEditors;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>Preview of a formatted receipt (see <see cref="ReceiptFormatter"/>), with a Print action backed by <see cref="ReceiptPrintDocument"/>.</summary>
public sealed class ReceiptPreviewForm : XtraForm
{
    private readonly string _receiptText;
    private readonly PrintDialog _printDialog = new();

    /// <summary>Builds the preview.</summary>
    public ReceiptPreviewForm(string receiptText)
    {
        _receiptText = receiptText;

        Text = "Receipt Preview";
        Width = 420;
        Height = 560;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;

        var textEdit = new MemoEdit
        {
            Dock = DockStyle.Fill,
            Text = receiptText,
            Properties = { ReadOnly = true, WordWrap = false },
            Font = new Font(FontFamily.GenericMonospace, 9f),
        };

        var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, FlowDirection = FlowDirection.RightToLeft };
        var closeButton = new SimpleButton { Text = "Close", DialogResult = DialogResult.OK };
        var printButton = new SimpleButton { Text = "Print" };
        printButton.Click += (_, _) => Print();
        buttonPanel.Controls.Add(closeButton);
        buttonPanel.Controls.Add(printButton);

        Controls.Add(textEdit);
        Controls.Add(buttonPanel);
        AcceptButton = closeButton;
    }

    private void Print()
    {
        using var document = new ReceiptPrintDocument(_receiptText);
        _printDialog.Document = document;

        if (_printDialog.ShowDialog(this) == DialogResult.OK)
        {
            document.Print();
        }
    }
}
