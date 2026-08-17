using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Catalog.Brands;

/// <summary>
/// Create/edit dialog for a Brand - just a name. Control tree (fields,
/// <c>AddField</c> calls) lives in <c>BrandEditForm.Designer.cs</c>; this
/// file holds behavior only.
/// </summary>
public sealed partial class BrandEditForm : MasterDataEditFormBase
{
    /// <summary>Builds the dialog.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public BrandEditForm() : base("Edit Brand")
    {
        InitializeComponent();
        }

    /// <summary>Builds the dialog. <paramref name="title"/> is the dialog's caption; <paramref name="name"/> pre-populates the name field when editing an existing brand.</summary>
    public BrandEditForm(string title, string? name = null) : base(title)
    {
        InitializeComponent();
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;

        _nameEdit.Text = name ?? string.Empty;
    }

    /// <summary>The entered brand name.</summary>
    public string NameValue => _nameEdit.Text.Trim();

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
