using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;

namespace Clovent.Desktop.Restaurant.SmartPos;

/// <summary>
/// Professional Back Office Create/Edit dialog for Quick Order Templates and Deals.
/// Provides a structured Product -> Variant -> Auto-fetched Selling Price workflow,
/// allows authorized price overrides, preserves existing deal prices, displays live totals,
/// and enforces robust DPI-independent, responsive dialog sizing.
/// </summary>
public sealed class QuickOrderTemplateEditForm : XtraForm
{
    private const int TargetWidth = 960;
    private const int TargetHeight = 660;
    private const int MinTargetWidth = 840;
    private const int MinTargetHeight = 540;

    // Header controls
    private readonly TextEdit _nameEdit = new();
    private readonly TextEdit _descriptionEdit = new();
    private readonly SpinEdit _displayOrderEdit = new();
    private readonly CheckEdit _activeCheck = new();

    // Item editor workflow controls
    private readonly LookUpEdit _productLookup = new();
    private readonly LookUpEdit _variantLookup = new();
    private readonly SpinEdit _quantityEdit = new();
    private readonly SpinEdit _priceEdit = new();
    private readonly TextEdit _catalogPriceDisplay = new();
    private readonly LabelControl _lineTotalPreviewLabel = new();
    private readonly SimpleButton _btnAddOrUpdateItem = new();
    private readonly SimpleButton _btnCancelItemEdit = new();
    private readonly SimpleButton _btnResetPrice = new();

    // Items grid
    private readonly GridControl _itemsGrid = new();
    private readonly GridView _itemsView = new();
    private readonly BindingList<TemplateItemRow> _rows = [];
    private readonly SimpleButton _btnEditSelected = new();
    private readonly SimpleButton _btnRemoveSelected = new();
    private readonly LabelControl _linesCountLabel = new();

    // Footer controls
    private readonly LabelControl _totalLabel = new();
    private readonly SimpleButton _saveButton = new();
    private readonly SimpleButton _cancelButton = new();

    // State & Catalog data
    private readonly IReadOnlyList<ProductOptionRow> _variantOptions;
    private readonly List<ProductOptionRowSummary> _distinctProducts;
    private TemplateItemRow? _editingRow;
    private bool _isPopulatingLineEditor;

    private TableLayoutPanel _mainContainer = null!;
    private Control _detailsPanel = null!;
    private Control _itemEditorPanel = null!;
    private Control _toolbarPanel = null!;
    private Control _gridPanel = null!;
    private Control _footerPanel = null!;

    /// <summary>Builds the dialog for a new or existing template.</summary>
    public QuickOrderTemplateEditForm(
        string title,
        IReadOnlyList<ProductOptionRow> variantOptions,
        QuickOrderTemplateEditModel? existing = null)
    {
        _variantOptions = variantOptions ?? [];
        _distinctProducts = _variantOptions
            .Where(o => o.ProductId != Guid.Empty)
            .GroupBy(o => o.ProductId)
            .Select(g => new ProductOptionRowSummary(g.Key, g.First().ProductName))
            .OrderBy(p => p.ProductName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Enforce DPI-independent explicit form bounds to prevent WinForms/DevExpress auto-shrinking
        AutoScaleMode = AutoScaleMode.None;
        AutoSize = false;
        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        Font = new Font("Segoe UI", 9.5F);
        int initialW = DesktopDpi.Scale(TargetWidth, this);
        int initialH = DesktopDpi.Scale(TargetHeight, this);
        int minW = DesktopDpi.Scale(MinTargetWidth, this);
        int minH = DesktopDpi.Scale(MinTargetHeight, this);
        Size = new Size(initialW, initialH);
        ClientSize = new Size(initialW, initialH);
        MinimumSize = new Size(minW, minH);
        MaximizeBox = true;
        MinimizeBox = false;
        ShowInTaskbar = false;

        BuildFormLayout(existing != null);

        // Populate existing template data if provided
        if (existing != null)
        {
            _nameEdit.Text = existing.Name;
            _descriptionEdit.Text = existing.Description ?? string.Empty;
            _displayOrderEdit.Value = existing.DisplayOrder;
            _activeCheck.Checked = existing.IsActive;

            foreach (var item in existing.Items)
            {
                var opt = _variantOptions.FirstOrDefault(o => o.VariantId == item.VariantId);
                var row = new TemplateItemRow(
                    item.VariantId,
                    item.Quantity,
                    item.TemplateUnitPrice ?? (opt?.UnitPrice ?? 0m),
                    _variantOptions)
                {
                    ProductId = opt?.ProductId ?? Guid.Empty,
                    ProductName = opt?.ProductName ?? "Unknown Product",
                    VariantName = opt?.VariantName ?? "Unknown Variant",
                    CatalogPrice = opt?.UnitPrice ?? 0m
                };
                _rows.Add(row);
            }
        }
        else
        {
            _activeCheck.Checked = true;
            _displayOrderEdit.Value = 1;
        }

        RefreshTotals();
        UpdateRemoveButtonState();
    }

    /// <summary>The entered template name.</summary>
    public string NameValue => _nameEdit.Text.Trim();

    /// <summary>The optional description.</summary>
    public string? DescriptionValue => string.IsNullOrWhiteSpace(_descriptionEdit.Text) ? null : _descriptionEdit.Text.Trim();

    /// <summary>The entered display order.</summary>
    public int DisplayOrderValue => (int)_displayOrderEdit.Value;

    /// <summary>Whether the template is active.</summary>
    public bool IsActiveValue => _activeCheck.Checked;

    /// <summary>The dialog's item rows as command inputs.</summary>
    public IReadOnlyList<(Guid VariantId, decimal Quantity, decimal? TemplateUnitPrice)> ItemValues =>
        [.. _rows.Select(r => (r.VariantId, r.Quantity, (decimal?)r.UnitPrice))];

    /// <summary>Formats the live-total caption - public so the total wording is testable.</summary>
    public static string ComposeTotalText(decimal total) =>
        $"Total: {CurrencyDisplay.FormatPlain(total)}";

    protected override void OnLoad(EventArgs e)
    {
#if DEBUG
        SmartPosLayoutTelemetry.LogQuickOrderEvent(this, "OnLoad Start", _detailsPanel, _itemEditorPanel, _toolbarPanel, _gridPanel, _footerPanel, _saveButton, _cancelButton);
#endif
        base.OnLoad(e);
        ApplyResponsiveSize();
        CenterToParentOrScreen();
#if DEBUG
        SmartPosLayoutTelemetry.LogQuickOrderEvent(this, "OnLoad End", _detailsPanel, _itemEditorPanel, _toolbarPanel, _gridPanel, _footerPanel, _saveButton, _cancelButton);
#endif
    }

    private void ApplyResponsiveSize()
    {
        int scaledTargetW = DesktopDpi.Scale(TargetWidth, this);
        int scaledTargetH = DesktopDpi.Scale(TargetHeight, this);
        int scaledMinW = DesktopDpi.Scale(MinTargetWidth, this);
        int scaledMinH = DesktopDpi.Scale(MinTargetHeight, this);

        var screen = Owner != null ? Screen.FromControl(Owner) : (Screen.PrimaryScreen ?? Screen.FromPoint(new Point(0, 0)));
        var work = screen.WorkingArea;

        int maxAvailableW = work.Width;
        int maxAvailableH = work.Height;
        if (maxAvailableW < scaledTargetW && DeviceDpi > 96)
        {
            maxAvailableW = (int)Math.Round(maxAvailableW * (DeviceDpi / 96.0));
            maxAvailableH = (int)Math.Round(maxAvailableH * (DeviceDpi / 96.0));
        }

        int margin = DesktopDpi.Scale(48, this);
        int clampedW = Math.Min(scaledTargetW, maxAvailableW - margin);
        int clampedH = Math.Min(scaledTargetH, maxAvailableH - margin);

        MinimumSize = new Size(Math.Min(scaledMinW, clampedW), Math.Min(scaledMinH, clampedH));
        ClientSize = new Size(clampedW, clampedH);
    }

    private void CenterToParentOrScreen()
    {
        try
        {
            Form? parent = Owner as Form;
            if (parent == null && Owner is Control c)
            {
                parent = c.FindForm();
            }

            if (parent != null && parent.WindowState != FormWindowState.Minimized && parent.Visible)
            {
                var r = parent.RectangleToScreen(parent.ClientRectangle);
                Location = new Point(
                    Math.Max(0, r.Left + (r.Width - Width) / 2),
                    Math.Max(0, r.Top + (r.Height - Height) / 2));
                return;
            }

            var screen = Screen.FromControl(this);
            var work = screen.WorkingArea;
            Location = new Point(
                Math.Max(0, work.Left + (work.Width - Width) / 2),
                Math.Max(0, work.Top + (work.Height - Height) / 2));
        }
        catch
        {
            // Fallback to standard WinForms centering
        }
    }

    private void BuildFormLayout(bool isEditing)
    {
        var mainContainer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(16, 12, 16, 12),
            Margin = new Padding(0)
        };
        // 5 dedicated regions:
        // Row 0: Template Details (Auto-sized, never squashed)
        // Row 1: Line Item Builder (Auto-sized, never squashed)
        // Row 2: Grid Toolbar (Auto-sized)
        // Row 3: Items Grid (Consumes 100% of remaining available space)
        // Row 4: Footer Actions & Summary (Auto-sized)
        mainContainer.RowStyles.Clear();
        mainContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        mainContainer.ColumnStyles.Clear();
        mainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        _detailsPanel = BuildTemplateDetailsPanel();
        _itemEditorPanel = BuildLineItemEditorPanel();
        _toolbarPanel = BuildGridToolbar();
        _gridPanel = BuildItemsGridPanel();
        _footerPanel = BuildFooterPanel(isEditing);

        mainContainer.Controls.Add(_detailsPanel, 0, 0);
        mainContainer.Controls.Add(_itemEditorPanel, 0, 1);
        mainContainer.Controls.Add(_toolbarPanel, 0, 2);
        mainContainer.Controls.Add(_gridPanel, 0, 3);
        mainContainer.Controls.Add(_footerPanel, 0, 4);

        _mainContainer = mainContainer;
        Controls.Add(mainContainer);
    }

    private Control BuildTemplateDetailsPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 6,
            Margin = new Padding(0, 0, 0, DesktopDpi.Scale(8, this)),
            Padding = new Padding(0)
        };
        panel.ColumnStyles.Clear();
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        panel.RowStyles.Clear();
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 0: Section Heading
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 1: Name Label
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 2: Name Editor
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 3: Description Label
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 4: Description Editor
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 5: Display Order + Active Checkbox

        var headerBox = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, DesktopDpi.Scale(4, this)),
            Padding = new Padding(0)
        };

        var lblHeading = new LabelControl
        {
            Text = "TEMPLATE DETAILS",
            AutoSizeMode = LabelAutoSizeMode.Default,
            Margin = new Padding(0, 0, DesktopDpi.Scale(12, this), 0)
        };
        lblHeading.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblHeading.Appearance.ForeColor = Color.FromArgb(15, 23, 42); // Slate-900
        lblHeading.Appearance.Options.UseFont = true;
        lblHeading.Appearance.Options.UseForeColor = true;
        headerBox.Controls.Add(lblHeading);



        var lblName = new LabelControl
        {
            Text = "Name *",
            AutoSizeMode = LabelAutoSizeMode.Default,
            Margin = new Padding(0, 0, 0, 2)
        };
        lblName.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblName.Appearance.ForeColor = Color.FromArgb(71, 85, 105);
        lblName.Appearance.Options.UseFont = true;
        lblName.Appearance.Options.UseForeColor = true;

        SmartPosControlSizing.ConfigureEditor(_nameEdit, 300, 34);
        _nameEdit.Dock = DockStyle.Fill;
        _nameEdit.Margin = new Padding(0, 0, 0, 6);
        _nameEdit.Properties.NullValuePrompt = "e.g. Special Lunch Deal";

        var lblDesc = new LabelControl
        {
            Text = "Description",
            AutoSizeMode = LabelAutoSizeMode.Default,
            Margin = new Padding(0, 0, 0, 2)
        };
        lblDesc.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblDesc.Appearance.ForeColor = Color.FromArgb(71, 85, 105);
        lblDesc.Appearance.Options.UseFont = true;
        lblDesc.Appearance.Options.UseForeColor = true;

        SmartPosControlSizing.ConfigureEditor(_descriptionEdit, 300, 34);
        _descriptionEdit.Dock = DockStyle.Fill;
        _descriptionEdit.Margin = new Padding(0, 0, 0, 6);
        _descriptionEdit.Properties.NullValuePrompt = "e.g. 1 Chicken Haleem Full Plate + 2 Garlic Nan + 1 Fresh Salad";

        var orderBox = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        var lblOrder = new LabelControl
        {
            Text = "Display Order",
            AutoSizeMode = LabelAutoSizeMode.Default,
            Margin = new Padding(0, 8, 8, 0)
        };
        lblOrder.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblOrder.Appearance.ForeColor = Color.FromArgb(71, 85, 105);
        lblOrder.Appearance.Options.UseFont = true;
        lblOrder.Appearance.Options.UseForeColor = true;

        SmartPosControlSizing.ConfigureEditor(_displayOrderEdit, 90, 34);
        _displayOrderEdit.Properties.IsFloatValue = false;
        _displayOrderEdit.Properties.MinValue = 0;
        _displayOrderEdit.Properties.MaxValue = 9999;
        _displayOrderEdit.Margin = new Padding(0, 0, 20, 0);

        _activeCheck.Text = "Active (available in POS)";
        _activeCheck.Properties.AutoWidth = true;
        _activeCheck.Margin = new Padding(0, 7, 0, 0);

        orderBox.Controls.Add(lblOrder);
        orderBox.Controls.Add(_displayOrderEdit);
        orderBox.Controls.Add(_activeCheck);

        panel.Controls.Add(headerBox, 0, 0);
        panel.Controls.Add(lblName, 0, 1);
        panel.Controls.Add(_nameEdit, 0, 2);
        panel.Controls.Add(lblDesc, 0, 3);
        panel.Controls.Add(_descriptionEdit, 0, 4);
        panel.Controls.Add(orderBox, 0, 5);

        return panel;
    }

    private Control BuildLineItemEditorPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 4,
            Margin = new Padding(0, 0, 0, 8),
            Padding = new Padding(0)
        };
        panel.ColumnStyles.Clear();
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        panel.RowStyles.Clear();
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 0: Section Heading
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 1: Product / Variant / Qty grid
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 2: Price grid
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 3: Action buttons + line total

        var lblHeading = new LabelControl
        {
            Text = "DEAL / TEMPLATE ITEMS",
            AutoSizeMode = LabelAutoSizeMode.Default,
            Margin = new Padding(0, 0, 0, 4)
        };
        lblHeading.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblHeading.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
        lblHeading.Appearance.Options.UseFont = true;
        lblHeading.Appearance.Options.UseForeColor = true;

        // Sub-grid 1: Product, Variant, Qty
        var productGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 3,
            RowCount = 2,
            Margin = new Padding(0, 0, 0, 6),
            Padding = new Padding(0)
        };
        productGrid.ColumnStyles.Clear();
        productGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F)); // Product
        productGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F)); // Variant
        productGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10F)); // Qty
        productGrid.RowStyles.Clear();
        productGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));           // Labels
        productGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));           // Editors

        var lblProduct = new LabelControl { Text = "Product", AutoSizeMode = LabelAutoSizeMode.Default, Margin = new Padding(0, 0, 0, 2) };
        lblProduct.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblProduct.Appearance.ForeColor = Color.FromArgb(71, 85, 105);
        lblProduct.Appearance.Options.UseFont = true;
        lblProduct.Appearance.Options.UseForeColor = true;

        var lblVariant = new LabelControl { Text = "Variant / Portion", AutoSizeMode = LabelAutoSizeMode.Default, Margin = new Padding(0, 0, 0, 2) };
        lblVariant.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblVariant.Appearance.ForeColor = Color.FromArgb(71, 85, 105);
        lblVariant.Appearance.Options.UseFont = true;
        lblVariant.Appearance.Options.UseForeColor = true;

        var lblQty = new LabelControl { Text = "Qty", AutoSizeMode = LabelAutoSizeMode.Default, Margin = new Padding(0, 0, 0, 2) };
        lblQty.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblQty.Appearance.ForeColor = Color.FromArgb(71, 85, 105);
        lblQty.Appearance.Options.UseFont = true;
        lblQty.Appearance.Options.UseForeColor = true;

        SmartPosControlSizing.ConfigureEditor(_productLookup, 240, 34);
        _productLookup.Dock = DockStyle.Fill;
        _productLookup.Margin = new Padding(0, 0, 10, 0);
        _productLookup.Properties.DataSource = _distinctProducts;
        _productLookup.Properties.DisplayMember = nameof(ProductOptionRowSummary.ProductName);
        _productLookup.Properties.ValueMember = nameof(ProductOptionRowSummary.ProductId);
        _productLookup.Properties.Columns.Clear();
        _productLookup.Properties.Columns.Add(new LookUpColumnInfo(nameof(ProductOptionRowSummary.ProductName), "Product Name"));
        _productLookup.Properties.ShowHeader = false;
        _productLookup.Properties.NullText = "Select Product";
        _productLookup.Properties.SearchMode = SearchMode.AutoFilter;
        _productLookup.EditValueChanged += ProductLookup_EditValueChanged;

        SmartPosControlSizing.ConfigureEditor(_variantLookup, 170, 34);
        _variantLookup.Dock = DockStyle.Fill;
        _variantLookup.Margin = new Padding(0, 0, 10, 0);
        _variantLookup.Properties.DisplayMember = nameof(ProductOptionRow.VariantName);
        _variantLookup.Properties.ValueMember = nameof(ProductOptionRow.VariantId);
        _variantLookup.Properties.Columns.Clear();
        _variantLookup.Properties.Columns.Add(new LookUpColumnInfo(nameof(ProductOptionRow.VariantName), "Variant"));
        _variantLookup.Properties.ShowHeader = false;
        _variantLookup.Properties.NullText = "Select Variant";
        _variantLookup.EditValueChanged += VariantLookup_EditValueChanged;

        SmartPosControlSizing.ConfigureEditor(_quantityEdit, 75, 34);
        _quantityEdit.Dock = DockStyle.Fill;
        _quantityEdit.Margin = new Padding(0);
        _quantityEdit.Properties.IsFloatValue = false;
        _quantityEdit.Properties.MinValue = 1;
        _quantityEdit.Properties.MaxValue = 9999;
        _quantityEdit.Value = 1;
        _quantityEdit.EditValueChanged += (_, _) => UpdateLineTotalPreview();

        productGrid.Controls.Add(lblProduct, 0, 0);
        productGrid.Controls.Add(lblVariant, 1, 0);
        productGrid.Controls.Add(lblQty, 2, 0);
        productGrid.Controls.Add(_productLookup, 0, 1);
        productGrid.Controls.Add(_variantLookup, 1, 1);
        productGrid.Controls.Add(_quantityEdit, 2, 1);

        // Sub-grid 2: Deal Price, Catalog Price
        var priceGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 3,
            RowCount = 2,
            Margin = new Padding(0, 0, 0, 6),
            Padding = new Padding(0)
        };
        priceGrid.ColumnStyles.Clear();
        priceGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F)); // Deal Price
        priceGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F)); // Catalog Price
        priceGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));  // Spacer
        priceGrid.RowStyles.Clear();
        priceGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));            // Labels
        priceGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));            // Editors

        var lblPrice = new LabelControl { Text = "Deal Price", AutoSizeMode = LabelAutoSizeMode.Default, Margin = new Padding(0, 0, 0, 2) };
        lblPrice.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblPrice.Appearance.ForeColor = Color.FromArgb(71, 85, 105);
        lblPrice.Appearance.Options.UseFont = true;
        lblPrice.Appearance.Options.UseForeColor = true;

        var lblCatalog = new LabelControl { Text = "Catalog Price", AutoSizeMode = LabelAutoSizeMode.Default, Margin = new Padding(0, 0, 0, 2) };
        lblCatalog.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblCatalog.Appearance.ForeColor = Color.FromArgb(71, 85, 105);
        lblCatalog.Appearance.Options.UseFont = true;
        lblCatalog.Appearance.Options.UseForeColor = true;

        SmartPosControlSizing.ConfigureEditor(_priceEdit, 150, 34);
        _priceEdit.Dock = DockStyle.Fill;
        _priceEdit.Margin = new Padding(0, 0, 12, 0);
        _priceEdit.Properties.IsFloatValue = true;
        _priceEdit.Properties.MinValue = 0m;
        _priceEdit.Properties.MaxValue = 9_999_999m;
        _priceEdit.Properties.Mask.EditMask = "N" + CurrencyDisplay.DecimalPlaces;
        _priceEdit.Properties.Mask.UseMaskAsDisplayFormat = true;
        _priceEdit.Value = 0m;
        _priceEdit.EditValueChanged += (_, _) => UpdateLineTotalPreview();

        SmartPosControlSizing.ConfigureEditor(_catalogPriceDisplay, 150, 34);
        _catalogPriceDisplay.Dock = DockStyle.Fill;
        _catalogPriceDisplay.Margin = new Padding(0);
        _catalogPriceDisplay.Properties.ReadOnly = true;
        _catalogPriceDisplay.Properties.AllowFocused = false;
        _catalogPriceDisplay.Properties.Appearance.BackColor = Color.FromArgb(241, 245, 249);
        _catalogPriceDisplay.Properties.Appearance.ForeColor = Color.FromArgb(71, 85, 105);
        _catalogPriceDisplay.Properties.Appearance.TextOptions.HAlignment = HorzAlignment.Far;
        _catalogPriceDisplay.Text = "—";

        priceGrid.Controls.Add(lblPrice, 0, 0);
        priceGrid.Controls.Add(lblCatalog, 1, 0);
        priceGrid.Controls.Add(_priceEdit, 0, 1);
        priceGrid.Controls.Add(_catalogPriceDisplay, 1, 1);

        // Sub-grid 3: Actions & Live total preview
        var actionsGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 5,
            RowCount = 1,
            Margin = new Padding(0, 2, 0, 0),
            Padding = new Padding(0)
        };
        actionsGrid.ColumnStyles.Clear();
        actionsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));      // Reset Price
        actionsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));      // Line Total Preview
        actionsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // Spacer
        actionsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));      // Cancel Edit
        actionsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));      // Add/Update Item
        actionsGrid.RowStyles.Clear();
        actionsGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _btnResetPrice.Text = "Use Current Price";
        SmartPosControlSizing.ConfigureButton(_btnResetPrice, 140, 34);
        _btnResetPrice.Cursor = Cursors.Hand;
        _btnResetPrice.Margin = new Padding(0, 0, DesktopDpi.Scale(12, this), 0);
        _btnResetPrice.Click += BtnResetPrice_Click;

        _lineTotalPreviewLabel.Text = "Line Total: —";
        _lineTotalPreviewLabel.AutoSizeMode = LabelAutoSizeMode.Default;
        _lineTotalPreviewLabel.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _lineTotalPreviewLabel.Appearance.ForeColor = Color.FromArgb(51, 65, 85);
        _lineTotalPreviewLabel.Appearance.Options.UseFont = true;
        _lineTotalPreviewLabel.Appearance.Options.UseForeColor = true;
        _lineTotalPreviewLabel.Margin = new Padding(0, DesktopDpi.Scale(7, this), 0, 0);

        _btnCancelItemEdit.Text = "Cancel Edit";
        SmartPosControlSizing.ConfigureButton(_btnCancelItemEdit, 95, 34);
        _btnCancelItemEdit.Margin = new Padding(0, 0, DesktopDpi.Scale(8, this), 0);
        _btnCancelItemEdit.Visible = false;
        _btnCancelItemEdit.Cursor = Cursors.Hand;
        _btnCancelItemEdit.Click += (_, _) => ResetLineEditor();

        _btnAddOrUpdateItem.Text = "Add Item";
        _btnAddOrUpdateItem.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _btnAddOrUpdateItem.Appearance.BackColor = Color.FromArgb(13, 148, 136); // Teal-600
        _btnAddOrUpdateItem.Appearance.ForeColor = Color.White;
        _btnAddOrUpdateItem.Appearance.Options.UseFont = true;
        _btnAddOrUpdateItem.Appearance.Options.UseBackColor = true;
        _btnAddOrUpdateItem.Appearance.Options.UseForeColor = true;
        SmartPosControlSizing.ConfigureButton(_btnAddOrUpdateItem, 120, 34);
        _btnAddOrUpdateItem.Margin = new Padding(0);
        _btnAddOrUpdateItem.Cursor = Cursors.Hand;
        _btnAddOrUpdateItem.Click += BtnAddOrUpdateItem_Click;

        actionsGrid.Controls.Add(_btnResetPrice, 0, 0);
        actionsGrid.Controls.Add(_lineTotalPreviewLabel, 1, 0);
        actionsGrid.Controls.Add(_btnCancelItemEdit, 3, 0);
        actionsGrid.Controls.Add(_btnAddOrUpdateItem, 4, 0);

        panel.Controls.Add(lblHeading, 0, 0);
        panel.Controls.Add(productGrid, 0, 1);
        panel.Controls.Add(priceGrid, 0, 2);
        panel.Controls.Add(actionsGrid, 0, 3);

        return panel;
    }

    private Control BuildGridToolbar()
    {
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, DesktopDpi.Scale(6, this)),
            Padding = new Padding(0)
        };

        _btnEditSelected.Text = "Edit Selected";
        SmartPosControlSizing.ConfigureButton(_btnEditSelected, 120, 34);
        _btnEditSelected.Margin = new Padding(0, 0, DesktopDpi.Scale(8, this), 0);
        _btnEditSelected.Cursor = Cursors.Hand;
        _btnEditSelected.Click += (_, _) => LoadFocusedRowIntoEditor();

        _btnRemoveSelected.Text = "Remove Selected";
        SmartPosControlSizing.ConfigureButton(_btnRemoveSelected, 135, 34);
        _btnRemoveSelected.Margin = new Padding(0);
        _btnRemoveSelected.Cursor = Cursors.Hand;
        _btnRemoveSelected.Click += (_, _) => RemoveSelectedRow();

        toolbar.Controls.Add(_btnEditSelected);
        toolbar.Controls.Add(_btnRemoveSelected);

        return toolbar;
    }

    private Control BuildItemsGridPanel()
    {
        var panel = new PanelControl
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyles.NoBorder,
            Margin = new Padding(0, 0, 0, 8),
            Padding = new Padding(0)
        };

        _itemsGrid.Dock = DockStyle.Fill;
        _itemsGrid.MainView = _itemsView;
        _itemsGrid.ViewCollection.Add(_itemsView);
        _itemsGrid.DataSource = _rows;

        _itemsView.OptionsBehavior.Editable = false;
        _itemsView.OptionsBehavior.ReadOnly = true;
        _itemsView.OptionsView.ShowGroupPanel = false;
        _itemsView.OptionsView.ShowIndicator = false;
        _itemsView.OptionsView.ColumnAutoWidth = true;
        _itemsView.OptionsView.EnableAppearanceEvenRow = true;
        _itemsView.RowHeight = DesktopDpi.Scale(32, this);
        _itemsView.ColumnPanelRowHeight = DesktopDpi.Scale(36, this);
        _itemsView.Appearance.HeaderPanel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
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

        AddCol(nameof(TemplateItemRow.ProductName), "Product", 280, DesktopDpi.Scale(160, this), HorzAlignment.Near);
        AddCol(nameof(TemplateItemRow.VariantName), "Variant / Portion", 220, DesktopDpi.Scale(130, this), HorzAlignment.Near);
        AddCol(nameof(TemplateItemRow.Quantity), "Qty", 100, DesktopDpi.Scale(50, this), HorzAlignment.Far);
        AddCol(nameof(TemplateItemRow.UnitPriceText), "Deal Price", 130, DesktopDpi.Scale(90, this), HorzAlignment.Far);
        AddCol(nameof(TemplateItemRow.CatalogPriceText), "Catalog Price", 130, DesktopDpi.Scale(90, this), HorzAlignment.Far);
        AddCol(nameof(TemplateItemRow.EffectiveTotalText), "Line Total", 140, DesktopDpi.Scale(95, this), HorzAlignment.Far);

        var qtyCol = _itemsView.Columns[nameof(TemplateItemRow.Quantity)];
        if (qtyCol != null)
        {
            qtyCol.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            qtyCol.DisplayFormat.FormatString = "0.##";
        }

        _itemsView.CustomColumnDisplayText += (s, e) =>
        {
            if (e.Column.FieldName == nameof(TemplateItemRow.Quantity) && e.Value is decimal q)
            {
                e.DisplayText = q % 1 == 0 ? q.ToString("0") : q.ToString("0.##");
            }
        };

        _itemsView.FocusedRowChanged += (_, _) => UpdateRemoveButtonState();
        _itemsView.RowCountChanged += (_, _) => UpdateRemoveButtonState();
        _itemsView.DoubleClick += (_, _) => LoadFocusedRowIntoEditor();
        _itemsView.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Delete)
            {
                RemoveSelectedRow();
                e.Handled = true;
            }
        };

        panel.Controls.Add(_itemsGrid);
        return panel;
    }

    private Control BuildFooterPanel(bool isEditing)
    {
        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            RowCount = 1,
            ColumnCount = 4,
            Margin = new Padding(0, 4, 0, 0),
            Padding = new Padding(0)
        };
        footer.ColumnStyles.Clear();
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // Left summary label
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));      // Total label
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));      // Cancel
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));      // Save

        footer.RowStyles.Clear();
        footer.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _linesCountLabel.Text = "0 items · 0 units";
        _linesCountLabel.AutoSizeMode = LabelAutoSizeMode.Default;
        _linesCountLabel.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        _linesCountLabel.Appearance.ForeColor = Color.FromArgb(100, 116, 139);
        _linesCountLabel.Appearance.Options.UseFont = true;
        _linesCountLabel.Appearance.Options.UseForeColor = true;
        _linesCountLabel.Margin = new Padding(0, 8, 0, 0);

        _totalLabel.Text = "TOTAL: 0.00";
        _totalLabel.AutoSizeMode = LabelAutoSizeMode.Default;
        _totalLabel.Appearance.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        _totalLabel.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
        _totalLabel.Appearance.Options.UseFont = true;
        _totalLabel.Appearance.Options.UseForeColor = true;
        _totalLabel.Margin = new Padding(0, 6, 20, 0);

        _cancelButton.Text = "Cancel";
        SmartPosControlSizing.ConfigureButton(_cancelButton, 95, 34);
        _cancelButton.Margin = new Padding(0, 0, DesktopDpi.Scale(8, this), 0);
        _cancelButton.Cursor = Cursors.Hand;
        _cancelButton.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        _saveButton.Text = isEditing ? "Save Changes" : "Create Template";
        _saveButton.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _saveButton.Appearance.BackColor = Color.FromArgb(13, 148, 136); // Teal-600
        _saveButton.Appearance.ForeColor = Color.White;
        _saveButton.Appearance.Options.UseFont = true;
        _saveButton.Appearance.Options.UseBackColor = true;
        _saveButton.Appearance.Options.UseForeColor = true;
        SmartPosControlSizing.ConfigureButton(_saveButton, 135, 34);
        _saveButton.Margin = new Padding(0);
        _saveButton.Cursor = Cursors.Hand;
        _saveButton.Click += (_, _) => TrySave();

        footer.Controls.Add(_linesCountLabel, 0, 0);
        footer.Controls.Add(_totalLabel, 1, 0);
        footer.Controls.Add(_cancelButton, 2, 0);
        footer.Controls.Add(_saveButton, 3, 0);

        AcceptButton = _saveButton;
        CancelButton = _cancelButton;

        return footer;
    }

    // -------------------------------------------------------------
    // Workflow & Event Handlers
    // -------------------------------------------------------------

    private void ProductLookup_EditValueChanged(object? sender, EventArgs e)
    {
        if (_isPopulatingLineEditor) return;

        if (_productLookup.EditValue is Guid productId && productId != Guid.Empty)
        {
            var variants = _variantOptions
                .Where(o => o.ProductId == productId)
                .OrderBy(o => o.VariantName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            _variantLookup.Properties.DataSource = variants;

            // Auto-select if only 1 variant exists
            if (variants.Count == 1)
            {
                _variantLookup.EditValue = variants[0].VariantId;
            }
            else
            {
                _variantLookup.EditValue = null;
                _priceEdit.Value = 0m;
                _catalogPriceDisplay.Text = "—";
                UpdateLineTotalPreview();
            }
        }
        else
        {
            _variantLookup.Properties.DataSource = null;
            _variantLookup.EditValue = null;
            _priceEdit.Value = 0m;
            _catalogPriceDisplay.Text = "—";
            UpdateLineTotalPreview();
        }
    }

    private void VariantLookup_EditValueChanged(object? sender, EventArgs e)
    {
        if (_isPopulatingLineEditor) return;

        if (_variantLookup.EditValue is Guid variantId && variantId != Guid.Empty)
        {
            var opt = _variantOptions.FirstOrDefault(o => o.VariantId == variantId);
            if (opt != null)
            {
                // Auto-fetch current active selling price from catalog
                _priceEdit.Value = opt.UnitPrice;
                _catalogPriceDisplay.Text = CurrencyDisplay.FormatPlain(opt.UnitPrice);
            }
        }
        else
        {
            _catalogPriceDisplay.Text = "—";
        }
        UpdateLineTotalPreview();
    }

    private void BtnResetPrice_Click(object? sender, EventArgs e)
    {
        if (_variantLookup.EditValue is Guid variantId && variantId != Guid.Empty)
        {
            var opt = _variantOptions.FirstOrDefault(o => o.VariantId == variantId);
            if (opt != null)
            {
                _priceEdit.Value = opt.UnitPrice;
                _catalogPriceDisplay.Text = CurrencyDisplay.FormatPlain(opt.UnitPrice);
                UpdateLineTotalPreview();
            }
        }
    }

    private void UpdateLineTotalPreview()
    {
        var qty = _quantityEdit.Value;
        var price = _priceEdit.Value;
        var total = qty * price;
        _lineTotalPreviewLabel.Text = $"Line Total: {CurrencyDisplay.FormatPlain(total)}";
    }

    private void BtnAddOrUpdateItem_Click(object? sender, EventArgs e)
    {
        if (_productLookup.EditValue is not Guid productId || productId == Guid.Empty)
        {
            XtraMessageBox.Show(this, "Please select a product.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_variantLookup.EditValue is not Guid variantId || variantId == Guid.Empty)
        {
            XtraMessageBox.Show(this, "Please select a variant / portion.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var opt = _variantOptions.FirstOrDefault(o => o.VariantId == variantId);
        var qty = _quantityEdit.Value;
        var unitPrice = _priceEdit.Value;

        if (qty <= 0)
        {
            XtraMessageBox.Show(this, "Quantity must be at least 1.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (unitPrice < 0)
        {
            XtraMessageBox.Show(this, "Unit price cannot be negative.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_editingRow != null)
        {
            // Updating existing row
            _editingRow.ProductId = productId;
            _editingRow.VariantId = variantId;
            _editingRow.ProductName = opt?.ProductName ?? "Product";
            _editingRow.VariantName = opt?.VariantName ?? "Variant";
            _editingRow.Quantity = qty;
            _editingRow.UnitPrice = unitPrice;
            _editingRow.CatalogPrice = opt?.UnitPrice ?? 0m;
        }
        else
        {
            // Check if variant is already present
            var existingRow = _rows.FirstOrDefault(r => r.VariantId == variantId);
            if (existingRow != null)
            {
                existingRow.Quantity += qty;
                existingRow.UnitPrice = unitPrice; // Update to specified deal price
            }
            else
            {
                var newRow = new TemplateItemRow(variantId, qty, unitPrice, _variantOptions)
                {
                    ProductId = productId,
                    ProductName = opt?.ProductName ?? "Product",
                    VariantName = opt?.VariantName ?? "Variant",
                    CatalogPrice = opt?.UnitPrice ?? 0m
                };
                _rows.Add(newRow);
            }
        }

        ResetLineEditor();
        RefreshTotals();
        _itemsView.RefreshData();
        UpdateRemoveButtonState();
    }

    private void LoadFocusedRowIntoEditor()
    {
        var row = _itemsView.GetFocusedRow() as TemplateItemRow
            ?? (_itemsView.RowCount > 0 ? _itemsView.GetRow(Math.Max(0, _itemsView.FocusedRowHandle)) as TemplateItemRow : null)
            ?? _rows.FirstOrDefault();

        if (row == null) return;

        _isPopulatingLineEditor = true;
        try
        {
            _editingRow = row;
            _productLookup.EditValue = row.ProductId;

            var variants = _variantOptions
                .Where(o => o.ProductId == row.ProductId)
                .OrderBy(o => o.VariantName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            _variantLookup.Properties.DataSource = variants;
            _variantLookup.EditValue = row.VariantId;

            _quantityEdit.Value = row.Quantity;
            _priceEdit.Value = row.UnitPrice;
            _catalogPriceDisplay.Text = CurrencyDisplay.FormatPlain(row.CatalogPrice);

            _btnAddOrUpdateItem.Text = "Update Item";
            _btnCancelItemEdit.Visible = true;
            UpdateLineTotalPreview();
        }
        finally
        {
            _isPopulatingLineEditor = false;
        }
    }

    private void ResetLineEditor()
    {
        _isPopulatingLineEditor = true;
        try
        {
            _editingRow = null;
            _productLookup.EditValue = null;
            _variantLookup.Properties.DataSource = null;
            _variantLookup.EditValue = null;
            _quantityEdit.Value = 1;
            _priceEdit.Value = 0m;
            _catalogPriceDisplay.Text = "—";
            _btnAddOrUpdateItem.Text = "Add Item";
            _btnCancelItemEdit.Visible = false;
            _lineTotalPreviewLabel.Text = "Line Total: —";
        }
        finally
        {
            _isPopulatingLineEditor = false;
        }
    }

    private void RemoveSelectedRow()
    {
        var row = _itemsView.GetFocusedRow() as TemplateItemRow
            ?? (_itemsView.RowCount > 0 ? _itemsView.GetRow(Math.Max(0, _itemsView.FocusedRowHandle)) as TemplateItemRow : null)
            ?? _rows.LastOrDefault();

        if (row == null) return;

        if (_editingRow == row)
        {
            ResetLineEditor();
        }

        _rows.Remove(row);
        RefreshTotals();
        _itemsView.RefreshData();
        UpdateRemoveButtonState();
    }

    private void UpdateRemoveButtonState()
    {
        var hasSelection = _itemsView.FocusedRowHandle >= 0 && _rows.Count > 0;
        _btnRemoveSelected.Enabled = hasSelection;
        _btnEditSelected.Enabled = hasSelection;
    }

    private void RefreshTotals()
    {
        var total = _rows.Sum(r => r.EffectiveTotal);
        _totalLabel.Text = ComposeTotalText(total);
        var totalUnits = _rows.Sum(r => r.Quantity);
        _linesCountLabel.Text = $"{_rows.Count} line{(_rows.Count == 1 ? "" : "s")} / {totalUnits:0.##} unit{(totalUnits == 1 ? "" : "s")}";
    }

    private void TrySave()
    {
        if (string.IsNullOrWhiteSpace(_nameEdit.Text))
        {
            XtraMessageBox.Show(this, "Template name is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _nameEdit.Focus();
            return;
        }

        if (_rows.Count == 0)
        {
            XtraMessageBox.Show(this, "Please add at least one item to the template.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_rows.Any(r => r.VariantId == Guid.Empty))
        {
            XtraMessageBox.Show(this, "Every item must have a valid product variant selected.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_rows.Any(r => r.Quantity <= 0m))
        {
            XtraMessageBox.Show(this, "Every item quantity must be greater than zero.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_rows.Any(r => r.UnitPrice < 0m))
        {
            XtraMessageBox.Show(this, "Unit prices cannot be negative.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}

/// <summary>The pre-filled values an existing template passes into <see cref="QuickOrderTemplateEditForm"/>.</summary>
public sealed record QuickOrderTemplateEditModel(
    string Name,
    string? Description,
    int DisplayOrder,
    IReadOnlyList<(Guid VariantId, decimal Quantity, decimal? TemplateUnitPrice)> Items,
    bool IsActive = true);

/// <summary>One item row in the template dialog's grid.</summary>
public sealed class TemplateItemRow
{
    private IReadOnlyList<ProductOptionRow> _options;

    /// <summary>Creates an empty row for picking.</summary>
    public TemplateItemRow(IReadOnlyList<ProductOptionRow> options) : this(Guid.Empty, 1m, 0m, options)
    {
    }

    /// <summary>Restores an existing item.</summary>
    public TemplateItemRow(Guid variantId, decimal quantity, decimal unitPrice, IReadOnlyList<ProductOptionRow> options)
    {
        _options = options;
        VariantId = variantId;
        Quantity = quantity;
        UnitPrice = unitPrice;

        var opt = _options.FirstOrDefault(o => o.VariantId == variantId);
        if (opt != null)
        {
            ProductId = opt.ProductId;
            ProductName = opt.ProductName;
            VariantName = opt.VariantName;
            CatalogPrice = opt.UnitPrice;
        }
        else
        {
            ProductName = "Item";
            VariantName = "Standard";
            CatalogPrice = unitPrice;
        }
    }

    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public Guid VariantId { get; set; }
    public string VariantName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal CatalogPrice { get; set; }

    /// <summary>The row's effective unit price - the override when set, otherwise the variant's current catalog price.</summary>
    public decimal EffectiveUnitPrice => UnitPrice > 0m
        ? UnitPrice
        : CatalogPrice;

    /// <summary>The row's total after quantity.</summary>
    public decimal EffectiveTotal => EffectiveUnitPrice * Quantity;

    /// <summary>Formatted Line Total.</summary>
    public string EffectiveTotalText => CurrencyDisplay.FormatPlain(EffectiveTotal);

    /// <summary>Formatted Unit Price.</summary>
    public string UnitPriceText => CurrencyDisplay.FormatPlain(UnitPrice);

    /// <summary>Formatted Catalog Price.</summary>
    public string CatalogPriceText => CurrencyDisplay.FormatPlain(CatalogPrice);

    /// <summary>Refreshes the cached option list.</summary>
    public void RefreshDerived(IReadOnlyList<ProductOptionRow> options) => _options = options;
}
