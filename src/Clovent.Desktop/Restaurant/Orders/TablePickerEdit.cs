using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>
/// Dedicated Table Selection dropdown control for Restaurant POS.
/// Displays "[ Table: T-03 ▼ ]" or "[ Table: None ▼ ]" when closed,
/// and provides a clean list of tables with occupancy status when opened.
/// </summary>
public sealed class TablePickerEdit : ComboBoxEdit
{
    private readonly Dictionary<string, Guid> _idsByDisplay = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Guid, string> _displayById = [];
    private bool _suppressSelectionEvent;

    /// <summary>Raised whenever <see cref="SelectedId"/> settles on a new value.</summary>
    public event EventHandler? SelectionChanged;

    /// <summary>The selected table's id, or <see langword="null"/> if none is selected.</summary>
    public Guid? SelectedId { get; private set; }

    /// <summary>Initializes a new instance of <see cref="TablePickerEdit"/>.</summary>
    public TablePickerEdit()
    {
        Name = "_tablePicker";
        Properties.TextEditStyle = TextEditStyles.DisableTextEditor;
        Properties.AutoHeight = false;
        BorderStyle = BorderStyles.Simple;
        Properties.BorderStyle = BorderStyles.Simple;
        Properties.Appearance.BorderColor = Color.FromArgb(203, 213, 225);
        Properties.Appearance.Options.UseBorderColor = true;
        Properties.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        Properties.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
        Properties.Appearance.Options.UseFont = true;
        Properties.Appearance.Options.UseForeColor = true;

        SelectedIndexChanged += OnSelectedIndexChanged;

        CustomDisplayText += (s, e) =>
        {
            if (SelectedId.HasValue && _displayById.TryGetValue(SelectedId.Value, out var display))
            {
                var code = display.Split(' ')[0].Replace("Table:", "").Trim();
                e.DisplayText = $"Table: {code}";
            }
            else if (e.Value is string str && !string.IsNullOrWhiteSpace(str) && str != "(No Table)" && str != "No Table")
            {
                var code = str.Split(' ')[0].Replace("Table:", "").Trim();
                e.DisplayText = $"Table: {code}";
            }
            else
            {
                e.DisplayText = "Table: None";
            }
        };
    }

    /// <summary>Replaces the picker's table items.</summary>
    public void LoadItems(IReadOnlyList<(Guid Id, string Display)> items)
    {
        _suppressSelectionEvent = true;
        try
        {
            var prevId = SelectedId;
            _idsByDisplay.Clear();
            _displayById.Clear();
            Properties.Items.Clear();

            Properties.Items.Add("(No Table)");

            foreach (var (id, display) in items)
            {
                _idsByDisplay[display] = id;
                _displayById[id] = display;
                Properties.Items.Add(display);
            }

            if (prevId.HasValue && _displayById.TryGetValue(prevId.Value, out var disp))
            {
                EditValue = disp;
                SelectedItem = disp;
                SelectedId = prevId;
            }
            else if (!prevId.HasValue)
            {
                EditValue = "(No Table)";
                SelectedItem = "(No Table)";
                SelectedId = null;
            }
        }
        finally
        {
            _suppressSelectionEvent = false;
        }
    }

    /// <summary>Selects the table matching the given id, or clears selection if null.</summary>
    public void SelectId(Guid? id)
    {
        _suppressSelectionEvent = true;
        try
        {
            if (id is null || id == Guid.Empty)
            {
                SelectedId = null;
                EditValue = "(No Table)";
                SelectedItem = "(No Table)";
                return;
            }

            if (_displayById.TryGetValue(id.Value, out var display))
            {
                SelectedId = id;
                EditValue = display;
                SelectedItem = display;
            }
            else
            {
                SelectedId = id;
            }
        }
        finally
        {
            _suppressSelectionEvent = false;
        }
    }

    private void OnSelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_suppressSelectionEvent) return;

        var text = (SelectedItem as string) ?? (EditValue as string);
        if (string.IsNullOrEmpty(text) || text == "(No Table)")
        {
            SelectedId = null;
        }
        else if (_idsByDisplay.TryGetValue(text, out var id))
        {
            SelectedId = id;
        }
        else
        {
            SelectedId = null;
        }

        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }
}
