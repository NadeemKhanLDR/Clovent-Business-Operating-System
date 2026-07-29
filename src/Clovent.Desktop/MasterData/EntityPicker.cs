using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;

namespace Clovent.Desktop.MasterData;

/// <summary>
/// A single labelled dropdown scoping a list view to one parent entity -
/// the flat-list counterpart to <see cref="OrganizationHierarchySelector"/>'s
/// cascading levels, added for Milestone 14 ("Product Catalog &amp;
/// Inventory Foundation") screens scoped by a single parent rather than an
/// Organization/Company/Branch chain (Variant management scoped by
/// Product, Barcode/Price/WarehouseStock/StockAdjustment/InventoryTransactions
/// scoped by ProductVariant or Warehouse). The caller supplies the
/// (id, display) pairs directly - this control has no query knowledge of
/// its own, keeping it reusable across every entity kind.
/// </summary>
public sealed class EntityPicker : XtraUserControl
{
    private readonly ComboBoxEdit _combo = new();
    private readonly Dictionary<string, Guid> _idsByDisplay = [];

    /// <summary>Raised whenever <see cref="SelectedId"/> settles on a new value (including becoming <see langword="null"/>).</summary>
    public event EventHandler? SelectionChanged;

    /// <summary>The selected entity's id, or <see langword="null"/> if none is selected.</summary>
    public Guid? SelectedId { get; private set; }

    /// <summary>Builds the picker with the given label text.</summary>
    public EntityPicker(string labelText, int comboWidth = 260)
    {
        Dock = DockStyle.Top;
        Height = 32;

        _combo.Width = comboWidth;
        _combo.Properties.TextEditStyle = TextEditStyles.DisableTextEditor;

        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        layout.Controls.Add(new LabelControl { Text = labelText, Padding = new Padding(0, 6, 4, 0) });
        layout.Controls.Add(_combo);
        Controls.Add(layout);

        _combo.SelectedIndexChanged += (_, _) => OnSelected();
    }

    /// <summary>Replaces the picker's options. Selects the first item, if any, or clears the selection otherwise.</summary>
    public void LoadItems(IReadOnlyList<(Guid Id, string Display)> items)
    {
        _idsByDisplay.Clear();
        _combo.Properties.Items.Clear();

        foreach (var (id, display) in items)
        {
            _idsByDisplay[display] = id;
            _combo.Properties.Items.Add(display);
        }

        if (_combo.Properties.Items.Count > 0)
        {
            _combo.SelectedIndex = 0;
        }
        else
        {
            OnSelected();
        }
    }

    /// <summary>
    /// Selects the option matching <paramref name="id"/>, if present, without
    /// reloading the item list - used by screens that reload their options
    /// after every state change (e.g. a table's occupancy) but want the
    /// user's current selection preserved across that reload, unlike
    /// <see cref="LoadItems"/>'s always-select-first behavior. A no-op if
    /// <paramref name="id"/> is <see langword="null"/> or not present.
    /// </summary>
    public void SelectId(Guid? id)
    {
        if (id is null)
        {
            return;
        }

        foreach (var (display, itemId) in _idsByDisplay)
        {
            if (itemId == id)
            {
                _combo.SelectedItem = display;
                return;
            }
        }
    }

    private void OnSelected()
    {
        SelectedId = _combo.SelectedItem is string display && _idsByDisplay.TryGetValue(display, out var id)
            ? id
            : null;

        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }
}
