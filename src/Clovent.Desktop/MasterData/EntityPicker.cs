using DevExpress.XtraEditors;

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
/// its own, keeping it reusable across every entity kind. Control tree
/// lives in <c>EntityPicker.Designer.cs</c>; this file holds behavior only.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class EntityPicker : DevExpress.XtraEditors.XtraUserControl
{
    private readonly Dictionary<string, Guid> _idsByDisplay = [];

    /// <summary>Raised whenever <see cref="SelectedId"/> settles on a new value (including becoming <see langword="null"/>).</summary>
    public event EventHandler? SelectionChanged;

    /// <summary>The selected entity's id, or <see langword="null"/> if none is selected.</summary>
    public Guid? SelectedId { get; private set; }

    /// <summary>
    /// Builds the picker with the given label text.
    /// </summary>
    /// <param name="labelText">The caption shown to the left of the dropdown.</param>
    /// <param name="comboWidth">The dropdown's width in pixels.</param>
    /// <param name="fontSizePoints">
    /// Overrides the label/dropdown font size (points) - <see langword="null"/>
    /// keeps this skin's default, used by every one of this control's ~30
    /// callers. <c>RestaurantPosView</c>'s Table picker passes a larger value
    /// (see its own field declaration) so the currently selected table stays
    /// legible at arm's length, the same "read at a glance under time
    /// pressure" reasoning already applied to the Current Bill grid's larger
    /// row height/font.
    /// </param>
    /// <param name="labelControlName">
    /// Sets the inner caption's <see cref="Control.Name"/> - <see langword="null"/>
    /// (the default) leaves it unset, used by every caller that doesn't need
    /// to target this specific label from <c>Forms.Base.Appearance.AppearanceManager</c>'s
    /// Control-scope rules (e.g. "RestaurantPosView.TableLabel").
    /// </param>
    public EntityPicker(string labelText, int comboWidth = 260, float? fontSizePoints = null, string? labelControlName = null)
    {
        InitializeComponent();

        _combo.Width = comboWidth;
        _label.Text = labelText;
        _label.Name = labelControlName ?? string.Empty;

        if (fontSizePoints is { } size)
        {
            var font = new Font(_combo.Font.FontFamily, size, FontStyle.Bold);
            _combo.Properties.Appearance.Font = font;
            _combo.Properties.Appearance.Options.UseFont = true;
            _label.Appearance.Font = font;
            _label.Appearance.Options.UseFont = true;
        }

        Size = _layout.Size;
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

    private void Combo_SelectedIndexChanged(object? sender, EventArgs e) => OnSelected();

    private void Layout_SizeChanged(object? sender, EventArgs e) => Size = _layout.Size;

    private void OnSelected()
    {
        SelectedId = _combo.SelectedItem is string display && _idsByDisplay.TryGetValue(display, out var id)
            ? id
            : null;

        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Exposes the inner <see cref="ComboBoxEdit"/> so callers can configure auto-complete and text-edit style without requiring <c>EntityPicker</c> to duplicate every <see cref="ComboBoxEdit.Properties"/> member.</summary>
    public ComboBoxEdit ComboBox => _combo;
}
