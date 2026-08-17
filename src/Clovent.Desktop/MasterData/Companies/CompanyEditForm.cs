using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.MasterData.Companies;

/// <summary>Create/edit dialog for a Company - name and an optional tax id.</summary>
public sealed partial class CompanyEditForm : MasterDataEditFormBase
{
    /// <summary>Builds the dialog, pre-filled when editing an existing company.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public CompanyEditForm() : base("Edit Company")
    {
        InitializeComponent();
        }

    /// <summary>Builds the dialog. <paramref name="title"/> is the dialog's caption; <paramref name="name"/> and <paramref name="taxId"/> pre-populate the fields when editing an existing company.</summary>
    public CompanyEditForm(string title, string? name = null, string? taxId = null) : base(title)
    {
        InitializeComponent();
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;

        _nameEdit.Text = name ?? string.Empty;
        _taxIdEdit.Text = taxId ?? string.Empty;
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
