using Clovent.Desktop.Forms.Base;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>Preview of a formatted receipt (see <see cref="ReceiptFormatter"/>), with a Print action backed by <see cref="ReceiptPrintDocument"/>. Control tree lives in <c>ReceiptPreviewForm.Designer.cs</c>; this file holds behavior only.</summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class ReceiptPreviewForm : DevExpress.XtraEditors.XtraForm
{
    private const string PlacementKey = nameof(ReceiptPreviewForm);

    private readonly string _receiptText;

    /// <summary>Builds the preview.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public ReceiptPreviewForm()
    {
        _receiptText = null!;

        InitializeComponent();
    }

    /// <summary>Builds the preview for <paramref name="receiptText"/> (the already-formatted receipt body).</summary>
    public ReceiptPreviewForm(string receiptText) : base()
    {
        InitializeComponent();

        _receiptText = receiptText;

        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;

        _textEdit.Text = receiptText;
        WindowPlacementStore.Restore(this, PlacementKey);
    }
    private void ReceiptPreviewForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;
        WindowPlacementStore.Save(this, PlacementKey);
    }

    private void PrintButton_Click(object? sender, EventArgs e) => Print();

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
