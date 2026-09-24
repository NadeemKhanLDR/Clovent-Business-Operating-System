using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Identity.Users;

/// <summary>
/// PIN entry dialog for User Administration's Set PIN action: masked New
/// PIN + Confirm PIN only. The stored PIN is never displayed - editing a
/// user's PIN always means assigning a new one (or clearing it via the
/// command's null-PIN path, which UsersForm confirms separately before
/// invoking), mirroring how <c>PasswordPromptForm</c> never shows a stored
/// password. Control tree lives in <c>PinPromptForm.Designer.cs</c>; this
/// file holds behavior only.
/// </summary>
public sealed partial class PinPromptForm : MasterDataEditFormBase
{
    /// <summary>Builds the dialog for the Visual Studio Designer - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public PinPromptForm() : this("Set PIN")
    {
    }

    /// <summary>Builds the dialog. <paramref name="title"/> is the dialog's caption, e.g. "Set PIN - cashier1".</summary>
    public PinPromptForm(string title) : base(title)
    {
        InitializeComponent();

        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;

        AddField("New PIN:", _newPinEdit);
        AddField("Confirm PIN:", _confirmPinEdit);
    }

    /// <summary>The entered new PIN (trimmed - PINs never carry whitespace).</summary>
    public string NewPin => _newPinEdit.Text.Trim();

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (string.IsNullOrEmpty(_newPinEdit.Text.Trim()))
        {
            error = "New PIN is required.";
            return false;
        }

        if (_newPinEdit.Text.Trim() != _confirmPinEdit.Text.Trim())
        {
            error = "New PIN and confirmation do not match.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
