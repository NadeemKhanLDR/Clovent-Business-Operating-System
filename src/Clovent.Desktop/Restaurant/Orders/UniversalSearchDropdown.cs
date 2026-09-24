using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Clovent.Restaurant.Application.UniversalPosSearch.Dtos;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>The category of one universal search hit.</summary>
internal enum UniversalSearchItemKind
{
    Product,
    Customer,
    Order,
    Table
}

/// <summary>One selectable row of the universal search dropdown.</summary>
internal sealed record UniversalSearchItem(
    UniversalSearchItemKind Kind,
    Guid Id,
    string Title,
    string Subtitle,
    string CategoryLabel)
{
    public string KindGlyph => Kind switch
    {
        UniversalSearchItemKind.Product => "🍽",
        UniversalSearchItemKind.Customer => "👤",
        UniversalSearchItemKind.Order => "🧾",
        _ => "🪑"
    };
}

/// <summary>
/// Compact keyboard-friendly dropdown shown under the POS search box: up to
/// four hits per category (Products / Customers / Orders / Tables), section
/// headers, Up/Down navigation and Enter selection. Rendering only - all
/// actions are delegated through <see cref="ItemInvoked"/>.
/// </summary>
internal sealed class UniversalSearchDropdown : Form
{
    private const int MaxPerCategory = 4;

    private static readonly Color Back = Color.White;
    private static readonly Color Border = Color.FromArgb(203, 213, 225);
    private static readonly Color HeaderFore = Color.FromArgb(100, 116, 139);
    private static readonly Color SelectedBack = Color.FromArgb(240, 253, 250);

    private readonly Panel _rowsPanel;
    private readonly List<Control> _selectableRows = [];
    private int _selectedIndex = -1;

    /// <summary>Raised when the cashier activates a row (Enter or click).</summary>
    public event Action<UniversalSearchItem>? ItemInvoked;

    public bool HasSelection => _selectableRows.Count > 0;

    public UniversalSearchDropdown()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = false;
        BackColor = Border; // 1px frame via padding
        Padding = new Padding(1);
        Size = new Size(420, 40);

        _rowsPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Back
        };
        Controls.Add(_rowsPanel);
    }

    /// <summary>
    /// Never take focus on show: the search box keeps the caret so typing,
    /// Up/Down/Enter/Esc keep working while the dropdown is open.
    /// </summary>
    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            // WS_POPUP: a thin floating palette with no caption or taskbar entry.
            var cp = base.CreateParams;
            cp.Style |= unchecked((int)0x80000000); // WS_POPUP
            return cp;
        }
    }

    /// <summary>Populates the dropdown from a result set. Returns true when any row exists.</summary>
    public bool SetResults(UniversalPosSearchResultsDto results)
    {
        _rowsPanel.SuspendLayout();
        _rowsPanel.Controls.Clear();
        _selectableRows.Clear();
        _selectedIndex = -1;

        int y = 0;
        const int rowWidth = 418;
        void AddHeader(string text)
        {
            var header = new Label
            {
                Text = text,
                Bounds = new Rectangle(0, y, rowWidth, 22),
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = HeaderFore,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };
            _rowsPanel.Controls.Add(header);
            y += header.Height;
        }

        UniversalSearchItem Item(UniversalSearchItemKind kind, Guid id, string title, string subtitle) =>
            new UniversalSearchItem(kind, id, title, subtitle, string.Empty);

        bool any = false;

        if (results.Products.Count > 0)
        {
            AddHeader("PRODUCTS");
            foreach (var p in results.Products)
            {
                var item = Item(UniversalSearchItemKind.Product, p.VariantId,
                    string.IsNullOrWhiteSpace(p.VariantName) || p.VariantName == p.ProductName ? p.ProductName : $"{p.ProductName} - {p.VariantName}",
                    $"{Clovent.Desktop.Forms.Base.CurrencyDisplay.FormatPlain(p.UnitPrice)}{(p.CategoryName is { } cat ? $" • {cat}" : string.Empty)}");
                y = AddRow(item, y);
                any = true;
            }
        }

        if (results.Customers.Count > 0)
        {
            AddHeader("CUSTOMERS");
            foreach (var c in results.Customers)
            {
                y = AddRow(Item(UniversalSearchItemKind.Customer, c.CustomerId, $"[{c.Code}] {c.Name}", c.Phone ?? "No phone"), y);
                any = true;
            }
        }

        if (results.Orders.Count > 0)
        {
            AddHeader("ORDERS");
            foreach (var o in results.Orders)
            {
                y = AddRow(Item(UniversalSearchItemKind.Order, o.OrderId, o.OrderNumber, $"{o.OrderType} • {o.Status} • {Clovent.Desktop.Forms.Base.CurrencyDisplay.FormatPlain(o.TotalAmount)}"), y);
                any = true;
            }
        }

        if (results.Tables.Count > 0)
        {
            AddHeader("TABLES");
            foreach (var t in results.Tables)
            {
                y = AddRow(Item(UniversalSearchItemKind.Table, t.TableId, t.TableName,
                    (t.AreaName is { } area ? $"{area} • " : string.Empty) + (t.HasOpenOrder ? "Open order" : "Free")), y);
                any = true;
            }
        }

        _rowsPanel.ResumeLayout(true);

        var height = Math.Min(y, 320);
        Size = new Size(420, height + 2);
        if (any)
        {
            SetSelection(0);
        }

        return any;
    }

    private int AddRow(UniversalSearchItem item, int y)
    {
        const int rowWidth = 418;
        var row = new Panel
        {
            Bounds = new Rectangle(0, y, rowWidth, 30),
            BackColor = Back,
            Tag = item,
            Cursor = Cursors.Hand
        };

        var glyph = new Label
        {
            Text = item.KindGlyph,
            Dock = DockStyle.Left,
            Width = 30,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 10F)
        };
        var title = new Label
        {
            Text = item.Title,
            Dock = DockStyle.Left,
            Width = 210,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(15, 23, 42),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };
        var subtitle = new Label
        {
            Text = item.Subtitle,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            BackColor = Color.Transparent,
            ForeColor = HeaderFore,
            Font = new Font("Segoe UI", 8.5F),
            Padding = new Padding(0, 0, 8, 0)
        };

        row.Controls.Add(subtitle);
        row.Controls.Add(title);
        row.Controls.Add(glyph);

        void Activate()
        {
            ItemInvoked?.Invoke(item);
        }

        // MouseDown, not Click: the dropdown never takes focus, so a click
        // would otherwise move focus off the search box and race the invoke.
        row.MouseDown += (_, _) => Activate();
        foreach (Control child in row.Controls)
        {
            child.MouseDown += (_, _) => Activate();
        }
        row.MouseEnter += (_, _) => SetSelection(_selectableRows.IndexOf(row));

        _rowsPanel.Controls.Add(row);
        _selectableRows.Add(row);
        return y + row.Height;
    }

    /// <summary>Moves the keyboard selection by the given delta, returning the new index.</summary>
    public int MoveSelection(int delta)
    {
        if (_selectableRows.Count == 0)
        {
            return -1;
        }

        var next = _selectedIndex < 0 && delta > 0 ? 0
            : _selectedIndex < 0 ? _selectableRows.Count - 1
            : Math.Clamp(_selectedIndex + delta, 0, _selectableRows.Count - 1);
        SetSelection(next);
        if (_selectableRows[next] is { } row)
        {
            _rowsPanel.ScrollControlIntoView(row);
        }
        return next;
    }

    /// <summary>Invokes the currently selected row, if any.</summary>
    public void InvokeSelected()
    {
        if (_selectedIndex >= 0 && _selectedIndex < _selectableRows.Count
            && _selectableRows[_selectedIndex].Tag is UniversalSearchItem item)
        {
            ItemInvoked?.Invoke(item);
        }
    }

    private void SetSelection(int index)
    {
        for (int i = 0; i < _selectableRows.Count; i++)
        {
            _selectableRows[i].BackColor = i == index ? SelectedBack : Back;
        }

        _selectedIndex = index;
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        BeginInvoke(() => Hide());
    }
}
