using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.MasterData.Organizations;

/// <summary>Create/edit dialog for an Organization - name and an optional tax id.</summary>
public sealed partial class OrganizationEditForm : MasterDataEditFormBase
{
    /// <summary>Design-time-only constructor for the Visual Studio WinForms Designer - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public OrganizationEditForm() : base("Edit Organization")
    {
        InitializeComponent();
        }

    /// <summary>Builds the dialog. <paramref name="title"/> is the dialog's caption; pre-filled with <paramref name="name"/>/<paramref name="taxId"/> when editing an existing organization.</summary>
    public OrganizationEditForm(string title, string? name = null, string? taxId = null) : base(title)
    {
        InitializeComponent();
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;

        _nameEdit.Text = name ?? string.Empty;
        _taxIdEdit.Text = taxId ?? string.Empty;
    }



    /// <summary>The entered organization name.</summary>
    public string OrganizationName => _nameEdit.Text.Trim();

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
