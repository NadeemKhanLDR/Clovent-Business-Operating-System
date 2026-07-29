using Clovent.Desktop.MasterData;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Restaurant.Shared;

/// <summary>
/// Single-combo selection prompt shared by every Restaurant POS action that
/// needs to pick one existing record from a short list - which discount or
/// service charge to remove from an order, for instance.
/// </summary>
public sealed class SelectionPromptForm : MasterDataEditFormBase
{
    private readonly ComboBoxEdit _combo = new();
    private readonly Dictionary<string, Guid?> _itemsByDisplay;

    /// <summary>Builds the dialog.</summary>
    public SelectionPromptForm(string title, string label, IReadOnlyList<(Guid Id, string Display)> options) : base(title)
    {
        _itemsByDisplay = ComboBoxBinder.Bind(_combo, options, includeEmpty: false);

        AddField(label, _combo);
    }

    /// <summary>The selected item's id.</summary>
    public Guid? SelectedId => ComboBoxBinder.GetSelectedId(_combo, _itemsByDisplay);

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (SelectedId is null)
        {
            error = "Select an item.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
