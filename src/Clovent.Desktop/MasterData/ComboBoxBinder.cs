using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;

namespace Clovent.Desktop.MasterData;

/// <summary>
/// Binds a plain <see cref="ComboBoxEdit"/> (used inside an entity-specific
/// <see cref="MasterDataEditFormBase"/> field, alongside its own
/// <c>AddField</c> label) to a set of (id, display) pairs - the edit-dialog
/// counterpart to <see cref="EntityPicker"/>'s list-scoping combo. Added for
/// Milestone 14 ("Product Catalog &amp; Inventory Foundation") create/edit
/// dialogs with a foreign-key field (Product's Category/Group/Brand/base
/// Unit, Variant's Unit, Price's Currency, WarehouseStock/StockAdjustment/StockTransfer's
/// Warehouse/Variant pickers).
/// </summary>
public static class ComboBoxBinder
{
    private const string EmptyDisplay = "(none)";

    /// <summary>Populates <paramref name="combo"/> from <paramref name="items"/>, returning the display-to-id map <see cref="GetSelectedId"/> and <see cref="SelectById"/> need.</summary>
    /// <param name="combo">The combo box to populate.</param>
    /// <param name="items">The (id, display) pairs to populate it with.</param>
    /// <param name="includeEmpty">Whether to add a leading "(none)" option mapping to <see langword="null"/> - for optional foreign keys.</param>
    public static Dictionary<string, Guid?> Bind(ComboBoxEdit combo, IReadOnlyList<(Guid Id, string Display)> items, bool includeEmpty = false)
    {
        combo.Properties.TextEditStyle = TextEditStyles.DisableTextEditor;

        var map = new Dictionary<string, Guid?>();
        combo.Properties.Items.Clear();

        if (includeEmpty)
        {
            map[EmptyDisplay] = null;
            combo.Properties.Items.Add(EmptyDisplay);
        }

        foreach (var (id, display) in items)
        {
            map[display] = id;
            combo.Properties.Items.Add(display);
        }

        if (combo.Properties.Items.Count > 0)
        {
            combo.SelectedIndex = 0;
        }

        return map;
    }

    /// <summary>Reads back the currently-selected id from a combo <see cref="Bind"/> populated.</summary>
    public static Guid? GetSelectedId(ComboBoxEdit combo, IReadOnlyDictionary<string, Guid?> map) =>
        combo.SelectedItem is string display && map.TryGetValue(display, out var id) ? id : null;

    /// <summary>Selects the option matching <paramref name="id"/>, if present - for pre-populating an edit dialog. A no-op if <paramref name="id"/> is <see langword="null"/> or not found.</summary>
    public static void SelectById(ComboBoxEdit combo, IReadOnlyDictionary<string, Guid?> map, Guid? id)
    {
        if (id is null)
        {
            return;
        }

        foreach (var (display, mappedId) in map)
        {
            if (mappedId == id)
            {
                combo.SelectedItem = display;
                return;
            }
        }
    }
}
