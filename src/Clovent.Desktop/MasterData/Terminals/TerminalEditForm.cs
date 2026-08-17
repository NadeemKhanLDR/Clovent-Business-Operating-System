using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.MasterData.Terminals;

/// <summary>
/// Create/edit dialog for a Terminal - name and, for new terminals only, a
/// code (immutable after creation).
/// </summary>
public sealed partial class TerminalEditForm : MasterDataEditFormBase
{
    /// <summary>Design-time-only constructor for the Visual Studio WinForms Designer - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public TerminalEditForm() : base("Edit Terminal")
    {
        InitializeComponent();
        }

    /// <summary>
    /// Builds the dialog. <paramref name="title"/> is the dialog's caption;
    /// <paramref name="name"/> pre-populates the display name. Pass
    /// <paramref name="code"/> when editing so the (disabled) field still
    /// shows it - the field is only enabled when <paramref name="isNew"/> is
    /// <see langword="true"/>, since the code is immutable after creation.
    /// </summary>
    public TerminalEditForm(string title, string? name = null, string? code = null, bool isNew = true) : base(title)
    {
        InitializeComponent();
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;

        _nameEdit.Text = name ?? string.Empty;
        _codeEdit.Text = code ?? string.Empty;
        _codeEdit.Enabled = isNew;
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
