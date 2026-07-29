using Clovent.Desktop.MasterData;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.MasterData.Terminals;

/// <summary>
/// Create/edit dialog for a Terminal - name and, for new terminals only, a
/// code (immutable after creation).
/// </summary>
public sealed class TerminalEditForm : MasterDataEditFormBase
{
    private readonly TextEdit _nameEdit = new();
    private readonly TextEdit _codeEdit = new();

    /// <summary>Builds the dialog. Pass <paramref name="code"/> when editing so the (disabled) field still shows it.</summary>
    public TerminalEditForm(string title, string? name = null, string? code = null, bool isNew = true) : base(title)
    {
        _nameEdit.Text = name ?? string.Empty;
        _codeEdit.Text = code ?? string.Empty;
        _codeEdit.Enabled = isNew;

        AddField("Name:", _nameEdit);
        AddField("Code:", _codeEdit);
    }

    /// <summary>The entered terminal name.</summary>
    public string TerminalNameValue => _nameEdit.Text.Trim();

    /// <summary>The entered terminal code (only meaningful when creating).</summary>
    public string CodeValue => _codeEdit.Text.Trim();

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (string.IsNullOrWhiteSpace(_nameEdit.Text))
        {
            error = "Name is required.";
            return false;
        }

        if (_codeEdit.Enabled && string.IsNullOrWhiteSpace(_codeEdit.Text))
        {
            error = "Code is required.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
