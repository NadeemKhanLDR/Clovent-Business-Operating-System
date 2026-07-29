using Clovent.Desktop.MasterData;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Catalog.Categories;

/// <summary>Create/edit dialog for a Product Category - name and an optional parent category (for the "Beverages -&gt; Soft Drinks" hierarchy).</summary>
public sealed class ProductCategoryEditForm : MasterDataEditFormBase
{
    private readonly TextEdit _nameEdit = new();
    private readonly ComboBoxEdit _parentCombo = new();
    private readonly Dictionary<string, Guid?> _parentsByDisplay;

    /// <summary>Builds the dialog. <paramref name="parentOptions"/> should exclude the category being edited, if any, to prevent a self-parent.</summary>
    public ProductCategoryEditForm(string title, IReadOnlyList<(Guid Id, string Display)> parentOptions, string? name = null, Guid? parentCategoryId = null)
        : base(title)
    {
        _nameEdit.Text = name ?? string.Empty;
        _parentsByDisplay = ComboBoxBinder.Bind(_parentCombo, parentOptions, includeEmpty: true);
        ComboBoxBinder.SelectById(_parentCombo, _parentsByDisplay, parentCategoryId);

        AddField("Name:", _nameEdit);
        AddField("Parent Category:", _parentCombo);
    }

    /// <summary>The entered category name.</summary>
    public string NameValue => _nameEdit.Text.Trim();

    /// <summary>The selected parent category, or <see langword="null"/> for a top-level category.</summary>
    public Guid? ParentCategoryId => ComboBoxBinder.GetSelectedId(_parentCombo, _parentsByDisplay);

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
