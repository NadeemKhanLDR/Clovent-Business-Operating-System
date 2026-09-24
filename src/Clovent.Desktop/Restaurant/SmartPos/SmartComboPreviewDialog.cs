using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Restaurant.Application.SmartCombos;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;

namespace Clovent.Desktop.Restaurant.SmartPos;

/// <summary>
/// Professional DPI-aware modal dialog for reviewing a Smart Combo Opportunity.
/// Displays structured items composition in a DevExpress grid, key sales metrics,
/// interactive deal price & discount settings, cost/margin verification, and dismiss reason.
/// </summary>
public sealed class SmartComboPreviewDialog : XtraForm
{
    public sealed record PreviewItemRow(string ProductName, string VariantName, int Quantity, decimal Price, string PriceText);

    private readonly TextEdit _name = new();
    private readonly SpinEdit _price = new();
    private readonly SpinEdit _discount = new();
    private readonly LabelControl _marginDisplay = new();
    private readonly PanelControl _marginNotePanel = new();
    private readonly LabelControl _marginNoteLabel = new();
    private readonly ComboBoxEdit _reason = new();
    private readonly SimpleButton _btnCreate = new();
    private readonly SimpleButton _btnDismiss = new();
    private readonly SimpleButton _btnClose = new();

    private readonly GridControl _itemsGrid = new();
    private readonly GridView _itemsView = new();

    private readonly ComboOpportunity _opportunity;
    private readonly ComboAnalysis _analysis;
    private bool _isUpdatingPriceFromDiscount;
    private bool _isUpdatingDiscountFromPrice;

    public string ComboName => _name.Text.Trim();
    public decimal DealPrice => _price.Value;
    public string? Reason => string.IsNullOrWhiteSpace(_reason.Text) ? null : _reason.Text.Trim();

    public SmartComboPreviewDialog(ComboOpportunity opportunity, ComboAnalysis analysis, bool canCreate, bool canDismiss)
    {
        _opportunity = opportunity ?? throw new ArgumentNullException(nameof(opportunity));
        _analysis = analysis ?? throw new ArgumentNullException(nameof(analysis));

        AutoScaleMode = AutoScaleMode.None;
        AutoSize = false;
        Text = "Smart Combo Opportunity";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        Font = new Font("Segoe UI", 9.5F);

        try
        {
            var iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Resources", "cbos.ico");
            if (System.IO.File.Exists(iconPath))
            {
                Icon = new Icon(iconPath);
            }
            else if (!string.IsNullOrEmpty(Environment.ProcessPath) && System.IO.File.Exists(Environment.ProcessPath))
            {
                Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath);
            }
        }
        catch
        {
            // Gracefully ignore icon load failure
        }

        // Apply DPI scaling with safe screen bounds clamping
        int targetW = DesktopDpi.Scale(760, this);
        int targetH = DesktopDpi.Scale(660, this);
        int minW = DesktopDpi.Scale(680, this);
        int minH = DesktopDpi.Scale(580, this);

        try
        {
            var screen = Screen.FromControl(this);
            var work = screen.WorkingArea;
            if (targetW > work.Width - 30) targetW = work.Width - 30;
            if (targetH > work.Height - 30) targetH = work.Height - 30;
            if (minW > targetW) minW = targetW;
            if (minH > targetH) minH = targetH;
        }
        catch
        {
            // Fallback to scaled targets
        }

        Size = new Size(targetW, targetH);
        ClientSize = new Size(targetW, targetH);
        MinimumSize = new Size(minW, minH);

        KeyPreview = true;
        KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        };

        BuildLayout(canCreate, canDismiss);
    }

    private void BuildLayout(bool canCreate, bool canDismiss)
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 7,
            Padding = new Padding(16, 12, 16, 12),
            Margin = new Padding(0)
        };

        root.ColumnStyles.Clear();
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        root.RowStyles.Clear();
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));             // 0: Header
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));             // 1: Suggested Combo
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));        // 2: Combo Items Grid & Total
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));             // 3: Sales Evidence Card
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));             // 4: Deal Settings
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));             // 5: Dismiss Reason
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));             // 6: Bottom Actions

        root.Controls.Add(BuildHeaderSection(), 0, 0);
        root.Controls.Add(BuildSuggestedComboSection(), 0, 1);
        root.Controls.Add(BuildItemsSection(), 0, 2);
        root.Controls.Add(BuildEvidenceSection(), 0, 3);
        root.Controls.Add(BuildDealSettingsSection(), 0, 4);
        root.Controls.Add(BuildDismissSection(), 0, 5);
        root.Controls.Add(BuildActionsSection(canCreate, canDismiss), 0, 6);

        Controls.Add(root);

        // Initial values & recalculation
        _name.Text = _opportunity.Name;
        _price.Value = _opportunity.SuggestedPrice;
        RecalculateFromPrice();
    }

    private Control BuildHeaderSection()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, DesktopDpi.Scale(8, this)),
            Padding = new Padding(0)
        };

        var title = new LabelControl
        {
            Text = "SMART COMBO OPPORTUNITY",
            AutoSizeMode = LabelAutoSizeMode.Default,
            Margin = new Padding(0, 0, 0, 2)
        };
        title.Appearance.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        title.Appearance.ForeColor = Color.FromArgb(15, 23, 42); // Slate-900
        title.Appearance.Options.UseFont = true;
        title.Appearance.Options.UseForeColor = true;

        var subtitle = new LabelControl
        {
            Text = "Review discovered sales evidence, customize combo pricing, and convert into a POS quick deal.",
            AutoSizeMode = LabelAutoSizeMode.Default,
            Margin = new Padding(0)
        };
        subtitle.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        subtitle.Appearance.ForeColor = Color.FromArgb(100, 116, 139); // Slate-500
        subtitle.Appearance.Options.UseFont = true;
        subtitle.Appearance.Options.UseForeColor = true;

        panel.Controls.Add(title);
        panel.Controls.Add(subtitle);
        return panel;
    }

    private Control BuildSuggestedComboSection()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, DesktopDpi.Scale(8, this)),
            Padding = new Padding(0)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var lbl = new LabelControl
        {
            Text = "Suggested Combo Name",
            AutoSizeMode = LabelAutoSizeMode.Default,
            Margin = new Padding(0, 0, 0, 3)
        };
        lbl.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lbl.Appearance.ForeColor = Color.FromArgb(71, 85, 105);
        lbl.Appearance.Options.UseFont = true;
        lbl.Appearance.Options.UseForeColor = true;

        _name.Dock = DockStyle.Fill;
        _name.Properties.MaxLength = 100;
        _name.Properties.AutoHeight = false;
        _name.Height = DesktopDpi.Scale(34, this);
        _name.Margin = new Padding(0);

        panel.Controls.Add(lbl, 0, 0);
        panel.Controls.Add(_name, 0, 1);
        return panel;
    }

    private Control BuildItemsSection()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0, 0, 0, DesktopDpi.Scale(8, this)),
            Padding = new Padding(0)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // Label: COMBO ITEMS
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));   // Grid
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // Normal Total

        var lbl = new LabelControl
        {
            Text = "COMBO ITEMS",
            AutoSizeMode = LabelAutoSizeMode.Default,
            Margin = new Padding(0, 0, 0, 4)
        };
        lbl.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lbl.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
        lbl.Appearance.Options.UseFont = true;
        lbl.Appearance.Options.UseForeColor = true;

        // Configure DevExpress GridControl
        _itemsGrid.Dock = DockStyle.Fill;
        _itemsGrid.MainView = _itemsView;
        _itemsGrid.ViewCollection.Add(_itemsView);

        _itemsView.OptionsBehavior.Editable = false;
        _itemsView.OptionsBehavior.ReadOnly = true;
        _itemsView.OptionsView.ShowGroupPanel = false;
        _itemsView.OptionsView.ShowIndicator = false;
        _itemsView.OptionsView.ColumnAutoWidth = true;
        _itemsView.OptionsView.EnableAppearanceEvenRow = true;
        _itemsView.RowHeight = DesktopDpi.Scale(28, this);
        _itemsView.ColumnPanelRowHeight = DesktopDpi.Scale(32, this);

        _itemsView.Appearance.HeaderPanel.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        _itemsView.Appearance.HeaderPanel.ForeColor = Color.FromArgb(51, 65, 85);
        _itemsView.Appearance.Row.Font = new Font("Segoe UI", 9F);
        _itemsView.Appearance.EvenRow.BackColor = Color.FromArgb(248, 250, 252);
        _itemsView.Appearance.FocusedRow.BackColor = Color.FromArgb(224, 242, 254);
        _itemsView.Appearance.FocusedRow.ForeColor = Color.FromArgb(15, 23, 42);

        _itemsView.Columns.Clear();
        void AddCol(string field, string caption, int width, int minWidth, HorzAlignment align)
        {
            var col = _itemsView.Columns.AddVisible(field, caption);
            col.Width = width;
            col.MinWidth = minWidth;
            col.AppearanceHeader.TextOptions.HAlignment = align;
            col.AppearanceCell.TextOptions.HAlignment = align;
        }

        AddCol(nameof(PreviewItemRow.ProductName), "Product", 280, DesktopDpi.Scale(140, this), HorzAlignment.Near);
        AddCol(nameof(PreviewItemRow.VariantName), "Variant / Portion", 220, DesktopDpi.Scale(120, this), HorzAlignment.Near);
        AddCol(nameof(PreviewItemRow.Quantity), "Qty", 80, DesktopDpi.Scale(50, this), HorzAlignment.Center);
        AddCol(nameof(PreviewItemRow.PriceText), "Price", 120, DesktopDpi.Scale(80, this), HorzAlignment.Far);

        var rows = _opportunity.Items.Select(i => new PreviewItemRow(
            i.ProductName,
            i.VariantName,
            1,
            i.Price,
            CurrencyDisplay.FormatPlain(i.Price))).ToList();
        _itemsGrid.DataSource = rows;

        // Normal Total label
        var totalBox = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, DesktopDpi.Scale(4, this), 0, 0),
            Padding = new Padding(0)
        };

        var lblTotal = new LabelControl
        {
            Text = $"Normal Total: {CurrencyDisplay.FormatPlain(_opportunity.NormalPrice)}",
            AutoSizeMode = LabelAutoSizeMode.Default,
            Margin = new Padding(0)
        };
        lblTotal.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblTotal.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
        lblTotal.Appearance.Options.UseFont = true;
        lblTotal.Appearance.Options.UseForeColor = true;
        totalBox.Controls.Add(lblTotal);

        panel.Controls.Add(lbl, 0, 0);
        panel.Controls.Add(_itemsGrid, 0, 1);
        panel.Controls.Add(totalBox, 0, 2);

        return panel;
    }

    private Control BuildEvidenceSection()
    {
        var panel = new PanelControl
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BorderStyle = BorderStyles.Simple,
            Margin = new Padding(0, 0, 0, DesktopDpi.Scale(8, this)),
            Padding = new Padding(12, 8, 12, 8)
        };
        panel.Appearance.BackColor = Color.FromArgb(248, 250, 252); // Slate-50
        panel.Appearance.BorderColor = Color.FromArgb(226, 232, 240); // Slate-200
        panel.Appearance.Options.UseBackColor = true;
        panel.Appearance.Options.UseBorderColor = true;

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 4,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        content.ColumnStyles.Clear();
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

        content.RowStyles.Clear();
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Heading
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 1 metrics
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 2 metrics
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Explanatory text

        var lblHeading = new LabelControl
        {
            Text = "SALES EVIDENCE",
            AutoSizeMode = LabelAutoSizeMode.Default,
            Margin = new Padding(0, 0, 0, 6)
        };
        lblHeading.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblHeading.Appearance.ForeColor = Color.FromArgb(71, 85, 105);
        lblHeading.Appearance.Options.UseFont = true;
        lblHeading.Appearance.Options.UseForeColor = true;
        content.Controls.Add(lblHeading, 0, 0);
        content.SetColumnSpan(lblHeading, 4);

        Control MakeMetric(string label, string val)
        {
            var p = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, DesktopDpi.Scale(8, this), DesktopDpi.Scale(4, this))
            };
            var l = new LabelControl { Text = label, AutoSizeMode = LabelAutoSizeMode.Default, Margin = new Padding(0) };
            l.Appearance.Font = new Font("Segoe UI", 8F, FontStyle.Regular);
            l.Appearance.ForeColor = Color.FromArgb(100, 116, 139);
            l.Appearance.Options.UseFont = true;
            l.Appearance.Options.UseForeColor = true;

            var v = new LabelControl { Text = val, AutoSizeMode = LabelAutoSizeMode.Default, Margin = new Padding(0) };
            v.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            v.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
            v.Appearance.Options.UseFont = true;
            v.Appearance.Options.UseForeColor = true;

            p.Controls.Add(l);
            p.Controls.Add(v);
            return p;
        }

        content.Controls.Add(MakeMetric("Bought Together", $"{_opportunity.Frequency} orders"), 0, 1);
        content.Controls.Add(MakeMetric("Eligible Orders", $"{_opportunity.EligibleOrders}"), 1, 1);
        content.Controls.Add(MakeMetric("Support", $"{_opportunity.Support:P2}"), 2, 1);
        content.Controls.Add(MakeMetric("Attach Rate", $"{_opportunity.AttachRate:P2}"), 3, 1);

        content.Controls.Add(MakeMetric("Lift", $"{_opportunity.Lift:N2}"), 0, 2);
        var periodStr = $"{_analysis.FromUtc:dd MMM yyyy} – {_analysis.ToUtc:dd MMM yyyy}";
        var periodMetric = MakeMetric("Analysis Period", periodStr);
        content.Controls.Add(periodMetric, 1, 2);
        content.SetColumnSpan(periodMetric, 3);

        var lblExplanation = new LabelControl
        {
            Text = $"These items appeared together in {_opportunity.Frequency} of {_opportunity.EligibleOrders} completed orders. Direction: {_opportunity.Antecedent} → {_opportunity.Consequent}.",
            AutoSizeMode = LabelAutoSizeMode.Default,
            Margin = new Padding(0, DesktopDpi.Scale(4, this), 0, 0)
        };
        lblExplanation.Appearance.Font = new Font("Segoe UI", 8F, FontStyle.Regular);
        lblExplanation.Appearance.ForeColor = Color.FromArgb(100, 116, 139);
        lblExplanation.Appearance.Options.UseFont = true;
        lblExplanation.Appearance.Options.UseForeColor = true;
        content.Controls.Add(lblExplanation, 0, 3);
        content.SetColumnSpan(lblExplanation, 4);

        panel.Controls.Add(content);
        return panel;
    }

    private Control BuildDealSettingsSection()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, DesktopDpi.Scale(8, this)),
            Padding = new Padding(0)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Heading
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Price / Discount / Margin Grid
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Cost/Margin note panel

        var lblHeading = new LabelControl
        {
            Text = "DEAL SETTINGS",
            AutoSizeMode = LabelAutoSizeMode.Default,
            Margin = new Padding(0, 0, 0, 4)
        };
        lblHeading.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblHeading.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
        lblHeading.Appearance.Options.UseFont = true;
        lblHeading.Appearance.Options.UseForeColor = true;
        panel.Controls.Add(lblHeading, 0, 0);

        // Price, Discount, Margin row
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, 4),
            Padding = new Padding(0)
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));

        var lblPrice = new LabelControl { Text = "Suggested Deal Price", AutoSizeMode = LabelAutoSizeMode.Default, Margin = new Padding(0, 0, 0, 2) };
        lblPrice.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblPrice.Appearance.ForeColor = Color.FromArgb(71, 85, 105);
        lblPrice.Appearance.Options.UseFont = true;
        lblPrice.Appearance.Options.UseForeColor = true;

        var lblDiscount = new LabelControl { Text = "Discount (%)", AutoSizeMode = LabelAutoSizeMode.Default, Margin = new Padding(0, 0, 0, 2) };
        lblDiscount.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblDiscount.Appearance.ForeColor = Color.FromArgb(71, 85, 105);
        lblDiscount.Appearance.Options.UseFont = true;
        lblDiscount.Appearance.Options.UseForeColor = true;

        var lblMarginHeader = new LabelControl { Text = "Estimated Margin", AutoSizeMode = LabelAutoSizeMode.Default, Margin = new Padding(0, 0, 0, 2) };
        lblMarginHeader.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblMarginHeader.Appearance.ForeColor = Color.FromArgb(71, 85, 105);
        lblMarginHeader.Appearance.Options.UseFont = true;
        lblMarginHeader.Appearance.Options.UseForeColor = true;

        _price.Dock = DockStyle.Fill;
        _price.Properties.MinValue = 0;
        _price.Properties.MaxValue = _opportunity.NormalPrice > 0 ? _opportunity.NormalPrice : 999999m;
        _price.Properties.Mask.EditMask = "n" + CurrencyDisplay.DecimalPlaces;
        _price.Properties.Mask.UseMaskAsDisplayFormat = true;
        _price.Properties.AutoHeight = false;
        _price.Height = DesktopDpi.Scale(34, this);
        _price.Margin = new Padding(0, 0, DesktopDpi.Scale(10, this), 0);
        _price.EditValueChanged += (_, _) => RecalculateFromPrice();

        _discount.Dock = DockStyle.Fill;
        _discount.Properties.MinValue = 0m;
        _discount.Properties.MaxValue = 100m;
        _discount.Properties.Mask.EditMask = "n2";
        _discount.Properties.Mask.UseMaskAsDisplayFormat = true;
        _discount.Properties.AutoHeight = false;
        _discount.Height = DesktopDpi.Scale(34, this);
        _discount.Margin = new Padding(0, 0, DesktopDpi.Scale(10, this), 0);
        _discount.EditValueChanged += (_, _) => RecalculateFromDiscount();

        _marginDisplay.Dock = DockStyle.Fill;
        _marginDisplay.AutoSizeMode = LabelAutoSizeMode.None;
        _marginDisplay.Height = DesktopDpi.Scale(34, this);
        _marginDisplay.Margin = new Padding(0);
        _marginDisplay.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _marginDisplay.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
        _marginDisplay.Appearance.TextOptions.VAlignment = VertAlignment.Center;
        _marginDisplay.Appearance.Options.UseFont = true;
        _marginDisplay.Appearance.Options.UseForeColor = true;
        _marginDisplay.Appearance.Options.UseTextOptions = true;

        grid.Controls.Add(lblPrice, 0, 0);
        grid.Controls.Add(lblDiscount, 1, 0);
        grid.Controls.Add(lblMarginHeader, 2, 0);
        grid.Controls.Add(_price, 0, 1);
        grid.Controls.Add(_discount, 1, 1);
        grid.Controls.Add(_marginDisplay, 2, 1);

        panel.Controls.Add(grid, 0, 1);

        // Warning or info note panel
        _marginNotePanel.Dock = DockStyle.Fill;
        _marginNotePanel.AutoSize = true;
        _marginNotePanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _marginNotePanel.BorderStyle = BorderStyles.Simple;
        _marginNotePanel.Margin = new Padding(0, DesktopDpi.Scale(4, this), 0, 0);
        _marginNotePanel.Padding = new Padding(10, 6, 10, 6);

        _marginNoteLabel.Dock = DockStyle.Fill;
        _marginNoteLabel.AutoSizeMode = LabelAutoSizeMode.Vertical;
        _marginNoteLabel.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
        _marginNoteLabel.Appearance.Options.UseFont = true;
        _marginNoteLabel.Appearance.Options.UseForeColor = true;

        _marginNotePanel.Controls.Add(_marginNoteLabel);
        panel.Controls.Add(_marginNotePanel, 0, 2);

        return panel;
    }

    private void RecalculateFromPrice()
    {
        if (_isUpdatingPriceFromDiscount) return;
        _isUpdatingDiscountFromPrice = true;
        try
        {
            var price = _price.Value;
            var normal = _opportunity.NormalPrice;
            decimal discPct = normal > 0 ? ((normal - price) / normal) * 100m : 0m;
            discPct = Math.Max(0m, Math.Min(100m, discPct));
            _discount.Value = Math.Round(discPct, 2);

            UpdateMarginState(price);
        }
        finally
        {
            _isUpdatingDiscountFromPrice = false;
        }
    }

    private void RecalculateFromDiscount()
    {
        if (_isUpdatingDiscountFromPrice) return;
        _isUpdatingPriceFromDiscount = true;
        try
        {
            var discPct = _discount.Value;
            var normal = _opportunity.NormalPrice;
            decimal price = normal * (1m - (discPct / 100m));
            price = Math.Max(0m, Math.Min(normal, price));
            _price.Value = Math.Round(price, CurrencyDisplay.DecimalPlaces);

            UpdateMarginState(_price.Value);
        }
        finally
        {
            _isUpdatingPriceFromDiscount = false;
        }
    }

    private void UpdateMarginState(decimal dealPrice)
    {
        if (dealPrice <= 0)
        {
            _marginDisplay.Text = "Invalid price";
            _marginNotePanel.Appearance.BackColor = Color.FromArgb(254, 242, 242); // Red-50
            _marginNotePanel.Appearance.BorderColor = Color.FromArgb(254, 202, 202);
            _marginNotePanel.Appearance.Options.UseBackColor = true;
            _marginNotePanel.Appearance.Options.UseBorderColor = true;
            _marginNoteLabel.Appearance.ForeColor = Color.FromArgb(185, 28, 28);
            _marginNoteLabel.Text = "Enter a positive deal price.";
            return;
        }

        if (_opportunity.Cost is { } cost)
        {
            var netRev = _opportunity.NetRevenue(dealPrice);
            var profit = netRev - cost;
            var marginPct = netRev > 0 ? profit / netRev : 0m;

            _marginDisplay.Text = $"{marginPct:P1}";
            _marginNotePanel.Appearance.BackColor = Color.FromArgb(240, 253, 244); // Green-50
            _marginNotePanel.Appearance.BorderColor = Color.FromArgb(187, 247, 208);
            _marginNotePanel.Appearance.Options.UseBackColor = true;
            _marginNotePanel.Appearance.Options.UseBorderColor = true;
            _marginNoteLabel.Appearance.ForeColor = Color.FromArgb(22, 101, 52);
            _marginNoteLabel.Text = $"Estimated Gross Profit: {CurrencyDisplay.Format(profit)}  ·  Catalog Cost: {CurrencyDisplay.Format(cost)}  ·  Net Margin: {marginPct:P2}";
        }
        else
        {
            _marginDisplay.Text = "Cost unavailable";
            _marginNotePanel.Appearance.BackColor = Color.FromArgb(255, 251, 235); // Amber-50
            _marginNotePanel.Appearance.BorderColor = Color.FromArgb(254, 243, 199);
            _marginNotePanel.Appearance.Options.UseBackColor = true;
            _marginNotePanel.Appearance.Options.UseBorderColor = true;
            _marginNoteLabel.Appearance.ForeColor = Color.FromArgb(180, 83, 9);
            _marginNoteLabel.Text = "Cost data is unavailable, therefore margin cannot be verified.";
        }
    }

    private Control BuildDismissSection()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, DesktopDpi.Scale(8, this)),
            Padding = new Padding(0)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var lbl = new LabelControl
        {
            Text = "Dismiss Reason",
            AutoSizeMode = LabelAutoSizeMode.Default,
            Margin = new Padding(0, 0, 0, 3)
        };
        lbl.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lbl.Appearance.ForeColor = Color.FromArgb(71, 85, 105);
        lbl.Appearance.Options.UseFont = true;
        lbl.Appearance.Options.UseForeColor = true;

        _reason.Dock = DockStyle.Fill;
        _reason.Properties.MaxLength = 250;
        _reason.Properties.NullValuePrompt = "Select reason (optional)";
        _reason.Properties.Items.AddRange(new object[]
        {
            "Similar deal already active",
            "Operational constraints / prep capacity",
            "Margin too low / unprofitable",
            "Seasonal / temporary items",
            "Other"
        });
        _reason.Properties.AutoHeight = false;
        _reason.Height = DesktopDpi.Scale(34, this);
        _reason.Margin = new Padding(0);

        panel.Controls.Add(lbl, 0, 0);
        panel.Controls.Add(_reason, 0, 1);
        return panel;
    }

    private Control BuildActionsSection(bool canCreate, bool canDismiss)
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, DesktopDpi.Scale(4, this), 0, 0),
            Padding = new Padding(0)
        };

        _btnCreate.Text = "Create Deal";
        _btnCreate.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _btnCreate.Appearance.BackColor = Color.FromArgb(13, 148, 136); // Teal-600
        _btnCreate.Appearance.ForeColor = Color.White;
        _btnCreate.Appearance.Options.UseFont = true;
        _btnCreate.Appearance.Options.UseBackColor = true;
        _btnCreate.Appearance.Options.UseForeColor = true;
        SmartPosControlSizing.ConfigureButton(_btnCreate, 130, 36);
        _btnCreate.Margin = new Padding(0);
        _btnCreate.Enabled = canCreate;
        _btnCreate.Cursor = Cursors.Hand;
        _btnCreate.Click += (_, _) =>
        {
            if (ComboName.Length == 0)
            {
                XtraMessageBox.Show(this, "Enter a combo name.", "Smart Combo Opportunity", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _name.Focus();
                return;
            }

            if (DealPrice <= 0)
            {
                XtraMessageBox.Show(this, "Enter a positive deal price.", "Smart Combo Opportunity", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _price.Focus();
                return;
            }

            DialogResult = DialogResult.Yes;
            Close();
        };

        _btnDismiss.Text = "Dismiss";
        _btnDismiss.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        SmartPosControlSizing.ConfigureButton(_btnDismiss, 110, 36);
        _btnDismiss.Margin = new Padding(0, 0, DesktopDpi.Scale(8, this), 0);
        _btnDismiss.Enabled = canDismiss;
        _btnDismiss.Cursor = Cursors.Hand;
        _btnDismiss.Click += (_, _) =>
        {
            DialogResult = DialogResult.No;
            Close();
        };

        _btnClose.Text = "Close";
        _btnClose.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        SmartPosControlSizing.ConfigureButton(_btnClose, 95, 36);
        _btnClose.Margin = new Padding(0, 0, DesktopDpi.Scale(8, this), 0);
        _btnClose.Cursor = Cursors.Hand;
        _btnClose.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        CancelButton = _btnClose;

        panel.Controls.Add(_btnCreate);
        panel.Controls.Add(_btnDismiss);
        panel.Controls.Add(_btnClose);
        return panel;
    }

    /// <summary>
    /// Preserved for backward compatibility and test verification.
    /// </summary>
    public static string Evidence(ComboOpportunity x, ComboAnalysis a) =>
        "ITEMS (one of each)\r\n" + string.Join("\r\n", x.Items.Select(i => $"1x  {i.DisplayName}     {CurrencyDisplay.Format(i.Price)}")) +
        $"\r\n\r\nNormal total: {CurrencyDisplay.Format(x.NormalPrice)}\r\n\r\nSALES EVIDENCE\r\n" +
        $"These items appeared together in {x.Frequency} of {x.EligibleOrders} completed orders.\r\nPeriod (UTC): {a.FromUtc:u} – {a.ToUtc:u}\r\n" +
        $"Support: {x.Support:P2}\r\nDirection: {x.Antecedent} → {x.Consequent}\r\nAttach rate: {x.AttachRate:P2}    Lift: {x.Lift:N3}\r\n" +
        "Attach rate = combination orders / antecedent orders.\r\nLift = attach rate / consequent support.\r\n\r\n" +
        (x.Cost is { } cost ? $"Estimated catalog cost: {CurrencyDisplay.Format(cost)}\r\nNormal gross profit: {CurrencyDisplay.Format(x.NormalNetRevenue - cost)} ({(x.NormalNetRevenue - cost) / x.NormalNetRevenue:P2})" : "Cost data unavailable") +
        "\r\n\r\nCreate Deal publishes only after your approval. Prices follow existing POS tax behavior.";
}
