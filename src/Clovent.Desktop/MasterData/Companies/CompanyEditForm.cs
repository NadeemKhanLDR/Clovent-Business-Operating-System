using Clovent.Desktop.MasterData;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.MasterData.Companies;

/// <summary>Create/edit dialog for a Company - name and an optional tax id.</summary>
public sealed class CompanyEditForm : MasterDataEditFormBase
{
    private readonly TextEdit _nameEdit = new();
    private readonly TextEdit _taxIdEdit = new();

    /// <summary>Builds the dialog, pre-filled when editing an existing company.</summary>
    public CompanyEditForm(string title, string? name = null, string? taxId = null) : base(title)
    {
        _nameEdit.Text = name ?? string.Empty;
        _taxIdEdit.Text = taxId ?? string.Empty;

        AddField("Name:", _nameEdit);
        AddField("Tax Id:", _taxIdEdit);
    }

    /// <summary>The entered company name.</summary>
    public string CompanyNameValue => _nameEdit.Text.Trim();

    /// <summary>The entered tax id, or <see langword="null"/> if left blank.</summary>
    public string? TaxId => string.IsNullOrWhiteSpace(_taxIdEdit.Text) ? null : _taxIdEdit.Text.Trim();

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
