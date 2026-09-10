using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Catalog.Application.Barcodes.Queries;
using Clovent.Catalog.Application.Categories.Dtos;
using Clovent.Catalog.Application.Categories.Queries;
using Clovent.Catalog.Application.Prices.Queries;
using Clovent.Catalog.Application.Variants.Dtos;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Catalog.Application.Products.Queries;
using Clovent.Catalog.Prices;
using Clovent.Desktop.Authorization;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Desktop.Forms.Base.Localization;
using Clovent.Desktop.Forms.Restaurant.MenuItems;
using Clovent.Desktop.Inventory.WarehouseStocks;
using Clovent.Desktop.Restaurant.Shared;
using Clovent.Desktop.Restaurant.Customers;
using Clovent.Desktop.Sessions;
using Clovent.Desktop.Startup;
using Clovent.Restaurant.Application.Customers.Queries;
using Clovent.Restaurant.Application.Customers.Commands;
using Clovent.Identity.Application.Authorization;
using Clovent.MasterData.Application.Currencies.Queries;
using Clovent.MasterData.Application.Warehouses.Queries;
using Clovent.Restaurant.Application;
using Clovent.Restaurant.Application.ActivityLogs.Commands;
using Clovent.Restaurant.Application.Discounts.Commands;
using Clovent.Restaurant.Application.Discounts.Queries;
using Clovent.Restaurant.Application.KitchenTickets.Commands;
using Clovent.Restaurant.Application.OrderLines.Commands;
using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.Application.OrderLines.Queries;
using Clovent.Restaurant.Application.Orders;
using Clovent.Restaurant.Application.Orders.Commands;
using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Application.Orders.Queries;
using Clovent.Restaurant.Application.PaymentMethods.Dtos;
using Clovent.Restaurant.Application.PaymentMethods.Queries;
using Clovent.Restaurant.Application.Payments.Commands;
using Clovent.Restaurant.Application.ServiceCharges.Commands;
using Clovent.Restaurant.Application.ServiceCharges.Queries;
using Clovent.Restaurant.Application.Tables.Queries;
using Clovent.Restaurant.Orders;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>
/// Rebuilt Restaurant POS form. Directly hosts the complete POS visual hierarchy
/// through Designer-declared controls. Maintains stable, visual-designer compatibility
/// and preserves all runtime business logic, authentication, credit limit checks, and MediatR queries.
/// </summary>
public sealed partial class RestaurantPosForm : XtraForm
{
    private const string FeatureCode = "pos";
    private const int TenderStripHeight = 140;

    // Declared and constructed here, NOT in RestaurantPosForm.Designer.cs.
    // EntityPicker has no public parameterless constructor, so the WinForms
    // Designer cannot serialize it and deletes it from InitializeComponent
    // every time it regenerates that file - which left these fields null and
    // crashed the screen on load with NullReferenceException. Anything the
    // Designer cannot round-trip has to live in this hand-written half of the
    // partial class, where it is safe from regeneration.
    private readonly Clovent.Desktop.MasterData.EntityPicker _warehousePicker = new("Warehouse:");
    private readonly TablePickerEdit _tablePicker = new();

    private static readonly Color PageBackColor = Color.FromArgb(241, 245, 249);
    private static readonly Color HeaderBackColor = Color.White;
    private static readonly Color RailBackColor = Color.FromArgb(51, 65, 85);
    private static readonly Color RailSelectedBackColor = Color.FromArgb(13, 148, 136);
    private static readonly Color RailForeColor = Color.White;
    private static readonly Color TileBackColor = Color.White;
    private static readonly Color TileBorderColor = Color.FromArgb(226, 232, 240);
    private static readonly Color TilePriceColor = Color.FromArgb(13, 148, 136);
    private static readonly Color AccentColor = Color.FromArgb(13, 148, 136);
    private static readonly Color SuccessColor = Color.FromArgb(22, 163, 74);
    private static readonly Color WarningColor = Color.FromArgb(217, 119, 6);
    private static readonly Color DangerColor = Color.FromArgb(220, 38, 38);
    private static readonly Color NeutralColor = Color.FromArgb(71, 85, 105);
    private static readonly Color StripBackColor = Color.FromArgb(248, 250, 252);
    private static readonly Color DividerColor = Color.FromArgb(226, 232, 240);
    private static readonly Color ChangeColor = Color.FromArgb(22, 163, 74);
    private static readonly Color UnselectedMethodFill = Color.White;
    private static readonly Color SelectedMethodBorder = Color.FromArgb(15, 23, 42);
    private static readonly Color UnavailableMethodFill = Color.FromArgb(226, 232, 240);
    private static readonly Color UnavailableMethodText = Color.FromArgb(148, 163, 184);

    private readonly IServiceScope _scope;
    private readonly ScreenOperationGate _gate = new();
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;
    private readonly IMenuItemsChangeNotifier _changeNotifier;
    private readonly IManagerAuthorizationService _managerAuthorization;
    private readonly ILogger<RestaurantPosForm> _logger;
    private readonly ISplashScreenService _splashScreenService;

    // View Toggle controls:
    private SimpleButton _btnGridView = null!;
    private SimpleButton _btnListView = null!;
    private FlowLayoutPanel _togglePanel = null!;
    private string _viewMode = "Grid";

    // Paging controls:
    // Paging controls are created programmatically at runtime to avoid designer interference.
    private TableLayoutPanel _paginationPanel = null!;
    private SimpleButton _btnPrevPage = null!;
    private SimpleButton _btnNextPage = null!;
    private TableLayoutPanel _pnlPageButtons = null!;
    private LabelControl _lblPageInfo = null!;

    private FlowLayoutPanel _productListFlow = null!;

    // The single product viewport (tiles grid + list view + empty label).
    // Owned by InitializeRuntime, placed into the center layout by
    // RestructureLayout.
    private Panel _productViewport = null!;

    private bool _isRefreshingRail;
    private bool _hasUnsavedEdits;

    // Left Active Orders sidebar: one card per order,
    // vertical scroll, built from the existing read-side queries.
    private Panel _sidebarPanel = null!;
    private FlowLayoutPanel _sidebarOrdersFlow = null!;
    private TableLayoutPanel _tlpRightRows = null!;
    private DevExpress.XtraEditors.ComboBoxEdit _cboSidebarFilter = null!;
    private SimpleButton _btnToggleActiveOrders = null!;
    private TableLayoutPanel _tlpSidebar = null!;
    private TableLayoutPanel _tlpBody = null!;
    private bool _activeOrdersExpanded = true;
    private System.Windows.Forms.Timer? _sidebarAnimationTimer;
    private int _sidebarTargetWidth;
    private int _sidebarCurrentWidth;
    
    private LabelControl _lblFoodiesMenuHeader = null!;
    private Panel _categoriesScrollContainer = null!;
    private SimpleButton _btnCategoriesScrollLeft = null!;
    private SimpleButton _btnCategoriesScrollRight = null!;
    
    // Center selected order custom cart list
    private FlowLayoutPanel _flowOrderedItems = null!;
    private LabelControl _lblOrderedItemsHeader = null!;
    private Panel _pnlOrderedItemsContainer = null!;
    private LabelControl _lblCartTableNo = null!;
    private LabelControl _lblCartOrderNo = null!;
    private LabelControl _lblSummarySubtotal = null!;
    private LabelControl _lblSummaryTax = null!;
    private LabelControl _lblSummaryDiscount = null!;
    private LabelControl _lblSummaryService = null!;
    private LabelControl _lblSummaryTotalPayable = null!;
    
    // Active orders category filter selection: "ActiveOrders", "TakeAway", "Closed", "WaitList"
    private string _activeOrdersFilter = "ActiveOrders";

    private int _currentPage = 1;
    private int _pageSize = 50;

    private OrderDto? _currentOrder;
    private bool _isRefreshingOrder;
    private readonly Dictionary<Guid, ProductVariantDto> _variantsById = [];
    private readonly Dictionary<Guid, decimal> _sellingPricesByVariantId = [];
    private readonly Dictionary<Guid, Image> _tileImagesByProductId = [];
    private readonly Dictionary<Guid, string> _productNamesById = [];
    private Dictionary<string, bool> _permissions = [];
    private List<ProductVariantDto> _activeVariants = [];
    private IReadOnlyCollection<OrderLineDto> _currentOrderLines = [];
    private readonly SemaphoreSlim _orderMutationLock = new(1, 1);
    private Guid? _selectedCategoryId;
    private IReadOnlyList<ProductCategoryDto> _loadedCategories = [];
    private bool _sortCategoriesByColor;

    // Payment tender strip state:
    private Guid? _orderId;
    private string _performedBy = "Unknown";
    private decimal _balance;
    private List<(Guid PaymentMethodId, string Name)> _paymentMethods = [];
    private Guid? _selectedPaymentMethodId;
    private LookUpEdit _paymentMethodLookup = null!;
    private bool _methodsLoaded;
    private bool _amountEntryIsPreset = true;
    private bool _isRecordingPayment;

    /// <summary>Design-time-only constructor for Visual Studio WinForms Designer - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public RestaurantPosForm()
    {
        _scope = null!;
        _mediator = null!;
        _featurePolicy = null!;
        _currentSession = null!;
        _changeNotifier = null!;
        _managerAuthorization = null!;
        _logger = null!;
        _splashScreenService = null!;

        try
        {
            InitializeComponent();
            AttachPickers();
            InitializeDesignTime();
        }
        catch (Exception ex)
        {
            LogDesignerException(ex, "RestaurantPosForm Parameterless Constructor");
            throw;
        }
    }

    /// <summary>Builds the screen and starts its own DI scope for Scoped services.</summary>
    public RestaurantPosForm(
        IServiceScopeFactory scopeFactory,
        ICurrentSession currentSession,
        IMenuItemsChangeNotifier changeNotifier,
        IManagerAuthorizationService managerAuthorization,
        ISplashScreenService splashScreenService)
    {
        try
        {
            _managerAuthorization = managerAuthorization;
            _scope = scopeFactory.CreateScope();
            _mediator = new SerializedMediator(_scope.ServiceProvider.GetRequiredService<IMediator>(), _gate);
            _featurePolicy = new SerializedFeatureAuthorizationPolicy(_scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>(), _gate);
            _logger = _scope.ServiceProvider.GetRequiredService<ILogger<RestaurantPosForm>>();
            _currentSession = currentSession;
            _changeNotifier = changeNotifier;
            _splashScreenService = splashScreenService;

            InitializeComponent();
            AttachPickers();

            if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            {
                InitializeDesignTime();
                return;
            }

            InitializeRuntime();
        }
        catch (Exception ex)
        {
            LogDesignerException(ex, "RestaurantPosForm Parameterized Constructor");
            throw;
        }
    }

    /// <summary>
    /// Parents the two <see cref="EntityPicker"/>s and wires the table
    /// selection handler. Called immediately after every
    /// <c>InitializeComponent()</c>, because the Designer cannot serialize
    /// these controls and so cannot do it itself - see the field
    /// declarations at the top of this file.
    /// </summary>
    private void AttachPickers()
    {
        _warehousePicker.Name = nameof(_warehousePicker);
        _warehousePicker.Margin = new Padding(8);

        _tablePicker.Name = nameof(_tablePicker);
        _tablePicker.Margin = new Padding(0, 1, 4, 1);
        _tablePicker.SelectionChanged += TablePicker_SelectionChanged;

        _warehousePicker.Dock = DockStyle.Fill;
        tlpOrderContext.Controls.Add(_warehousePicker, 0, 2);
    }

    private static void LogDesignerException(Exception ex, string context)
    {
        try
        {
            var logDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Clovent", "Logs");
            System.IO.Directory.CreateDirectory(logDir);
            var logPath = System.IO.Path.Combine(logDir, "designer_exception.txt");
            System.IO.File.AppendAllText(logPath, $"[{DateTime.Now}] Context: {context}\r\nException: {ex}\r\n\r\n");
        }
        catch { }
    }

    /// <summary>Seeds this screen with fake design-time data.</summary>
    private void InitializeDesignTime()
    {
        _warehousePicker.LoadItems(RestaurantPosDesignDataProvider.Warehouses);
        _tablePicker.LoadItems(RestaurantPosDesignDataProvider.Tables);
        _cashierLabel.Text = $"Cashier: {RestaurantPosDesignDataProvider.CashierName}";

        _loadedCategories = RestaurantPosDesignDataProvider.Categories;
        _activeVariants = [.. RestaurantPosDesignDataProvider.Variants];

        _variantsById.Clear();
        foreach (var variant in _activeVariants)
        {
            _variantsById[variant.ProductVariantId] = variant;
        }

        _sellingPricesByVariantId.Clear();
        foreach (var (variantId, price) in RestaurantPosDesignDataProvider.SellingPricesByVariantId)
        {
            _sellingPricesByVariantId[variantId] = price;
        }

        BuildCategoryButtons();
        RenderProductTiles(_activeVariants);

        _lineGrid.DataSource = RestaurantPosDesignDataProvider.CartLines
            .Select(l => new OrderLineRow(l.OrderLineId, l.Sku, l.Name, l.Quantity, l.UnitPrice, l.LineTotal, l.Notes, l.IsVoided, l.IsPriceOverridden))
            .ToList();

        var totals = RestaurantPosDesignDataProvider.SampleTotals;
        _subtotalLabel.Text = $"{PosStrings.Subtotal}: {CurrencyDisplay.FormatPlain(totals.Subtotal)}";
        _discountLabel.Text = $"{PosStrings.Discount}: -{CurrencyDisplay.FormatPlain(totals.Discount)}";
        _taxLabel.Text = $"{PosStrings.Tax}: {CurrencyDisplay.FormatPlain(totals.Tax)}";
        _serviceChargeLabel.Text = $"{PosStrings.ServiceCharge}: {CurrencyDisplay.FormatPlain(totals.ServiceCharge)}";
        _grandTotalLabel.Text = $"{PosStrings.GrandTotal}: {CurrencyDisplay.FormatPlain(totals.GrandTotal)}";
        _paidLabel.Text = $"{PosStrings.Paid}: {CurrencyDisplay.FormatPlain(totals.Paid)}";
        _balanceLabel.Text = $"{PosStrings.Balance}: {CurrencyDisplay.FormatPlain(totals.Balance)}";

        // Payment design-time initialization:
        _paymentMethods = [.. RestaurantPosDesignDataProvider.PaymentMethodNames.Select(name => (Guid.NewGuid(), name))];
        _balance = totals.Balance;
        _paymentBalanceLabel.Text = CurrencyDisplay.FormatPlain(_balance);
        _amountEdit.Text = FormatPlain(_balance);
        _amountEntryIsPreset = true;
        BuildMethodButtons();
        UpdateChangeDisplay();
    }

    private void InitializeRuntime()
    {
        // WinForms + DevExpress can apply DPI scaling twice to the designer-set
        // MinimumSize (1040x700), inflating the minimum window height far above
        // 700 logical pixels and preventing the POS from fitting a 1366x768
        // screen. Re-anchor it to the intended logical size, scaled exactly once.
        MinimumSize = new Size(
            Clovent.Desktop.Forms.Base.DesktopDpi.Scale(1040, this),
            Clovent.Desktop.Forms.Base.DesktopDpi.Scale(700, this));

        _changeNotifier.Changed += MenuItemsChangeNotifier_Changed;
        AppearanceManager.Changed += AppearanceManager_Changed;

        _cashierLabel.Text = _currentSession.DisplayName is { } name ? $"Cashier: {name}" : "Cashier: Not signed in";

        LocalizationHelper.LocalizeControl(this);

        if (_orderStatusLabel != null)
        {
            _orderStatusLabel.MinimumSize = new Size(Clovent.Desktop.Forms.Base.DesktopDpi.Scale(185, this), 0);
            _orderStatusLabel.AutoSize = true;
        }

        // Hard guarantee that only Qty and Price are editable: even if a
        // column's ReadOnly flag is ever lost, the Total/Item/Notes cells can
        // never open an editor (no "100.0000000" edit mode).
        _lineGridView.ShowingEditor += (_, e) =>
        {
            if (_lineGridView.FocusedColumn is not { } column ||
                (column != _lineGridColumnUnitPrice && column != _lineGridColumnQuantity))
            {
                e.Cancel = true;
            }
        };
        _lineGridView.ValidatingEditor += LineGridView_ValidatingEditor;
        _lineGridView.CellValueChanged += LineGridView_CellValueChanged;
        _lineGridView.FocusedRowChanged += (_, _) =>
        {
            if (_flowOrderedItems == null) return;
            var focusedId = _lineGridView.GetFocusedRow() is OrderLineRow r ? r.OrderLineId : Guid.Empty;
            var stripeRows = _flowOrderedItems.Controls.OfType<TableLayoutPanel>().Where(t => t.Tag is Guid).ToList();
            for (int i = 0; i < stripeRows.Count; i++)
            {
                var row = stripeRows[i];
                row.BackColor = GetCartRowBg(i, (row.Tag as Guid?) == focusedId);
            }
        };
        _lineGridView.CustomColumnDisplayText += (_, e) =>
        {
            if (e.Value is not decimal amount)
            {
                return;
            }

            if (e.Column.FieldName == nameof(OrderLineRow.UnitPrice) || e.Column.FieldName == nameof(OrderLineRow.LineTotal))
            {
                e.DisplayText = CurrencyDisplay.FormatPlain(amount);
            }
            else if (e.Column.FieldName == nameof(OrderLineRow.Quantity))
            {
                e.DisplayText = amount.ToString("0.##");
            }
        };

        // tlpSearch holds the quick-add strip (qty, barcode, add button).
        // The product search editor lives in the top header now.
        tlpSearch.SuspendLayout();
        tlpSearch.ColumnCount = 3;
        tlpSearch.ColumnStyles.Clear();
        tlpSearch.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Qty
        tlpSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // Barcode
        tlpSearch.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Add barcode button
        tlpSearch.ResumeLayout();

        _btnGridView = new SimpleButton
        {
            Text = "▦",
            ToolTip = "Grid View",
            Width = 32,
            Height = 26,
            ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat,
            Margin = new Padding(0, 0, 4, 0),
            Cursor = Cursors.Hand,
            ShowToolTips = true
        };
        _btnGridView.ImageOptions.ImageUri = "svgimages/reports/grid.svg";
        _btnGridView.Appearance.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        _btnGridView.Appearance.Options.UseFont = true;
        _btnGridView.Click += (s, e) => SetViewMode("Grid");

        _btnListView = new SimpleButton
        {
            Text = "▤",
            ToolTip = "List View",
            Width = 32,
            Height = 26,
            ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat,
            Margin = new Padding(0),
            Cursor = Cursors.Hand,
            ShowToolTips = true
        };
        _btnListView.ImageOptions.ImageUri = "svgimages/actions/listbullets.svg";
        _btnListView.Appearance.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        _btnListView.Appearance.Options.UseFont = true;
        _btnListView.Click += (s, e) => SetViewMode("List");

        _togglePanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            Margin = new Padding(0, 3, 16, 3),
            Padding = new Padding(0),
            BackColor = Color.Transparent
        };
        _togglePanel.Controls.Add(_btnGridView);
        _togglePanel.Controls.Add(_btnListView);

        // Product viewport: the ONLY container of the product tiles grid,
        // the list view and the empty-state label. It is placed into its
        // own TableLayoutPanel row by RestructureLayout - no docking
        // conflicts, no z-order games.
        _productViewport = new Panel
        {
            Name = "_productViewport",
            Dock = DockStyle.Fill,
            AutoScroll = false,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = PageBackColor
        };

        // List View panel — AutoScroll so rows scroll when overflowing; pagination lives outside this panel
        _productListFlow = new FlowLayoutPanel
        {
            AutoScroll = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            Padding = new Padding(4, 4, 4, 4),
            TabIndex = 1,
            Visible = false
        };
        _productViewport.Controls.Add(_productTilesFlow); // reparents out of pnlProducts
        _productViewport.Controls.Add(_productListFlow);
        _productViewport.Controls.Add(_tilesEmptyLabel);
        _productViewport.Resize += (_, _) => SizeListRows();

        // Pagination footer: created here, placed into its own dedicated
        // row of the center TableLayoutPanel by RestructureLayout so it can
        // never be covered by or compete with the product grid.
        _paginationPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(16, 0, 16, 0),
            Margin = new Padding(0)
        };
        _paginationPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _paginationPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _paginationPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _paginationPanel.Paint += (s, e) =>
        {
            using var pen = new Pen(DividerColor);
            e.Graphics.DrawLine(pen, 0, 0, _paginationPanel.Width, 0);
        };

        _lblPageInfo = new LabelControl
        {
            Text = "Showing 0–0 of 0 items",
            Dock = DockStyle.Fill,
            Padding = new Padding(0),
            AutoSizeMode = LabelAutoSizeMode.None
        };
        _lblPageInfo.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _lblPageInfo.Appearance.ForeColor = Color.FromArgb(71, 85, 105); // slate gray
        _lblPageInfo.Appearance.Options.UseFont = true;
        _lblPageInfo.Appearance.Options.UseForeColor = true;
        _lblPageInfo.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Near;
        _lblPageInfo.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _lblPageInfo.Appearance.Options.UseTextOptions = true;

        _pnlPageButtons = new TableLayoutPanel
        {
            RowCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.None,
            Anchor = AnchorStyles.Right, // hug right edge, vertically centered in footer
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.Transparent
        };
        // Single fixed-height row: every button is Dock=Fill, so all four
        // render byte-identical outer heights regardless of style/state.
        _pnlPageButtons.RowStyles.Add(new RowStyle(SizeType.Absolute, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(28, this)));
        _paginationPanel.Controls.Add(_lblPageInfo, 0, 0);
        _paginationPanel.Controls.Add(_pnlPageButtons, 1, 0);

        _btnPrevPage = new SimpleButton
        {
            Text = "‹ Previous",
            Cursor = Cursors.Hand,
            AutoSize = false,
            Dock = DockStyle.Fill,
            AllowFocus = false,
            ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat,
            Margin = new Padding(2, 0, 2, 0),
            Padding = new Padding(0)
        };
        _btnPrevPage.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _btnPrevPage.Appearance.Options.UseFont = true;
        _btnPrevPage.Click += (_, _) => ChangePage(_currentPage - 1);

        _btnNextPage = new SimpleButton
        {
            Text = "Next ›",
            Cursor = Cursors.Hand,
            AutoSize = false,
            Dock = DockStyle.Fill,
            AllowFocus = false,
            ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat,
            Margin = new Padding(2, 0, 2, 0),
            Padding = new Padding(0)
        };
        _btnNextPage.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _btnNextPage.Appearance.Options.UseFont = true;
        _btnNextPage.Click += (s, e) => ChangePage(_currentPage + 1);



        var memoEdit = new DevExpress.XtraEditors.Repository.RepositoryItemMemoEdit
        {
            WordWrap = true
        };
        _lineGrid.RepositoryItems.Add(memoEdit);
        _lineGridColumnName.ColumnEdit = memoEdit;
        _lineGridView.RowHeight = 44;

        // Load view mode preference and set it
        _viewMode = Clovent.Desktop.Forms.Base.PosSettingsStore.LoadViewMode();
        SetViewMode(_viewMode);

        // Load Active Orders sidebar state (collapsed/expanded) from user prefs
        _activeOrdersExpanded = !Clovent.Desktop.Forms.Base.PosSettingsStore.LoadActiveOrdersCollapsed();

        // Cart item column: primary line is the menu item name, secondary
        // line the portion ("Half"/"Full"), so the cell must wrap.
        _lineGridView.Appearance.Row.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
        _lineGridView.Appearance.Row.Options.UseTextOptions = true;

        // Visual selection/highlighting for cart rows
        var selectionColor = Color.FromArgb(240, 253, 250);
        _lineGridView.Appearance.FocusedRow.BackColor = selectionColor;
        _lineGridView.Appearance.FocusedRow.ForeColor = Color.Black;
        _lineGridView.Appearance.FocusedRow.Options.UseBackColor = true;
        _lineGridView.Appearance.FocusedRow.Options.UseForeColor = true;
        
        _lineGridView.Appearance.SelectedRow.BackColor = selectionColor;
        _lineGridView.Appearance.SelectedRow.ForeColor = Color.Black;
        _lineGridView.Appearance.SelectedRow.Options.UseBackColor = true;
        _lineGridView.Appearance.SelectedRow.Options.UseForeColor = true;
        
        _lineGridView.Appearance.HideSelectionRow.BackColor = selectionColor;
        _lineGridView.Appearance.HideSelectionRow.ForeColor = Color.Black;
        _lineGridView.Appearance.HideSelectionRow.Options.UseBackColor = true;
        _lineGridView.Appearance.HideSelectionRow.Options.UseForeColor = true;
        _lineGridColumnName.AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
        _lineGridColumnName.AppearanceCell.Options.UseTextOptions = true;

        _lineGridView.FocusedRowChanged += (s, e) => UpdateCartSelectionHighlights();

        // Runs last: the layout restructure above depends on every panel
        // this method creates (_paginationPanel, _productTilesFlow, ...),
        // which don't exist until the code above has run.
        RestructureLayout();
    }

    /// <summary>
    /// Rebuilds the screen's top-level hierarchy into the reference layout:
    ///
    ///   tlpMain
    ///   ├── Active Orders rail  (left, fixed width, scrollable card stack)
    ///   ├── Menu                (center, fills the remaining width)
    ///   └── Right panel         (fixed width)
    ///       ├── Current order   (context header, cart lines, totals)
    ///       └── Payment         (methods, tender, keypad, quick cash, actions)
    ///
    /// Everything is composed with Dock/TableLayoutPanel styles - no
    /// SetChildIndex juggling, no pixel compensation. The payment panel is a
    /// fixed-percentage row of the right column, so the digit pad can never
    /// be squeezed out by the product grid.
    /// </summary>
    private void RestructureLayout()
    {
        bool isSmallScreen = this.ClientSize.Width > 0 && this.ClientSize.Width <= 1100;
        int sidebarWidth = isSmallScreen 
            ? Clovent.Desktop.Forms.Base.DesktopDpi.Scale(230, this) 
            : Clovent.Desktop.Forms.Base.DesktopDpi.Scale(250, this);
        int rightWidth = isSmallScreen 
            ? Clovent.Desktop.Forms.Base.DesktopDpi.Scale(380, this) 
            : Clovent.Desktop.Forms.Base.DesktopDpi.Scale(410, this);

        tlpMain.SuspendLayout();
        this.SuspendLayout();

        // Establish tlpMain as the single root control of the Form
        this.Controls.Clear();
        this.Controls.Add(tlpMain);
        tlpMain.Dock = DockStyle.Fill;

        tlpMain.Controls.Clear();
        tlpMain.ColumnStyles.Clear();
        tlpMain.RowStyles.Clear();
        tlpMain.ColumnCount = 1;
        tlpMain.RowCount = 2;
        tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(44, this)));
        tlpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _tlpBody = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = PageBackColor
        };
        _tlpBody.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _tlpBody.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, sidebarWidth));
        _tlpBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _tlpBody.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, rightWidth));

        pnlHeader.Dock = DockStyle.Fill;
        tlpMain.Controls.Add(pnlHeader, 0, 0);
        tlpMain.Controls.Add(_tlpBody, 0, 1);

        // ==========================================
        // 0. SAFE CONTROL DETACHMENT
        // ==========================================
        // Set Parent = null on all designer-defined controls we want to keep
        // to prevent WinForms from disposing them when parents are cleared.
        foreach (Control c in new Control[] { 
            _logoLabel, _cashierLabel, _newDineInButton, _newTakeAwayButton, 
            _orderStatusLabel, _refreshButton, _printBillButton, _paymentHistoryButton, 
            _moreActionsButton, _logoutButton, _productSearchEdit, pnlSearch, _productTilesFlow, 
            _tilesEmptyLabel, _categoryButtonsPanel, _allCategoriesButton, 
            _lineGrid, pnlCartActions, pnlTotals, pnlOrderContext, pnlPayment,
            _subtotalLabel, _discountLabel, _taxLabel, _serviceChargeLabel, 
            _grandTotalLabel, _paidLabel, _balanceLabel, _totalsDividerPanel,
            pnlPaymentMethods, pnlAmountTendered, pnlKeypad, pnlQuickCash,
            _recordButton, _splitPaymentButton, _holdButton, _recallButton
        })
        {
            if (c != null)
            {
                c.Parent = null;
            }
        }

        // ==========================================
        _sidebarPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.White
        };
        var sidebarBorder = new Panel
        {
            Dock = DockStyle.Right,
            Width = 1,
            BackColor = DividerColor
        };
        _sidebarPanel.Controls.Add(sidebarBorder);

        var tlpSidebar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.White
        };
        _tlpSidebar = tlpSidebar;
        tlpSidebar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlpSidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(48, this)));  // Row 0: Sidebar Header
        tlpSidebar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Row 1: Sidebar list

        var pnlSidebarHeader = new Panel
        {
            Dock = DockStyle.Fill,
            Height = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(48, this),
            BackColor = Color.White
        };
        
        var tlpHeaderLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.Transparent
        };
        tlpHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(48, this)));
        tlpHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlpHeaderLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _btnToggleActiveOrders = new SimpleButton
        {
            Text = "☰",
            Dock = DockStyle.Fill,
            Cursor = Cursors.Hand,
            AllowFocus = false,
            ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder
        };
        _btnToggleActiveOrders.Appearance.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        _btnToggleActiveOrders.Appearance.ForeColor = Color.FromArgb(71, 85, 105);
        _btnToggleActiveOrders.Appearance.Options.UseFont = true;
        _btnToggleActiveOrders.Appearance.Options.UseForeColor = true;
        _btnToggleActiveOrders.Click += (s, e) =>
        {
            StartSidebarAnimation(!_activeOrdersExpanded);
        };

        _cboSidebarFilter = new DevExpress.XtraEditors.ComboBoxEdit
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(2, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(8, this), Clovent.Desktop.Forms.Base.DesktopDpi.Scale(8, this), Clovent.Desktop.Forms.Base.DesktopDpi.Scale(8, this)),
            Cursor = Cursors.Hand
        };
        _cboSidebarFilter.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _cboSidebarFilter.Properties.Items.AddRange(new object[] { "Active Orders", "Take Away", "Closed", "Wait List" });
        _cboSidebarFilter.SelectedIndex = 0;
        _cboSidebarFilter.Properties.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _cboSidebarFilter.Properties.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
        _cboSidebarFilter.Properties.Appearance.BackColor = Color.White;
        _cboSidebarFilter.Properties.Appearance.Options.UseFont = true;
        _cboSidebarFilter.Properties.Appearance.Options.UseForeColor = true;
        _cboSidebarFilter.Properties.Appearance.Options.UseBackColor = true;
        _cboSidebarFilter.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;
        _cboSidebarFilter.Properties.Appearance.BorderColor = Color.FromArgb(203, 213, 225);
        _cboSidebarFilter.Properties.Appearance.Options.UseBorderColor = true;
        _cboSidebarFilter.Properties.AppearanceDropDown.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _cboSidebarFilter.Properties.AppearanceDropDown.ForeColor = Color.FromArgb(15, 23, 42);
        _cboSidebarFilter.SelectedIndexChanged += async (s, e) =>
        {
            _activeOrdersFilter = _cboSidebarFilter.Text switch
            {
                "Take Away" => "TakeAway",
                "Closed" => "Closed",
                "Wait List" => "WaitList",
                _ => "ActiveOrders"
            };
            await RefreshActiveOrdersAsync();
        };

        tlpHeaderLayout.Controls.Add(_btnToggleActiveOrders, 0, 0);
        tlpHeaderLayout.Controls.Add(_cboSidebarFilter, 1, 0);
        pnlSidebarHeader.Controls.Add(tlpHeaderLayout);

        var sidebarHeaderDivider = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 1,
            BackColor = DividerColor
        };
        pnlSidebarHeader.Controls.Add(sidebarHeaderDivider);
        
        _sidebarOrdersFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.White,
            Padding = new Padding(8, 8, 8, 8),
            Margin = new Padding(0)
        };
        _sidebarOrdersFlow.HorizontalScroll.Maximum = 0;
        _sidebarOrdersFlow.AutoScrollMinSize = new Size(0, 0);
        _sidebarOrdersFlow.Resize += (_, _) => SizeSidebarOrderCards();

        tlpSidebar.Controls.Add(pnlSidebarHeader, 0, 0);
        tlpSidebar.Controls.Add(_sidebarOrdersFlow, 0, 1);

        _sidebarPanel.Controls.Add(tlpSidebar);

        // ==========================================
        // 2. TOP APPLICATION HEADER
        // ==========================================
        pnlHeader.Controls.Clear();
        pnlHeader.Height = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(44, this);
        pnlHeader.Appearance.BackColor = Color.White;
        pnlHeader.Appearance.Options.UseBackColor = true;
        pnlHeader.Padding = new Padding(0);
        pnlHeader.AutoSize = false;

        var tlpHeaderNew = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 10,
            RowCount = 1,
            Margin = new Padding(0),
            BackColor = Color.White
        };
        tlpHeaderNew.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        void StyleHeaderAction(SimpleButton btn, string text, Color back, Color fore)
        {
            btn.Text = text;
            btn.AutoSize = true;
            btn.Padding = new Padding(12, 0, 12, 0); // Give buttons some horizontal breathing room
            btn.Dock = DockStyle.Fill;
            btn.Margin = new Padding(4, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(4, this), 4, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(4, this));
            btn.MinimumSize = new Size(0, 0);
            btn.Cursor = Cursors.Hand;
            btn.AllowFocus = false;
            btn.ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            btn.LookAndFeel.UseDefaultLookAndFeel = false;
            btn.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Flat;
            btn.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btn.Appearance.BackColor = back;
            btn.Appearance.ForeColor = fore;
            btn.Appearance.BorderColor = back == Color.White ? DividerColor : back;
            btn.Appearance.Options.UseFont = true;
            btn.Appearance.Options.UseBackColor = true;
            btn.Appearance.Options.UseForeColor = true;
            btn.Appearance.Options.UseBorderColor = true;
            // Enforce fore color on all visual states so skin cannot override
            btn.AppearanceHovered.ForeColor = fore;
            btn.AppearanceHovered.BackColor = ControlPaint.Light(back, 0.15f);
            btn.AppearanceHovered.Options.UseForeColor = true;
            btn.AppearanceHovered.Options.UseBackColor = true;
            btn.AppearancePressed.ForeColor = fore;
            btn.AppearancePressed.BackColor = ControlPaint.Dark(back, 0.1f);
            btn.AppearancePressed.Options.UseForeColor = true;
            btn.AppearancePressed.Options.UseBackColor = true;
        }

        // Column 0: brand
        var lblBrand = new LabelControl
        {
            Text = "Clovent POS",
            Dock = DockStyle.Fill,
            AutoSizeMode = LabelAutoSizeMode.Horizontal,
            Padding = new Padding(16, 0, 8, 0)
        };
        lblBrand.Appearance.Font = new Font("Segoe UI", 12.5F, FontStyle.Bold);
        lblBrand.Appearance.ForeColor = AccentColor;
        lblBrand.Appearance.Options.UseFont = true;
        lblBrand.Appearance.Options.UseForeColor = true;
        lblBrand.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lblBrand.Appearance.Options.UseTextOptions = true;
        tlpHeaderNew.Controls.Add(lblBrand, 0, 0);
        tlpHeaderNew.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        // Columns 1-2: order starters
        StyleHeaderAction(_newDineInButton, "+ Dine In", AccentColor, Color.White);
        StyleHeaderAction(_newTakeAwayButton, "+ Take Away", Color.FromArgb(15, 23, 42), Color.White);
        tlpHeaderNew.Controls.Add(_newDineInButton, 1, 0);
        tlpHeaderNew.Controls.Add(_newTakeAwayButton, 2, 0);
        tlpHeaderNew.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tlpHeaderNew.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        // Column 3: global search
        _productSearchEdit.Dock = DockStyle.Fill;
        _productSearchEdit.Margin = new Padding(8, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(4, this), 8, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(4, this));
        _productSearchEdit.Properties.NullValuePrompt = "Search menu, orders and more...";
        _productSearchEdit.Properties.NullValuePromptShowForEmptyValue = true;
        _productSearchEdit.Properties.NullText = "";
        tlpHeaderNew.Controls.Add(_productSearchEdit, 3, 0);
        tlpHeaderNew.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        // Column 4: cashier identity
        _cashierLabel.Dock = DockStyle.Fill;
        _cashierLabel.AutoSizeMode = LabelAutoSizeMode.Horizontal;
        _cashierLabel.Margin = new Padding(8, 0, 6, 0);
        _cashierLabel.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
        _cashierLabel.Appearance.ForeColor = Color.FromArgb(71, 85, 105);
        _cashierLabel.Appearance.Options.UseFont = true;
        _cashierLabel.Appearance.Options.UseForeColor = true;
        _cashierLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        _cashierLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _cashierLabel.Appearance.Options.UseTextOptions = true;
        tlpHeaderNew.Controls.Add(_cashierLabel, 4, 0);
        tlpHeaderNew.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        // Column 5: compact order status badge
        _orderStatusLabel.Dock = DockStyle.None;
        _orderStatusLabel.Anchor = AnchorStyles.None;
        _orderStatusLabel.AutoSizeMode = LabelAutoSizeMode.None;
        _orderStatusLabel.Height = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(28, this);
        _orderStatusLabel.Margin = new Padding(4, 0, 4, 0);
        _orderStatusLabel.Padding = new Padding(10, 0, 10, 0);
        _orderStatusLabel.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        _orderStatusLabel.Appearance.ForeColor = AccentColor;
        _orderStatusLabel.Appearance.Options.UseFont = true;
        _orderStatusLabel.Appearance.Options.UseForeColor = true;
        _orderStatusLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        _orderStatusLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _orderStatusLabel.Appearance.Options.UseTextOptions = true;
        _orderStatusLabel.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;
        _orderStatusLabel.Appearance.BorderColor = Color.FromArgb(153, 246, 228);
        _orderStatusLabel.Appearance.Options.UseBorderColor = true;
        tlpHeaderNew.Controls.Add(_orderStatusLabel, 5, 0);
        tlpHeaderNew.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        // Column 6: Cancel Order button
        _cancelOrderButton.Parent = null;
        _cancelOrderButton.Text = "Cancel Order";
        _cancelOrderButton.Dock = DockStyle.None;
        _cancelOrderButton.Anchor = AnchorStyles.None;
        _cancelOrderButton.AutoSize = false;
        _cancelOrderButton.Height = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(28, this);
        _cancelOrderButton.Width = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(96, this);
        _cancelOrderButton.Margin = new Padding(2, 0, 6, 0);
        _cancelOrderButton.Padding = new Padding(0);
        _cancelOrderButton.Cursor = Cursors.Hand;
        _cancelOrderButton.AllowFocus = false;
        _cancelOrderButton.ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat;
        _cancelOrderButton.Appearance.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        _cancelOrderButton.Appearance.BackColor = Color.FromArgb(254, 242, 242);
        _cancelOrderButton.Appearance.ForeColor = Color.FromArgb(220, 38, 38);
        _cancelOrderButton.Appearance.BorderColor = Color.FromArgb(252, 165, 165);
        _cancelOrderButton.Appearance.Options.UseFont = true;
        _cancelOrderButton.Appearance.Options.UseBackColor = true;
        _cancelOrderButton.Appearance.Options.UseForeColor = true;
        _cancelOrderButton.Appearance.Options.UseBorderColor = true;
        _cancelOrderButton.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        _cancelOrderButton.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _cancelOrderButton.Appearance.Options.UseTextOptions = true;
        // Click is wired once in InitializeComponent (Designer); do NOT
        // subscribe again here - a second subscription made one click fire
        // CancelOrderButton_Click twice, showing the reason dialog twice.
        tlpHeaderNew.Controls.Add(_cancelOrderButton, 6, 0);
        tlpHeaderNew.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        // Columns 7-9: History / More Actions / Logout
        StyleHeaderAction(_paymentHistoryButton, "History", Color.White, Color.FromArgb(71, 85, 105));
        StyleHeaderAction(_moreActionsButton, "More ▼", Color.White, Color.FromArgb(71, 85, 105));
        StyleHeaderAction(_logoutButton, "Logout", Color.White, Color.FromArgb(220, 38, 38));
        
        tlpHeaderNew.Controls.Add(_paymentHistoryButton, 7, 0);
        tlpHeaderNew.Controls.Add(_moreActionsButton, 8, 0);
        tlpHeaderNew.Controls.Add(_logoutButton, 9, 0);
        tlpHeaderNew.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tlpHeaderNew.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tlpHeaderNew.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var headerBorder = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 1,
            BackColor = DividerColor
        };
        pnlHeader.Controls.Add(tlpHeaderNew);
        pnlHeader.Controls.Add(headerBorder);

        // ==========================================
        // 3. CENTER PANEL (pnlProducts)
        // ==========================================
        pnlProducts.Controls.Clear();
        pnlProducts.Padding = new Padding(16, 10, 16, 8);
        pnlProducts.Appearance.BackColor = PageBackColor;
        pnlProducts.Appearance.Options.UseBackColor = true;

        var tlpCenterRows = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Margin = new Padding(0),
            BackColor = Color.Transparent
        };
        tlpCenterRows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlpCenterRows.RowStyles.Add(new RowStyle(SizeType.Absolute, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(36, this)));            // Row 0: Foodies Menu heading + View Toggle
        tlpCenterRows.RowStyles.Add(new RowStyle(SizeType.Absolute, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(60, this)));            // Row 1: Category cards
        tlpCenterRows.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));                                                             // Row 2: product viewport
        tlpCenterRows.RowStyles.Add(new RowStyle(SizeType.Absolute, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(40, this)));            // Row 3: pagination footer

        // Hide pnlSearch (barcode quick-add)
        pnlSearch.Visible = false;

        // Row 0: Foodies Menu header + View Toggle
        var pnlFoodiesMenuHeader = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 0)
        };
        _lblFoodiesMenuHeader = new LabelControl
        {
            Text = "Foodies Menu",
            Dock = DockStyle.Fill,
            AutoSizeMode = LabelAutoSizeMode.None
        };
        _lblFoodiesMenuHeader.Appearance.Font = new Font("Segoe UI", 11.5F, FontStyle.Bold);
        _lblFoodiesMenuHeader.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
        _lblFoodiesMenuHeader.Appearance.Options.UseFont = true;
        _lblFoodiesMenuHeader.Appearance.Options.UseForeColor = true;
        _lblFoodiesMenuHeader.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _lblFoodiesMenuHeader.Appearance.Options.UseTextOptions = true;

        if (_togglePanel is not null)
        {
            _togglePanel.Dock = DockStyle.Right;
            _togglePanel.Margin = new Padding(0);
            pnlFoodiesMenuHeader.Controls.Add(_togglePanel);
        }
        pnlFoodiesMenuHeader.Controls.Add(_lblFoodiesMenuHeader);
        tlpCenterRows.Controls.Add(pnlFoodiesMenuHeader, 0, 0);

        // Row 1: category cards with scroll arrows
        _categoriesScrollContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        var tlpCategories = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0),
            BackColor = Color.Transparent
        };
        tlpCategories.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32));
        tlpCategories.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlpCategories.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32));
        tlpCategories.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        SimpleButton ScrollArrow(string glyph, Padding margin)
        {
            var btn = new SimpleButton
            {
                Text = glyph,
                Dock = DockStyle.Fill,
                Margin = margin,
                AllowFocus = false,
                ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat
            };
            btn.Appearance.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btn.Appearance.ForeColor = AccentColor;
            btn.Appearance.Options.UseFont = true;
            btn.Appearance.Options.UseForeColor = true;
            
            btn.AppearanceDisabled.ForeColor = Color.FromArgb(148, 163, 184); // slate-400
            btn.AppearanceDisabled.Options.UseForeColor = true;
            return btn;
        }

        _btnCategoriesScrollLeft = ScrollArrow("\u2039", new Padding(0, 2, 0, 2));
        _btnCategoriesScrollRight = ScrollArrow("\u203a", new Padding(0, 2, 0, 2));

        var pnlCategoryButtonsWrapper = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = false,
            BackColor = Color.Transparent,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        _categoryButtonsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.None,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0),
            Padding = new Padding(2, 2, 2, 2)
        };
        pnlCategoryButtonsWrapper.Controls.Add(_categoryButtonsPanel);
        pnlCategoryButtonsWrapper.Resize += (s, e) =>
        {
            _categoryButtonsPanel.Location = new Point(0, 0);
            _categoryButtonsPanel.Width = pnlCategoryButtonsWrapper.Width;
            _categoryButtonsPanel.Height = pnlCategoryButtonsWrapper.Height + 20; // push scrollbar down
        };

        _btnCategoriesScrollLeft.Click += (s, e) =>
        {
            _categoryButtonsPanel.HorizontalScroll.Value = Math.Max(0, _categoryButtonsPanel.HorizontalScroll.Value - 150);
            UpdateCategoryScrollButtons();
        };
        _btnCategoriesScrollRight.Click += (s, e) =>
        {
            _categoryButtonsPanel.HorizontalScroll.Value = Math.Min(_categoryButtonsPanel.HorizontalScroll.Maximum, _categoryButtonsPanel.HorizontalScroll.Value + 150);
            UpdateCategoryScrollButtons();
        };
        _categoryButtonsPanel.Resize += (s, e) => UpdateCategoryScrollButtons();
        tlpCategories.Controls.Add(_btnCategoriesScrollLeft, 0, 0);
        tlpCategories.Controls.Add(pnlCategoryButtonsWrapper, 1, 0);
        tlpCategories.Controls.Add(_btnCategoriesScrollRight, 2, 0);
        _categoriesScrollContainer.Controls.Add(tlpCategories);
        tlpCenterRows.Controls.Add(_categoriesScrollContainer, 0, 1);

        // Row 2: the product viewport
        _productViewport.Dock = DockStyle.Fill;
        _productViewport.Margin = new Padding(0);
        
        // Re-add the product controls that were detached during the safe control detachment phase
        if (!_productViewport.Controls.Contains(_productTilesFlow))
        {
            _productViewport.Controls.Add(_productTilesFlow);
        }
        if (!_productViewport.Controls.Contains(_productListFlow))
        {
            _productViewport.Controls.Add(_productListFlow);
        }
        if (!_productViewport.Controls.Contains(_tilesEmptyLabel))
        {
            _productViewport.Controls.Add(_tilesEmptyLabel);
        }
        tlpCenterRows.Controls.Add(_productViewport, 0, 2);

        // Row 3: pagination footer
        _paginationPanel.Margin = new Padding(0);
        tlpCenterRows.Controls.Add(_paginationPanel, 0, 3);

        pnlProducts.Controls.Add(tlpCenterRows);

        // ==========================================
        // 4. RIGHT PANEL (pnlCurrentOrder)
        // ==========================================
        pnlCurrentOrder.Controls.Clear();
        pnlCurrentOrder.Padding = new Padding(12, 4, 12, 2);
        pnlCurrentOrder.Appearance.BackColor = Color.White;
        pnlCurrentOrder.Appearance.Options.UseBackColor = true;

        var pnlCartHeader = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };
        _lblCartTableNo = new LabelControl
        {
            Text = "Table No #00",
            Dock = DockStyle.Left,
            AutoSizeMode = LabelAutoSizeMode.None,
            Width = 180
        };
        _lblCartTableNo.Appearance.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        _lblCartTableNo.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
        _lblCartTableNo.Appearance.Options.UseFont = true;
        _lblCartTableNo.Appearance.Options.UseForeColor = true;
        _lblCartTableNo.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _lblCartTableNo.Appearance.Options.UseTextOptions = true;

        _lblCartOrderNo = new LabelControl
        {
            Text = "No active order",
            Dock = DockStyle.Fill,
            AutoSizeMode = LabelAutoSizeMode.None
        };
        _lblCartOrderNo.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        _lblCartOrderNo.Appearance.ForeColor = Color.FromArgb(100, 116, 139);
        _lblCartOrderNo.Appearance.Options.UseFont = true;
        _lblCartOrderNo.Appearance.Options.UseForeColor = true;
        _lblCartOrderNo.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        _lblCartOrderNo.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _lblCartOrderNo.Appearance.Options.UseTextOptions = true;

        pnlCartHeader.Controls.Add(_lblCartTableNo);
        pnlCartHeader.Controls.Add(_lblCartOrderNo);

        _lblOrderedItemsHeader = new LabelControl
        {
            Text = "Ordered Items",
            Dock = DockStyle.Fill,
            AutoSizeMode = LabelAutoSizeMode.None
        };
        _lblOrderedItemsHeader.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _lblOrderedItemsHeader.Appearance.ForeColor = Color.FromArgb(71, 85, 105);
        _lblOrderedItemsHeader.Appearance.Options.UseFont = true;
        _lblOrderedItemsHeader.Appearance.Options.UseForeColor = true;
        _lblOrderedItemsHeader.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _lblOrderedItemsHeader.Appearance.Options.UseTextOptions = true;

        _pnlOrderedItemsContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(248, 250, 252) // slate-50 background for empty space
        };
        _flowOrderedItems = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.White,
            Padding = new Padding(0, 2, 0, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(4, this)),
            AutoSize = false
        };
        _flowOrderedItems.Resize += (s, e) => SizeOrderedItemRows();
        _pnlOrderedItemsContainer.Controls.Clear();
        
        // Line-editing strip (Horizontal TableLayoutPanel Toolbar)
        pnlCartActions.Visible = true;
        pnlCartActions.AutoSize = false;
        pnlCartActions.Dock = DockStyle.Bottom; // Pinned to the bottom of the ordered items viewport container
        pnlCartActions.Padding = new Padding(0);

        pnlCartActions.Controls.Clear();
        var tlpCartActions = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 7,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(2, 2, 2, 2),
            BackColor = Color.FromArgb(248, 250, 252) // slate-50 background
        };
        for (int i = 0; i < 7; i++)
        {
            tlpCartActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / 7F));
        }
        tlpCartActions.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        // Use clear, professional short text labels and descriptive tooltips
        _decreaseQuantityButton.Text = "-";
        _decreaseQuantityButton.ToolTip = "Decrease Quantity";
        _increaseQuantityButton.Text = "+";
        _increaseQuantityButton.ToolTip = "Increase Quantity";
        _editQuantityButton.Text = "Qty";
        _editQuantityButton.ToolTip = "Edit Quantity";
        _editLineNotesButton.Text = "Notes";
        _editLineNotesButton.ToolTip = "Add Notes";
        _overridePriceButton.Text = "Price";
        _overridePriceButton.ToolTip = "Override Price";
        _voidLineButton.Text = "Void";
        _voidLineButton.ToolTip = "Void Item";
        _removeLineButton.Text = "Delete";
        _removeLineButton.ToolTip = "Delete Item";

        var actionBtns = new Control[] {
            _decreaseQuantityButton,
            _increaseQuantityButton,
            _editQuantityButton,
            _editLineNotesButton,
            _overridePriceButton,
            _voidLineButton,
            _removeLineButton
        };

        for (int i = 0; i < actionBtns.Length; i++)
        {
            var btn = actionBtns[i];
            btn.Parent = null; // Unparent first to prevent conflicts
            btn.Dock = DockStyle.Fill;
            btn.Margin = new Padding(2, 1, 2, 1);
            btn.Padding = new Padding(0);
            btn.MinimumSize = new Size(0, 0);
            btn.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            if (btn is SimpleButton sb)
            {
                sb.ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat;
                sb.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                sb.Appearance.BackColor = Color.White;
                sb.Appearance.BorderColor = Color.FromArgb(203, 213, 225);
                sb.Appearance.ForeColor = Color.FromArgb(51, 65, 85);
                sb.Appearance.Options.UseFont = true;
                sb.Appearance.Options.UseBackColor = true;
                sb.Appearance.Options.UseBorderColor = true;
                sb.Appearance.Options.UseForeColor = true;
            }
            tlpCartActions.Controls.Add(btn, i, 0);
        }
        pnlCartActions.Controls.Add(tlpCartActions);
        pnlCartActions.Height = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(32, this);

        // Add to parent container. With Fill and Bottom docking:
        _pnlOrderedItemsContainer.Controls.Add(pnlCartActions);
        _pnlOrderedItemsContainer.Controls.Add(_flowOrderedItems);
        
        _pnlOrderedItemsContainer.Resize += (s, e) =>
        {
            SizeOrderedItemRows();
        };

        _lineGrid.Visible = false;
        _pnlOrderedItemsContainer.Controls.Add(_lineGrid);

        foreach (Control control in flowCartActions.Controls)
        {
            if (control is SimpleButton btn)
            {
                btn.MinimumSize = new Size(0, 26);
                btn.Padding = new Padding(8, 4, 8, 4);
                btn.Height = 26;
            }
        }

        // Payment Summary
        pnlTotals.Controls.Clear();
        pnlTotals.Dock = DockStyle.Fill;
        pnlTotals.Height = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(68, this);
        pnlTotals.Padding = new Padding(0, 2, 0, 2);
        pnlTotals.BackColor = Color.White;

        var tlpSummary = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 4,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.Transparent
        };
        tlpSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));  // Col 0: Subtotal/Tax/Total/Balance label
        tlpSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22F));  // Col 1: Subtotal/Tax value
        tlpSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));  // Col 2: Discount/ServiceCharge label
        tlpSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22F));  // Col 3: Discount/ServiceCharge/Total/Balance value

        tlpSummary.RowStyles.Add(new RowStyle(SizeType.Percent, 24F));  // Row 0: Subtotal / Discount
        tlpSummary.RowStyles.Add(new RowStyle(SizeType.Percent, 24F));  // Row 1: Tax / Service Charge
        tlpSummary.RowStyles.Add(new RowStyle(SizeType.Percent, 26F));  // Row 2: TOTAL PAYABLE | amount
        tlpSummary.RowStyles.Add(new RowStyle(SizeType.Percent, 26F));  // Row 3: Balance Due | amount

        // Draw separator line above Total Payable (Row 2)
        tlpSummary.CellPaint += (s, e) =>
        {
            if (e.Row == 2)
            {
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1.5F); // slate-200 line
                e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Top, e.CellBounds.Right, e.CellBounds.Top);
            }
        };

        LabelControl SummaryLabel(string labelText, bool isBold = false, int leftGap = 2)
        {
            var lbl = new LabelControl { Text = labelText, Dock = DockStyle.Fill, AutoSizeMode = LabelAutoSizeMode.None, Margin = new Padding(0), Padding = new Padding(Clovent.Desktop.Forms.Base.DesktopDpi.Scale(leftGap, this), 0, 2, 0) };
            lbl.Appearance.Font = new Font("Segoe UI", isBold ? 9.5F : 8.5F, isBold ? FontStyle.Bold : FontStyle.Regular);
            lbl.Appearance.ForeColor = isBold ? Color.FromArgb(15, 23, 42) : Color.FromArgb(100, 116, 139);
            lbl.Appearance.Options.UseFont = true;
            lbl.Appearance.Options.UseForeColor = true;
            lbl.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Near;
            lbl.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            lbl.Appearance.Options.UseTextOptions = true;
            return lbl;
        }

        LabelControl SummaryValue(bool isBold = false, int rightGap = 8, Color? foreColor = null)
        {
            var lbl = new LabelControl { Text = "0.00", Dock = DockStyle.Fill, AutoSizeMode = LabelAutoSizeMode.None, Margin = new Padding(0), Padding = new Padding(2, 0, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(rightGap, this), 0) };
            lbl.Appearance.Font = new Font("Segoe UI", isBold ? 10F : 8.5F, isBold ? FontStyle.Bold : FontStyle.Regular);
            lbl.Appearance.ForeColor = foreColor ?? (isBold ? Color.FromArgb(13, 148, 136) : Color.FromArgb(15, 23, 42));
            lbl.Appearance.Options.UseFont = true;
            lbl.Appearance.Options.UseForeColor = true;
            lbl.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            lbl.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            lbl.Appearance.Options.UseTextOptions = true;
            return lbl;
        }

        _lblSummarySubtotal = SummaryValue();
        _lblSummaryDiscount = SummaryValue();
        _lblSummaryTax = SummaryValue();
        _lblSummaryService = SummaryValue();
        _lblSummaryTotalPayable = SummaryValue(isBold: true, foreColor: Color.FromArgb(13, 148, 136));

        // Row 0
        tlpSummary.Controls.Add(SummaryLabel("Subtotal"), 0, 0);
        tlpSummary.Controls.Add(_lblSummarySubtotal, 1, 0);
        tlpSummary.Controls.Add(SummaryLabel("Discount"), 2, 0);
        tlpSummary.Controls.Add(_lblSummaryDiscount, 3, 0);

        // Row 1
        tlpSummary.Controls.Add(SummaryLabel("Tax"), 0, 1);
        tlpSummary.Controls.Add(_lblSummaryTax, 1, 1);
        tlpSummary.Controls.Add(SummaryLabel("Service Charge"), 2, 1);
        tlpSummary.Controls.Add(_lblSummaryService, 3, 1);

        // Row 2: TOTAL PAYABLE (left) and amount (right)
        var lblTotalPayableText = SummaryLabel("TOTAL PAYABLE", isBold: true);
        tlpSummary.Controls.Add(lblTotalPayableText, 0, 2);
        tlpSummary.SetColumnSpan(lblTotalPayableText, 3);
        tlpSummary.Controls.Add(_lblSummaryTotalPayable, 3, 2);

        // Row 3: Balance Due (left) and amount (right)
        var lblBalanceDueText = SummaryLabel("Balance Due", isBold: true);
        lblBalanceDueText.Appearance.ForeColor = Color.FromArgb(71, 85, 105);
        _paymentBalanceLabel.Dock = DockStyle.Fill;
        _paymentBalanceLabel.AutoSizeMode = LabelAutoSizeMode.None;
        _paymentBalanceLabel.Margin = new Padding(0);
        _paymentBalanceLabel.Padding = new Padding(2, 0, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(8, this), 0);
        _paymentBalanceLabel.Appearance.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _paymentBalanceLabel.Appearance.ForeColor = Color.FromArgb(30, 41, 59);
        _paymentBalanceLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        _paymentBalanceLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _paymentBalanceLabel.Appearance.Options.UseFont = true;
        _paymentBalanceLabel.Appearance.Options.UseForeColor = true;
        _paymentBalanceLabel.Appearance.Options.UseTextOptions = true;

        tlpSummary.Controls.Add(lblBalanceDueText, 0, 3);
        tlpSummary.SetColumnSpan(lblBalanceDueText, 3);
        tlpSummary.Controls.Add(_paymentBalanceLabel, 3, 3);

        pnlTotals.Controls.Add(tlpSummary);

        // Bottom actions in two clean, equal-height rows:
        // Row 0: [ Hold ] [ Recall ]
        // Row 1: [ Record Payment ] [ Split Payment ] [ Print Bill ] [ Place Order ]
        var pnlRightBottomActions = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.White
        };
        pnlRightBottomActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        pnlRightBottomActions.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        pnlRightBottomActions.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        int actionGap = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(1, this);

        var pnlHoldRecallRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(2, this)),
            Padding = new Padding(0),
            BackColor = Color.White
        };
        pnlHoldRecallRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));  // Hold
        pnlHoldRecallRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));  // Recall
        pnlHoldRecallRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));  // Clear
        pnlHoldRecallRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var pnlCheckoutRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.White
        };
        pnlCheckoutRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));  // Record Payment
        pnlCheckoutRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 26F));  // Split Payment
        pnlCheckoutRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16F));  // Print Bill
        pnlCheckoutRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22F));  // Place Order
        pnlCheckoutRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _holdButton.Parent = null;
        _recallButton.Parent = null;
        _clearButton.Parent = null;

        _holdButton.Text = "Hold";
        _holdButton.Dock = DockStyle.Fill;
        _holdButton.Margin = new Padding(0, 0, actionGap, 0);
        _holdButton.MinimumSize = new Size(0, 0);
        _holdButton.Padding = new Padding(0);
        _holdButton.Cursor = Cursors.Hand;
        _holdButton.AllowFocus = false;
        _holdButton.ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat;
        _holdButton.Appearance.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        _holdButton.Appearance.BackColor = Color.FromArgb(217, 119, 6); // Amber-600
        _holdButton.Appearance.ForeColor = Color.White;
        _holdButton.Appearance.BorderColor = Color.FromArgb(217, 119, 6);
        _holdButton.Appearance.Options.UseFont = true;
        _holdButton.Appearance.Options.UseBackColor = true;
        _holdButton.Appearance.Options.UseForeColor = true;
        _holdButton.Appearance.Options.UseBorderColor = true;
        _holdButton.Appearance.TextOptions.Trimming = DevExpress.Utils.Trimming.None;
        _holdButton.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        _holdButton.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _holdButton.Appearance.Options.UseTextOptions = true;

        _recallButton.Text = "Recall";
        _recallButton.Dock = DockStyle.Fill;
        _recallButton.Margin = new Padding(actionGap, 0, actionGap, 0);
        _recallButton.MinimumSize = new Size(0, 0);
        _recallButton.Padding = new Padding(0);
        _recallButton.Cursor = Cursors.Hand;
        _recallButton.AllowFocus = false;
        _recallButton.ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat;
        _recallButton.Appearance.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        _recallButton.Appearance.BackColor = Color.FromArgb(79, 70, 229); // Indigo-600
        _recallButton.Appearance.ForeColor = Color.White;
        _recallButton.Appearance.BorderColor = Color.FromArgb(79, 70, 229);
        _recallButton.Appearance.Options.UseFont = true;
        _recallButton.Appearance.Options.UseBackColor = true;
        _recallButton.Appearance.Options.UseForeColor = true;
        _recallButton.Appearance.Options.UseBorderColor = true;
        _recallButton.Appearance.TextOptions.Trimming = DevExpress.Utils.Trimming.None;
        _recallButton.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        _recallButton.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _recallButton.Appearance.Options.UseTextOptions = true;

        _clearButton.Text = "Clear";
        _clearButton.Dock = DockStyle.Fill;
        _clearButton.Margin = new Padding(actionGap, 0, 0, 0);
        _clearButton.MinimumSize = new Size(0, 0);
        _clearButton.Padding = new Padding(0);
        _clearButton.Cursor = Cursors.Hand;
        _clearButton.AllowFocus = false;
        _clearButton.ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat;
        _clearButton.Appearance.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        _clearButton.Appearance.BackColor = Color.White;
        _clearButton.Appearance.ForeColor = Color.FromArgb(71, 85, 105); // Slate-600
        _clearButton.Appearance.BorderColor = Color.FromArgb(203, 213, 225); // Slate-300
        _clearButton.Appearance.Options.UseFont = true;
        _clearButton.Appearance.Options.UseBackColor = true;
        _clearButton.Appearance.Options.UseForeColor = true;
        _clearButton.Appearance.Options.UseBorderColor = true;
        _clearButton.Appearance.TextOptions.Trimming = DevExpress.Utils.Trimming.None;
        _clearButton.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        _clearButton.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _clearButton.Appearance.Options.UseTextOptions = true;

        _holdButton.Click -= HoldButton_Click;
        _holdButton.Click += HoldButton_Click;
        _recallButton.Click -= RecallButton_Click;
        _recallButton.Click += RecallButton_Click;
        _clearButton.Click -= ClearButton_Click;
        _clearButton.Click += ClearButton_Click;

        pnlHoldRecallRow.Controls.Add(_holdButton, 0, 0);
        pnlHoldRecallRow.Controls.Add(_recallButton, 1, 0);
        pnlHoldRecallRow.Controls.Add(_clearButton, 2, 0);

        var btnPrint = new SimpleButton
        {
            Text = "Print Bill",
            Dock = DockStyle.Fill,
            Margin = new Padding(actionGap, 0, actionGap, 0),
            Padding = new Padding(0),
            Cursor = Cursors.Hand,
            AllowFocus = false,
            ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat
        };
        btnPrint.Appearance.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        btnPrint.Appearance.BackColor = Color.White;
        btnPrint.Appearance.ForeColor = Color.FromArgb(71, 85, 105);
        btnPrint.Appearance.BorderColor = Color.FromArgb(203, 213, 225);
        btnPrint.Appearance.Options.UseFont = true;
        btnPrint.Appearance.Options.UseBackColor = true;
        btnPrint.Appearance.Options.UseForeColor = true;
        btnPrint.Appearance.Options.UseBorderColor = true;
        btnPrint.Appearance.TextOptions.Trimming = DevExpress.Utils.Trimming.None;
        btnPrint.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        btnPrint.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        btnPrint.Appearance.Options.UseTextOptions = true;
        btnPrint.Click += PrintBillButton_Click;

        var btnPlaceOrder = new SimpleButton
        {
            Text = "Place Order",
            Dock = DockStyle.Fill,
            Margin = new Padding(actionGap, 0, 0, 0),
            Padding = new Padding(0),
            Cursor = Cursors.Hand,
            AllowFocus = false,
            ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat
        };
        btnPlaceOrder.Appearance.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        btnPlaceOrder.Appearance.BackColor = Color.FromArgb(13, 148, 136);
        btnPlaceOrder.Appearance.ForeColor = Color.White;
        btnPlaceOrder.Appearance.BorderColor = Color.FromArgb(13, 148, 136);
        btnPlaceOrder.Appearance.Options.UseFont = true;
        btnPlaceOrder.Appearance.Options.UseBackColor = true;
        btnPlaceOrder.Appearance.Options.UseForeColor = true;
        btnPlaceOrder.Appearance.Options.UseBorderColor = true;
        btnPlaceOrder.Appearance.TextOptions.Trimming = DevExpress.Utils.Trimming.None;
        btnPlaceOrder.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        btnPlaceOrder.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        btnPlaceOrder.Appearance.Options.UseTextOptions = true;

        btnPlaceOrder.Click += async (s, e) => {
            if (_currentOrder is null) return;
            await EnsureOrderResumedIfHeldAsync();
            var totals = await _mediator.Send(new GetOrderSummaryQuery(_currentOrder.OrderId));
            if (totals.Balance > 0.005m)
            {
                ShowPaymentDialog();
            }
            else
            {
                await TryRunAsync(CompleteAsync, "complete this order");
            }
        };

        // Record Payment / Split Payment join Print Bill / Place Order in the checkout action row
        _recordButton.Parent = null;
        _splitPaymentButton.Parent = null;

        _recordButton.Dock = DockStyle.Fill;
        _recordButton.Margin = new Padding(0, 0, actionGap, 0);
        _recordButton.MinimumSize = new Size(0, 0);
        _recordButton.Padding = new Padding(0);
        _recordButton.Appearance.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        _recordButton.Appearance.Options.UseFont = true;
        _recordButton.Appearance.TextOptions.Trimming = DevExpress.Utils.Trimming.None;
        _recordButton.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        _recordButton.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _recordButton.Appearance.Options.UseTextOptions = true;

        _splitPaymentButton.Dock = DockStyle.Fill;
        _splitPaymentButton.Margin = new Padding(actionGap, 0, actionGap, 0);
        _splitPaymentButton.MinimumSize = new Size(0, 0);
        _splitPaymentButton.Padding = new Padding(0);
        _splitPaymentButton.Appearance.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        _splitPaymentButton.Appearance.Options.UseFont = true;
        _splitPaymentButton.Appearance.TextOptions.Trimming = DevExpress.Utils.Trimming.None;
        _splitPaymentButton.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        _splitPaymentButton.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _splitPaymentButton.Appearance.Options.UseTextOptions = true;

        pnlCheckoutRow.Controls.Add(_recordButton, 0, 0);
        pnlCheckoutRow.Controls.Add(_splitPaymentButton, 1, 0);
        pnlCheckoutRow.Controls.Add(btnPrint, 2, 0);
        pnlCheckoutRow.Controls.Add(btnPlaceOrder, 3, 0);

        pnlRightBottomActions.Controls.Add(pnlHoldRecallRow, 0, 0);
        pnlRightBottomActions.Controls.Add(pnlCheckoutRow, 0, 1);


        // Customer context row with dedicated bottom separator
        int customerRowH = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(32, this);
        int sepRowH = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(8, this);

        pnlOrderContext.Dock = DockStyle.Fill;
        pnlOrderContext.AutoSize = false;
        pnlOrderContext.Margin = new Padding(0);
        pnlOrderContext.Padding = new Padding(0);
        pnlOrderContext.BackColor = Color.White;
        pnlOrderContext.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(203, 213, 225), 1F);
            int y = pnlOrderContext.Height - 1;
            e.Graphics.DrawLine(pen, 0, y, pnlOrderContext.Width, y);
        };

        tlpOrderContext.SuspendLayout();
        tlpOrderContext.Controls.Clear();
        tlpOrderContext.ColumnStyles.Clear();
        tlpOrderContext.RowStyles.Clear();
        tlpOrderContext.ColumnCount = 1;
        tlpOrderContext.RowCount = 2;
        tlpOrderContext.AutoSize = false;
        tlpOrderContext.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlpOrderContext.RowStyles.Add(new RowStyle(SizeType.Absolute, customerRowH));
        tlpOrderContext.RowStyles.Add(new RowStyle(SizeType.Absolute, sepRowH));

        // Customer row: Percent column for picker + fixed-width column for "+ New"
        _customerContainer.SuspendLayout();
        _customerContainer.AutoSize = false;
        _customerContainer.Dock = DockStyle.Fill;
        _customerContainer.Margin = new Padding(0);
        _customerContainer.Padding = new Padding(0);
        _customerContainer.ColumnStyles.Clear();
        _customerContainer.RowStyles.Clear();
        _customerContainer.ColumnCount = 2;
        _customerContainer.RowCount = 1;
        _customerContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _customerContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(68, this)));
        _customerContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _customerPicker.Dock = DockStyle.Fill;
        _customerPicker.Margin = new Padding(0, 0, 6, 0);
        _customerPicker.Properties.AutoHeight = false;
        _customerPicker.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;
        _customerPicker.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;
        _customerPicker.Properties.Appearance.BorderColor = Color.FromArgb(203, 213, 225);
        _customerPicker.Properties.Appearance.Options.UseBorderColor = true;

        _newCustomerButton.Dock = DockStyle.Fill;
        _newCustomerButton.Margin = new Padding(0);
        _newCustomerButton.Padding = new Padding(0);
        _newCustomerButton.AutoSize = false;
        _newCustomerButton.MinimumSize = new Size(0, 0);
        _newCustomerButton.ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;
        _newCustomerButton.Appearance.BorderColor = Color.FromArgb(203, 213, 225);
        _newCustomerButton.Appearance.Options.UseBorderColor = true;
        _newCustomerButton.Appearance.BackColor = Color.White;
        _newCustomerButton.Appearance.Options.UseBackColor = true;
        _newCustomerButton.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
        _newCustomerButton.Appearance.Options.UseForeColor = true;
        _newCustomerButton.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        _newCustomerButton.Appearance.Options.UseFont = true;

        _customerContainer.Controls.Add(_customerPicker, 0, 0);
        _customerContainer.Controls.Add(_newCustomerButton, 1, 0);
        _customerContainer.ResumeLayout(true);

        _tablePicker.Visible = true;

        // Dedicated 1px separator with explicit background color — clearly visible border
        var customerSeparator = new Panel
        {
            Height = 1,
            Dock = DockStyle.None,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Margin = new Padding(0, 3, 0, 4),
            BackColor = Color.FromArgb(203, 213, 225) // slate-300
        };

        tlpOrderContext.Controls.Add(_customerContainer, 0, 0);
        tlpOrderContext.Controls.Add(customerSeparator, 0, 1);
        tlpOrderContext.ResumeLayout(true);

        _lblCartTableNo.Parent = null;
        _lblCartOrderNo.Parent = null;
        _lblOrderedItemsHeader.Parent = null;

        var pnlCartHeaderCombined = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.White
        };
        pnlCartHeaderCombined.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
        pnlCartHeaderCombined.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
        pnlCartHeaderCombined.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
        pnlCartHeaderCombined.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _tablePicker.Dock = DockStyle.Fill;
        _tablePicker.Margin = new Padding(0, 1, 4, 1);

        _lblCartOrderNo.Dock = DockStyle.Fill;
        _lblCartOrderNo.Margin = new Padding(0);
        _lblCartOrderNo.AutoSizeMode = LabelAutoSizeMode.None;
        _lblCartOrderNo.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
        _lblCartOrderNo.Appearance.ForeColor = Color.FromArgb(100, 116, 139);
        _lblCartOrderNo.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        _lblCartOrderNo.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _lblCartOrderNo.Appearance.Options.UseFont = true;
        _lblCartOrderNo.Appearance.Options.UseForeColor = true;
        _lblCartOrderNo.Appearance.Options.UseTextOptions = true;

        _lblOrderedItemsHeader.Dock = DockStyle.Fill;
        _lblOrderedItemsHeader.Margin = new Padding(0);
        _lblOrderedItemsHeader.AutoSizeMode = LabelAutoSizeMode.None;
        _lblOrderedItemsHeader.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _lblOrderedItemsHeader.Appearance.ForeColor = Color.FromArgb(71, 85, 105);
        _lblOrderedItemsHeader.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        _lblOrderedItemsHeader.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _lblOrderedItemsHeader.Appearance.Options.UseFont = true;
        _lblOrderedItemsHeader.Appearance.Options.UseForeColor = true;
        _lblOrderedItemsHeader.Appearance.Options.UseTextOptions = true;

        pnlCartHeaderCombined.Controls.Add(_tablePicker, 0, 0);
        pnlCartHeaderCombined.Controls.Add(_lblCartOrderNo, 1, 0);
        pnlCartHeaderCombined.Controls.Add(_lblOrderedItemsHeader, 2, 0);

        _methodsHeaderLabel.Parent = null;
        _methodButtonsFlow.Parent = null;

        _paymentMethodLookup = new LookUpEdit
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0)
        };
        _paymentMethodLookup.Properties.AutoHeight = false;
        _paymentMethodLookup.Properties.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _paymentMethodLookup.Properties.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
        _paymentMethodLookup.Properties.Appearance.Options.UseFont = true;
        _paymentMethodLookup.Properties.Appearance.Options.UseForeColor = true;
        _paymentMethodLookup.Properties.AppearanceDropDown.Font = new Font("Segoe UI", 9.5F);
        _paymentMethodLookup.Properties.AppearanceDropDown.Options.UseFont = true;
        _paymentMethodLookup.Properties.NullText = "Select Payment Method";
        _paymentMethodLookup.Properties.ShowHeader = false;
        _paymentMethodLookup.Properties.ShowFooter = false;

        // Single-row payment selector: label on the LEFT, dropdown on the RIGHT
        // of the SAME row. "Balance Due" lives in the totals block instead.
        pnlPaymentMethods.AutoSize = false;
        pnlPaymentMethods.Dock = DockStyle.Fill;
        pnlPaymentMethods.Padding = new Padding(0);
        pnlPaymentMethods.SuspendLayout();
        pnlPaymentMethods.Controls.Clear();
        pnlPaymentMethods.ColumnCount = 2;
        pnlPaymentMethods.RowCount = 1;
        pnlPaymentMethods.ColumnStyles.Clear();
        pnlPaymentMethods.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        pnlPaymentMethods.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        pnlPaymentMethods.RowStyles.Clear();
        pnlPaymentMethods.RowStyles.Add(new RowStyle(SizeType.Absolute, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(32, this)));

        _methodsHeaderLabel.Text = "PAYMENT METHOD";
        _methodsHeaderLabel.Dock = DockStyle.Fill;
        _methodsHeaderLabel.AutoSizeMode = LabelAutoSizeMode.None;
        _methodsHeaderLabel.Padding = new Padding(0, 0, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(8, this), 0);
        _methodsHeaderLabel.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        _methodsHeaderLabel.Appearance.ForeColor = Color.FromArgb(71, 85, 105);
        _methodsHeaderLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Near;
        _methodsHeaderLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _methodsHeaderLabel.Appearance.Options.UseFont = true;
        _methodsHeaderLabel.Appearance.Options.UseForeColor = true;
        _methodsHeaderLabel.Appearance.Options.UseTextOptions = true;

        pnlPaymentMethods.Controls.Add(_methodsHeaderLabel, 0, 0);
        pnlPaymentMethods.Controls.Add(_paymentMethodLookup, 1, 0);
        pnlPaymentMethods.ResumeLayout(true);


        // Single-row tender strip:
        // AMOUNT TENDERED | 5,822.00 | [Exact] | CHANGE | 0.00
        pnlAmountTendered.SuspendLayout();
        pnlAmountTendered.Dock = DockStyle.Fill;
        pnlAmountTendered.AutoSize = false;
        pnlAmountTendered.Margin = new Padding(0);
        pnlAmountTendered.Padding = new Padding(0);
        pnlAmountTendered.Controls.Clear();
        pnlAmountTendered.ColumnStyles.Clear();
        pnlAmountTendered.RowStyles.Clear();
        pnlAmountTendered.ColumnCount = 5;
        pnlAmountTendered.RowCount = 1;
        pnlAmountTendered.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));                                                            // label
        pnlAmountTendered.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));                                                        // amount edit
        pnlAmountTendered.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(62, this)));     // Exact
        pnlAmountTendered.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));                                                            // CHANGE label
        pnlAmountTendered.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));                                                        // change value
        pnlAmountTendered.RowStyles.Add(new RowStyle(SizeType.Absolute, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(32, this)));

        _amountTitleLabel.Dock = DockStyle.None;
        _amountTitleLabel.AutoSize = true;
        _amountTitleLabel.Anchor = AnchorStyles.Left; // vertically centered in the row
        _amountTitleLabel.Margin = new Padding(0, 0, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(6, this), 0);
        _amountTitleLabel.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        _amountTitleLabel.Appearance.ForeColor = Color.FromArgb(71, 85, 105);
        _amountTitleLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Near;
        _amountTitleLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _amountTitleLabel.Appearance.Options.UseFont = true;
        _amountTitleLabel.Appearance.Options.UseForeColor = true;
        _amountTitleLabel.Appearance.Options.UseTextOptions = true;

        _changeTitleLabel.Dock = DockStyle.None;
        _changeTitleLabel.AutoSize = true;
        _changeTitleLabel.Anchor = AnchorStyles.Left; // vertically centered in the row
        _changeTitleLabel.Margin = new Padding(0, 0, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(6, this), 0);
        _changeTitleLabel.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        _changeTitleLabel.Appearance.ForeColor = Color.FromArgb(71, 85, 105);
        _changeTitleLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Near;
        _changeTitleLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _changeTitleLabel.Appearance.Options.UseFont = true;
        _changeTitleLabel.Appearance.Options.UseForeColor = true;
        _changeTitleLabel.Appearance.Options.UseTextOptions = true;

        _amountEdit.Dock = DockStyle.Fill;
        _amountEdit.Margin = new Padding(0);
        _amountEdit.Properties.AutoHeight = false;
        _amountEdit.Properties.Appearance.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        _amountEdit.Properties.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
        _amountEdit.Properties.Appearance.Options.UseFont = true;
        _amountEdit.Properties.Appearance.Options.UseForeColor = true;

        _changeValueLabel.Dock = DockStyle.Fill;
        _changeValueLabel.AutoSizeMode = LabelAutoSizeMode.None;
        _changeValueLabel.Margin = new Padding(0);
        _changeValueLabel.Padding = new Padding(0);
        _changeValueLabel.Appearance.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        _changeValueLabel.Appearance.ForeColor = Color.FromArgb(13, 148, 136);
        _changeValueLabel.Appearance.Options.UseFont = true;
        _changeValueLabel.Appearance.Options.UseForeColor = true;
        _changeValueLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        _changeValueLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _changeValueLabel.Appearance.Options.UseTextOptions = true;

        if (_exactAmountButton != null)
        {
            _exactAmountButton.Dock = DockStyle.Fill;
            _exactAmountButton.Margin = new Padding(Clovent.Desktop.Forms.Base.DesktopDpi.Scale(4, this), 0, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(4, this), 0);
            _exactAmountButton.Padding = new Padding(0);
            _exactAmountButton.MinimumSize = new Size(0, 0);
            _exactAmountButton.AutoSize = false;
        }

        pnlAmountTendered.Controls.Add(_amountTitleLabel, 0, 0);
        pnlAmountTendered.Controls.Add(_amountEdit, 1, 0);
        if (_exactAmountButton != null) pnlAmountTendered.Controls.Add(_exactAmountButton, 2, 0);
        pnlAmountTendered.Controls.Add(_changeTitleLabel, 3, 0);
        pnlAmountTendered.Controls.Add(_changeValueLabel, 4, 0);
        pnlAmountTendered.ResumeLayout(true);

        // Horizontal Cash Input Module: Keypad (Left, 3x5) + Quick Cash (Right, 2x3)
        int keypadW = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(210, this);
        int quickCashW = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(140, this);
        int cashModuleH = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(112, this);

        // 1. Numeric Keypad setup (Left: 3 columns, 5 rows)
        pnlKeypad.SuspendLayout();
        pnlKeypad.Dock = DockStyle.Fill;
        pnlKeypad.Margin = new Padding(0, 0, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(4, this), 0);
        pnlKeypad.Padding = new Padding(0);
        pnlKeypad.ColumnCount = 3;
        pnlKeypad.RowCount = 5;
        pnlKeypad.ColumnStyles.Clear();
        pnlKeypad.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
        pnlKeypad.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        pnlKeypad.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        pnlKeypad.RowStyles.Clear();
        pnlKeypad.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
        pnlKeypad.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
        pnlKeypad.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
        pnlKeypad.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
        pnlKeypad.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));

        pnlKeypad.Controls.Clear();
        pnlKeypad.Controls.Add(_keypad7Button, 0, 0);
        pnlKeypad.Controls.Add(_keypad8Button, 1, 0);
        pnlKeypad.Controls.Add(_keypad9Button, 2, 0);
        pnlKeypad.Controls.Add(_keypad4Button, 0, 1);
        pnlKeypad.Controls.Add(_keypad5Button, 1, 1);
        pnlKeypad.Controls.Add(_keypad6Button, 2, 1);
        pnlKeypad.Controls.Add(_keypad1Button, 0, 2);
        pnlKeypad.Controls.Add(_keypad2Button, 1, 2);
        pnlKeypad.Controls.Add(_keypad3Button, 2, 2);
        pnlKeypad.Controls.Add(_keypadDecimalButton, 0, 3);
        pnlKeypad.Controls.Add(_keypad0Button, 1, 3);
        pnlKeypad.Controls.Add(_keypadBackspaceButton, 2, 3);
        pnlKeypad.Controls.Add(_keypadClearButton, 0, 4);
        pnlKeypad.SetColumnSpan(_keypadClearButton, 3);

        var keypadButtons = new[]
        {
            _keypad7Button, _keypad8Button, _keypad9Button,
            _keypad4Button, _keypad5Button, _keypad6Button,
            _keypad1Button, _keypad2Button, _keypad3Button,
            _keypadDecimalButton, _keypad0Button, _keypadBackspaceButton,
            _keypadClearButton
        };

        foreach (var btn in keypadButtons)
        {
            if (btn != null)
            {
                btn.Dock = DockStyle.Fill;
                btn.Margin = new Padding(1);
                btn.Padding = new Padding(0);
                btn.MinimumSize = new Size(0, 0);
                btn.AutoSize = false;
                btn.Appearance.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                btn.Appearance.BackColor = Color.FromArgb(241, 245, 249);
                btn.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
                btn.Appearance.BorderColor = Color.FromArgb(203, 213, 225);
                btn.Appearance.Options.UseFont = true;
                btn.Appearance.Options.UseBackColor = true;
                btn.Appearance.Options.UseForeColor = true;
                btn.Appearance.Options.UseBorderColor = true;
                btn.LookAndFeel.UseDefaultLookAndFeel = false;
                btn.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Flat;
                if (btn.Text == "<-" || btn.Text == "?" || btn.Text == "\u232b")
                {
                    btn.Text = "⌫";
                }
            }
        }
        pnlKeypad.ResumeLayout(true);

        // 2. Quick cash setup (Right: 2 columns, 3 rows)
        pnlQuickCash.SuspendLayout();
        pnlQuickCash.Dock = DockStyle.Fill;
        pnlQuickCash.Margin = new Padding(Clovent.Desktop.Forms.Base.DesktopDpi.Scale(4, this), 0, 0, 0);
        pnlQuickCash.Padding = new Padding(0);
        pnlQuickCash.ColumnCount = 2;
        pnlQuickCash.RowCount = 3;
        pnlQuickCash.ColumnStyles.Clear();
        pnlQuickCash.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        pnlQuickCash.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        pnlQuickCash.RowStyles.Clear();
        pnlQuickCash.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
        pnlQuickCash.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
        pnlQuickCash.RowStyles.Add(new RowStyle(SizeType.Percent, 33.34F));

        pnlQuickCash.Controls.Clear();
        pnlQuickCash.Controls.Add(_quickCash100Button, 0, 0);
        pnlQuickCash.Controls.Add(_quickCash200Button, 1, 0);
        pnlQuickCash.Controls.Add(_quickCash500Button, 0, 1);
        pnlQuickCash.Controls.Add(_quickCash1000Button, 1, 1);
        pnlQuickCash.Controls.Add(_quickCash2000Button, 0, 2);
        pnlQuickCash.Controls.Add(_quickCash5000Button, 1, 2);

        var qcButtons = new[] { _quickCash100Button, _quickCash200Button, _quickCash500Button, _quickCash1000Button, _quickCash2000Button, _quickCash5000Button };
        foreach (var btn in qcButtons)
        {
            if (btn != null)
            {
                btn.Dock = DockStyle.Fill;
                btn.Margin = new Padding(1);
                btn.Padding = new Padding(0);
                btn.MinimumSize = new Size(0, 0);
                btn.AutoSize = false;
                btn.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                btn.Appearance.BackColor = Color.FromArgb(240, 253, 250);
                btn.Appearance.ForeColor = Color.FromArgb(13, 148, 136);
                btn.Appearance.BorderColor = Color.FromArgb(153, 246, 228);
                btn.Appearance.Options.UseFont = true;
                btn.Appearance.Options.UseBackColor = true;
                btn.Appearance.Options.UseForeColor = true;
                btn.Appearance.Options.UseBorderColor = true;
                btn.LookAndFeel.UseDefaultLookAndFeel = false;
                btn.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Flat;
            }
        }
        pnlQuickCash.ResumeLayout(true);

        // 3. Combined Horizontal Cash Module (Keypad Left, Quick Cash Right)
        var pnlCashModule = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.White
        };
        pnlCashModule.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, keypadW));
        pnlCashModule.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, quickCashW));
        pnlCashModule.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        pnlCashModule.Controls.Add(pnlKeypad, 0, 0);
        pnlCashModule.Controls.Add(pnlQuickCash, 1, 0);

        int totalCashModuleW = keypadW + quickCashW;

        // Centering wrapper: centers fixed-size child in row
        Panel MakeCenteringWrapper(Control child, int childWidth, int childHeight)
        {
            var wrapper = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(0),
                BackColor = Color.White
            };
            child.Dock = DockStyle.None;
            child.Anchor = AnchorStyles.None;
            child.Size = new Size(childWidth, childHeight);
            wrapper.Controls.Add(child);
            wrapper.Resize += (_, _) =>
            {
                child.Location = new Point(
                    Math.Max(0, (wrapper.ClientSize.Width - child.Width) / 2),
                    Math.Max(0, (wrapper.ClientSize.Height - child.Height) / 2));
            };
            return wrapper;
        }

        var cashModuleWrapper = MakeCenteringWrapper(pnlCashModule, totalCashModuleW, cashModuleH);

        // Reset bottom action button minimum sizes so they don't force overflow
        if (_recordButton != null)
        {
            _recordButton.MinimumSize = new Size(0, 0);
            _recordButton.Padding = new Padding(0);
            _recordButton.AutoSize = false;
        }
        if (_splitPaymentButton != null)
        {
            _splitPaymentButton.MinimumSize = new Size(0, 0);
            _splitPaymentButton.Padding = new Padding(0);
            _splitPaymentButton.AutoSize = false;
        }

        _tlpRightRows = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 8,
            Margin = new Padding(0),
            BackColor = Color.White
        };
        _tlpRightRows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        int row0H = customerRowH + sepRowH + Clovent.Desktop.Forms.Base.DesktopDpi.Scale(2, this);
        int row1H = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(28, this);
        int row3H = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(60, this);
        int row4H = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(32, this);
        int row5H = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(32, this);
        int row6H = cashModuleH + Clovent.Desktop.Forms.Base.DesktopDpi.Scale(2, this);
        int row7H = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(66, this);

        _tlpRightRows.RowStyles.Add(new RowStyle(SizeType.Absolute, row0H));  // Row 0: Customer context + separator
        _tlpRightRows.RowStyles.Add(new RowStyle(SizeType.Absolute, row1H));  // Row 1: Cart header combined
        _tlpRightRows.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));   // Row 2: Ordered Items viewport (flexible)
        _tlpRightRows.RowStyles.Add(new RowStyle(SizeType.Absolute, row3H));  // Row 3: Payment Summary (60px)
        _tlpRightRows.RowStyles.Add(new RowStyle(SizeType.Absolute, row4H));  // Row 4: Payment Method (32px)
        _tlpRightRows.RowStyles.Add(new RowStyle(SizeType.Absolute, row5H));  // Row 5: Amount Tendered (32px)
        _tlpRightRows.RowStyles.Add(new RowStyle(SizeType.Absolute, row6H));  // Row 6: Keypad (Left) + Quick Cash (Right)
        _tlpRightRows.RowStyles.Add(new RowStyle(SizeType.Absolute, row7H));  // Row 7: Hold/Recall (32px) + Record/Split/Print/Place Order (32px)

        // Draw clean divider lines under sections to visually separate them
        _tlpRightRows.CellPaint += (s, e) =>
        {
            if (e.Row == 0 || e.Row == 1 || e.Row == 2 || e.Row == 3 || e.Row == 4 || e.Row == 5)
            {
                using var pen = new Pen(Color.FromArgb(203, 213, 225), 1F); // slate-300 line for clear separation
                e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
            }
        };

        void AddRightRow(Control control, int row, Padding? margin = null)
        {
            control.Dock = DockStyle.Fill;
            control.Margin = margin ?? new Padding(0);
            _tlpRightRows.Controls.Add(control, 0, row);
        }

        AddRightRow(pnlOrderContext, 0);
        AddRightRow(pnlCartHeaderCombined, 1);
        AddRightRow(_pnlOrderedItemsContainer, 2);
        AddRightRow(pnlTotals, 3);
        AddRightRow(pnlPaymentMethods, 4);
        AddRightRow(pnlAmountTendered, 5);
        AddRightRow(cashModuleWrapper, 6);   // Combined Keypad Left + Quick Cash Right
        AddRightRow(pnlRightBottomActions, 7, new Padding(0, 0, 0, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(2, this)));

        pnlCurrentOrder.Controls.Add(_tlpRightRows);

        // ==========================================
        // 5. MAIN CONTAINER ASSEMBLY (Add to tlpBody)
        // ==========================================
        _sidebarPanel.Dock = DockStyle.Fill;
        pnlProducts.Dock = DockStyle.Fill;
        pnlCurrentOrder.Dock = DockStyle.Fill;

        _tlpBody.Controls.Add(_sidebarPanel, 0, 0);
        _tlpBody.Controls.Add(pnlProducts, 1, 0);
        _tlpBody.Controls.Add(pnlCurrentOrder, 2, 0);

        SetDoubleBuffered(this);
        SetDoubleBuffered(tlpMain);
        SetDoubleBuffered(_tlpBody);
        SetDoubleBuffered(_sidebarPanel);
        SetDoubleBuffered(_sidebarOrdersFlow);
        SetDoubleBuffered(pnlProducts);
        SetDoubleBuffered(tlpCenterRows);
        SetDoubleBuffered(_productViewport);
        SetDoubleBuffered(_productTilesFlow);
        SetDoubleBuffered(_productListFlow);
        SetDoubleBuffered(_categoriesScrollContainer);
        SetDoubleBuffered(_paginationPanel);
        SetDoubleBuffered(pnlCurrentOrder);
        SetDoubleBuffered(_tlpRightRows);
        SetDoubleBuffered(_flowOrderedItems);

        StylePaymentControls();
        ApplyActiveOrdersState();

        tlpMain.ResumeLayout(true);
        this.ResumeLayout(true);
    }

    private static void SetDoubleBuffered(Control? control)
    {
        if (control == null) return;
        typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(control, true, null);
    }

    private void ApplyActiveOrdersState()
    {
        if (_tlpBody == null || _sidebarPanel == null || _cboSidebarFilter == null || _sidebarOrdersFlow == null) 
            return;

        int expandedWidth = (this.ClientSize.Width > 0 && this.ClientSize.Width <= 1100)
            ? Clovent.Desktop.Forms.Base.DesktopDpi.Scale(230, this) 
            : Clovent.Desktop.Forms.Base.DesktopDpi.Scale(250, this);

        int sidebarWidth = _activeOrdersExpanded 
            ? expandedWidth 
            : Clovent.Desktop.Forms.Base.DesktopDpi.Scale(48, this);

        _tlpBody.ColumnStyles[0].Width = sidebarWidth;

        _cboSidebarFilter.Visible = _activeOrdersExpanded;
        _sidebarOrdersFlow.Visible = _activeOrdersExpanded;

        _tlpBody.PerformLayout();
        _sidebarPanel.PerformLayout();
        ApplyProductFilter();
    }

    private DateTime _animStartTime;
    private int _animStartWidth;
    private const int AnimDurationMs = 200;

    private void StartSidebarAnimation(bool expand)
    {
        if (_tlpBody == null || _sidebarPanel == null || _cboSidebarFilter == null || _sidebarOrdersFlow == null)
            return;

        _activeOrdersExpanded = expand;
        Clovent.Desktop.Forms.Base.PosSettingsStore.SaveActiveOrdersCollapsed(!_activeOrdersExpanded);

        int expandedWidth = (this.ClientSize.Width > 0 && this.ClientSize.Width <= 1100)
            ? Clovent.Desktop.Forms.Base.DesktopDpi.Scale(230, this) 
            : Clovent.Desktop.Forms.Base.DesktopDpi.Scale(250, this);

        _sidebarTargetWidth = expand 
            ? expandedWidth 
            : Clovent.Desktop.Forms.Base.DesktopDpi.Scale(48, this);

        _animStartWidth = (int)_tlpBody.ColumnStyles[0].Width;
        _animStartTime = DateTime.UtcNow;

        _categoryButtonsPanel?.SuspendLayout();
        _productTilesFlow?.SuspendLayout();
        _productListFlow?.SuspendLayout();
        _flowOrderedItems?.SuspendLayout();

        if (_sidebarAnimationTimer == null)
        {
            _sidebarAnimationTimer = new System.Windows.Forms.Timer { Interval = 15 };
            _sidebarAnimationTimer.Tick += (s, e) =>
            {
                double elapsed = (DateTime.UtcNow - _animStartTime).TotalMilliseconds;
                double progress = Math.Clamp(elapsed / AnimDurationMs, 0.0, 1.0);
                
                // EaseOutCubic: 1 - (1 - t)^3
                double ease = 1.0 - Math.Pow(1.0 - progress, 3);
                
                int currentWidth = (int)(_animStartWidth + (_sidebarTargetWidth - _animStartWidth) * ease);

                if (progress >= 1.0)
                {
                    currentWidth = _sidebarTargetWidth;
                    _sidebarAnimationTimer.Stop();

                    _categoryButtonsPanel?.ResumeLayout(true);
                    _productTilesFlow?.ResumeLayout(true);
                    _productListFlow?.ResumeLayout(true);
                    _flowOrderedItems?.ResumeLayout(true);
                    UpdateCategoryScrollButtons();
                    SizeOrderedItemRows();
                    SizeSidebarOrderCards();
                    ApplyProductFilter();
                }

                if (_tlpBody != null && _tlpBody.ColumnStyles.Count > 0)
                {
                    _tlpBody.ColumnStyles[0].Width = currentWidth;
                }
                
                bool showDetails = currentWidth > Clovent.Desktop.Forms.Base.DesktopDpi.Scale(130, this);
                if (_cboSidebarFilter.Visible != showDetails) _cboSidebarFilter.Visible = showDetails;
                if (_sidebarOrdersFlow.Visible != showDetails) _sidebarOrdersFlow.Visible = showDetails;
            };
        }

        _sidebarAnimationTimer.Start();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.Alt | Keys.B))
        {
            StartSidebarAnimation(!_activeOrdersExpanded);
            return true;
        }
        if (keyData == (Keys.Control | Keys.Alt | Keys.L))
        {
            SetViewMode("List");
            return true;
        }
        if (keyData == (Keys.Control | Keys.Alt | Keys.G))
        {
            SetViewMode("Grid");
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }
    private void SelectPaymentMethodByName(string name)
    {
        var method = _paymentMethods.FirstOrDefault(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));
        if (method.PaymentMethodId != Guid.Empty)
        {
            _selectedPaymentMethodId = method.PaymentMethodId;
            BuildMethodButtons();
        }
    }

    private void ShowUnderConstruction(string feature)
    {
        XtraMessageBox.Show(this, $"The '{feature}' feature is available in the main Back Office dashboard. Please switch to the main dashboard tab to configure it.", feature, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ShowPaymentDialog()
    {
        if (_currentOrder is null)
        {
            XtraMessageBox.Show(this, "Start a New Dine-In or New Take Away order first.", "No Order Started", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // The payment panel lives inline at the bottom of the right
        // column (see RestructureLayout) - there is no dialog to pop up
        // anymore. Direct the cashier to it and preset the tender amount
        // to the balance, exactly as the old dialog's Load handler did.
        _ = LoadPaymentAsync();
        _amountEdit.Focus();
    }

    private Color GetCartRowBg(int rowIndex, bool isFocused)
    {
        if (isFocused) return Color.FromArgb(204, 251, 241); // teal-100 highlight for selected row
        return (rowIndex % 2 == 1) ? Color.FromArgb(241, 245, 249) : Color.White; // slate-100 / white alternating stripes
    }

    private void UpdateCartSelectionHighlights()
    {
        if (_flowOrderedItems == null) return;
        var focusedLineId = _lineGridView.GetFocusedRow() is OrderLineRow r ? r.OrderLineId : Guid.Empty;
        var rows = _flowOrderedItems.Controls.OfType<TableLayoutPanel>().Where(t => t.Tag is Guid).ToList();
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var isFocused = (row.Tag as Guid?) == focusedLineId;
            row.BackColor = GetCartRowBg(i, isFocused);
        }
    }

    private void RenderOrderedItemsList(IReadOnlyList<OrderLineDto> lines)
    {
        if (_flowOrderedItems is null) return;

        if (_lblOrderedItemsHeader is not null)
        {
            _lblOrderedItemsHeader.Text = lines.Count == 0
                ? "Ordered Items 00"
                : $"Ordered Items   {lines.Count:D2}";
        }

        var focusedLineId = _lineGridView.GetFocusedRow() is OrderLineRow r ? r.OrderLineId : Guid.Empty;

        var existingRows = _flowOrderedItems.Controls.OfType<TableLayoutPanel>().Where(t => t.Tag is Guid).ToList();
        var existingIds = existingRows.Select(t => (Guid)t.Tag).ToList();
        var newIds = lines.Select(l => l.OrderLineId).ToList();

        if (existingIds.SequenceEqual(newIds))
        {
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var row = existingRows[i];
                var isFocused = line.OrderLineId == focusedLineId;
                row.BackColor = GetCartRowBg(i, isFocused);

                if (row.Controls.Count >= 3)
                {
                    if (row.Controls[0] is LabelControl lblQty)
                    {
                        lblQty.Text = $"{line.Quantity:N0}x";
                    }
                    if (row.Controls[2] is LabelControl lblPrice)
                    {
                        lblPrice.Text = CurrencyDisplay.FormatPlain(line.LineTotal);
                    }
                }
            }
            return;
        }

        _flowOrderedItems.SuspendLayout();
        _flowOrderedItems.Controls.Clear();

        for (int i = 0; i < lines.Count; i++)
        {
            _flowOrderedItems.Controls.Add(BuildOrderedItemRow(lines[i], i));
        }

        _flowOrderedItems.ResumeLayout(true);
        _flowOrderedItems.PerformLayout();

        SizeOrderedItemRows();
    }

    private Control BuildOrderedItemRow(OrderLineDto line, int rowIndex)
    {
        var isFocused = _lineGridView.GetFocusedRow() is OrderLineRow r && r.OrderLineId == line.OrderLineId;
        int rowWidth = Math.Max(_flowOrderedItems.ClientSize.Width - _flowOrderedItems.Padding.Horizontal - 2, 260);
        var row = new TableLayoutPanel
        {
            Tag = line.OrderLineId,
            ColumnCount = 4,
            RowCount = 1,
            Width = rowWidth,
            Height = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(36, this),
            AutoSize = false,
            Margin = new Padding(0),
            Padding = new Padding(4, 2, 4, 2),
            BackColor = GetCartRowBg(rowIndex, isFocused)
        };

        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(36, this))); // Qty
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));                                                // Name (Fill)
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(68, this))); // Price
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(32, this))); // Delete

        row.CellPaint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(241, 245, 249), 1F); // slate-100 line
            e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
        };

        var variantId = line.ProductVariantId;
        _variantsById.TryGetValue(variantId, out var v);
        var productName = v is not null ? _productNamesById.GetValueOrDefault(v.ProductId, v.Name) : "(unknown)";
        
        var lblQty = new LabelControl
        {
            Text = $"{line.Quantity:N0}x",
            Dock = DockStyle.Fill,
            AutoSizeMode = LabelAutoSizeMode.None,
            Margin = new Padding(0)
        };
        lblQty.Appearance.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblQty.Appearance.ForeColor = Color.FromArgb(13, 148, 136);
        lblQty.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        lblQty.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lblQty.Appearance.Options.UseFont = true;
        lblQty.Appearance.Options.UseForeColor = true;
        lblQty.Appearance.Options.UseTextOptions = true;

        var namePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 1,
            Padding = new Padding(2, 2, 2, 2),
            Margin = new Padding(0),
            BackColor = Color.Transparent
        };
        namePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        namePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var cleanPortion = v is not null ? GetVariantPosLabel(v, productName) : string.Empty;
        var showPortion = (string.IsNullOrEmpty(cleanPortion)
            || cleanPortion.Equals("Regular", StringComparison.OrdinalIgnoreCase)
            || cleanPortion.Equals(productName, StringComparison.OrdinalIgnoreCase)) ? "" : cleanPortion;
        // Variant ("Half"/"Full") is part of the SAME primary name line.
        var lblProdName = new LabelControl
        {
            Text = string.IsNullOrEmpty(showPortion) ? productName : $"{productName} - {showPortion}",
            Dock = DockStyle.Fill,
            AutoSizeMode = LabelAutoSizeMode.None,
            Margin = new Padding(0)
        };
        lblProdName.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblProdName.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
        lblProdName.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lblProdName.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Near;
        lblProdName.Appearance.TextOptions.Trimming = DevExpress.Utils.Trimming.EllipsisCharacter;
        lblProdName.Appearance.Options.UseFont = true;
        lblProdName.Appearance.Options.UseForeColor = true;
        lblProdName.Appearance.Options.UseTextOptions = true;

        namePanel.Controls.Add(lblProdName, 0, 0);

        var lblPrice = new LabelControl
        {
            Text = CurrencyDisplay.FormatPlain(line.LineTotal),
            Dock = DockStyle.Fill,
            AutoSizeMode = LabelAutoSizeMode.None,
            Margin = new Padding(0)
        };
        lblPrice.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblPrice.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
        lblPrice.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        lblPrice.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lblPrice.Appearance.Options.UseFont = true;
        lblPrice.Appearance.Options.UseForeColor = true;
        lblPrice.Appearance.Options.UseTextOptions = true;

        void SelectThisLine()
        {
            for (int i = 0; i < _lineGridView.RowCount; i++)
            {
                if (_lineGridView.GetRow(i) is OrderLineRow r && r.OrderLineId == line.OrderLineId)
                {
                    _lineGridView.FocusedRowHandle = i;
                    break;
                }
            }
            UpdateCartSelectionHighlights();
        }

        var btnDelete = new SimpleButton
        {
            Text = "✕",
            Dock = DockStyle.Fill,
            Cursor = Cursors.Hand,
            AllowFocus = false,
            ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder,
            Margin = new Padding(0)
        };
        btnDelete.Appearance.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        btnDelete.Appearance.ForeColor = Color.FromArgb(239, 68, 68);
        btnDelete.Appearance.Options.UseFont = true;
        btnDelete.Appearance.Options.UseForeColor = true;
        btnDelete.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        btnDelete.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        btnDelete.Appearance.Options.UseTextOptions = true;
        
        btnDelete.Click += async (s, e) =>
        {
            SelectThisLine();
            await TryRunAsync(RemoveLineAsync, "remove this line");
        };

        row.Controls.Add(lblQty, 0, 0);
        row.Controls.Add(namePanel, 1, 0);
        row.Controls.Add(lblPrice, 2, 0);
        row.Controls.Add(btnDelete, 3, 0);

        row.Click += (s, e) => SelectThisLine();
        lblQty.Click += (s, e) => SelectThisLine();
        namePanel.Click += (s, e) => SelectThisLine();
        lblProdName.Click += (s, e) => SelectThisLine();
        lblPrice.Click += (s, e) => SelectThisLine();

        return row;
    }

    /// <summary>
    /// Rebuilds the Active Orders rail from the live read-side: open orders
    /// first, then held ones. Each card shows the table (or Take Away),
    /// order number, item count, running total, status and relative age -
    /// all from existing queries. Clicking a card loads that order through
    /// the same selection path the table picker uses, so no new order is
    /// ever created by switching tables.
    /// </summary>
    private async Task RefreshActiveOrdersAsync()
    {
        if (_sidebarOrdersFlow is null || IsDisposed || _isRefreshingRail)
        {
            return;
        }

        if (!IsHandleCreated)
        {
            CreateControl();
            if (!IsHandleCreated)
            {
                return;
            }
        }

        _isRefreshingRail = true;
        try
        {
            var tables = await _mediator.Send(new ListAllTablesQuery());
            var tableCodes = tables.ToDictionary(t => t.TableId, t => t.Code);

            var orders = new List<OrderDto>();
            if (_activeOrdersFilter == "TakeAway")
            {
                var openOrders = await _mediator.Send(new ListOpenOrdersQuery());
                var heldOrders = await _mediator.Send(new ListHeldOrdersQuery());
                orders.AddRange(openOrders.Where(o => o.OrderType == "TakeAway"));
                orders.AddRange(heldOrders.Where(o => o.OrderType == "TakeAway"));
            }
            else if (_activeOrdersFilter == "Closed")
            {
                var allOrders = await _mediator.Send(new ListAllOrdersQuery());
                orders.AddRange(allOrders.Where(o => o.Status == "Completed"));
            }
            else if (_activeOrdersFilter == "WaitList")
            {
                orders.AddRange(await _mediator.Send(new ListHeldOrdersQuery()));
            }
            else // "ActiveOrders" (default)
            {
                orders.AddRange(await _mediator.Send(new ListOpenOrdersQuery()));
                orders.AddRange(await _mediator.Send(new ListHeldOrdersQuery()));
            }

            orders = [.. orders.Where(o => o.OrderLineIds.Count > 0).DistinctBy(o => o.OrderId).OrderByDescending(o => o.CreatedAtUtc)];

            // Clear current selection if it does not belong to the selected filter
            if (_currentOrder != null)
            {
                bool belongs = _activeOrdersFilter switch
                {
                    "TakeAway" => _currentOrder.OrderType == "TakeAway" && (_currentOrder.Status == "Open" || _currentOrder.Status == "Held"),
                    "Closed" => _currentOrder.Status == "Completed",
                    "WaitList" => _currentOrder.Status == "Held",
                    _ => _currentOrder.Status == "Open" || _currentOrder.Status == "Held"
                };
                if (!belongs)
                {
                    _currentOrder = null;
                    _tablePicker.SelectId(null);
                    await RefreshOrderAsync();
                }
            }

            var existingCards = _sidebarOrdersFlow.Controls.OfType<DevExpress.XtraEditors.PanelControl>().Where(c => c.Tag is Guid).ToList();
            var existingOrderIds = existingCards.Select(c => (Guid)c.Tag).ToList();
            var newOrderIds = orders.Select(o => o.OrderId).ToList();

            if (existingOrderIds.SequenceEqual(newOrderIds) && orders.Count > 0)
            {
                for (int i = 0; i < orders.Count; i++)
                {
                    var order = orders[i];
                    var card = existingCards[i];
                    var isSelected = _currentOrder?.OrderId == order.OrderId;
                    card.Appearance.BorderColor = isSelected ? Color.FromArgb(13, 148, 136) : Color.FromArgb(226, 232, 240);
                    card.Appearance.Options.UseBorderColor = true;

                    var summary = await _mediator.Send(new GetOrderSummaryQuery(order.OrderId));
                    if (card.Controls.Count > 0 && card.Controls[0] is TableLayoutPanel tlp)
                    {
                        var lblCount = tlp.GetControlFromPosition(0, 1) as LabelControl;
                        if (lblCount != null) lblCount.Text = $"{order.OrderLineIds.Count} items · {RelativeAge(order.CreatedAtUtc)}";
                        var lblTotal = tlp.GetControlFromPosition(1, 1) as LabelControl;
                        if (lblTotal != null) lblTotal.Text = CurrencyDisplay.FormatPlain(summary.GrandTotal);
                    }
                }
                return;
            }

            _sidebarOrdersFlow.SuspendLayout();
            foreach (Control card in _sidebarOrdersFlow.Controls)
            {
                card.Dispose();
            }
            _sidebarOrdersFlow.Controls.Clear();

            foreach (var order in orders)
            {
                var summary = await _mediator.Send(new GetOrderSummaryQuery(order.OrderId));
                _sidebarOrdersFlow.Controls.Add(BuildSidebarOrderCard(order, tableCodes.GetValueOrDefault(order.TableId ?? Guid.Empty), summary));
            }

            if (orders.Count == 0)
            {
                var sidebarEmpty = new DevExpress.XtraEditors.LabelControl
                {
                    Text = _activeOrdersFilter switch
                    {
                        "TakeAway" => "No take away orders.",
                        "Closed" => "No closed orders.",
                        "WaitList" => "No wait list orders.",
                        _ => "No active orders."
                    },
                    Dock = DockStyle.Top,
                    AutoSizeMode = LabelAutoSizeMode.None,
                    Height = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(50, this),
                    Margin = new Padding(4, 8, 4, 0)
                };
                sidebarEmpty.Appearance.Font = new Font("Segoe UI", 9F);
                sidebarEmpty.Appearance.ForeColor = Color.FromArgb(100, 116, 139);
                sidebarEmpty.Appearance.Options.UseFont = true;
                sidebarEmpty.Appearance.Options.UseForeColor = true;
                sidebarEmpty.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                sidebarEmpty.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
                sidebarEmpty.Appearance.Options.UseTextOptions = true;
                _sidebarOrdersFlow.Controls.Add(sidebarEmpty);
            }

            _sidebarOrdersFlow.ResumeLayout(true);
            SizeSidebarOrderCards();
        }
        finally
        {
            _isRefreshingRail = false;
        }
    }

    private Control BuildSidebarOrderCard(OrderDto order, string? tableCode, OrderTotals totals)
    {
        var isSelected = _currentOrder?.OrderId == order.OrderId;
        var isHeld = order.Status == "Held";
        var isTakeAway = order.OrderType == "TakeAway";
        var isCompleted = order.Status == "Completed";

        int cardHeight = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(54, this);
        var card = new DevExpress.XtraEditors.PanelControl
        {
            Height = cardHeight,
            Padding = new Padding(8, 4, 8, 4),
            Cursor = Cursors.Hand,
            Tag = order.OrderId,
            Margin = new Padding(0, 0, 0, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(4, this))
        };

        var backColor = isSelected ? Color.FromArgb(240, 253, 250)
            : isCompleted ? Color.FromArgb(241, 245, 249)
            : Color.White;
        var borderColor = isSelected ? Color.FromArgb(13, 148, 136) : Color.FromArgb(226, 232, 240);

        card.Appearance.BackColor = backColor;
        card.Appearance.Options.UseBackColor = true;
        card.Appearance.BorderColor = borderColor;
        card.Appearance.Options.UseBorderColor = true;
        card.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.Transparent
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        LabelControl Label(string text, float size, FontStyle style, Color color, bool right = false)
        {
            var label = new LabelControl { Text = text, Dock = DockStyle.Fill, AutoSizeMode = LabelAutoSizeMode.None, Margin = new Padding(0) };
            label.Appearance.Font = new Font("Segoe UI", size, style);
            label.Appearance.ForeColor = color;
            label.Appearance.Options.UseFont = true;
            label.Appearance.Options.UseForeColor = true;
            label.Appearance.TextOptions.HAlignment = right ? DevExpress.Utils.HorzAlignment.Far : DevExpress.Utils.HorzAlignment.Near;
            label.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            label.Appearance.TextOptions.Trimming = DevExpress.Utils.Trimming.EllipsisCharacter;
            label.Appearance.Options.UseTextOptions = true;
            return label;
        }

        var (pillText, pillBack, pillFore) = isCompleted
            ? ("Served", Color.FromArgb(220, 252, 231), Color.FromArgb(21, 128, 61))
            : isHeld
                ? ("Wait List", Color.FromArgb(254, 243, 199), Color.FromArgb(161, 98, 7))
                : isTakeAway
                    ? ("Take Away", Color.FromArgb(254, 243, 199), Color.FromArgb(161, 98, 7))
                    : ("Open", Color.FromArgb(204, 251, 241), Color.FromArgb(13, 148, 136));

        string orderTypeText = isTakeAway ? "Take Away" : tableCode is { } code ? $"Table {code}" : "Dine In";
        var title = Label($"{orderTypeText} · {order.OrderNumber}", 8.5F, FontStyle.Bold, Color.FromArgb(15, 23, 42));

        var pill = new LabelControl
        {
            Text = pillText,
            Dock = DockStyle.Right,
            AutoSizeMode = LabelAutoSizeMode.None,
            Width = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(66, this),
            Height = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(18, this),
            Margin = new Padding(0, 1, 0, 1)
        };
        pill.Appearance.Font = new Font("Segoe UI", 7.5F, FontStyle.Bold);
        pill.Appearance.ForeColor = pillFore;
        pill.Appearance.BackColor = pillBack;
        pill.Appearance.Options.UseFont = true;
        pill.Appearance.Options.UseForeColor = true;
        pill.Appearance.Options.UseBackColor = true;
        pill.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        pill.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        pill.Appearance.Options.UseTextOptions = true;

        layout.Controls.Add(title, 0, 0);
        layout.Controls.Add(pill, 1, 0);
        layout.Controls.Add(Label($"{order.OrderLineIds.Count} items · {RelativeAge(order.CreatedAtUtc)}", 7.5F, FontStyle.Regular, Color.FromArgb(100, 116, 139)), 0, 1);
        layout.Controls.Add(Label(CurrencyDisplay.FormatPlain(totals.GrandTotal), 9F, FontStyle.Bold, Color.FromArgb(13, 148, 136), right: true), 1, 1);

        card.Controls.Add(layout);

        void Hook(Control parent)
        {
            parent.Click += async (_, _) => await TryRunAsync(() => LoadOrderFromRailAsync(order), "open this order");
            foreach (Control child in parent.Controls) Hook(child);
        }
        Hook(card);

        return card;
    }

    private void SizeSidebarOrderCards()
    {
        if (_sidebarOrdersFlow is null || _sidebarOrdersFlow.Controls.Count == 0) return;

        int width = Math.Max(_sidebarOrdersFlow.ClientSize.Width - _sidebarOrdersFlow.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth, 160);
        int cardHeight = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(54, this);
        foreach (Control control in _sidebarOrdersFlow.Controls)
        {
            if (control is DevExpress.XtraEditors.PanelControl card)
            {
                if (card.Width != width) card.Width = width;
                if (card.Height != cardHeight && card.Tag is Guid) card.Height = cardHeight;
            }
        }
    }

    private void SizeOrderedItemRows()
    {
        if (_flowOrderedItems is null || _flowOrderedItems.Controls.Count == 0) return;

        int width = Math.Max(_flowOrderedItems.ClientSize.Width - _flowOrderedItems.Padding.Horizontal - 2, 260);
        foreach (Control control in _flowOrderedItems.Controls)
        {
            if (control is Control row && row.Width != width)
            {
                row.Width = width;
            }
        }
    }

    private static string RelativeAge(DateTimeOffset createdUtc)
    {
        var age = DateTimeOffset.UtcNow - createdUtc;
        return age.TotalMinutes < 1 ? "just now"
            : age.TotalMinutes < 60 ? $"{(int)age.TotalMinutes} min ago"
            : age.TotalHours < 24 ? $"{(int)age.TotalHours} hr ago"
            : $"{(int)age.TotalDays} d ago";
    }

    /// <summary>
    /// Loads the rail-clicked order: dine-in orders go through the table
    /// picker selection (the existing load path, which also refreshes the
    /// picker), take-away orders load directly. Never creates an order.
    /// </summary>
    private async Task LoadOrderFromRailAsync(OrderDto order)
    {
        if (_currentOrder != null && _currentOrder.OrderId != order.OrderId && _currentOrderLines.Count > 0)
        {
            var res = XtraMessageBox.Show(this,
                $"Order '{_currentOrder.OrderNumber}' is currently active with items. Would you like to put it on Hold before opening '{order.OrderNumber}'?",
                "Active Order In Progress",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (res == DialogResult.Cancel)
            {
                return;
            }

            if (res == DialogResult.Yes)
            {
                try
                {
                    await _mediator.Send(new HoldOrderCommand(_currentOrder.OrderId));
                    await LogActivityAsync("Hold Order", $"{_currentOrder.OrderNumber}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to hold current order {OrderId} before switching", _currentOrder.OrderId);
                    XtraMessageBox.Show(this, $"Failed to hold current order: {ex.Message}", "Hold Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }

        if (order.Status == "Held")
        {
            order = await _mediator.Send(new ResumeOrderCommand(order.OrderId));
            await LogActivityAsync("Recall Order", $"{order.OrderNumber}");
        }

        if (order.TableId is { } tableId)
        {
            _tablePicker.SelectId(tableId);
            return;
        }

        _currentOrder = await _mediator.Send(new GetOrderByIdQuery(order.OrderId));
        await RefreshOrderAsync();
        await RefreshActiveOrdersAsync();
    }

    private void SetViewMode(string mode)
    {
        _viewMode = mode;
        Clovent.Desktop.Forms.Base.PosSettingsStore.SaveViewMode(mode);

        _btnGridView.Appearance.BackColor = mode == "Grid" ? Color.FromArgb(13, 148, 136) : Color.FromArgb(241, 245, 249);
        _btnGridView.Appearance.ForeColor = mode == "Grid" ? Color.White : Color.FromArgb(71, 85, 105);
        _btnGridView.Appearance.Options.UseBackColor = true;
        _btnGridView.Appearance.Options.UseForeColor = true;

        _btnListView.Appearance.BackColor = mode == "List" ? Color.FromArgb(13, 148, 136) : Color.FromArgb(241, 245, 249);
        _btnListView.Appearance.ForeColor = mode == "List" ? Color.White : Color.FromArgb(71, 85, 105);
        _btnListView.Appearance.Options.UseBackColor = true;
        _btnListView.Appearance.Options.UseForeColor = true;

        _productTilesFlow.Visible = (mode == "Grid");
        _productListFlow.Visible = (mode == "List");

        ApplyProductFilter();
    }

    private void UpdatePaginationUI(int currentPage, int totalPages, int totalProducts)
    {
        if (_lblPageInfo == null || _pnlPageButtons == null || _btnPrevPage == null || _btnNextPage == null) return;
        int start = totalProducts == 0 ? 0 : (currentPage - 1) * _pageSize + 1;
        int end = Math.Min(currentPage * _pageSize, totalProducts);
        _lblPageInfo.Text = $"Showing {start}–{end} of {totalProducts} items";

        _pnlPageButtons.SuspendLayout();
        _pnlPageButtons.Controls.Clear();
        _pnlPageButtons.ColumnStyles.Clear();

        var btns = new List<Control>();

        _btnPrevPage.Enabled = currentPage > 1;
        _btnPrevPage.Appearance.BackColor = _btnPrevPage.Enabled ? Color.FromArgb(71, 85, 105) : Color.FromArgb(241, 245, 249);
        _btnPrevPage.Appearance.ForeColor = _btnPrevPage.Enabled ? Color.White : Color.FromArgb(148, 163, 184);
        _btnPrevPage.Appearance.BorderColor = _btnPrevPage.Enabled ? Color.FromArgb(71, 85, 105) : Color.FromArgb(203, 213, 225);
        _btnPrevPage.Appearance.Options.UseBackColor = true;
        _btnPrevPage.Appearance.Options.UseForeColor = true;
        _btnPrevPage.Appearance.Options.UseBorderColor = true;
        btns.Add(_btnPrevPage);

        // Calculate sliding window for page buttons
        int startPage = Math.Max(1, currentPage - 2);
        int endPage = Math.Min(totalPages, startPage + 4);
        if (endPage - startPage < 4)
        {
            startPage = Math.Max(1, endPage - 4);
        }

        if (startPage > 1)
        {
            btns.Add(CreatePageNumButton(1, currentPage == 1));
            if (startPage > 2)
            {
                var lblEllipsis = new LabelControl { Text = "...", AutoSize = true, Anchor = AnchorStyles.None, Margin = new Padding(4, 0, 4, 0) };
                lblEllipsis.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                lblEllipsis.Appearance.ForeColor = Color.FromArgb(71, 85, 105);
                lblEllipsis.Appearance.Options.UseFont = true;
                lblEllipsis.Appearance.Options.UseForeColor = true;
                btns.Add(lblEllipsis);
            }
        }

        for (int p = startPage; p <= endPage; p++)
        {
            btns.Add(CreatePageNumButton(p, p == currentPage));
        }

        if (endPage < totalPages)
        {
            if (endPage < totalPages - 1)
            {
                var lblEllipsis = new LabelControl { Text = "...", AutoSize = true, Anchor = AnchorStyles.None, Margin = new Padding(4, 0, 4, 0) };
                lblEllipsis.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                lblEllipsis.Appearance.ForeColor = Color.FromArgb(71, 85, 105);
                lblEllipsis.Appearance.Options.UseFont = true;
                lblEllipsis.Appearance.Options.UseForeColor = true;
                btns.Add(lblEllipsis);
            }
            btns.Add(CreatePageNumButton(totalPages, currentPage == totalPages));
        }

        _btnNextPage.Enabled = currentPage < totalPages;
        _btnNextPage.Appearance.BackColor = _btnNextPage.Enabled ? Color.FromArgb(13, 148, 136) : Color.FromArgb(241, 245, 249);
        _btnNextPage.Appearance.ForeColor = _btnNextPage.Enabled ? Color.White : Color.FromArgb(148, 163, 184);
        _btnNextPage.Appearance.BorderColor = _btnNextPage.Enabled ? Color.FromArgb(13, 148, 136) : Color.FromArgb(203, 213, 225);
        _btnNextPage.Appearance.Options.UseBackColor = true;
        _btnNextPage.Appearance.Options.UseForeColor = true;
        _btnNextPage.Appearance.Options.UseBorderColor = true;
        
        _btnPrevPage.Text = "‹ Previous";
        _btnPrevPage.AutoSize = false;
        _btnPrevPage.Dock = DockStyle.Fill;

        _btnNextPage.Text = "Next ›";
        _btnNextPage.AutoSize = false;
        _btnNextPage.Dock = DockStyle.Fill;

        btns.Add(_btnNextPage);

        _pnlPageButtons.ColumnCount = btns.Count;
        for (int i = 0; i < btns.Count; i++)
        {
            if (btns[i] == _btnPrevPage || btns[i] == _btnNextPage)
            {
                _pnlPageButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(90, this)));
            }
            else if (btns[i] is SimpleButton)
            {
                _pnlPageButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(36, this)));
            }
            else
            {
                _pnlPageButtons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            }
            _pnlPageButtons.Controls.Add(btns[i], i, 0);
        }

        _pnlPageButtons.ResumeLayout();
    }

    private SimpleButton CreatePageNumButton(int pageNum, bool isCurrent)
    {
        var btn = new SimpleButton
        {
            Text = pageNum.ToString(),
            AutoSize = false,
            Dock = DockStyle.Fill,
            Cursor = Cursors.Hand,
            AllowFocus = false,
            ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat,
            Margin = new Padding(2, 0, 2, 0),
            Padding = new Padding(0)
        };
        btn.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        btn.Appearance.Options.UseFont = true;

        if (isCurrent)
        {
            btn.Appearance.BackColor = Color.FromArgb(13, 148, 136);
            btn.Appearance.ForeColor = Color.White;
            btn.Appearance.BorderColor = Color.FromArgb(13, 148, 136);
        }
        else
        {
            btn.Appearance.BackColor = Color.FromArgb(241, 245, 249);
            btn.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
            btn.Appearance.BorderColor = Color.FromArgb(203, 213, 225);
        }
        btn.Appearance.Options.UseBackColor = true;
        btn.Appearance.Options.UseForeColor = true;
        btn.Appearance.Options.UseBorderColor = true;

        btn.Click += (s, e) => ChangePage(pageNum);
        return btn;
    }

    private void ChangePage(int page)
    {
        _currentPage = page;
        ApplyProductFilter();
    }

    private void DisposeManagedResources()
    {
        // Null under the parameterless (designer) constructor, which leaves
        // every injected dependency unset - the rest of this method already
        // null-guards for that, and this line did not, so disposing a
        // design-time instance threw NullReferenceException.
        if (_changeNotifier is not null)
        {
            _changeNotifier.Changed -= MenuItemsChangeNotifier_Changed;
        }

        AppearanceManager.Changed -= AppearanceManager_Changed;
        _scope?.Dispose();
        _gate?.Dispose();
        _orderMutationLock?.Dispose();

        foreach (var image in _tileImagesByProductId.Values)
        {
            image.Dispose();
        }
    }

    private async void RestaurantPosForm_Load(object? sender, EventArgs e)
    {
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;
        
        WindowState = FormWindowState.Maximized;

        try
        {
            await LoadAsync();
        }
        finally
        {
            _splashScreenService.Close();
        }

        // QA hook: render the POS at an exact logical client size (e.g. POS_QA_SIZE=1366x768).
        // Off by default; used only by the visual test harness.
        var qaSize = Environment.GetEnvironmentVariable("POS_QA_SIZE");
        if (!string.IsNullOrWhiteSpace(qaSize))
        {
            var parts = qaSize.Split('x');
            if (parts.Length == 2 && int.TryParse(parts[0], out var qaW) && int.TryParse(parts[1], out var qaH))
            {
                WindowState = FormWindowState.Normal;
                BeginInvoke(() =>
                {
                    ClientSize = new Size(
                        Clovent.Desktop.Forms.Base.DesktopDpi.Scale(qaW, this),
                        Clovent.Desktop.Forms.Base.DesktopDpi.Scale(qaH, this));
                });
            }
        }

        var qaOrder = Environment.GetEnvironmentVariable("POS_QA_ORDER");
        if (!string.IsNullOrWhiteSpace(qaOrder))
        {
            BeginInvoke(async () =>
            {
                var allOpen = await _mediator.Send(new ListOpenOrdersQuery());
                var target = allOpen.FirstOrDefault(o => o.OrderNumber.Equals(qaOrder, StringComparison.OrdinalIgnoreCase));
                if (target != null)
                {
                    await LoadOrderFromRailAsync(target);
                }
            });
        }

        var qaFilter = Environment.GetEnvironmentVariable("POS_QA_FILTER");
        if (!string.IsNullOrWhiteSpace(qaFilter))
        {
            BeginInvoke(() =>
            {
                if (_cboSidebarFilter != null)
                {
                    _cboSidebarFilter.Text = qaFilter;
                }
            });
        }

        var qaCollapsed = Environment.GetEnvironmentVariable("POS_QA_SIDEBAR_COLLAPSED");
        if (!string.IsNullOrWhiteSpace(qaCollapsed) && bool.TryParse(qaCollapsed, out var col) && col)
        {
            BeginInvoke(() =>
            {
                _activeOrdersExpanded = false;
                ApplyActiveOrdersState();
            });
        }

        var qaExpanded = Environment.GetEnvironmentVariable("POS_QA_SIDEBAR_EXPANDED");
        if (!string.IsNullOrWhiteSpace(qaExpanded) && bool.TryParse(qaExpanded, out var exp) && exp)
        {
            BeginInvoke(() =>
            {
                _activeOrdersExpanded = true;
                ApplyActiveOrdersState();
            });
        }

        var qaTable = Environment.GetEnvironmentVariable("POS_QA_SELECT_TABLE");
        if (!string.IsNullOrWhiteSpace(qaTable))
        {
            BeginInvoke(async () =>
            {
                await Task.Delay(400);
                var tables = await _mediator.Send(new ListAllTablesQuery());
                var match = tables.FirstOrDefault(t => t.Code.Equals(qaTable, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    _tablePicker.SelectId(match.TableId);
                    await OnTableSelectedAsync();
                    _tablePicker.Refresh();
                }
            });
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (!Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
        {
            _ = TryRunAsync(RefreshActiveOrdersAsync, "load active orders rail");
        }
    }

    private void AppearanceManager_Changed(object? sender, EventArgs e)
    {
        AppearanceManager.Apply(this, "Restaurant", nameof(RestaurantPosForm));
    }

    private async void MenuItemsChangeNotifier_Changed(object? sender, EventArgs e)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        try
        {
            await ReloadMenuItemsAsync();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            _logger.LogError(ex, "Restaurant POS failed to refresh menu items after a change notification.");
        }
    }

    private async void TablePicker_SelectionChanged(object? sender, EventArgs e)
    {
        // RefreshOrderAsync -> ReloadTablesAsync -> _tablePicker.LoadItems sets
        // SelectedIndex, which raises SelectionChanged; without this guard that
        // re-entered RefreshOrderAsync and looped, rebinding the grid and totals
        // repeatedly (the runtime blinking). Same guard CustomerPicker_SelectionChanged
        // already uses.
        if (_isRefreshingOrder) return;

        await TryRunAsync(OnTableSelectedAsync, "select this table");
    }

    private async void NewDineInButton_Click(object? sender, EventArgs e) => await TryRunAsync(NewDineInAsync, "start a new dine-in order");

    private async void NewTakeAwayButton_Click(object? sender, EventArgs e) => await TryRunAsync(NewTakeAwayAsync, "start a new take-away order");

    private async void HoldButton_Click(object? sender, EventArgs e) => await TryRunAsync(HoldOrderAsync, "hold this order");

    private async void ResumeButton_Click(object? sender, EventArgs e) => await TryRunAsync(RecallOrderAsync, "recall held orders");

    private async void RecallButton_Click(object? sender, EventArgs e) => await TryRunAsync(RecallOrderAsync, "recall held orders");

    private async void ClearButton_Click(object? sender, EventArgs e) => await TryRunAsync(ClearOrderAsync, "clear current order");

    private async void SendToKitchenButton_Click(object? sender, EventArgs e) => await TryRunAsync(SendToKitchenAsync, "send this order to the kitchen");

    private async void CompleteButton_Click(object? sender, EventArgs e) => await TryRunAsync(CompleteAsync, "complete this order");

    private async void PaymentHistoryButton_Click(object? sender, EventArgs e) => await TryRunAsync(ShowPaymentHistoryAsync, "open the payment history");

    private async void VoidOrderButton_Click(object? sender, EventArgs e) => await TryRunAsync(VoidOrderAsync, "void this order");

    private async void CancelOrderButton_Click(object? sender, EventArgs e) => await TryRunAsync(CancelOrderAsync, "clear this order");

    private async void ReopenButton_Click(object? sender, EventArgs e) => await TryRunAsync(() => RunOrderActionAsync(new ReopenOrderCommand(_currentOrder!.OrderId)), "reopen this order");

    private async void TransferTableButton_Click(object? sender, EventArgs e) => await TryRunAsync(TransferTableAsync, "transfer this order to another table");

    private async void MergeTablesButton_Click(object? sender, EventArgs e) => await TryRunAsync(MergeTablesAsync, "merge these tables");

    private async void SplitBillButton_Click(object? sender, EventArgs e) => await TryRunAsync(SplitBillAsync, "split this bill");

    private async void OrderNotesButton_Click(object? sender, EventArgs e) => await TryRunAsync(SetOrderNotesAsync, "save order notes");

    private async void CustomerNotesButton_Click(object? sender, EventArgs e) => await TryRunAsync(SetCustomerNotesAsync, "save customer notes");

    private async void AddDiscountButton_Click(object? sender, EventArgs e) => await TryRunAsync(AddDiscountAsync, "apply this discount");

    private async void RemoveDiscountButton_Click(object? sender, EventArgs e) => await TryRunAsync(RemoveDiscountAsync, "remove this discount");

    private async void AddServiceChargeButton_Click(object? sender, EventArgs e) => await TryRunAsync(AddServiceChargeAsync, "apply this service charge");

    private async void RemoveServiceChargeButton_Click(object? sender, EventArgs e) => await TryRunAsync(RemoveServiceChargeAsync, "remove this service charge");

    private void ProductSearchEdit_EditValueChanged(object? sender, EventArgs e) => ApplyProductFilter();

    private void AllCategoriesButton_Click(object? sender, EventArgs e) => SelectCategory(null);

    private async void AddByBarcodeButton_Click(object? sender, EventArgs e) => await TryRunAsync(AddByBarcodeAsync, "add this item");

    private async void BarcodeEdit_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.Handled = true;
            await TryRunAsync(AddByBarcodeAsync, "add this item");
        }
    }

    private async Task ProductTileTappedAsync(Guid variantId)
    {
        if (_currentOrder is null)
        {
            XtraMessageBox.Show(this, "Start a New Dine-In or New Take Away order first.", "No Order Started", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        await AddProductToCurrentOrder(variantId, _addQuantityEdit.Value);
    }

    private async Task AddProductToCurrentOrder(Guid variantId, decimal quantity)
    {
        await _orderMutationLock.WaitAsync();
        try
        {
            await EnsureOrderResumedIfHeldAsync();
            _hasUnsavedEdits = true;

            var existingLine = _currentOrderLines.FirstOrDefault(l =>
                !l.IsVoided &&
                l.ProductVariantId == variantId &&
                string.IsNullOrEmpty(l.Notes));

            if (existingLine is not null)
            {
                await _mediator.Send(new SetOrderLineQuantityCommand(existingLine.OrderLineId, existingLine.Quantity + quantity));
            }
            else
            {
                await _mediator.Send(new AddOrderLineCommand(_currentOrder!.OrderId, variantId, quantity));
            }

            await RefreshOrderAsync();
        }
        finally
        {
            _orderMutationLock.Release();
        }
    }

    private async void DecreaseQuantityButton_Click(object? sender, EventArgs e) => await TryRunAsync(() => BumpQuantityAsync(-1), "update the quantity");

    private async void IncreaseQuantityButton_Click(object? sender, EventArgs e) => await TryRunAsync(() => BumpQuantityAsync(1), "update the quantity");

    private async void EditQuantityButton_Click(object? sender, EventArgs e) => await TryRunAsync(EditQuantityAsync, "update the quantity");

    private async void EditLineNotesButton_Click(object? sender, EventArgs e) => await TryRunAsync(EditLineNotesAsync, "save item notes");

    private async void OverridePriceButton_Click(object? sender, EventArgs e) => await TryRunAsync(OverridePriceAsync, "override this item's price");

    private async void VoidLineButton_Click(object? sender, EventArgs e) => await TryRunAsync(VoidLineAsync, "void this line");

    private async void RemoveLineButton_Click(object? sender, EventArgs e) => await TryRunAsync(RemoveLineAsync, "remove this line");

    private async void RefreshButton_Click(object? sender, EventArgs e) => await TryRunAsync(RefreshOrderAsync, "refresh the order");

    /// <summary>
    /// Signs out and returns to the sign-in screen, reusing the exact same
    /// pattern <see cref="Forms.Shell.MainForm.SignOutAsync"/> already
    /// established (session sign-out, best-effort activity log, a fresh
    /// <see cref="Forms.Identity.LoginForm"/> shown as an owned modal
    /// dialog) rather than inventing a second one. It never touches
    /// <see cref="_currentOrder"/> - no save, no payment, no customer
    /// mutation - clicking Logout is purely a session/window action; the
    /// order itself is already persisted server-side by every action that
    /// created or changed it, exactly like navigating away from this screen
    /// any other way already leaves it.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="Forms.Shell.MainForm"/> (a DI singleton
    /// <c>Application.Run</c> is only ever called on once, per that type's
    /// own doc comment), <see cref="RestaurantPosForm"/> can itself be the
    /// process's only top-level window (<c>Program.cs</c>'s "pos"
    /// sign-in branch calls <c>Application.Run(posForm)</c> directly,
    /// bypassing <see cref="Forms.Shell.MainForm"/> entirely for a
    /// cashier-only session) - so unlike <c>SignOutAsync</c>, re-signing in
    /// here has three distinct outcomes to handle, not one: staying in POS
    /// (reload this same window for the new cashier), switching to Back
    /// Office (this standalone window has no Shell to hand off to, so one
    /// is resolved and shown), or cancelling (nothing left to run for a
    /// standalone POS window, so it closes). Each is exactly what
    /// <c>Program.cs</c>'s own <c>switch (loginForm.SelectedModuleKey)</c>
    /// already does for the very first sign-in - this just performs the
    /// same decision a second time, from inside an already-running session,
    /// instead of inventing a different one.
    /// </remarks>
    private async void LogoutButton_Click(object? sender, EventArgs e)
    {
        if (XtraMessageBox.Show(this, "Are you sure you want to logout?", "Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        var outgoingUserDisplayName = _currentSession.DisplayName ?? "Unknown";

        try
        {
            await _mediator.Send(new RecordActivityCommand("Logout", null, outgoingUserDisplayName, Environment.MachineName));
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Best-effort, same reasoning as every other activity-log call in this class (see LogActivityAsync).
        }

        _currentSession.SignOut();
        Hide();

        string? selectedModuleKey;
        using (var loginScope = _scope.ServiceProvider.CreateScope())
        {
            var loginForm = loginScope.ServiceProvider.GetRequiredService<Clovent.Desktop.Forms.Identity.LoginForm>();
            loginForm.ShowDialog(this);
            selectedModuleKey = loginForm.SelectedModuleKey;
        }

        switch (selectedModuleKey)
        {
            case "pos":
                // Same cashier-facing window, new session - reload every
                // permission/cashier-label/order-list value as the newly
                // signed-in user rather than tearing down and recreating
                // this form.
                _cashierLabel.Text = _currentSession.DisplayName is { } name ? $"Cashier: {name}" : "Cashier: Not signed in";
                await TryRunAsync(LoadAsync, "load the new session");
                Show();
                break;

            case "backoffice":
                // This window was launched standalone for a POS-only
                // sign-in (Program.cs bypasses Forms.Shell.MainForm
                // entirely in that case), so there is no existing Shell to
                // hand off to - resolve one, exactly like Program.cs's own
                // "backoffice" branch does for the first sign-in.
                var shell = _scope.ServiceProvider.GetRequiredService<Clovent.Desktop.Forms.Shell.MainForm>();
                var navigationService = _scope.ServiceProvider.GetRequiredService<Clovent.Desktop.Navigation.INavigationService>();
                navigationService.NavigateTo("dashboard", "Dashboard");
                shell.Show();
                Close();
                break;

            default:
                // Cancelled, or denied permission for both modules -
                // nothing left to run for a standalone POS window.
                Close();
                break;
        }
    }

    private async Task LoadAsync()
    {
        UseWaitCursor = true;
        try
        {
            await LoadCoreAsync();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            _logger.LogError(ex, "Restaurant POS failed to load.");

            XtraMessageBox.Show(
                this,
                $"Unable to load the Restaurant POS screen.\n\nReason:\n{FriendlyErrorText.Summarize(ex)}\n\nClose and reopen this screen to try again.",
                "Load Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private async Task LoadCoreAsync()
    {
        await CurrencyDisplayLoader.ConfigureAsync(_mediator);

        // Inline cart editors: Price shows the configured currency precision
        // (50.00, not 50.0000) and Qty edits whole numbers (2, not 2.0000).
        // Persistence still goes through the same commands as the buttons -
        // see LineGridView_CellValueChanged.
        var priceEditor = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
        priceEditor.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.Numeric;
        priceEditor.Mask.EditMask = "F" + CurrencyDisplay.DecimalPlaces;
        priceEditor.Mask.UseMaskAsDisplayFormat = true;
        _lineGrid.RepositoryItems.Add(priceEditor);
        _lineGridColumnUnitPrice.ColumnEdit = priceEditor;

        var quantityEditor = new DevExpress.XtraEditors.Repository.RepositoryItemSpinEdit();
        quantityEditor.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.Numeric;
        quantityEditor.Mask.EditMask = "D";
        quantityEditor.IsFloatValue = false;
        quantityEditor.MinValue = 1;
        _lineGrid.RepositoryItems.Add(quantityEditor);
        _lineGridColumnQuantity.ColumnEdit = quantityEditor;

        var warehouses = await _mediator.Send(new ListAllWarehousesQuery());
        _warehousePicker.LoadItems([.. warehouses.Select(w => (w.WarehouseId, w.Name))]);
        _warehousePicker.Visible = warehouses.Count > 1;

        // Once per screen, before the first RefreshOrderAsync below needs them.
        await UpdatePermissionsAsync();

        await ReloadMenuItemsAsync();
        await ReloadTablesAsync();
        await ReloadCustomersAsync();
        await RefreshOrderAsync();
        await RefreshActiveOrdersAsync();

        AppearanceManager.Apply(this, "Restaurant", nameof(RestaurantPosForm));
    }

    private async Task ReloadMenuItemsAsync()
    {
        var variants = await _mediator.Send(new ListProductVariantsQuery());
        _variantsById.Clear();
        foreach (var variant in variants)
        {
            _variantsById[variant.ProductVariantId] = variant;
        }
        _activeVariants = [.. variants.Where(v => v.Status == "Active").OrderBy(v => v.SortOrder).ThenBy(v => v.Name)];

        var products = await _mediator.Send(new ListProductsQuery());
        _productNamesById.Clear();
        foreach (var product in products)
        {
            _productNamesById[product.ProductId] = product.Name;
        }

        _sellingPricesByVariantId.Clear();
        foreach (var image in _tileImagesByProductId.Values)
        {
            image.Dispose();
        }
        _tileImagesByProductId.Clear();

        var sellingPrices = await _mediator.Send(new ListActiveProductPricesByTypeQuery(PriceType.Selling));
        var newestSellingPriceByVariantId = sellingPrices
            .GroupBy(p => p.ProductVariantId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.EffectiveFromUtc).First().Amount);

        foreach (var variant in _activeVariants)
        {
            _sellingPricesByVariantId[variant.ProductVariantId] = newestSellingPriceByVariantId.GetValueOrDefault(variant.ProductVariantId, 0m);

            if (!_tileImagesByProductId.ContainsKey(variant.ProductId) && MenuItemImageStore.Load(variant.ProductId) is { } image)
            {
                _tileImagesByProductId[variant.ProductId] = image;
            }
        }

        var categories = await _mediator.Send(new ListProductCategoriesQuery());
        _loadedCategories = [.. categories.Where(c => c.Status == "Active")];
        BuildCategoryButtons();
        ApplyProductFilter();
    }

    private void BuildCategoryButtons()
    {
        if (_categoryButtonsPanel is null) return;

        _categoryButtonsPanel.SuspendLayout();
        _categoryButtonsPanel.Controls.Clear();

        int allCount = _activeVariants.Select(v => v.ProductId).Distinct().Count();
        var allCard = BuildCategoryCard(null, "All Menu", "🍔", allCount);
        _categoryButtonsPanel.Controls.Add(allCard);

        var ordered = _sortCategoriesByColor
            ? _loadedCategories.OrderBy(c => c.ColorHex ?? "zzz").ThenBy(c => c.SortOrder).ThenBy(c => c.Name)
            : _loadedCategories.OrderBy(c => c.SortOrder).ThenBy(c => c.Name);

        foreach (var category in ordered)
        {
            int count = _activeVariants.Where(v => v.ProductCategoryId == category.ProductCategoryId).Select(v => v.ProductId).Distinct().Count();
            var card = BuildCategoryCard(category.ProductCategoryId, category.Name, CategoryIcon(category.Name), count);
            _categoryButtonsPanel.Controls.Add(card);
        }

        _categoryButtonsPanel.ResumeLayout(true);
        _categoryButtonsPanel.PerformLayout();
        UpdateCategoryScrollButtons();
    }

    private Control BuildCategoryCard(Guid? categoryId, string name, string icon, int count)
    {
        bool isSelected = _selectedCategoryId == categoryId;
        
        var card = new DevExpress.XtraEditors.PanelControl
        {
            Width = 140,
            Height = 44,
            Padding = new Padding(4),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 8, 0)
        };

        card.Appearance.BackColor = isSelected ? Color.FromArgb(204, 251, 241) : Color.White;
        card.Appearance.Options.UseBackColor = true;
        card.Appearance.BorderColor = isSelected ? Color.FromArgb(13, 148, 136) : Color.FromArgb(226, 232, 240);
        card.Appearance.Options.UseBorderColor = true;
        card.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(0),
            BackColor = Color.Transparent
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        var lblIcon = new LabelControl
        {
            Text = icon,
            Dock = DockStyle.Fill,
            AutoSizeMode = LabelAutoSizeMode.None
        };
        lblIcon.Appearance.Font = new Font("Segoe UI", 12F);
        lblIcon.Appearance.Options.UseFont = true;
        lblIcon.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        lblIcon.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lblIcon.Appearance.Options.UseTextOptions = true;

        var lblName = new LabelControl
        {
            Text = name,
            Dock = DockStyle.Fill,
            AutoSizeMode = LabelAutoSizeMode.None
        };
        lblName.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblName.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
        lblName.Appearance.Options.UseFont = true;
        lblName.Appearance.Options.UseForeColor = true;
        lblName.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Near;
        lblName.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lblName.Appearance.Options.UseTextOptions = true;

        var lblCount = new LabelControl
        {
            Text = $"{count} items",
            Dock = DockStyle.Fill,
            AutoSizeMode = LabelAutoSizeMode.None
        };
        lblCount.Appearance.Font = new Font("Segoe UI", 7.5F, FontStyle.Regular);
        lblCount.Appearance.ForeColor = Color.FromArgb(100, 116, 139);
        lblCount.Appearance.Options.UseFont = true;
        lblCount.Appearance.Options.UseForeColor = true;
        lblCount.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Near;
        lblCount.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lblCount.Appearance.Options.UseTextOptions = true;

        layout.Controls.Add(lblIcon, 0, 0);
        layout.SetRowSpan(lblIcon, 2);
        layout.Controls.Add(lblName, 1, 0);
        layout.Controls.Add(lblCount, 1, 1);

        card.Controls.Add(layout);

        void ClickAction()
        {
            SelectCategory(categoryId);
            BuildCategoryButtons();
        }

        card.Click += (s, e) => ClickAction();
        lblIcon.Click += (s, e) => ClickAction();
        lblName.Click += (s, e) => ClickAction();
        lblCount.Click += (s, e) => ClickAction();

        return card;
    }

    private void SortByNameButton_Click(object? sender, EventArgs e) => SetCategorySortMode(byColor: false);

    private void SortByColorButton_Click(object? sender, EventArgs e) => SetCategorySortMode(byColor: true);

    private void SetCategorySortMode(bool byColor)
    {
        if (_sortCategoriesByColor == byColor)
        {
            return;
        }

        _sortCategoriesByColor = byColor;
        UpdateSortModeButtonSelection();
        BuildCategoryButtons();
    }

    private void UpdateSortModeButtonSelection()
    {
        SetCategoryButtonSelected(_sortByNameButton, !_sortCategoriesByColor, null);
        SetCategoryButtonSelected(_sortByColorButton, _sortCategoriesByColor, null);
    }

    private static string CategoryIcon(string categoryName)
    {
        var name = categoryName.ToLowerInvariant();

        return name switch
        {
            _ when name.Contains("chicken") => "🍗",
            _ when name.Contains("beef") || name.Contains("steak") => "🥩",
            _ when name.Contains("bbq") || name.Contains("grill") || name.Contains("kabab") || name.Contains("kebab") => "🍢",
            _ when name.Contains("rice") || name.Contains("biryani") => "🍚",
            _ when name.Contains("bread") || name.Contains("naan") || name.Contains("roti") => "🍞",
            _ when name.Contains("dessert") || name.Contains("sweet") || name.Contains("cake") => "🍰",
            _ when name.Contains("burger") => "🍔",
            _ when name.Contains("seafood") || name.Contains("fish") => "🐟",
            _ when name.Contains("soup") => "🍲",
            _ when name.Contains("salad") => "🥗",
            _ => "🍽️",
        };
    }

    private void RestaurantPosForm_Resize(object? sender, EventArgs e)
    {
        if (_tlpBody == null || _tlpRightRows == null || _productViewport == null) return;
        ApplyColumnWidths();
        if (_tlpBody.ColumnStyles.Count >= 3)
        {
            bool isSmallScreen = this.ClientSize.Width > 0 && this.ClientSize.Width <= 1100;
            int rightWidth = isSmallScreen 
                ? Clovent.Desktop.Forms.Base.DesktopDpi.Scale(380, this) 
                : Clovent.Desktop.Forms.Base.DesktopDpi.Scale(410, this);
            _tlpBody.ColumnStyles[2].Width = rightWidth;

            if (_activeOrdersExpanded)
            {
                int sidebarWidth = isSmallScreen 
                    ? Clovent.Desktop.Forms.Base.DesktopDpi.Scale(230, this) 
                    : Clovent.Desktop.Forms.Base.DesktopDpi.Scale(250, this);
                _tlpBody.ColumnStyles[0].Width = sidebarWidth;
            }
        }
        SizeSidebarOrderCards();
        SizeOrderedItemRows();
        if (_variantsById != null && _variantsById.Count > 0 && _productTilesFlow != null && _lblPageInfo != null)
        {
            ApplyProductFilter();
        }
    }

    private int CalculateResponsiveColumns()
    {
        int storedCols = Clovent.Desktop.Forms.Base.PosSettingsStore.LoadItemsPerRow();
        int centerW = _productViewport?.ClientSize.Width ?? (_productTilesFlow?.ClientSize.Width ?? 0);
        int minCardW = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(125, this);
        int responsiveMax = centerW > 0 ? Math.Max(2, centerW / minCardW) : 4;
        int columns = Math.Min(storedCols, responsiveMax);
        if (columns < 2) columns = 2;
        if (columns > 8) columns = 8;
        return columns;
    }

    private void MoreActionsButton_Click(object? sender, EventArgs e) =>
        _moreActionsMenu.Show(_moreActionsButton, new Point(0, _moreActionsButton.Height));

    private void SelectCategory(Guid? categoryId)
    {
        _selectedCategoryId = categoryId;
        UpdateCategoryButtonSelection();
        ApplyProductFilter();
    }

    private void UpdateCategoryButtonSelection()
    {
        SetCategoryButtonSelected(_allCategoriesButton, _selectedCategoryId is null, null);

        // _categoryButtonsPanel.Controls contains _categorySortFlow (a FlowLayoutPanel)
        // in addition to the per-category SimpleButtons.  The old implicit-cast
        // foreach broke the moment another control type was placed inside the panel.
        foreach (Control control in _categoryButtonsPanel.Controls)
        {
            if (control is SimpleButton btn && btn.Tag is Guid id)
            {
                SetCategoryButtonSelected(btn, _selectedCategoryId == id, id);
            }
        }
    }

    private void SetCategoryButtonSelected(SimpleButton button, bool selected, Guid? categoryId)
    {
        button.Appearance.BackColor = selected ? AccentColor : Color.White;
        button.Appearance.ForeColor = selected ? Color.White : Color.FromArgb(51, 65, 85);
        button.Appearance.BorderColor = selected ? AccentColor : DividerColor;
    }

    private void ApplyProductFilter()
    {
        var searchText = _productSearchEdit.Text?.Trim();

        IEnumerable<ProductVariantDto> filtered = _variantsById.Values;

        if (_selectedCategoryId.HasValue)
        {
            filtered = filtered.Where(v => v.ProductCategoryId == _selectedCategoryId.Value);
        }

        if (!string.IsNullOrEmpty(searchText))
        {
            filtered = filtered.Where(v =>
                v.Sku.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                v.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        int columns = CalculateResponsiveColumns();
        _pageSize = columns * 3;

        var productGroups = filtered.GroupBy(v => v.ProductId).ToList();
        int totalProducts = productGroups.Count;
        int totalPages = (int)Math.Ceiling((double)totalProducts / _pageSize);
        if (totalPages < 1) totalPages = 1;

        if (_currentPage > totalPages) _currentPage = totalPages;
        if (_currentPage < 1) _currentPage = 1;

        // Slice the current page
        var pageGroups = productGroups.Skip((_currentPage - 1) * _pageSize).Take(_pageSize).ToList();
        var pageVariants = pageGroups.SelectMany(g => g).ToList();

        UpdatePaginationUI(_currentPage, totalPages, totalProducts);

        RenderProductTiles(pageVariants);
    }

    private void RenderProductTiles(IReadOnlyList<ProductVariantDto> variants)
    {
        if (_viewMode == "Grid")
        {
            _productTilesFlow.SuspendLayout();

            var oldTiles = _productTilesFlow.Controls.Cast<Control>().ToList();
            _productTilesFlow.Controls.Clear();
            foreach (var oldTile in oldTiles)
            {
                DisposeTile(oldTile);
            }

            int columns = CalculateResponsiveColumns();

            _productTilesFlow.RowCount = 3;
            _productTilesFlow.ColumnCount = columns;

            _productTilesFlow.RowStyles.Clear();
            for (int r = 0; r < 3; r++)
            {
                _productTilesFlow.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333F));
            }

            _productTilesFlow.ColumnStyles.Clear();
            float colWidthPercent = 100F / columns;
            for (int c = 0; c < columns; c++)
            {
                _productTilesFlow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, colWidthPercent));
            }

            var groups = variants.GroupBy(v => v.ProductId).ToList();
            for (int i = 0; i < groups.Count; i++)
            {
                int r = i / columns;
                int c = i % columns;
                if (r < 3)
                {
                    var tile = BuildProductTile([.. groups[i]]);
                    tile.Dock = DockStyle.Fill;
                    tile.Margin = new Padding(4);
                    _productTilesFlow.Controls.Add(tile, c, r);
                }
            }

            _productTilesFlow.ResumeLayout();
            _productTilesFlow.Invalidate(true);

            _productTilesFlow.Visible = true;
            if (_productListFlow is not null)
            {
                _productListFlow.Visible = false;
            }
        }
        else
        {
            _productListFlow.SuspendLayout();

            var oldRows = _productListFlow.Controls.Cast<Control>().ToList();
            _productListFlow.Controls.Clear();
            foreach (var oldRow in oldRows)
            {
                DisposeTile(oldRow);
            }

            var groups = variants.GroupBy(v => v.ProductId).ToList();
            foreach (var group in groups)
            {
                _productListFlow.Controls.Add(BuildProductListRow([.. group]));
            }

            _productListFlow.ResumeLayout();
            SizeListRows();
            _productListFlow.Invalidate(true);

            _productTilesFlow.Visible = false;
            _productListFlow.Visible = true;
        }

        if (variants.Count == 0)
        {
            _tilesEmptyLabel.Text = _activeVariants.Count == 0
                ? "No menu items yet.\nAdd some from the Menu Items screen, then come back here to sell them."
                : "No menu items match this category or search.";
            _tilesEmptyLabel.Visible = true;
            _tilesEmptyLabel.BringToFront();
        }
        else
        {
            _tilesEmptyLabel.Visible = false;
        }
    }

    private static void DisposeTile(Control tile)
    {
        foreach (var pictureBox in tile.Controls.OfType<PictureBox>())
        {
            pictureBox.Image = null;
        }

        tile.Dispose();
    }

    /// <summary>
    /// Stretches the List View's rows to the flow's current width. The grid
    /// view needs no equivalent: its TableLayoutPanel percentage rows/columns
    /// resize every card automatically with the viewport.
    /// </summary>
    private void SizeListRows()
    {
        if (_productListFlow == null || _productListFlow.Controls.Count == 0) return;

        int usableWidth = _productListFlow.ClientSize.Width - _productListFlow.Padding.Horizontal;
        int scrollbarBuffer = SystemInformation.VerticalScrollBarWidth + 4;
        int rowWidth = Math.Max(usableWidth - scrollbarBuffer, 300);
        int rowH = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(80, this);

        foreach (Control control in _productListFlow.Controls)
        {
            if (control.Width != rowWidth)
                control.Width = rowWidth;
            if (control.Height != rowH)
                control.Height = rowH;
        }
    }

    private string GetVariantPosLabel(ProductVariantDto variant, string productName)
    {
        string rawName = variant.Name;
        int pipeIndex = rawName.IndexOf('|');
        if (pipeIndex >= 0)
        {
            return rawName.Substring(pipeIndex + 1).Trim();
        }

        // Fallback cleaning logic:
        string cleanName = rawName;
        if (cleanName.StartsWith(productName, StringComparison.OrdinalIgnoreCase))
        {
            cleanName = cleanName.Substring(productName.Length).TrimStart(' ', '-', ':');
        }
        if (string.IsNullOrEmpty(cleanName))
        {
            cleanName = "Regular";
        }
        if (cleanName.Equals("Half Plate", StringComparison.OrdinalIgnoreCase))
        {
            cleanName = "Half";
        }
        else if (cleanName.Equals("Full Plate", StringComparison.OrdinalIgnoreCase))
        {
            cleanName = "Full";
        }
        return cleanName.Trim();
    }

    /// <summary>
    /// Builds one grid-view product card. Sizing is fully delegated to the
    /// layout engine: the card is <see cref="DockStyle.Fill"/> inside its
    /// grid cell, and its internal rows are percentages of the card height
    /// (image ~38%, name, portion/price buttons), so every card resizes
    /// deterministically with the viewport - no manual width/height math.
    /// </summary>
    private Control BuildProductTile(IReadOnlyList<ProductVariantDto> productVariants)
    {
        var primaryVariant = productVariants[0];
        var productId = primaryVariant.ProductId;
        var hasImage = _tileImagesByProductId.TryGetValue(productId, out var image);
        var productName = _productNamesById.GetValueOrDefault(productId, primaryVariant.Name);

        var card = new DevExpress.XtraEditors.PanelControl
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(4),
            Padding = new Padding(0),
            Cursor = Cursors.Hand,
        };
        card.Appearance.BackColor = TileBackColor;
        card.Appearance.Options.UseBackColor = true;
        card.Appearance.BorderColor = TileBorderColor;
        card.Appearance.Options.UseBorderColor = true;

        var cardLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0),
            Padding = new Padding(4, 2, 4, 4),
            BackColor = Color.Transparent
        };
        cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        
        // Consistent proportions for all cards (image or no image)
        cardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 32F)); // Image/Placeholder takes 32%
        cardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 28F)); // Product information takes 28%
        cardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40F)); // Price/Portion buttons takes 40%

        // 1. Top zone: large centered image, or clean placeholder.
        Control topControl;
        if (hasImage && image is not null)
        {
            topControl = new PictureBox
            {
                Dock = DockStyle.Fill,
                Image = image,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = TileBackColor
            };
        }
        else
        {
            var placeholder = new DevExpress.XtraEditors.PanelControl
            {
                Dock = DockStyle.Fill,
                BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder,
            };
            placeholder.Appearance.BackColor = Color.FromArgb(241, 245, 249);
            placeholder.Appearance.Options.UseBackColor = true;

            var initial = productName.Length > 0 ? productName[0].ToString().ToUpper() : "?";
            var initialLabel = new DevExpress.XtraEditors.LabelControl
            {
                Text = initial,
                Dock = DockStyle.Fill,
                Name = "PlaceholderInitial",
            };
            initialLabel.Appearance.Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold);
            initialLabel.Appearance.ForeColor = Color.FromArgb(148, 163, 184); // slate-400
            initialLabel.Appearance.Options.UseFont = true;
            initialLabel.Appearance.Options.UseForeColor = true;
            initialLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            initialLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            initialLabel.Appearance.Options.UseTextOptions = true;

            placeholder.Controls.Add(initialLabel);
            topControl = placeholder;
        }

        // 2. Middle zone: Product Name (Top/Bold) & Category (Bottom/Muted) stacked cleanly
        var textPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
            Padding = new Padding(4, 0, 4, 0),
            BackColor = Color.Transparent
        };
        textPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        textPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        textPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var categoryName = _loadedCategories.FirstOrDefault(c => c.ProductCategoryId == primaryVariant.ProductCategoryId)?.Name ?? "";
        var categoryLabel = new DevExpress.XtraEditors.LabelControl
        {
            Text = categoryName.ToUpperInvariant(),
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 1, 0, 0),
            AutoSizeMode = LabelAutoSizeMode.Vertical
        };
        categoryLabel.Appearance.Font = new Font("Segoe UI", 7F, FontStyle.Bold);
        categoryLabel.Appearance.ForeColor = Color.FromArgb(148, 163, 184); // slate-400
        categoryLabel.Appearance.Options.UseFont = true;
        categoryLabel.Appearance.Options.UseForeColor = true;
        categoryLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        categoryLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Top;
        categoryLabel.Appearance.Options.UseTextOptions = true;

        var nameLabel = new DevExpress.XtraEditors.LabelControl
        {
            Text = productName,
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            Name = "ProductTileName",
            AutoSizeMode = LabelAutoSizeMode.Vertical
        };
        nameLabel.Appearance.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        nameLabel.Appearance.ForeColor = Color.FromArgb(15, 23, 42); // slate-800
        nameLabel.Appearance.Options.UseFont = true;
        nameLabel.Appearance.Options.UseForeColor = true;
        nameLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        nameLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Top;
        nameLabel.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
        nameLabel.Appearance.TextOptions.Trimming = DevExpress.Utils.Trimming.EllipsisCharacter;
        nameLabel.Appearance.Options.UseTextOptions = true;

        textPanel.Controls.Add(nameLabel, 0, 0);       // Product Name is on top
        textPanel.Controls.Add(categoryLabel, 0, 1);   // Category Name is on bottom

        // 3. Bottom zone: portion/price buttons
        Control buttonsPanel;
        if (productVariants.Count == 1)
        {
            var price = _sellingPricesByVariantId.GetValueOrDefault(primaryVariant.ProductVariantId);

            var priceButton = new DevExpress.XtraEditors.SimpleButton
            {
                Text = CurrencyDisplay.FormatPlain(price),
                Dock = DockStyle.Fill,
                Cursor = Cursors.Hand,
                Margin = new Padding(8, 2, 8, 2),
                MinimumSize = new Size(0, 0),
            };
            priceButton.Appearance.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            priceButton.Appearance.BackColor = Color.FromArgb(241, 245, 249);
            priceButton.Appearance.ForeColor = TilePriceColor;
            priceButton.Appearance.Options.UseFont = true;
            priceButton.Appearance.Options.UseBackColor = true;
            priceButton.Appearance.Options.UseForeColor = true;
            priceButton.Click += async (s, e) => await TryRunAsync(() => ProductTileTappedAsync(primaryVariant.ProductVariantId), "add this item");

            WireTileClick(card, primaryVariant.ProductVariantId);
            WireTileClick(topControl, primaryVariant.ProductVariantId);
            WireTileClick(nameLabel, primaryVariant.ProductVariantId);
            buttonsPanel = priceButton;
        }
        else
        {
            var tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = productVariants.Count,
                BackColor = TileBackColor,
                Padding = new Padding(2, 1, 2, 1),
                Margin = new Padding(0)
            };

            tlp.ColumnStyles.Clear();
            float colPercent = 100f / productVariants.Count;
            for (int i = 0; i < productVariants.Count; i++)
            {
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, colPercent));
            }
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            for (int i = 0; i < productVariants.Count; i++)
            {
                var variant = productVariants[i];
                var price = _sellingPricesByVariantId.GetValueOrDefault(variant.ProductVariantId);
                string cleanName = GetVariantPosLabel(variant, productName);

                var btn = new DevExpress.XtraEditors.SimpleButton
                {
                    Text = $"{cleanName}\n{CurrencyDisplay.FormatPlain(price)}",
                    Dock = DockStyle.Fill,
                    Cursor = Cursors.Hand,
                    Margin = new Padding(1),
                    MinimumSize = new Size(0, 0),
                };
                btn.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                btn.Appearance.BackColor = Color.FromArgb(241, 245, 249);
                btn.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
                btn.Appearance.Options.UseFont = true;
                btn.Appearance.Options.UseBackColor = true;
                btn.Appearance.Options.UseForeColor = true;
                btn.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
                btn.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
                btn.Appearance.Options.UseTextOptions = true;
                btn.Click += async (s, e) => await TryRunAsync(() => ProductTileTappedAsync(variant.ProductVariantId), "add this item");

                tlp.Controls.Add(btn, i, 0);
            }
            buttonsPanel = tlp;
        }

        cardLayout.Controls.Add(topControl, 0, 0);
        cardLayout.Controls.Add(textPanel, 0, 1);
        cardLayout.Controls.Add(buttonsPanel, 0, 2);

        card.Controls.Add(cardLayout);

        AppearanceManager.Apply(card, "Restaurant", nameof(RestaurantPosForm));
        return card;
    }

    private Control BuildProductListRow(IReadOnlyList<ProductVariantDto> productVariants)
    {
        var primaryVariant = productVariants[0];
        var productId = primaryVariant.ProductId;
        var hasImage = _tileImagesByProductId.TryGetValue(productId, out var image);
        var productName = _productNamesById.GetValueOrDefault(productId, primaryVariant.Name);

        // Fixed row height: compact and deterministic
        int rowH = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(85, this);
        int imgW  = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(75, this);

        // Outer container — fixed height, full width set by SizeListRows()
        int usableWidth = _productListFlow.ClientSize.Width - _productListFlow.Padding.Horizontal;
        int scrollbarBuffer = SystemInformation.VerticalScrollBarWidth + 4;
        int rowWidth = Math.Max(usableWidth - scrollbarBuffer, 300);

        var tlp = new TableLayoutPanel
        {
            ColumnCount = 3,
            RowCount = 1,
            Size = new Size(rowWidth, rowH),
            Margin = new Padding(0, 0, 0, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(2, this)),
            Padding = new Padding(0),
            BackColor = TileBackColor,
        };
        tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, imgW));
        tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        // Col 0: Image or initial placeholder
        Control imgControl;
        if (hasImage && image is not null)
        {
            imgControl = new PictureBox
            {
                Dock = DockStyle.Fill,
                Image = image,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = TileBackColor,
                Padding = new Padding(Clovent.Desktop.Forms.Base.DesktopDpi.Scale(4, this)),
                Margin = new Padding(0),
            };
        }
        else
        {
            var placeholder = new DevExpress.XtraEditors.PanelControl
            {
                Dock = DockStyle.Fill,
                BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder,
                Margin = new Padding(0),
            };
            placeholder.Appearance.BackColor = Color.FromArgb(241, 245, 249);
            placeholder.Appearance.Options.UseBackColor = true;

            var initial = productName.Length > 0 ? productName[0].ToString().ToUpper() : "?";
            var initialLabel = new DevExpress.XtraEditors.LabelControl
            {
                Text = initial,
                Dock = DockStyle.Fill,
            };
            initialLabel.Appearance.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold);
            initialLabel.Appearance.ForeColor = Color.FromArgb(148, 163, 184);
            initialLabel.Appearance.Options.UseFont = true;
            initialLabel.Appearance.Options.UseForeColor = true;
            initialLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            initialLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            initialLabel.Appearance.Options.UseTextOptions = true;
            placeholder.Controls.Add(initialLabel);
            imgControl = placeholder;
        }
        tlp.Controls.Add(imgControl, 0, 0);

        // Col 1: Product name + category stacked vertically
        var namePanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(Clovent.Desktop.Forms.Base.DesktopDpi.Scale(10, this), 0, 4, 0),
            Margin = new Padding(0),
            BackColor = TileBackColor,
        };

        var nameLabel = new DevExpress.XtraEditors.LabelControl
        {
            Text = productName,
            Dock = DockStyle.Top,
            AutoSizeMode = LabelAutoSizeMode.Vertical,
            Padding = new Padding(0, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(12, this), 0, 2),
            Name = "ProductListName",
        };
        nameLabel.Appearance.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        nameLabel.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
        nameLabel.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.NoWrap;
        nameLabel.Appearance.TextOptions.Trimming = DevExpress.Utils.Trimming.EllipsisCharacter;
        nameLabel.Appearance.Options.UseTextOptions = true;
        nameLabel.Appearance.Options.UseFont = true;
        nameLabel.Appearance.Options.UseForeColor = true;


        string categoryTag = "";  // Category name not available in ProductVariantDto; extend DTO if needed
        var catLabel = new DevExpress.XtraEditors.LabelControl
        {
            Text = categoryTag.ToUpper(),
            Dock = DockStyle.Top,
            AutoSizeMode = LabelAutoSizeMode.Vertical,
            Padding = new Padding(0),
        };
        catLabel.Appearance.Font = new Font("Segoe UI", 8F, FontStyle.Regular);
        catLabel.Appearance.ForeColor = Color.FromArgb(148, 163, 184);
        catLabel.Appearance.Options.UseFont = true;
        catLabel.Appearance.Options.UseForeColor = true;

        // Add in reverse order for Dock=Top stacking
        namePanel.Controls.Add(catLabel);
        namePanel.Controls.Add(nameLabel);
        tlp.Controls.Add(namePanel, 1, 0);

        // Col 2: Price/portion buttons
        var buttonsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = productVariants.Count,
            RowCount = 1,
            BackColor = TileBackColor,
            Padding = new Padding(4, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(16, this), Clovent.Desktop.Forms.Base.DesktopDpi.Scale(12, this), Clovent.Desktop.Forms.Base.DesktopDpi.Scale(16, this)),
            Margin = new Padding(0),
        };
        buttonsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        for (int i = 0; i < productVariants.Count; i++)
        {
            buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var variant = productVariants[i];
            var price = _sellingPricesByVariantId.GetValueOrDefault(variant.ProductVariantId);
            string cleanName = productVariants.Count > 1 ? GetVariantPosLabel(variant, productName) : "";
            
            string btnText = productVariants.Count == 1 
                ? CurrencyDisplay.FormatPlain(price) 
                : $"{cleanName}\n{CurrencyDisplay.FormatPlain(price)}";

            var btn = new DevExpress.XtraEditors.SimpleButton
            {
                Text = btnText,
                Cursor = Cursors.Hand,
                Margin = new Padding(2, 0, 2, 0),
                AutoSize = true,
                MinimumSize = new Size(Clovent.Desktop.Forms.Base.DesktopDpi.Scale(55, this), 0),
            };
            btn.Dock = DockStyle.Fill;
            btn.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            btn.Appearance.BackColor = Color.FromArgb(241, 245, 249);
            btn.Appearance.ForeColor = productVariants.Count == 1 ? TilePriceColor : Color.FromArgb(15, 23, 42);
            btn.Appearance.Options.UseFont = true;
            btn.Appearance.Options.UseBackColor = true;
            btn.Appearance.Options.UseForeColor = true;
            btn.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            btn.Appearance.Options.UseTextOptions = true;
            btn.Click += async (s, e) => await TryRunAsync(() => ProductTileTappedAsync(variant.ProductVariantId), "add this item");
            buttonsPanel.Controls.Add(btn, i, 0);
        }
        tlp.Controls.Add(buttonsPanel, 2, 0);

        if (productVariants.Count == 1)
        {
            WireTileClick(tlp, primaryVariant.ProductVariantId);
            WireTileClick(imgControl, primaryVariant.ProductVariantId);
            WireTileClick(nameLabel, primaryVariant.ProductVariantId);
        }

        // Draw a subtle bottom divider line
        tlp.Paint += (s, e) =>
        {
            using var pen = new Pen(TileBorderColor, 1);
            e.Graphics.DrawLine(pen, 0, tlp.Height - 1, tlp.Width, tlp.Height - 1);
        };

        AppearanceManager.Apply(tlp, "Restaurant", nameof(RestaurantPosForm));
        return tlp;
    }




    private void WireTileClick(Control control, Guid variantId)
    {
        control.Cursor = Cursors.Hand;
        control.Click += async (_, _) => await TryRunAsync(() => ProductTileTappedAsync(variantId), "add this item");
    }

    private async Task ReloadTablesAsync()
    {
        var selectedTableId = _currentOrder?.TableId ?? _tablePicker.SelectedId;
        var tables = await _mediator.Send(new ListAllTablesQuery());
        _tablePicker.LoadItems([.. tables.Select(t => (t.TableId, $"{t.Code} ({t.OccupancyStatus})"))]);
        if (_currentOrder != null && _currentOrder.OrderType == "TakeAway")
        {
            _tablePicker.SelectId(null);
            _tablePicker.Enabled = false;
        }
        else
        {
            _tablePicker.Enabled = _currentOrder == null || _currentOrder.Status is "Open" or "Held";
            _tablePicker.SelectId(selectedTableId);
        }
    }

    private async Task OnTableSelectedAsync()
    {
        var targetTableId = _tablePicker.SelectedId;
        if (targetTableId is null)
        {
            if (_currentOrder != null && _currentOrder.OrderType == "DineIn")
            {
                _tablePicker.SelectId(_currentOrder.TableId);
            }
            return;
        }

        var tableId = targetTableId.Value;

        // Check if there is an open or held order seated at this table:
        var tableOrder = await _mediator.Send(new GetOpenOrHeldOrderByTableQuery(tableId));

        if (tableOrder != null)
        {
            if (_currentOrder?.OrderId != tableOrder.OrderId)
            {
                if (_currentOrder != null && _currentOrderLines.Count > 0)
                {
                    var res = XtraMessageBox.Show(this,
                        $"Order '{_currentOrder.OrderNumber}' is currently active with items. Would you like to put it on Hold before switching tables?",
                        "Active Order In Progress",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question);

                    if (res == DialogResult.Cancel)
                    {
                        if (_currentOrder.TableId is { } curTbl)
                            _tablePicker.SelectId(curTbl);
                        return;
                    }

                    if (res == DialogResult.Yes)
                    {
                        try
                        {
                            await _mediator.Send(new HoldOrderCommand(_currentOrder.OrderId));
                            await LogActivityAsync("Hold Order", $"{_currentOrder.OrderNumber}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to hold current order {OrderId} before switching tables", _currentOrder.OrderId);
                            XtraMessageBox.Show(this, $"Failed to hold current order: {ex.Message}", "Hold Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            if (_currentOrder.TableId is { } curTbl)
                                _tablePicker.SelectId(curTbl);
                            return;
                        }
                    }
                }

                if (tableOrder.Status == "Held")
                {
                    tableOrder = await _mediator.Send(new ResumeOrderCommand(tableOrder.OrderId));
                    await LogActivityAsync("Recall Order", $"{tableOrder.OrderNumber}");
                }

                _currentOrder = tableOrder;
                await RefreshOrderAsync();
                await RefreshActiveOrdersAsync();
            }
            return;
        }

        // Table has NO order seated (it is Available):
        if (_currentOrder != null && _currentOrder.OrderType == "DineIn" && _currentOrder.TableId != tableId)
        {
            var tables = await _mediator.Send(new ListAllTablesQuery());
            var targetTable = tables.FirstOrDefault(t => t.TableId == tableId);
            if (targetTable != null && targetTable.OccupancyStatus != "Available")
            {
                XtraMessageBox.Show(this, $"Table {targetTable.Code} is {targetTable.OccupancyStatus} and cannot receive this order.", "Table Unavailable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _tablePicker.SelectId(_currentOrder.TableId);
                return;
            }

            _currentOrder = await _mediator.Send(new TransferOrderTableCommand(_currentOrder.OrderId, tableId));
            await RefreshOrderAsync();
            return;
        }

        // No active order: the table selection remains active in _tablePicker (showing "Table: T-01")
        // so that clicking "+ Dine In" immediately creates an order for this selected table.
    }

    private async Task NewDineInAsync()
    {
        if (_warehousePicker.SelectedId is not { } warehouseId)
        {
            XtraMessageBox.Show(this, "Select a location first.", "No Location Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_tablePicker.SelectedId is not { } tableId)
        {
            _tablePicker.ShowPopup();
            return;
        }

        _currentOrder = await _mediator.Send(new CreateOrderCommand(OrderType.DineIn, warehouseId, tableId));
        _hasUnsavedEdits = true;
        await RefreshOrderAsync();
        await RefreshActiveOrdersAsync();
        await LogActivityAsync("New Order", $"{_currentOrder.OrderNumber} (Dine-In)");
    }

    private async Task NewTakeAwayAsync()
    {
        if (_warehousePicker.SelectedId is not { } warehouseId)
        {
            XtraMessageBox.Show(this, "Select a location first.", "No Location Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _currentOrder = await _mediator.Send(new CreateOrderCommand(OrderType.TakeAway, warehouseId));
        _hasUnsavedEdits = true;
        await RefreshOrderAsync();
        await RefreshActiveOrdersAsync();
        await LogActivityAsync("New Order", $"{_currentOrder.OrderNumber} (Take Away)");
    }

    private async Task RunOrderActionAsync(IRequest<OrderDto> command)
    {
        _currentOrder = await _mediator.Send(command);
        await RefreshOrderAsync();
    }

    private async Task HoldOrderAsync()
    {
        if (_currentOrder is null)
        {
            XtraMessageBox.Show(this, "There is no active order to hold.", "No Active Order", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_currentOrderLines.Count == 0 || _currentOrder.OrderLineIds.Count == 0)
        {
            XtraMessageBox.Show(this, "Cannot hold an empty order. Please add at least one item first.", "Empty Order", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var heldOrder = await _mediator.Send(new HoldOrderCommand(_currentOrder.OrderId));
            await LogActivityAsync("Hold Order", $"{heldOrder.OrderNumber}");

            _currentOrder = null;
            _tablePicker.SelectId(null);
            await RefreshOrderAsync();
            await RefreshActiveOrdersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hold order {OrderId}", _currentOrder.OrderId);
            XtraMessageBox.Show(this, $"Failed to hold order: {ex.Message}", "Hold Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            // DO NOT clear cart or change _currentOrder - keep current order intact!
        }
    }

    private async Task RecallOrderAsync()
    {
        using var dialog = new RecallOrderDialog(_mediator, _logger);
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.SelectedOrder is null)
        {
            return;
        }

        var targetOrder = dialog.SelectedOrder;
        var wantOpen = dialog.SelectedAction == "Open";

        // Prevent duplicate recall if it was resumed, completed or voided elsewhere
        var freshTarget = await _mediator.Send(new GetOrderByIdQuery(targetOrder.OrderId));
        if (freshTarget is null
            || (wantOpen && freshTarget.Status != "Open")
            || (!wantOpen && freshTarget.Status != "Held"))
        {
            XtraMessageBox.Show(this, $"Order {targetOrder.OrderNumber} is no longer available for recall.", "Order Unavailable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            await RefreshActiveOrdersAsync();
            return;
        }

        // Section 8: Recall Conflict Handling
        if (_currentOrder != null && _currentOrder.OrderId != targetOrder.OrderId && _currentOrderLines.Count > 0)
        {
            var res = XtraMessageBox.Show(this,
                $"The current order '{_currentOrder.OrderNumber}' has unsaved changes.\n\nHold it before recalling '{targetOrder.OrderNumber}'?",
                "Active Order In Progress",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (res == DialogResult.Cancel)
            {
                return;
            }

            if (res == DialogResult.Yes)
            {
                try
                {
                    await _mediator.Send(new HoldOrderCommand(_currentOrder.OrderId));
                    await LogActivityAsync("Hold Order", $"{_currentOrder.OrderNumber}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to hold current order {OrderId} before recall", _currentOrder.OrderId);
                    XtraMessageBox.Show(this, $"Failed to hold current order: {ex.Message}", "Hold Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }

        try
        {
            _currentOrder = freshTarget;
            _hasUnsavedEdits = false;
            await LogActivityAsync("Recall Order", $"{freshTarget.OrderNumber}");

            if (freshTarget.OrderType == "DineIn" && freshTarget.TableId is { } tableId)
            {
                _tablePicker.SelectId(tableId);
            }
            else
            {
                _tablePicker.SelectId(null);
                _tablePicker.Enabled = false;
            }

            await RefreshOrderAsync();
            await RefreshActiveOrdersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to recall order {OrderId}", targetOrder.OrderId);
            XtraMessageBox.Show(this, $"Failed to recall order: {ex.Message}", "Recall Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            await RefreshActiveOrdersAsync();
        }
    }

    private async Task EnsureOrderResumedIfHeldAsync()
    {
        if (_currentOrder is { Status: "Held" } held)
        {
            _currentOrder = await _mediator.Send(new ResumeOrderCommand(held.OrderId));
            _hasUnsavedEdits = true;
            await RefreshActiveOrdersAsync();
        }
    }

    private async Task ClearOrderAsync()
    {
        if (_currentOrder is null && _currentOrderLines.Count == 0)
        {
            return;
        }

        if (_hasUnsavedEdits)
        {
            var confirm = XtraMessageBox.Show(
                this,
                "Clear current order?\n\nUnsaved changes will be discarded.",
                "Clear Order",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return;
            }
        }

        _hasUnsavedEdits = false;
        _currentOrder = null;
        _currentOrderLines = [];
        _tablePicker.SelectId(null);
        _addQuantityEdit.Value = 1;
        _amountEdit.Text = string.Empty;
        _amountEntryIsPreset = true;

        await RefreshOrderAsync();
        await RefreshActiveOrdersAsync();
    }

    private async Task VoidOrderAsync()
    {
        using var form = new TextPromptForm("Void Order", "Reason:", required: true);
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        // Voiding discards a recorded sale, so it is gated the same way a
        // credit-limit override is (defect D25). Deliberately the same
        // mechanism rather than a second one: one manager-authorization path
        // means one place to audit, one place to get the lockout policy right,
        // and no privileged action quietly ending up on a weaker check.
        var summary = _currentOrder is { } order
            ? $"Order {order.OrderNumber} ({order.OrderType}) will be voided."
            : "This order will be voided.";

        var authorization = await RequestManagerAuthorizationAsync(
            "Manager Authorization - Void Order",
            $"{summary}\n\nReason: {form.Value}\n\nA manager must authorize voiding an order.",
            "pos.void");

        if (authorization is null)
        {
            return;
        }

        _currentOrder = await _mediator.Send(new VoidOrderCommand(_currentOrder!.OrderId, form.Value!));
        await LogActivityAsync(
            "Void",
            $"Voided order {_currentOrder.OrderNumber}. Reason: {form.Value}. " +
            $"Approved by manager '{authorization.ManagerDisplayName}'.");
        await RefreshOrderAsync();
        await RefreshActiveOrdersAsync();
    }

    /// <summary>
    /// Challenges for a manager's credentials and confirms they hold
    /// <paramref name="featureCode"/>. Returns <see langword="null"/> when the
    /// operator cancels or the challenge fails - in which case the caller must
    /// change nothing at all.
    /// </summary>
    /// <remarks>
    /// The dialog only collects; <see cref="IManagerAuthorizationService"/>
    /// does the verifying, against the same Identity/Authentication
    /// infrastructure that backs signing in. Nothing is sent to the
    /// application layer until this returns a non-null result, so a denied
    /// challenge cannot leave a partial financial change behind.
    /// </remarks>
    private async Task<ManagerAuthorizationResult?> RequestManagerAuthorizationAsync(
        string title,
        string detail,
        string featureCode)
    {
        using var dialog = new ManagerAuthorizationForm(title, detail);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return null;
        }

        var result = await _managerAuthorization.AuthorizeAsync(
            dialog.ManagerUserName,
            dialog.ManagerPassword,
            featureCode);

        if (!result.Succeeded)
        {
            XtraMessageBox.Show(
                this,
                result.ErrorMessage ?? "Manager authorization failed.",
                "Authorization Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return null;
        }

        return result;
    }

    private async Task CancelOrderAsync()
    {
        if (_currentOrder is null) return;
        using var form = new TextPromptForm("Cancel Order", "Reason for cancellation:", "Customer cancelled", required: true);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            var cancelledNumber = _currentOrder.OrderNumber;
            await _mediator.Send(new CancelOrderCommand(_currentOrder.OrderId, form.Value!));
            await LogActivityAsync("Cancel Order", $"Cancelled order {cancelledNumber}. Reason: {form.Value}.");
            _currentOrder = null;
            _tablePicker.SelectId(null);
            _addQuantityEdit.Value = 1;
            await RefreshOrderAsync();
            await RefreshActiveOrdersAsync();
            await ReloadMenuItemsAsync();
        }
    }

    private async Task SendToKitchenAsync()
    {
        var ticket = await _mediator.Send(new SendOrderToKitchenCommand(_currentOrder!.OrderId));
        XtraMessageBox.Show(this, $"Kitchen ticket created ({ticket.Status}).", "Sent to Kitchen", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task CompleteAsync()
    {
        var totals = await _mediator.Send(new GetOrderSummaryQuery(_currentOrder!.OrderId));
        if (totals.Balance > 0.005m)
        {
            XtraMessageBox.Show(
                this,
                $"This bill still has {CurrencyDisplay.FormatPlain(totals.Balance)} outstanding. Collect payment before completing the order.",
                "Payment Not Collected",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        await _mediator.Send(new CompleteOrderCommand(_currentOrder!.OrderId));

        _currentOrder = null;
        _tablePicker.SelectId(null);
        _addQuantityEdit.Value = 1;
        await ReloadMenuItemsAsync();
        await ReloadTablesAsync();
        await RefreshOrderAsync();
        await RefreshActiveOrdersAsync();
    }

    private async Task ShowPaymentHistoryAsync()
    {
        if (_currentOrder is null)
        {
            return;
        }

        using var dialog = new PaymentHistoryDialog(_mediator, _logger, _currentOrder.OrderId, _currentSession.DisplayName ?? "Unknown");
        dialog.PaymentsChanged += async (_, _) => await TryRunAsync(RefreshOrderAsync, "refresh the bill after a payment change");
        dialog.ShowDialog(this);
        await RefreshOrderAsync();
    }

    private async Task TransferTableAsync()
    {
        var tables = await _mediator.Send(new ListAllTablesQuery());
        var options = tables
            .Where(t => t.OccupancyStatus == "Available" && t.TableId != _currentOrder!.TableId)
            .Select(t => (t.TableId, t.Code))
            .ToList();

        if (options.Count == 0)
        {
            XtraMessageBox.Show(this, "No available tables to transfer to.", "No Tables Available", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var form = new TableTransferDialog(options);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _currentOrder = await _mediator.Send(new TransferOrderTableCommand(_currentOrder!.OrderId, form.NewTableId!.Value));
            _tablePicker.SelectId(_currentOrder.TableId);
            await RefreshOrderAsync();
        }
    }

    private async Task MergeTablesAsync()
    {
        var tables = await _mediator.Send(new ListAllTablesQuery());
        var options = tables.Select(t => (t.TableId, t.Code)).ToList();

        using var form = new MergeTablesDialog(options, _currentOrder!.TableId);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _currentOrder = await _mediator.Send(new MergeTablesCommand(form.SourceTableId!.Value, form.TargetTableId!.Value));
            _tablePicker.SelectId(_currentOrder.TableId);
            await RefreshOrderAsync();
        }
    }

    private async Task SplitBillAsync()
    {
        var lines = await _mediator.Send(new ListOrderLinesByOrderQuery(_currentOrder!.OrderId));
        var activeLines = lines.Where(l => !l.IsVoided).ToList();
        if (activeLines.Count == 0)
        {
            XtraMessageBox.Show(this, "There are no active lines to split.", "Nothing to Split", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var lineOptions = activeLines.Select(l => (l.OrderLineId, $"{ResolveVariantName(l.ProductVariantId)} x{l.Quantity:N2}")).ToList();

        var tables = await _mediator.Send(new ListAllTablesQuery());
        var tableOptions = tables.Where(t => t.OccupancyStatus == "Available").Select(t => (t.TableId, t.Code)).ToList();
        if (tableOptions.Count == 0)
        {
            XtraMessageBox.Show(this, "No available tables for the split.", "No Tables Available", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var form = new BillSplitDialog(lineOptions, tableOptions);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new SplitOrderCommand(_currentOrder!.OrderId, form.SelectedOrderLineIds, form.TargetTableId!.Value));
            await RefreshOrderAsync();
        }
    }

    private async Task SetOrderNotesAsync()
    {
        using var form = new TextPromptForm("Order Notes", "Notes:", _currentOrder!.Notes);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _currentOrder = await _mediator.Send(new SetOrderNotesCommand(_currentOrder!.OrderId, form.Value));
            await RefreshOrderAsync();
        }
    }

    private async Task SetCustomerNotesAsync()
    {
        using var form = new TextPromptForm("Customer Notes", "Notes:", _currentOrder!.CustomerNotes);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _currentOrder = await _mediator.Send(new SetOrderCustomerNotesCommand(_currentOrder!.OrderId, form.Value));
            await RefreshOrderAsync();
        }
    }

    private async Task AddDiscountAsync()
    {
        using var form = new DiscountDialog();
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new ApplyDiscountToOrderCommand(_currentOrder!.OrderId, form.DiscountType, form.Value, form.Reason));
            await RefreshOrderAsync();
            await LogActivityAsync("Discount", $"{_currentOrder!.OrderNumber}: {form.DiscountType} {form.Value} ({form.Reason})");
        }
    }

    private async Task RemoveDiscountAsync()
    {
        var discounts = await _mediator.Send(new ListDiscountsByOrderQuery(_currentOrder!.OrderId));
        if (discounts.Count == 0)
        {
            XtraMessageBox.Show(this, "There are no discounts to remove.", "No Discounts", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var options = discounts.Select(d => (d.DiscountId, d.DiscountType == "Percentage" ? $"{d.DiscountType} {d.Value:N2}% - {d.Reason}" : $"{d.DiscountType} {CurrencyDisplay.Format(d.Value)} - {d.Reason}")).ToList();
        using var form = new SelectionPromptForm("Remove Discount", "Discount:", options);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new RemoveDiscountFromOrderCommand(_currentOrder!.OrderId, form.SelectedId!.Value));
            await RefreshOrderAsync();
        }
    }

    private async Task AddServiceChargeAsync()
    {
        using var form = new ServiceChargeDialog();
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new ApplyServiceChargeToOrderCommand(_currentOrder!.OrderId, form.ServiceChargeType, form.Value, form.Reason));
            await RefreshOrderAsync();
        }
    }

    private async Task RemoveServiceChargeAsync()
    {
        var charges = await _mediator.Send(new ListServiceChargesByOrderQuery(_currentOrder!.OrderId));
        if (charges.Count == 0)
        {
            XtraMessageBox.Show(this, "There are no service charges to remove.", "No Service Charges", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var options = charges.Select(c => (c.ServiceChargeId, c.ServiceChargeType == "Percentage" ? $"{c.ServiceChargeType} {c.Value:N2}% - {c.Reason}" : $"{c.ServiceChargeType} {CurrencyDisplay.Format(c.Value)} - {c.Reason}")).ToList();
        using var form = new SelectionPromptForm("Remove Service Charge", "Service Charge:", options);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new RemoveServiceChargeFromOrderCommand(_currentOrder!.OrderId, form.SelectedId!.Value));
            await RefreshOrderAsync();
        }
    }

    private async Task AddByBarcodeAsync()
    {
        var value = _barcodeEdit.Text.Trim();
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        if (_currentOrder is null)
        {
            XtraMessageBox.Show(this, "Start a New Dine-In or New Take Away order first.", "No Order Started", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Guid variantId;
        try
        {
            var barcode = await _mediator.Send(new GetBarcodeByValueQuery(value));
            variantId = barcode.ProductVariantId;
        }
        catch (NotFoundException)
        {
            var bySku = _variantsById.Values.FirstOrDefault(v => v.Sku.Equals(value, StringComparison.OrdinalIgnoreCase));
            if (bySku is null)
            {
                XtraMessageBox.Show(this, $"No product found for '{value}'.", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            variantId = bySku.ProductVariantId;
        }

        await AddProductToCurrentOrder(variantId, _addQuantityEdit.Value);
        _barcodeEdit.Text = string.Empty;
    }

    private async Task BumpQuantityAsync(decimal delta)
    {
        if (_lineGridView.GetFocusedRow() is not OrderLineRow row)
        {
            return;
        }

        var newQuantity = Math.Max(1, row.Quantity + delta);
        if (newQuantity == row.Quantity)
        {
            return;
        }

        await EnsureOrderResumedIfHeldAsync();
        _hasUnsavedEdits = true;
        await _mediator.Send(new SetOrderLineQuantityCommand(row.OrderLineId, newQuantity));
        await RefreshOrderAsync();
    }

    private async Task EditQuantityAsync()
    {
        if (_lineGridView.GetFocusedRow() is not OrderLineRow row)
        {
            return;
        }

        using var form = new QuantityPromptForm("Edit Quantity", "Quantity:");
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await EnsureOrderResumedIfHeldAsync();
            _hasUnsavedEdits = true;
            await _mediator.Send(new SetOrderLineQuantityCommand(row.OrderLineId, form.Quantity));
            await RefreshOrderAsync();
        }
    }

    private async Task EditLineNotesAsync()
    {
        if (_lineGridView.GetFocusedRow() is not OrderLineRow row)
        {
            return;
        }

        using var form = new TextPromptForm("Item Notes", "Notes:", row.Notes);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await EnsureOrderResumedIfHeldAsync();
            _hasUnsavedEdits = true;
            await _mediator.Send(new SetOrderLineNotesCommand(row.OrderLineId, form.Value));
            await RefreshOrderAsync();
        }
    }

    private async Task OverridePriceAsync()
    {
        if (_lineGridView.GetFocusedRow() is not OrderLineRow row)
        {
            return;
        }

        using var form = new PriceOverrideDialog(row.Name, row.UnitPrice);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await EnsureOrderResumedIfHeldAsync();
            _hasUnsavedEdits = true;
            var performedBy = _currentSession.DisplayName ?? "Unknown";
            await _mediator.Send(new OverrideOrderLinePriceCommand(row.OrderLineId, form.NewPrice, form.Reason, performedBy));
            await RefreshOrderAsync();
            await LogActivityAsync("Price Override", $"{_currentOrder!.OrderNumber}: {row.Name} {CurrencyDisplay.FormatPlain(row.UnitPrice)} -> {CurrencyDisplay.FormatPlain(form.NewPrice)} ({form.Reason})");
        }
    }

    private async Task VoidLineAsync()
    {
        if (_lineGridView.GetFocusedRow() is not OrderLineRow row)
        {
            return;
        }

        await EnsureOrderResumedIfHeldAsync();
        _hasUnsavedEdits = true;
        await _mediator.Send(new VoidOrderLineCommand(row.OrderLineId));
        await RefreshOrderAsync();
    }

    private async Task RemoveLineAsync()
    {
        if (_lineGridView.GetFocusedRow() is not OrderLineRow row)
        {
            return;
        }

        await EnsureOrderResumedIfHeldAsync();
        _hasUnsavedEdits = true;
        await _mediator.Send(new RemoveOrderLineCommand(_currentOrder!.OrderId, row.OrderLineId));
        await RefreshOrderAsync();
        await LogActivityAsync("Remove Line", $"{_currentOrder.OrderNumber}: {row.Name}");
    }

    private async Task RefreshOrderAsync()
    {
        var focusedLineId = _lineGridView.GetFocusedRow() is OrderLineRow focused ? focused.OrderLineId : (Guid?)null;

        _isRefreshingOrder = true;
        try
        {
            await BindOrderPaymentAsync(_currentOrder?.OrderId, _currentSession.DisplayName ?? "Unknown");

            if (_currentOrder is null)
            {
                _currentOrderLines = [];
                _lineGrid.DataSource = null;
                RenderOrderedItemsList([]);
                if (_lblCartTableNo is not null) _lblCartTableNo.Text = "No Table selected";
                if (_tablePicker.SelectedId == null)
                {
                    _tablePicker.SelectId(null);
                }
                _tablePicker.Enabled = true;
                if (_lblCartOrderNo is not null) _lblCartOrderNo.Text = "No active order";
                UpdateBillEmptyState(isEmpty: true);
                SetTotals(null);
                _orderStatusLabel.Text = PosStrings.NoOrderSelected;
                UpdateOrderStatusBadge();
                UpdateButtonStates();

                SetSelectedCustomerId(Guid.Empty);
                _customerPicker.Enabled = false;
                _newCustomerButton.Enabled = false;
                _customerDetailsLabel.Text = string.Empty;
                await RefreshActiveOrdersAsync();
                return;
            }

            _currentOrder = await _mediator.Send(new GetOrderByIdQuery(_currentOrder.OrderId));

            var isEditable = _currentOrder.Status is "Open" or "Held";
            _customerPicker.Enabled = isEditable;
            _newCustomerButton.Enabled = isEditable;

            SetSelectedCustomerId(_currentOrder.CustomerId ?? Guid.Empty);

            if (_currentOrder.CustomerId is { } custId)
            {
                var customer = await _mediator.Send(new GetCustomerByIdQuery(custId));
                if (customer is not null)
                {
                    _customerDetailsLabel.Text = $"{customer.Name} • Outstanding: {CurrencyDisplay.FormatPlain(customer.OutstandingBalance)}";
                    _customerDetailsLabel.ForeColor = customer.OutstandingBalance > 0 ? Color.Red : Color.Green;
                }
                else
                {
                    _customerDetailsLabel.Text = string.Empty;
                }
            }
            else
            {
                _customerDetailsLabel.Text = $"Walk-in Customer • Outstanding: {CurrencyDisplay.FormatPlain(0m)}";
                _customerDetailsLabel.ForeColor = Color.Green;
            }

            var lines = (await _mediator.Send(new ListOrderLinesByOrderQuery(_currentOrder.OrderId)))
                .Where(l => !l.IsVoided)
                .ToList();
            _currentOrderLines = lines;
            var rows = lines.Select(l => new OrderLineRow(
                l.OrderLineId,
                ResolveVariantSku(l.ProductVariantId),
                ResolveVariantName(l.ProductVariantId),
                l.Quantity,
                l.UnitPrice,
                l.LineTotal,
                l.Notes ?? string.Empty,
                l.IsVoided,
                l.IsPriceOverridden)).ToList();
            _lineGrid.DataSource = rows;
            RestoreFocusedLine(focusedLineId, rows);
            RenderOrderedItemsList(lines);

            var tables = await _mediator.Send(new ListAllTablesQuery());
            var tableCodes = tables.ToDictionary(t => t.TableId, t => t.Code);
            if (_lblCartTableNo is not null)
            {
                _lblCartTableNo.Text = _currentOrder.OrderType == "TakeAway" ? "Take Away" : tableCodes.TryGetValue(_currentOrder.TableId ?? Guid.Empty, out var code) ? $"Table No #{code}" : "Dine In";
            }
            if (_currentOrder.OrderType == "TakeAway")
            {
                _tablePicker.SelectId(null);
                _tablePicker.Enabled = false;
            }
            else
            {
                _tablePicker.Enabled = isEditable;
                _tablePicker.SelectId(_currentOrder.TableId);
            }
            if (_lblCartOrderNo is not null)
            {
                _lblCartOrderNo.Text = $"Order #{_currentOrder.OrderNumber}";
            }

            UpdateBillEmptyState(isEmpty: lines.Count == 0);

            var totals = await _mediator.Send(new GetOrderSummaryQuery(_currentOrder.OrderId));
            SetTotals(totals);

            _orderStatusLabel.Text = $"{_currentOrder.OrderNumber}  •  {_currentOrder.OrderType}  •  {_currentOrder.Status}";
            UpdateOrderStatusBadge();

            await ReloadTablesAsync();
            await RefreshActiveOrdersAsync();
        }
        finally
        {
            _isRefreshingOrder = false;
            UpdateButtonStates();
        }
    }

    /// <summary>
    /// Re-focuses the row whose <see cref="OrderLineRow.OrderLineId"/> matches
    /// the line that was focused before a cart rebind. Matching is by identity,
    /// not row index, because the refresh may reorder rows. If the line no
    /// longer exists (removed/voided), the grid keeps its default behavior.
    /// </summary>
    private void RestoreFocusedLine(Guid? orderLineId, List<OrderLineRow> rows)
    {
        if (orderLineId is null)
        {
            return;
        }

        var index = rows.FindIndex(r => r.OrderLineId == orderLineId);
        if (index >= 0)
        {
            _lineGridView.FocusedRowHandle = index;
            _lineGridView.SelectRow(index);
        }
    }

    private void SetTotals(OrderTotals? totals)
    {
        _subtotalLabel.Text = $"{PosStrings.Subtotal}: {CurrencyDisplay.FormatPlain(totals?.Subtotal ?? 0m)}";
        _discountLabel.Text = $"{PosStrings.Discount}: -{CurrencyDisplay.FormatPlain(totals?.DiscountTotal ?? 0m)}";
        _taxLabel.Text = $"{PosStrings.Tax}: {CurrencyDisplay.FormatPlain(totals?.TaxTotal ?? 0m)}";
        _serviceChargeLabel.Text = $"{PosStrings.ServiceCharge}: {CurrencyDisplay.FormatPlain(totals?.ServiceChargeTotal ?? 0m)}";
        _grandTotalLabel.Text = $"{PosStrings.GrandTotal}: {CurrencyDisplay.FormatPlain(totals?.GrandTotal ?? 0m)}";
        _paidLabel.Text = $"{PosStrings.Paid}: {CurrencyDisplay.FormatPlain(totals?.PaidTotal ?? 0m)}";
        _balanceLabel.Text = $"{PosStrings.Balance}: {CurrencyDisplay.FormatPlain(totals?.Balance ?? 0m)}";

        if (_lblSummarySubtotal is not null) _lblSummarySubtotal.Text = CurrencyDisplay.FormatPlain(totals?.Subtotal ?? 0m);
        if (_lblSummaryTax is not null) _lblSummaryTax.Text = CurrencyDisplay.FormatPlain(totals?.TaxTotal ?? 0m);
        if (_lblSummaryDiscount is not null) _lblSummaryDiscount.Text = $"-{CurrencyDisplay.FormatPlain(totals?.DiscountTotal ?? 0m)}";
        if (_lblSummaryService is not null) _lblSummaryService.Text = CurrencyDisplay.FormatPlain(totals?.ServiceChargeTotal ?? 0m);
        if (_lblSummaryTotalPayable is not null) _lblSummaryTotalPayable.Text = CurrencyDisplay.FormatPlain(totals?.GrandTotal ?? 0m);

        _balance = totals?.Balance ?? 0m;
        _paymentBalanceLabel.Text = CurrencyDisplay.FormatPlain(_balance);
        if (_amountEntryIsPreset)
        {
            _amountEdit.Text = FormatPlain(_balance);
        }
        UpdateChangeDisplay();
    }

    private async void PrintBillButton_Click(object? sender, EventArgs e) => await TryRunAsync(PrintBillAsync, "print the bill");

    private async Task PrintBillAsync()
    {
        if (_currentOrder is null)
        {
            return;
        }

        var receipt = await ReceiptFormatter.FormatAsync(_mediator, _currentOrder);
        using var preview = new ReceiptPreviewForm(receipt);
        preview.ShowDialog(this);
        await LogActivityAsync("Print", _currentOrder.OrderNumber);
    }

    private async Task LogActivityAsync(string action, string? details = null)
    {
        try
        {
            await _mediator.Send(new RecordActivityCommand(action, details, _currentSession.DisplayName ?? "Unknown", Environment.MachineName));
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Swallowed per architecture guidelines
        }
    }

    private Task TryRunAsync(Func<Task> action, string actionDescription) =>
        GuardedAction.RunAsync(this, _logger, action, actionDescription, RefreshOrderAsync);

    private void UpdateOrderStatusBadge()
    {
        var (back, fore) = _currentOrder?.Status switch
        {
            "Open" => (Color.FromArgb(223, 240, 216), Color.FromArgb(39, 174, 96)),
            "Held" => (Color.FromArgb(252, 235, 204), Color.FromArgb(211, 140, 32)),
            "Voided" or "Cancelled" => (Color.FromArgb(250, 224, 224), Color.FromArgb(192, 57, 43)),
            "Completed" => (Color.FromArgb(214, 234, 248), Color.FromArgb(41, 128, 185)),
            _ => (Color.FromArgb(236, 240, 241), Color.Gray),
        };

        _orderStatusLabel.Appearance.BackColor = back;
        _orderStatusLabel.Appearance.ForeColor = fore;
        _orderStatusLabel.Appearance.Options.UseForeColor = true;
    }

    private string ResolveVariantSku(Guid variantId) => _variantsById.TryGetValue(variantId, out var v) ? v.Sku : "(unknown)";

    /// <summary>
    /// Display name for a cart/order line: the PRIMARY line is always the
    /// actual menu item (product) name; the portion label ("Half"/"Full")
    /// is joined on the SAME line as "Product - Portion".
    /// </summary>
    private string ResolveVariantName(Guid variantId)
    {
        if (!_variantsById.TryGetValue(variantId, out var v))
        {
            return "(unknown)";
        }

        var productName = _productNamesById.GetValueOrDefault(v.ProductId, string.Empty);
        if (string.IsNullOrEmpty(productName))
        {
            // No product record available: use the variant's own name minus
            // any "Product | Portion" convention.
            var pipeIndex = v.Name.IndexOf('|');
            return pipeIndex > 0 ? v.Name[..pipeIndex].Trim() : v.Name;
        }

        var portion = GetVariantPosLabel(v, productName);
        return (portion is "Regular" || string.IsNullOrEmpty(portion))
            ? productName
            : $"{productName} - {portion}";
    }

    private async Task UpdatePermissionsAsync()
    {
        if (_currentSession.UserId is not { } userId)
        {
            _permissions = [];
            return;
        }

        string[] operations =
        [
            "create", "hold", "resume", "void", "cancel", "reopen", "sendtokitchen", "complete", "pay",
            "transfertable", "mergetables", "splitbill", "notes", "discount", "servicecharge", "additem", "editline",
            "priceoverride",
        ];

        _permissions = new Dictionary<string, bool>();
        foreach (var operation in operations)
        {
            _permissions[operation] = await _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}");
        }
    }

    /// <summary>
    /// Validates in-place edits of the cart's Price column: the value must be
    /// a non-negative number. Everything else about the override (permission,
    /// reason capture, persistence) is handled in
    /// <see cref="LineGridView_CellValueChanged"/> via the existing
    /// <c>OverrideOrderLinePriceCommand</c> path.
    /// </summary>
    private void LineGridView_ValidatingEditor(object? sender, DevExpress.XtraEditors.Controls.BaseContainerValidateEditorEventArgs e)
    {
        if (_lineGridView.FocusedColumn == _lineGridColumnUnitPrice)
        {
            if (!decimal.TryParse(Convert.ToString(e.Value), out var price) || price < 0)
            {
                e.Valid = false;
                e.ErrorText = "Enter a valid non-negative price.";
            }
        }
        else if (_lineGridView.FocusedColumn == _lineGridColumnQuantity)
        {
            // Same rule the +/- buttons enforce via the domain: quantity must
            // be a whole number of at least 1.
            if (!decimal.TryParse(Convert.ToString(e.Value), out var quantity) || quantity < 1 || quantity != Math.Floor(quantity))
            {
                e.Valid = false;
                e.ErrorText = "Enter a quantity of 1 or more.";
            }
        }
    }

    /// <summary>
    /// Persists an in-place Price-cell edit through the same
    /// <c>OverrideOrderLinePriceCommand</c> the Price Override button uses
    /// (so the permission gate, reason capture, and audit trail are shared),
    /// then refreshes - which re-focuses the same order line by id.
    /// </summary>
    private async void LineGridView_CellValueChanged(object? sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
    {
        if (_isRefreshingOrder || (e.Column != _lineGridColumnUnitPrice && e.Column != _lineGridColumnQuantity))
        {
            return;
        }

        if (_lineGridView.GetRow(e.RowHandle) is not OrderLineRow row)
        {
            return;
        }

        if (e.Column == _lineGridColumnUnitPrice)
        {
            var newPrice = Convert.ToDecimal(e.Value);
            if (newPrice == row.UnitPrice)
            {
                return;
            }

            await TryRunAsync(async () =>
            {
                await _mediator.Send(new OverrideOrderLinePriceCommand(row.OrderLineId, newPrice, "Inline price edit (POS cart)", _currentSession.DisplayName ?? "Unknown"));
                await RefreshOrderAsync();
            }, "update the price");
        }
        else
        {
            var newQuantity = Convert.ToDecimal(e.Value);
            if (newQuantity == row.Quantity)
            {
                return;
            }

            // Same command the +/- buttons and Edit Quantity dialog use.
            await TryRunAsync(async () =>
            {
                await _mediator.Send(new SetOrderLineQuantityCommand(row.OrderLineId, newQuantity));
                await RefreshOrderAsync();
            }, "update the quantity");
        }
    }

    private bool Permit(string operation) => _permissions.GetValueOrDefault(operation, false);

    private void UpdateCategoryScrollButtons()
    {
        if (_categoryButtonsPanel == null || _categoryButtonsPanel.IsDisposed) return;
        if (_btnCategoriesScrollLeft == null || _btnCategoriesScrollLeft.IsDisposed) return;
        if (_btnCategoriesScrollRight == null || _btnCategoriesScrollRight.IsDisposed) return;

        Action update = () =>
        {
            if (_categoryButtonsPanel.IsDisposed || _btnCategoriesScrollLeft.IsDisposed || _btnCategoriesScrollRight.IsDisposed) return;
            _btnCategoriesScrollLeft.Enabled = _categoryButtonsPanel.HorizontalScroll.Value > 0;
            int maxRight = 0;
            foreach (Control c in _categoryButtonsPanel.Controls) { maxRight = Math.Max(maxRight, c.Right); }
            _btnCategoriesScrollRight.Enabled = maxRight > _categoryButtonsPanel.HorizontalScroll.Value + _categoryButtonsPanel.ClientSize.Width + 5;
        };

        if (this.IsDisposed) return;

        if (this.IsHandleCreated)
        {
            this.BeginInvoke(update);
        }
        else
        {
            update();
        }
    }
    

    private void UpdateButtonStates()
    {
        var status = _currentOrder?.Status;
        var isOpen = status == "Open";
        var isHeld = status == "Held";
        var isDineIn = _currentOrder?.OrderType == "DineIn";
        var canReopen = status is "Voided" or "Cancelled";
        var hasOrder = _currentOrder is not null;
        var canEdit = isOpen || isHeld;

        _newDineInButton.Enabled = !hasOrder && Permit("create");
        _newTakeAwayButton.Enabled = !hasOrder && Permit("create");

        _holdButton.Enabled = isOpen && Permit("hold");
        _resumeButton.Enabled = isHeld && Permit("resume");
        _recallButton.Enabled = Permit("resume");
        _clearButton.Enabled = hasOrder || _currentOrderLines.Count > 0;
        _voidOrderButton.Enabled = canEdit && Permit("void");
        _cancelOrderButton.Enabled = canEdit && Permit("cancel");
        _cancelOrderButton.Visible = canEdit && hasOrder;
        _reopenButton.Enabled = canReopen && Permit("reopen");
        _sendToKitchenButton.Enabled = canEdit && Permit("sendtokitchen");
        _completeButton.Enabled = canEdit && Permit("complete");

        // Enable or disable the payment entry controls (excluding payment methods):
        var canPay = canEdit && Permit("pay");
        pnlAmountTendered.Enabled = canPay;
        pnlKeypad.Enabled = canPay;
        pnlQuickCash.Enabled = canPay;
        _recordButton.Enabled = canPay;
        _splitPaymentButton.Enabled = canPay;

        _paymentHistoryButton.Enabled = hasOrder;
        _transferTableButton.Enabled = canEdit && isDineIn && Permit("transfertable");
        _mergeTablesButton.Enabled = canEdit && isDineIn && Permit("mergetables");
        _splitBillButton.Enabled = canEdit && isDineIn && Permit("splitbill");
        _orderNotesButton.Enabled = canEdit && Permit("notes");
        _customerNotesButton.Enabled = canEdit && Permit("notes");
        _addDiscountButton.Enabled = canEdit && Permit("discount");
        _removeDiscountButton.Enabled = canEdit && Permit("discount");
        _addServiceChargeButton.Enabled = canEdit && Permit("servicecharge");
        _removeServiceChargeButton.Enabled = canEdit && Permit("servicecharge");

        _addByBarcodeButton.Enabled = canEdit && Permit("additem");
        _decreaseQuantityButton.Enabled = canEdit && Permit("editline");
        _increaseQuantityButton.Enabled = canEdit && Permit("editline");
        _editQuantityButton.Enabled = canEdit && Permit("editline");
        _editLineNotesButton.Enabled = canEdit && Permit("editline");
        _voidLineButton.Enabled = canEdit && Permit("editline");
        _removeLineButton.Enabled = canEdit && Permit("editline");
        _overridePriceButton.Enabled = canEdit && Permit("priceoverride");
        // The in-place Price-cell edit uses the same "priceoverride" permission
        // as the button; without it the column stays read-only.
        _lineGridColumnUnitPrice.OptionsColumn.ReadOnly = !(canEdit && Permit("priceoverride"));
        // The in-place Qty-cell edit uses the same "editline" permission as
        // the +/- buttons and Edit Quantity dialog.
        _lineGridColumnQuantity.OptionsColumn.ReadOnly = !(canEdit && Permit("editline"));

        _printBillButton.Enabled = hasOrder;
        _moreActionsButton.Enabled = hasOrder;
    }

    /// <summary>
    /// Rebuilds the searchable customer dropdown's data source - a Walk-in
    /// row (<see cref="Guid.Empty"/>, the same sentinel <c>_currentOrder.CustomerId</c>
    /// being <see langword="null"/> already maps to everywhere else in this
    /// class) followed by every active <see cref="Clovent.Restaurant.Application.Customers.Dtos.CustomerDto"/> from the
    /// existing <see cref="ListCustomersQuery"/> - no new query/search
    /// infrastructure, the same read this screen already used before the
    /// picker itself changed. <see cref="SetSelectedCustomerId"/> restores
    /// whichever id was selected, since reassigning
    /// <see cref="DevExpress.XtraEditors.LookUpEditBase.Properties"/>.DataSource
    /// resets <c>EditValue</c>.
    /// </summary>
    private async Task ReloadCustomersAsync(Guid? selectCustomerId = null)
    {
        var customers = await _mediator.Send(new ListCustomersQuery());
        List<CustomerPickerRow> pickerItems = [new CustomerPickerRow(Guid.Empty, "Walk-in Customer", string.Empty, string.Empty)];
        pickerItems.AddRange(customers
            .Where(c => c.IsActive)
            .Select(c => new CustomerPickerRow(c.CustomerId, c.Name, c.MobileNumber, CurrencyDisplay.FormatPlain(c.OutstandingBalance))));

        _customerPicker.Properties.DataSource = pickerItems;
        SetSelectedCustomerId(selectCustomerId ?? Guid.Empty);
    }

    private void SetSelectedCustomerId(Guid customerId)
    {
        var wasRefreshing = _isRefreshingOrder;
        _isRefreshingOrder = true;
        try
        {
            _customerPicker.EditValue = customerId;
        }
        finally
        {
            _isRefreshingOrder = wasRefreshing;
        }
    }

    private async void CustomerPicker_SelectionChanged(object? sender, EventArgs e)
    {
        if (_isRefreshingOrder || _currentOrder is null)
            return;

        var selectedGuid = _customerPicker.EditValue is Guid guid ? guid : Guid.Empty;
        var selectedId = selectedGuid == Guid.Empty ? (Guid?)null : selectedGuid;
        if (_currentOrder.CustomerId != selectedId)
        {
            await TryRunAsync(
                async () => await RunOrderActionAsync(new SetOrderCustomerCommand(_currentOrder.OrderId, selectedId)),
                "set the order's customer");
        }
    }

    private async void NewCustomerButton_Click(object? sender, EventArgs e)
    {
        using var form = new CustomerEditForm("New Customer");
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await TryRunAsync(async () =>
            {
                var newCustomer = await _mediator.Send(new CreateCustomerCommand(
                    form.CodeValue,
                    form.NameValue,
                    form.MobileValue,
                    form.AddressValue,
                    form.EmailValue,
                    form.OpeningBalanceValue,
                    form.CreditLimitValue,
                    form.NotesValue,
                    form.ShopNoValue,
                    form.Mobile2Value,
                    form.PhoneValue));

                await ReloadCustomersAsync(newCustomer.CustomerId);

                if (_currentOrder is not null && _currentOrder.Status is "Open" or "Held")
                {
                    await RunOrderActionAsync(new SetOrderCustomerCommand(_currentOrder.OrderId, newCustomer.CustomerId));
                }
            }, "create new customer");
        }
    }

    // ==========================================
    // PAYMENT PANEL INTEGRATION CODE-BEHIND
    // ==========================================

    private async Task BindOrderPaymentAsync(Guid? orderId, string performedBy)
    {
        _orderId = orderId;
        _performedBy = performedBy;
        await LoadPaymentAsync();
    }

    private async Task LoadPaymentAsync()
    {

        if (!_methodsLoaded)
        {
            var methods = await _mediator.Send(new ListPaymentMethodsQuery());
            _paymentMethods = [.. methods.Where(m => m.Status == "Active").Select(m => (m.PaymentMethodId, m.Name))];
            BuildMethodButtons();
            _methodsLoaded = true;
        }

        if (_orderId is not { } orderId)
        {
            _balance = 0m;
            _paymentBalanceLabel.Text = "0.00";
            _amountEdit.Text = string.Empty;
            _amountEntryIsPreset = true;
            UpdateChangeDisplay();
            pnlAmountTendered.Enabled = false;
            pnlKeypad.Enabled = false;
            pnlQuickCash.Enabled = false;
            _recordButton.Enabled = false;
            return;
        }

        pnlAmountTendered.Enabled = true;
        pnlKeypad.Enabled = true;
        pnlQuickCash.Enabled = true;
        _recordButton.Enabled = true;

        var totals = await _mediator.Send(new GetOrderSummaryQuery(orderId));
        _balance = Math.Max(totals.Balance, 0m);
        _paymentBalanceLabel.Text = CurrencyDisplay.FormatPlain(_balance);
        _amountEdit.Text = FormatPlain(_balance);
        _amountEntryIsPreset = true;
        UpdateChangeDisplay();
    }

    private void BuildMethodButtons()
    {
        if (_paymentMethodLookup == null) return;

        _paymentMethodLookup.Properties.DataSource = _paymentMethods.Select(m => new { m.PaymentMethodId, m.Name }).ToList();
        _paymentMethodLookup.Properties.ValueMember = "PaymentMethodId";
        _paymentMethodLookup.Properties.DisplayMember = "Name";
        _paymentMethodLookup.Properties.Columns.Clear();
        _paymentMethodLookup.Properties.Columns.Add(new DevExpress.XtraEditors.Controls.LookUpColumnInfo("Name", "Payment Method"));

        if ((_selectedPaymentMethodId is null || _selectedPaymentMethodId == Guid.Empty) && _paymentMethods.Count > 0)
        {
            var configuredDefault = PosSettingsStore.LoadDefaultPaymentMethod();
            var preferredMethod = _paymentMethods.FirstOrDefault(m => string.Equals(m.Name, configuredDefault, StringComparison.OrdinalIgnoreCase));
            var preferredId = preferredMethod.PaymentMethodId != Guid.Empty 
                ? preferredMethod.PaymentMethodId 
                : _paymentMethods[0].PaymentMethodId;

            SelectPaymentMethod(preferredId);
        }
        else if (_selectedPaymentMethodId is { } id && id != Guid.Empty)
        {
            _paymentMethodLookup.EditValue = id;
        }

        _paymentMethodLookup.EditValueChanged -= PaymentMethodLookup_EditValueChanged;
        _paymentMethodLookup.EditValueChanged += PaymentMethodLookup_EditValueChanged;
    }

    private void PaymentMethodLookup_EditValueChanged(object? sender, EventArgs e)
    {
        if (_paymentMethodLookup.EditValue is Guid methodId && methodId != Guid.Empty)
        {
            _selectedPaymentMethodId = methodId;
            PosPaymentMethodPreferenceStore.Save(methodId);
        }
    }

    private void SelectPaymentMethod(Guid paymentMethodId)
    {
        _selectedPaymentMethodId = paymentMethodId;
        if (_paymentMethodLookup != null && !Equals(_paymentMethodLookup.EditValue, paymentMethodId))
        {
            _paymentMethodLookup.EditValue = paymentMethodId;
        }
        PosPaymentMethodPreferenceStore.Save(paymentMethodId);
    }

    private void UpdateMethodButtonSelection()
    {
        if (_paymentMethodLookup != null && _selectedPaymentMethodId is { } id && id != Guid.Empty)
        {
            if (!Equals(_paymentMethodLookup.EditValue, id))
            {
                _paymentMethodLookup.EditValue = id;
            }
        }
    }

    private static Color ResolveMethodColor(string methodName)
    {
        var name = methodName.ToLowerInvariant();

        return Color.FromArgb(71, 85, 105); // slate-500
    }

    private void StylePaymentControls()
    {
        var keypadButtons = new[]
        {
            _keypad7Button, _keypad8Button, _keypad9Button,
            _keypad4Button, _keypad5Button, _keypad6Button,
            _keypad1Button, _keypad2Button, _keypad3Button,
            _keypadDecimalButton, _keypad0Button, _keypadBackspaceButton,
            _keypadClearButton
        };
        foreach (var btn in keypadButtons)
        {
            if (btn is null) continue;
            btn.ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat;
            btn.Appearance.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btn.Appearance.BackColor = Color.White;
            btn.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
            btn.Appearance.BorderColor = DividerColor;
            btn.Appearance.Options.UseFont = true;
            btn.Appearance.Options.UseBackColor = true;
            btn.Appearance.Options.UseForeColor = true;
            btn.Appearance.Options.UseBorderColor = true;
            btn.Cursor = Cursors.Hand;
            btn.AllowFocus = false;
        }

        var quickCashButtons = new[]
        {
            _quickCash100Button, _quickCash200Button, _quickCash500Button,
            _quickCash1000Button, _quickCash2000Button, _quickCash5000Button
        };
        foreach (var btn in quickCashButtons)
        {
            if (btn is null) continue;
            btn.ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat;
            btn.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btn.Appearance.BackColor = Color.FromArgb(240, 253, 250); // teal-50
            btn.Appearance.ForeColor = Color.FromArgb(15, 118, 110); // teal-700
            btn.Appearance.BorderColor = DividerColor;
            btn.Appearance.Options.UseFont = true;
            btn.Appearance.Options.UseBackColor = true;
            btn.Appearance.Options.UseForeColor = true;
            btn.Appearance.Options.UseBorderColor = true;
            btn.Cursor = Cursors.Hand;
            btn.AllowFocus = false;
        }

        _recordButton.ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat;
        _recordButton.Appearance.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        _recordButton.Appearance.BackColor = Color.FromArgb(22, 163, 74);
        _recordButton.Appearance.ForeColor = Color.White;
        _recordButton.Appearance.Options.UseFont = true;
        _recordButton.Appearance.Options.UseBackColor = true;
        _recordButton.Appearance.Options.UseForeColor = true;
        _recordButton.Cursor = Cursors.Hand;
        _recordButton.AllowFocus = false;

        _splitPaymentButton.ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat;
        _splitPaymentButton.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _splitPaymentButton.Appearance.BackColor = Color.White;
        _splitPaymentButton.Appearance.ForeColor = AccentColor;
        _splitPaymentButton.Appearance.BorderColor = AccentColor;
        _splitPaymentButton.Appearance.Options.UseFont = true;
        _splitPaymentButton.Appearance.Options.UseBackColor = true;
        _splitPaymentButton.Appearance.Options.UseForeColor = true;
        _splitPaymentButton.Appearance.Options.UseBorderColor = true;
        _splitPaymentButton.Cursor = Cursors.Hand;
        _splitPaymentButton.AllowFocus = false;
    }

    /// <summary>
    /// Formats an amount for the tender field: two decimal places always, no
    /// currency symbol or grouping (the field is parsed back with
    /// <see cref="decimal.TryParse(string, out decimal)"/>, so it must stay
    /// plain).
    /// </summary>
    /// <remarks>
    /// "0.##" was dropping trailing zeros, so a balance of 72.50 pre-filled as
    /// "72.5" while every other money field on the screen showed two decimals
    /// (defect D20).
    /// </remarks>
    private static string FormatPlain(decimal amount) => amount.ToString("0.00");

    private void ExactAmountButton_Click(object? sender, EventArgs e)
    {
        _amountEdit.Text = FormatPlain(_balance);
        _amountEntryIsPreset = true;
    }

    /// <summary>
    /// Records one payment, and refuses to start a second while the first is
    /// still in flight. <see cref="RecordPaymentAsync"/> awaits the server
    /// several times, and every await hands the UI thread back to the message
    /// pump with this button still live - so a double-click used to enter the
    /// handler twice, both entries reading the same pre-payment
    /// <c>_balance</c> and each recording a full-balance payment (a $280 bill
    /// settled twice as $560). <c>GuardedAction</c> only catches exceptions;
    /// it has no re-entry guard of its own, and <c>ScreenOperationGate</c>
    /// queues the second command rather than discarding it, so the guard has
    /// to live here. Mirrors the <c>_isRefreshingOrder</c> flag the selection
    /// handlers already use. Belt-and-braces only: the authoritative ceiling
    /// is <c>RecordPaymentCommandHandler</c>'s server-side balance check.
    /// </summary>
    /// <summary>
    /// Opens <see cref="SplitPaymentDialog"/> and records the accepted
    /// allocations through the same <c>RecordPaymentCommand</c> the Record
    /// Payment button uses (one per method, in dialog order), then runs the
    /// normal refresh/auto-complete pipeline. Payment methods, permissions
    /// ("pos.pay" gates this button exactly like Record Payment), audit, and
    /// history are all shared with the single-method flow.
    /// </summary>
    private async void SplitPaymentButton_Click(object? sender, EventArgs e) => await TryRunAsync(SplitPaymentAsync, "record a split payment");

    private async Task SplitPaymentAsync()
    {
        if (_orderId is not { } orderId)
        {
            return;
        }

        if (_balance <= 0)
        {
            XtraMessageBox.Show(this, "There is no outstanding balance to split.", "Split Payment", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Re-query on every open so methods added or deactivated in Back
        // Office are reflected in each new split dialog. This is the same
        // ListPaymentMethodsQuery source the POS payment buttons load from;
        // it deliberately does not touch _paymentMethods or the currently
        // selected/default payment method.
        var activeMethods = (await _mediator.Send(new ListPaymentMethodsQuery()))
            .Where(m => m.Status == "Active")
            .Select(m => (m.PaymentMethodId, m.Name))
            .ToList();

        if (activeMethods.Count == 0)
        {
            XtraMessageBox.Show(this, "There are no active payment methods to split across.", "Split Payment", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SplitPaymentDialog(_balance, activeMethods);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var remaining = _balance;
        foreach (var (paymentMethodId, methodName, amount) in dialog.Allocations)
        {
            var applied = Math.Min(amount, Math.Max(remaining, 0m));
            if (applied <= 0)
            {
                continue;
            }

            await _mediator.Send(new RecordPaymentCommand(orderId, paymentMethodId, applied, false));
            remaining -= applied;
            await LogActivityAsync("Payment", $"{CurrencyDisplay.FormatPlain(applied)} via {methodName} (split)");
        }

        await RefreshOrderAsync();
        UpdateChangeDisplay();

        if (_currentOrder is not null && PosPaymentRules.ShouldAutoComplete(_currentOrder.Status, _balance, paymentRecorded: true))
        {
            await CompleteAsync();
        }
    }

    private async void RecordButton_Click(object? sender, EventArgs e)
    {
        if (_isRecordingPayment) return;

        _isRecordingPayment = true;
        _recordButton.Enabled = false;

        try
        {
            await TryRunAsync(RecordPaymentAsync, "record this payment");
        }
        finally
        {
            _isRecordingPayment = false;
            _recordButton.Enabled = true;
        }
    }

    private void AmountEdit_EditValueChanged(object? sender, EventArgs e) => UpdateChangeDisplay();

    private void QuickCashButton_Click(object? sender, EventArgs e)
    {
        if (sender is SimpleButton button)
        {
            _amountEdit.Text = button.Text;
            _amountEntryIsPreset = true;
        }
    }

    /// <summary>First digit after a preset amount (initial balance, Exact, Quick Cash, or Clear) replaces the field instead of appending, matching normal POS keypad behavior; subsequent digits append.</summary>
    private void KeypadDigitButton_Click(object? sender, EventArgs e)
    {
        if (sender is SimpleButton button)
        {
            _amountEdit.Text = _amountEntryIsPreset ? button.Text : _amountEdit.Text + button.Text;
            _amountEntryIsPreset = false;
        }
    }

    private void KeypadDecimalButton_Click(object? sender, EventArgs e)
    {
        if (_amountEntryIsPreset)
        {
            _amountEdit.Text = "0.";
            _amountEntryIsPreset = false;
            return;
        }

        if (!_amountEdit.Text.Contains('.'))
        {
            _amountEdit.Text += ".";
        }
    }

    private void KeypadBackspaceButton_Click(object? sender, EventArgs e)
    {
        var text = _amountEntryIsPreset ? string.Empty : _amountEdit.Text;
        _amountEdit.Text = text.Length > 0 ? text[..^1] : text;
        _amountEntryIsPreset = false;
    }

    private void KeypadClearButton_Click(object? sender, EventArgs e)
    {
        _amountEdit.Text = string.Empty;
        _amountEntryIsPreset = true;
    }

    private void UpdateChangeDisplay()
    {
        if (decimal.TryParse(_amountEdit.Text, out var tendered) && tendered > _balance)
        {
            var change = tendered - _balance;
            _changeValueLabel.Text = CurrencyDisplay.FormatPlain(change);
            _changeValueLabel.ForeColor = ChangeColor;
        }
        else
        {
            _changeValueLabel.Text = CurrencyDisplay.FormatPlain(0m);
            _changeValueLabel.ForeColor = Color.Gray;
        }
    }

    private async Task RecordPaymentAsync()
    {
        if (_orderId is not { } orderId)
        {
            return;
        }

        await EnsureOrderResumedIfHeldAsync();

        if (_selectedPaymentMethodId is not { } paymentMethodId)
        {
            XtraMessageBox.Show(this, "Select a payment method.", "No Payment Method Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!decimal.TryParse(_amountEdit.Text, out var tendered) || tendered <= 0)
        {
            XtraMessageBox.Show(this, "Enter a positive amount.", "Invalid Amount", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var methodName = _paymentMethods.FirstOrDefault(m => m.PaymentMethodId == paymentMethodId).Name ?? "(unknown)";

        var isCash = string.Equals(methodName, "Cash", StringComparison.OrdinalIgnoreCase);
        if (!isCash && tendered > _balance + 0.005m)
        {
            XtraMessageBox.Show(
                this,
                $"{methodName} cannot be tendered for more than the balance due ({CurrencyDisplay.FormatPlain(_balance)}).\n\nOnly Cash accepts an amount greater than the balance - the excess is handed back as change.",
                "Amount Exceeds Balance",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var applied = Math.Min(tendered, _balance);

        var isCredit = string.Equals(methodName, "Credit", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(methodName, "Customer Credit", StringComparison.OrdinalIgnoreCase);

        bool exceedCreditLimitApproved = false;
        if (isCredit)
        {
            var order = await _mediator.Send(new GetOrderByIdQuery(orderId));
            if (order?.CustomerId is null)
            {
                XtraMessageBox.Show(this, "A customer must be selected for Credit / Pay Later sales.", "No Customer Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var customer = await _mediator.Send(new GetCustomerByIdQuery(order.CustomerId.Value));
            if (customer is null)
            {
                XtraMessageBox.Show(this, "The customer associated with this order could not be found.", "Customer Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!customer.IsActive)
            {
                XtraMessageBox.Show(this, $"The customer '{customer.Name}' is inactive.", "Customer Inactive", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_currentSession.UserId is { } userId)
            {
                var hasCreditPermission = await _featurePolicy.CanUseFeatureAsync(userId, "pos.creditsale");
                if (!hasCreditPermission)
                {
                    XtraMessageBox.Show(this, "You do not have permission to perform a Credit Sale.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (customer.OutstandingBalance + applied > customer.CreditLimit)
                {
                    var situation = $"Credit limit exceeded.\n\n" +
                                    $"This customer currently owes {CurrencyDisplay.FormatPlain(customer.OutstandingBalance)}.\n" +
                                    $"The new sale would increase the balance to {CurrencyDisplay.FormatPlain(customer.OutstandingBalance + applied)}, " +
                                    $"but the credit limit is {CurrencyDisplay.FormatPlain(customer.CreditLimit)}.";

                    // Exceeding a credit limit is a manager decision whoever is
                    // standing at the till, so the challenge is unconditional.
                    // Previously an operator who happened to hold the permission
                    // was shown a plain Yes/No box, which meant anyone able to
                    // reach a signed-in POS could approve it by clicking Yes
                    // (defect D7).
                    var authorization = await RequestManagerAuthorizationAsync(
                        "Manager Authorization - Credit Limit Override",
                        $"{situation}\n\nA manager must authorize this credit sale.",
                        "pos.exceedcreditlimit");

                    if (authorization is null)
                    {
                        // Denied or cancelled: nothing has been sent to the
                        // application layer at this point, so the order,
                        // balance, ledger, payments and audit log are all
                        // untouched by simply returning.
                        return;
                    }

                    exceedCreditLimitApproved = true;
                    await LogActivityAsync(
                        "Override",
                        $"Authorized credit limit override for customer '{customer.Name}' ({customer.Code}). " +
                        $"Approved by manager '{authorization.ManagerDisplayName}'.");
                }
            }
        }

        await _mediator.Send(new RecordPaymentCommand(orderId, paymentMethodId, applied, exceedCreditLimitApproved));
        await RefreshOrderAsync();
        await LogActivityAsync("Payment", $"{CurrencyDisplay.FormatPlain(applied)} via {methodName}");

        UpdateChangeDisplay();

        // _balance is re-read by RefreshOrderAsync above, so this reflects the server's
        // post-payment balance rather than whatever was typed into the tender field.
        if (_currentOrder is not null && PosPaymentRules.ShouldAutoComplete(_currentOrder.Status, _balance, paymentRecorded: true))
        {
            await CompleteAsync();
        }

        if (_currentOrder is null)
        {
            var paymentForm = _recordButton.FindForm();
            if (paymentForm != null && paymentForm != this)
            {
                paymentForm.DialogResult = DialogResult.OK;
                paymentForm.Close();
            }
        }
    }

    /// <summary>Row shape for <see cref="_customerPicker"/>'s popup grid - <see cref="BalanceDisplay"/> is pre-formatted (via <see cref="CurrencyDisplay"/>) rather than a raw <see cref="decimal"/> since the popup grid has no currency-aware column type of its own.</summary>
    private sealed record CustomerPickerRow(Guid CustomerId, string Name, string Phone, string BalanceDisplay);

    private sealed record OrderLineRow(
        Guid OrderLineId,
        string Sku,
        string Name,
        decimal Quantity,
        decimal UnitPrice,
        decimal LineTotal,
        string Notes,
        bool IsVoided,
        bool IsPriceOverridden);
}

