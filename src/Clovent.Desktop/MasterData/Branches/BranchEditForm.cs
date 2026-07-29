using Clovent.Desktop.MasterData;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.MasterData.Branches;

/// <summary>Create/edit dialog for a Branch - name plus an optional address.</summary>
public sealed class BranchEditForm : MasterDataEditFormBase
{
    private readonly TextEdit _nameEdit = new();
    private readonly TextEdit _streetEdit = new();
    private readonly TextEdit _cityEdit = new();
    private readonly TextEdit _stateEdit = new();
    private readonly TextEdit _postalCodeEdit = new();
    private readonly TextEdit _countryEdit = new();

    /// <summary>Builds the dialog, pre-filled when editing an existing branch.</summary>
    public BranchEditForm(
        string title,
        string? name = null,
        string? street = null,
        string? city = null,
        string? state = null,
        string? postalCode = null,
        string? country = null) : base(title)
    {
        _nameEdit.Text = name ?? string.Empty;
        _streetEdit.Text = street ?? string.Empty;
        _cityEdit.Text = city ?? string.Empty;
        _stateEdit.Text = state ?? string.Empty;
        _postalCodeEdit.Text = postalCode ?? string.Empty;
        _countryEdit.Text = country ?? string.Empty;

        AddField("Name:", _nameEdit);
        AddField("Street:", _streetEdit);
        AddField("City:", _cityEdit);
        AddField("State:", _stateEdit);
        AddField("Postal Code:", _postalCodeEdit);
        AddField("Country:", _countryEdit);
    }

    /// <summary>The entered branch name.</summary>
    public string BranchNameValue => _nameEdit.Text.Trim();

    /// <summary>The entered street, or <see langword="null"/> if left blank.</summary>
    public string? Street => Blank(_streetEdit.Text);

    /// <summary>The entered city, or <see langword="null"/> if left blank.</summary>
    public string? City => Blank(_cityEdit.Text);

    /// <summary>The entered state, or <see langword="null"/> if left blank.</summary>
    public string? State => Blank(_stateEdit.Text);

    /// <summary>The entered postal code, or <see langword="null"/> if left blank.</summary>
    public string? PostalCode => Blank(_postalCodeEdit.Text);

    /// <summary>The entered country, or <see langword="null"/> if left blank.</summary>
    public string? Country => Blank(_countryEdit.Text);

    private static string? Blank(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (string.IsNullOrWhiteSpace(_nameEdit.Text))
        {
            error = "Name is required.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
