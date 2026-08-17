using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Restaurant.Shared;

/// <summary>
/// Single-field free-text prompt shared by every Restaurant POS action that
/// needs nothing more than a line (or few) of text - order notes, customer
/// notes, item notes, and void/cancel reasons. Control tree lives in
/// <c>TextPromptForm.Designer.cs</c>; this file holds behavior only.
/// </summary>
public sealed partial class TextPromptForm : MasterDataEditFormBase
{
    private readonly bool _required;

    /// <summary>Designer-only constructor.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public TextPromptForm() : base("Prompt")
    {
        InitializeComponent();
    }

    /// <summary>Builds the dialog. Set <paramref name="required"/> for reasons (void/cancel); leave <see langword="false"/> for optional notes fields.</summary>
    public TextPromptForm(string title, string label, string? initialText = null, bool required = false) : base(title)
    {
        InitializeComponent();
        label1.Text = label;

        _required = required;
        _textEdit.Text = initialText ?? string.Empty;
        _textEdit.Dock = System.Windows.Forms.DockStyle.Fill;

        var okControls = this.Controls.Find("_okButton", true);
        if (okControls.Length > 0 && okControls[0] is DevExpress.XtraEditors.SimpleButton okBtn)
        {
            okBtn.Text = "Confirm";
        }

        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MinimizeBox = false;
        this.MaximizeBox = false;
        this.ClientSize = new System.Drawing.Size(420, 170);
        this.MaximumSize = new System.Drawing.Size(460, 220);
    }

    /// <summary>The entered text, trimmed, or <see langword="null"/> if blank.</summary>
    public string? Value => string.IsNullOrWhiteSpace(_textEdit.Text) ? null : _textEdit.Text.Trim();

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (_required && Value is null)
        {
            error = "This field is required.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
