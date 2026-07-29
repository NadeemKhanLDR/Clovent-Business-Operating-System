using Clovent.Desktop.MasterData;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Restaurant.Shared;

/// <summary>
/// Single-field free-text prompt shared by every Restaurant POS action that
/// needs nothing more than a line (or few) of text - order notes, customer
/// notes, item notes, and void/cancel reasons.
/// </summary>
public sealed class TextPromptForm : MasterDataEditFormBase
{
    private readonly MemoEdit _textEdit = new() { Height = 80 };
    private readonly bool _required;

    /// <summary>Builds the dialog. Set <paramref name="required"/> for reasons (void/cancel); leave <see langword="false"/> for optional notes fields.</summary>
    public TextPromptForm(string title, string label, string? initialText = null, bool required = false) : base(title)
    {
        _required = required;
        _textEdit.Text = initialText ?? string.Empty;

        AddField(label, _textEdit);
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
