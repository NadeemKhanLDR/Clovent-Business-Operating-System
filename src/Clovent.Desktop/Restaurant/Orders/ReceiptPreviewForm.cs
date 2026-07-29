using DevExpress.XtraEditors;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>Read-only preview of a formatted receipt (see <see cref="ReceiptFormatter"/>), shown before printing.</summary>
public sealed class ReceiptPreviewForm : XtraForm
{
    /// <summary>Builds the preview.</summary>
    public ReceiptPreviewForm(string receiptText)
    {
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

        var closeButton = new SimpleButton { Text = "Close", Dock = DockStyle.Bottom, DialogResult = DialogResult.OK };

        Controls.Add(textEdit);
        Controls.Add(closeButton);
        AcceptButton = closeButton;
    }
}
