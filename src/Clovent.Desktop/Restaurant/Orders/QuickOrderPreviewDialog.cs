using Clovent.Desktop.Forms.Base;
using Clovent.Restaurant.Application.QuickOrderTemplates.Dtos;
using DevExpress.XtraEditors;
using System.Drawing;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>
/// Modal Deal Preview for the POS Quick Orders strip: shows a quick-order
/// template's real, database-configured contents (items, quantities,
/// variants and resolved prices from <see cref="QuickOrderTemplateDto"/>)
/// before anything is added to the order. "Add Deal" simply returns
/// <see cref="DialogResult.OK"/> - the POS form then replays the items
/// through its normal add-item pipeline, so no pricing or order logic is
/// duplicated here. Opening and closing the preview never touches the
/// active order. Presentation only: every name, quantity and price comes
/// straight from the DTO the strip loaded via
/// <see cref="Clovent.Restaurant.Application.QuickOrderTemplates.Queries.ListActiveQuickOrderTemplatesQuery"/>.
/// </summary>
public sealed class QuickOrderPreviewDialog : XtraForm
{
    private static readonly Color AccentColor = Color.FromArgb(13, 148, 136);
    private static readonly Color HeaderBackColor = Color.FromArgb(15, 23, 42);
    private static readonly Color MutedColor = Color.FromArgb(71, 85, 105);
    private static readonly Color BorderColor = Color.FromArgb(203, 213, 225);
    private static readonly Color RowAltColor = Color.FromArgb(248, 250, 252);
    private static readonly Color TotalRowBackColor = Color.FromArgb(240, 253, 250);

    private readonly Control? _ownerRef;
    private readonly Func<bool>? _hasActiveOrder;
    private readonly Func<Task<IReadOnlyCollection<Clovent.Restaurant.Application.Tables.Dtos.TableDto>>>? _getTablesAsync;

    private SimpleButton _addButton = null!;
    private SimpleButton _closeButton = null!;

    public StartOrderChoice StartMode { get; private set; } = StartOrderChoice.Cancel;
    public Guid? SelectedTableId { get; private set; }

    public QuickOrderPreviewDialog(
        QuickOrderTemplateDto template,
        Control? owner = null,
        Func<bool>? hasActiveOrder = null,
        Func<Task<IReadOnlyCollection<Clovent.Restaurant.Application.Tables.Dtos.TableDto>>>? getTablesAsync = null)
    {
        _ownerRef = owner;
        _hasActiveOrder = hasActiveOrder;
        _getTablesAsync = getTablesAsync;

        Text = string.IsNullOrWhiteSpace(template.Name) ? "Quick Order Deal Preview" : $"Deal Preview - {template.Name}";
        AutoScaleMode = AutoScaleMode.None;
        Font = new Font("Segoe UI", 9.5F);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MinimizeBox = false;
        MaximizeBox = true;
        FormBorderStyle = FormBorderStyle.Sizable;
        BackColor = Color.White;

        // Apply DPI-aware sizing based on the parent/active screen working area.
        ApplyOperationalSize();

        // Root container layout:
        // Dedicated docked panels so Header, Deal Total, and Action Buttons
        // are pinned and permanently visible, while the items section scrolls.
        var buttonsPanel = BuildButtons();
        var totalPanel = BuildTotal(template);
        var headerPanel = BuildHeader(template);
        var itemsPanel = BuildItems(template);

        // WinForms Dock layout order (reverse of Add order):
        // 1. itemsPanel (Dock = Fill)
        // 2. totalPanel (Dock = Bottom)
        // 3. buttonsPanel (Dock = Bottom)
        // 4. headerPanel (Dock = Top)
        Controls.Add(itemsPanel);
        Controls.Add(totalPanel);
        Controls.Add(buttonsPanel);
        Controls.Add(headerPanel);

        AcceptButton = _addButton;
        CancelButton = _closeButton;
        KeyPreview = true;

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        };
    }

    /// <summary>
    /// Computes and applies operational sizing based on screen working area and DPI.
    /// Recommended logical size: 720 x 520 (at 96 DPI). Minimum size: 650 x 460.
    /// Clamped to screen working area so it never overflows or clips.
    /// </summary>
    public void ApplyOperationalSize(int? screenWidth = null, int? screenHeight = null)
    {
        var screen = _ownerRef != null ? Screen.FromControl(_ownerRef) : (Screen.PrimaryScreen ?? Screen.FromPoint(new Point(0, 0)));
        var work = screen.WorkingArea;

        int logicalTargetW = 720;
        int logicalTargetH = 520;

        int physTargetW = Scale(logicalTargetW);
        int physTargetH = Scale(logicalTargetH);

        // Clamp physical size so it comfortably fits within working area while guaranteeing minimum 650x460
        physTargetW = Math.Max(Scale(650), Math.Min(physTargetW, work.Width - Scale(32)));
        physTargetH = Math.Max(Scale(460), Math.Min(physTargetH, work.Height - Scale(32)));

        MinimumSize = new Size(Math.Min(Scale(650), physTargetW), Math.Min(Scale(460), physTargetH));
        ClientSize = new Size(physTargetW, physTargetH);
    }

    // ------------------------------------------------------------------
    // Header: deal name + "Total: Rs. X,XXX.00" + optional description.
    // Fixed at Top.
    // ------------------------------------------------------------------
    private Control BuildHeader(QuickOrderTemplateDto template)
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(0),
            BackColor = HeaderBackColor,
            Padding = new Padding(Scale(24), Scale(16), Scale(24), Scale(16))
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        header.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var name = new LabelControl
        {
            Text = string.IsNullOrWhiteSpace(template.Name) ? "Deal" : template.Name,
            Dock = DockStyle.Fill,
            AutoSizeMode = LabelAutoSizeMode.None,
            AutoEllipsis = true
        };
        name.Appearance.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
        name.Appearance.ForeColor = Color.White;
        name.Appearance.Options.UseFont = true;
        name.Appearance.Options.UseForeColor = true;
        name.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        name.Appearance.Options.UseTextOptions = true;
        header.Controls.Add(name, 0, 0);

        var price = new LabelControl
        {
            Text = $"Deal Price: {CurrencyDisplay.Format(template.TotalPrice)}",
            Dock = DockStyle.Fill,
            AutoSizeMode = LabelAutoSizeMode.Horizontal,
            Margin = new Padding(Scale(14), 0, 0, 0)
        };
        price.Appearance.Font = new Font("Segoe UI", 13.5F, FontStyle.Bold);
        price.Appearance.ForeColor = Color.FromArgb(153, 246, 228);
        price.Appearance.Options.UseFont = true;
        price.Appearance.Options.UseForeColor = true;
        price.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        price.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        price.Appearance.Options.UseTextOptions = true;
        header.Controls.Add(price, 1, 0);

        if (!string.IsNullOrWhiteSpace(template.Description))
        {
            var description = new LabelControl
            {
                Text = template.Description,
                Dock = DockStyle.Fill,
                AutoSizeMode = LabelAutoSizeMode.None,
                AutoEllipsis = true,
                AllowHtmlString = false,
                UseMnemonic = false,
                Margin = new Padding(0, Scale(6), 0, 0)
            };
            description.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            description.Appearance.ForeColor = Color.FromArgb(203, 213, 225);
            description.Appearance.Options.UseFont = true;
            description.Appearance.Options.UseForeColor = true;
            description.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            description.Appearance.Options.UseTextOptions = true;
            header.SetColumnSpan(description, 2);
            header.Controls.Add(description, 0, 1);
        }

        return header;
    }

    // ------------------------------------------------------------------
    // Items: column header row + one row per configured item.
    // The panel scrolls vertically for long deals; columns use percentage
    // and fixed sizing so item name flexes and price columns never clip.
    // ------------------------------------------------------------------
    private Control BuildItems(QuickOrderTemplateDto template)
    {
        var itemHeight = Scale(42);
        var qtyWidth = Scale(65);
        var unitWidth = Scale(130);
        var totalWidth = Scale(130);

        var scroll = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            BackColor = Color.White,
            AutoScroll = true
        };

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(Scale(20), 0, Scale(20), Scale(8)),
            BackColor = Color.White
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, qtyWidth));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, unitWidth));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, totalWidth));

        // Column captions
        grid.Controls.Add(ColumnLabel("QTY", DevExpress.Utils.HorzAlignment.Center, FontStyle.Bold, MutedColor), 0, 0);
        grid.Controls.Add(ColumnLabel("ITEM", DevExpress.Utils.HorzAlignment.Near, FontStyle.Bold, MutedColor), 1, 0);
        grid.Controls.Add(ColumnLabel("UNIT PRICE", DevExpress.Utils.HorzAlignment.Far, FontStyle.Bold, MutedColor), 2, 0);
        grid.Controls.Add(ColumnLabel("LINE TOTAL", DevExpress.Utils.HorzAlignment.Far, FontStyle.Bold, MutedColor), 3, 0);
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, Scale(36)));

        if (template.Items.Count == 0)
        {
            grid.RowCount = 2;
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, Scale(44)));
            var empty = ItemLabel("No items configured for this deal.", DevExpress.Utils.HorzAlignment.Near, FontStyle.Italic, MutedColor, Color.White, ellipsize: false);
            grid.SetColumnSpan(empty, 4);
            grid.Controls.Add(empty, 0, 1);
        }

        var index = 0;
        foreach (var item in template.Items)
        {
            var backColor = index % 2 == 1 ? RowAltColor : Color.White;
            var row = grid.RowCount;
            grid.RowCount = row + 1;
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, itemHeight));

            // Business names only, never internal IDs.
            // Variant name is displayed prominently if not redundant or 'Standard'.
            var showVariant = item.VariantName is { Length: > 0 }
                && !string.Equals(item.VariantName, "Standard", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(item.VariantName, item.ProductName, StringComparison.OrdinalIgnoreCase);
            var label = $"{item.ProductName}{(showVariant ? $" ({item.VariantName})" : string.Empty)}";

            var qtyText = $"{FormatQuantity(item.Quantity)}x";

            grid.Controls.Add(ItemLabel(qtyText, DevExpress.Utils.HorzAlignment.Center, FontStyle.Bold, Color.FromArgb(15, 23, 42), backColor, ellipsize: false), 0, row);
            grid.Controls.Add(ItemLabel(label, DevExpress.Utils.HorzAlignment.Near, FontStyle.Bold, Color.FromArgb(15, 23, 42), backColor, ellipsize: true), 1, row);
            grid.Controls.Add(ItemLabel(CurrencyDisplay.FormatPlain(item.UnitPrice), DevExpress.Utils.HorzAlignment.Far, FontStyle.Regular, MutedColor, backColor, ellipsize: false), 2, row);
            grid.Controls.Add(ItemLabel(CurrencyDisplay.FormatPlain(item.Total), DevExpress.Utils.HorzAlignment.Far, FontStyle.Bold, Color.FromArgb(15, 23, 42), backColor, ellipsize: false), 3, row);
            index++;
        }

        scroll.Controls.Add(grid);
        return scroll;
    }

    // ------------------------------------------------------------------
    // Deal total row: shows the DTO's configured/computed TotalPrice.
    // Fixed at Bottom (above buttons).
    // ------------------------------------------------------------------
    private Control BuildTotal(QuickOrderTemplateDto template)
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(Scale(24), Scale(14), Scale(24), Scale(14)),
            BackColor = TotalRowBackColor
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var caption = new LabelControl
        {
            Text = "Deal Total:",
            Dock = DockStyle.Fill,
            AutoSizeMode = LabelAutoSizeMode.None
        };
        caption.Appearance.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        caption.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
        caption.Appearance.Options.UseFont = true;
        caption.Appearance.Options.UseForeColor = true;
        caption.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        caption.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        caption.Appearance.Options.UseTextOptions = true;
        row.Controls.Add(caption, 0, 0);

        var amount = new LabelControl
        {
            Text = CurrencyDisplay.Format(template.TotalPrice),
            Dock = DockStyle.Fill,
            AutoSizeMode = LabelAutoSizeMode.Horizontal,
            Margin = new Padding(Scale(12), 0, 0, 0)
        };
        amount.Appearance.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        amount.Appearance.ForeColor = AccentColor;
        amount.Appearance.Options.UseFont = true;
        amount.Appearance.Options.UseForeColor = true;
        amount.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        amount.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Near;
        amount.Appearance.Options.UseTextOptions = true;
        row.Controls.Add(amount, 1, 0);

        return row;
    }

    // ------------------------------------------------------------------
    // Buttons: [Close] [Add Deal], bottom-right, fixed at Dock = Bottom.
    // ------------------------------------------------------------------
    private Control BuildButtons()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(Scale(20), Scale(12), Scale(20), Scale(14)),
            BackColor = Color.White
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _closeButton = new SimpleButton
        {
            Text = "Close",
            Size = new Size(Scale(120), Scale(44)),
            Margin = new Padding(0, 0, Scale(12), 0),
            Cursor = Cursors.Hand,
            TabIndex = 1,
            DialogResult = DialogResult.Cancel,
            ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat
        };
        _closeButton.Appearance.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _closeButton.Appearance.BackColor = Color.White;
        _closeButton.Appearance.ForeColor = MutedColor;
        _closeButton.Appearance.BorderColor = BorderColor;
        _closeButton.Appearance.Options.UseFont = true;
        _closeButton.Appearance.Options.UseBackColor = true;
        _closeButton.Appearance.Options.UseForeColor = true;
        _closeButton.Appearance.Options.UseBorderColor = true;
        _closeButton.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        _addButton = new SimpleButton
        {
            Text = "Add Deal",
            Size = new Size(Scale(150), Scale(44)),
            Cursor = Cursors.Hand,
            TabIndex = 0,
            DialogResult = DialogResult.None,
            ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat
        };
        _addButton.Appearance.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _addButton.Appearance.BackColor = AccentColor;
        _addButton.Appearance.ForeColor = Color.White;
        _addButton.Appearance.Options.UseFont = true;
        _addButton.Appearance.Options.UseBackColor = true;
        _addButton.Appearance.Options.UseForeColor = true;
        _addButton.Click += async (_, _) =>
        {
            if (!_addButton.Enabled) return;
            _addButton.Enabled = false;

            try
            {
                if (_hasActiveOrder != null && !_hasActiveOrder())
                {
                    using var choiceDialog = new StartOrderChoiceDialog(this);
                    if (choiceDialog.ShowDialog(this) != DialogResult.OK || choiceDialog.Choice == StartOrderChoice.Cancel)
                    {
                        // Cashier cancelled choice: keep Deal Preview open!
                        _addButton.Enabled = true;
                        return;
                    }

                    if (choiceDialog.Choice == StartOrderChoice.TakeAway)
                    {
                        StartMode = StartOrderChoice.TakeAway;
                        DialogResult = DialogResult.OK;
                        Close();
                        return;
                    }

                    if (choiceDialog.Choice == StartOrderChoice.DineIn)
                    {
                        var tables = _getTablesAsync != null ? await _getTablesAsync() : [];
                        using var tableDialog = new SelectTableDialog(tables, this);
                        if (tableDialog.ShowDialog(this) != DialogResult.OK || tableDialog.SelectedTableId == null)
                        {
                            // Cashier cancelled table selection: keep Deal Preview open!
                            _addButton.Enabled = true;
                            return;
                        }

                        StartMode = StartOrderChoice.DineIn;
                        SelectedTableId = tableDialog.SelectedTableId;
                        DialogResult = DialogResult.OK;
                        Close();
                        return;
                    }
                }

                // Active order exists (Case A)
                StartMode = StartOrderChoice.Cancel;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch
            {
                _addButton.Enabled = true;
                throw;
            }
        };

        row.Controls.Add(new Control { Dock = DockStyle.Fill, Margin = new Padding(0) }, 0, 0);
        row.Controls.Add(_closeButton, 1, 0);
        row.Controls.Add(_addButton, 2, 0);
        return row;
    }

    private LabelControl ColumnLabel(string text, DevExpress.Utils.HorzAlignment align, FontStyle style, Color foreColor)
    {
        var label = new LabelControl
        {
            Text = text,
            Dock = DockStyle.Fill,
            AutoSizeMode = LabelAutoSizeMode.None,
            Margin = new Padding(Scale(4), Scale(8), Scale(4), 0)
        };
        label.Appearance.Font = new Font("Segoe UI", 9F, style);
        label.Appearance.ForeColor = foreColor;
        label.Appearance.Options.UseFont = true;
        label.Appearance.Options.UseForeColor = true;
        label.Appearance.TextOptions.HAlignment = align;
        label.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Bottom;
        label.Appearance.Options.UseTextOptions = true;
        return label;
    }

    private LabelControl ItemLabel(string text, DevExpress.Utils.HorzAlignment align, FontStyle style, Color foreColor, Color backColor, bool ellipsize)
    {
        var label = new LabelControl
        {
            Text = text,
            Dock = DockStyle.Fill,
            AutoSizeMode = LabelAutoSizeMode.None,
            AutoEllipsis = ellipsize,
            Margin = new Padding(Scale(4), 0, Scale(4), 0)
        };
        label.Appearance.Font = new Font("Segoe UI", 10F, style);
        label.Appearance.ForeColor = foreColor;
        label.Appearance.BackColor = backColor;
        label.Appearance.Options.UseFont = true;
        label.Appearance.Options.UseForeColor = true;
        label.Appearance.Options.UseBackColor = true;
        label.Appearance.TextOptions.HAlignment = align;
        label.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        label.Appearance.Options.UseTextOptions = true;
        return label;
    }

    private static string FormatQuantity(decimal quantity) =>
        quantity == decimal.Truncate(quantity) ? ((int)quantity).ToString() : quantity.ToString("0.##");

    private int Scale(int v) => DesktopDpi.Scale(v, _ownerRef ?? this);
}
