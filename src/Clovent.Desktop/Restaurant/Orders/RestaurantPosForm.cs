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
using Clovent.Desktop.Navigation;
using Clovent.Desktop.Restaurant.Shared;
using Clovent.Desktop.Restaurant.SmartPos;
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
using Clovent.Restaurant.Application.CustomerReorder.Dtos;
using Clovent.Restaurant.Application.Discounts.Commands;
using Clovent.Restaurant.Application.Discounts.Queries;
using Clovent.Restaurant.Application.KitchenTickets.Commands;
using Clovent.Restaurant.Application.OrderHealth;
using Clovent.Restaurant.Application.QuickOrderTemplates.Dtos;
using Clovent.Restaurant.Application.QuickOrderTemplates.Queries;
using Clovent.Restaurant.Application.RestaurantPulse.Queries;
using Clovent.Restaurant.Application.SmartRecommendations.Dtos;
using Clovent.Restaurant.Application.SmartRecommendations.Commands;
using Clovent.Restaurant.Application.SmartRecommendations.Queries;
using Clovent.Restaurant.SmartRecommendations;
using Clovent.Restaurant.Application.UniversalPosSearch.Dtos;
using Clovent.Restaurant.Application.UniversalPosSearch.Queries;
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
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid;
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
    private IApplicationModeNavigator? _applicationModeNavigator;
    private readonly ContextMenuStrip _operationsMenu = new();
    private ToolStripMenuItem _shiftMenuItem = null!;
    private ToolStripMenuItem _openShiftMenuItem = null!;
    private ToolStripMenuItem _currentShiftMenuItem = null!;
    private ToolStripMenuItem _closeShiftMenuItem = null!;
    private ToolStripMenuItem _cashMovementMenuItem = null!;
    private ToolStripMenuItem _printLastReceiptMenuItem = null!;
    private ToolStripMenuItem _endOfDayMenuItem = null!;
    private ToolStripMenuItem _backOfficeMenuItem = null!;
    private bool _canAccessBackOffice;
    private bool _canPerformRefund;

    internal enum PosCloseInitiator
    {
        None,
        UserWindowClose,
        ModeSwitchToBackOffice,
        StartupFailureFallback,
        SessionSignOut
    }

    private PosCloseInitiator _closeInitiator = PosCloseInitiator.None;

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
    
    private LabelControl _lblFoodiesMenuHeader = null!;
    private TableLayoutPanel _tlpCenterRows = null!;
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
    private Guid? _defaultCustomerId;
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

    // ==========================================
    // SMART POS FEATURES (suggestions, quick
    // orders, order health, rush mode, universal
    // search, customer reorder, restaurant pulse)
    // ==========================================

    // Smart suggestion compact strip (single row above the ordered-items list):
    // Uses a compact trigger with a DevExpress PopupContainerEdit dropdown overlay.
    // The actual suggestion GridControl lives inside _suggestionPopupControl so it never
    // permanently consumes cart vertical space.
    private Panel _suggestionPanel = null!;
    private PictureEdit _suggestionHeaderIcon = null!;
    private LabelControl _suggestionHeaderLabel = null!;
    private PopupContainerEdit _suggestionPopupEdit = null!;
    private PopupContainerControl _suggestionPopupControl = null!;
    private GridControl _suggestionGrid = null!;
    private GridView _suggestionGridView = null!;
    private CheckEdit _suggestionSelectAllCheck = null!;
    private SimpleButton _suggestionAddSelectedButton = null!;
    private readonly List<SuggestedAddOnSelectionRow> _suggestionRows = [];
    private bool _suggestionSyncingSelection;
    private bool _suggestionAdding;
    internal readonly SuggestionDismissalTracker SuggestionTracker = new();
    private CancellationTokenSource? _suggestionCts;
    private string? _lastSuggestionBasketKey;
    private Guid? _suggestionOrderId;

    // Full (untruncated) recommendation list currently applicable to the
    // basket - feeds the embedded suggestion grid.
    private IReadOnlyList<BasketRecommendationDto> _currentSuggestions = [];

    // Upsell analytics: variants already counted as "offered" for the current
    // order, so repeated strip refreshes never inflate the offer count.
    private readonly HashSet<Guid> _recordedOfferedVariantIds = [];

    // Quick Orders strip:
    private Panel _quickOrdersStrip = null!;
    private SimpleButton _quickOrdersToggle = null!;
    private FlowLayoutPanel _quickOrdersFlowPanel = null!;
    private bool _quickOrdersExpanded;
    private IReadOnlyList<QuickOrderTemplateDto> _quickOrderTemplates = [];

    // Order health on the Active Orders rail:
    private System.Windows.Forms.Timer? _orderHealthTimer;
    private readonly Dictionary<Guid, OrderHealthCardState> _orderHealthCards = [];
    private OrderHealthThresholds _orderHealthThresholds = new();

    // Rush mode:
    internal readonly RushModeState RushMode = new();
    private LabelControl _rushModeBadge = null!;
    private TableLayoutPanel? _headerTable;

    // Universal smart search on the product search edit:
    private UniversalSearchDropdown? _searchDropdown;
    private CancellationTokenSource? _universalSearchCts;
    private readonly SearchResultsCache<UniversalPosSearchResultsDto> _universalSearchCache = new(TimeSpan.FromSeconds(10));

    // Smart More-menu entries (built at runtime so the Designer file stays untouched):
    private ToolStripSeparator _moreMenuSmartSeparator = null!;
    private ToolStripMenuItem _moreQuickOrdersItem = null!;
    private ToolStripMenuItem _moreShowSuggestionsItem = null!;
    private ToolStripMenuItem _moreRepeatLastOrderItem = null!;
    private ToolStripMenuItem _moreCustomerInsightsItem = null!;
    private ToolStripMenuItem _moreRestaurantPulseItem = null!;
    private ToolStripMenuItem _moreRushModeItem = null!;

    /// <summary>Label + creation time of one live Active Orders card, for cheap health ticks.</summary>
    private sealed record OrderHealthCardState(LabelControl StatusLabel, DateTimeOffset CreatedAtUtc, bool IsLive);

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
        _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<RestaurantPosForm>.Instance;
        _splashScreenService = null!;
        _applicationModeNavigator = null;

        try
        {
            InitializeComponent();
            AttachPickers();
            BuildOperationsMenu();
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
        ISplashScreenService splashScreenService,
        IApplicationModeNavigator? applicationModeNavigator = null)
    {
        try
        {
            _managerAuthorization = managerAuthorization;
            _scope = scopeFactory.CreateScope();
            _mediator = new SerializedMediator(_scope.ServiceProvider.GetRequiredService<IMediator>(), _gate);
            _featurePolicy = new SerializedFeatureAuthorizationPolicy(_scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>(), _gate);
            _logger = _scope.ServiceProvider.GetService<ILogger<RestaurantPosForm>>() ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<RestaurantPosForm>.Instance;
            _currentSession = currentSession;
            _changeNotifier = changeNotifier;
            _splashScreenService = splashScreenService;
            _applicationModeNavigator = applicationModeNavigator ?? _scope.ServiceProvider.GetService<IApplicationModeNavigator>();

            InitializeComponent();
            AttachPickers();
            BuildOperationsMenu();

            if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            {
                InitializeDesignTime();
                return;
            }

            InitializeRuntime();
            _logger?.LogInformation("POS_FORM_CONSTRUCTED: RestaurantPosForm constructor completed.");
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
        BuildEmbeddedSuggestionPanel();
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
        InitializeSmartFeatures();
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
            _logoLabel, _cashierLabel, _newTakeAwayButton, 
            _orderStatusLabel, _refreshButton, _printBillButton, _paymentHistoryButton, 
            _moreActionsButton, _operationsButton, _logoutButton, _productSearchEdit, pnlSearch, _productTilesFlow, 
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
            Name = "tlpHeaderNew",
            Dock = DockStyle.Fill,
            ColumnCount = 10,
            RowCount = 1,
            Margin = new Padding(0),
            BackColor = Color.White
        };
        tlpHeaderNew.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _headerTable = tlpHeaderNew;

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

        // Column 1: order starters. Dine-In has no button here any more -
        // selecting a table in the table picker starts the Dine-In order
        // automatically (see TablePicker_SelectionChanged).
        StyleHeaderAction(_newTakeAwayButton, "+ Take Away", Color.FromArgb(15, 23, 42), Color.White);
        tlpHeaderNew.Controls.Add(_newTakeAwayButton, 1, 0);
        tlpHeaderNew.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        // Column 2: global search
        _productSearchEdit.Dock = DockStyle.Fill;
        _productSearchEdit.Margin = new Padding(8, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(4, this), 8, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(4, this));
        _productSearchEdit.Properties.NullValuePrompt = "Search menu, orders and more...";
        _productSearchEdit.Properties.NullValuePromptShowForEmptyValue = true;
        _productSearchEdit.Properties.NullText = "";
        tlpHeaderNew.Controls.Add(_productSearchEdit, 2, 0);
        tlpHeaderNew.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        // Column 3: cashier identity
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
        tlpHeaderNew.Controls.Add(_cashierLabel, 3, 0);
        tlpHeaderNew.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        // Column 4: compact order status badge
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
        tlpHeaderNew.Controls.Add(_orderStatusLabel, 4, 0);
        tlpHeaderNew.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        // Column 5: Cancel Order button
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
        tlpHeaderNew.Controls.Add(_cancelOrderButton, 5, 0);
        tlpHeaderNew.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        // Columns 6-9: History / More Actions / Operations / Logout
        StyleHeaderAction(_paymentHistoryButton, "History", Color.White, Color.FromArgb(71, 85, 105));
        StyleHeaderAction(_moreActionsButton, "More ▼", Color.White, Color.FromArgb(71, 85, 105));

        StyleOperationsButton();

        StyleHeaderAction(_logoutButton, "Logout", Color.White, Color.FromArgb(220, 38, 38));
        
        tlpHeaderNew.Controls.Add(_paymentHistoryButton, 6, 0);
        tlpHeaderNew.Controls.Add(_moreActionsButton, 7, 0);
        tlpHeaderNew.Controls.Add(_operationsButton, 8, 0);
        tlpHeaderNew.Controls.Add(_logoutButton, 9, 0);
        tlpHeaderNew.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
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

        _tlpCenterRows = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Margin = new Padding(0),
            BackColor = Color.Transparent
        };
        _tlpCenterRows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _tlpCenterRows.RowStyles.Add(new RowStyle(SizeType.Absolute, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(36, this)));            // Row 0: Foodies Menu heading + View Toggle
        _tlpCenterRows.RowStyles.Add(new RowStyle(SizeType.Absolute, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(60, this)));            // Row 1: Category cards
        _tlpCenterRows.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));                                                              // Row 2: Quick Orders collapsible strip
        _tlpCenterRows.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));                                                             // Row 3: product viewport
        _tlpCenterRows.RowStyles.Add(new RowStyle(SizeType.Absolute, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(40, this)));            // Row 4: pagination footer

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
        _tlpCenterRows.Controls.Add(pnlFoodiesMenuHeader, 0, 0);

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
            _categoryButtonsPanel.Height = pnlCategoryButtonsWrapper.Height + Clovent.Desktop.Forms.Base.DesktopDpi.Scale(32, this); // push horizontal scrollbar below visible wrapper bounds
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
        _tlpCenterRows.Controls.Add(_categoriesScrollContainer, 0, 1);

        // Row 3: the product viewport (Row 2 is reserved for _quickOrdersStrip)
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
        _tlpCenterRows.Controls.Add(_productViewport, 0, 3);

        // Row 4: pagination footer
        _paginationPanel.Margin = new Padding(0);
        _tlpCenterRows.Controls.Add(_paginationPanel, 0, 4);

        pnlProducts.Controls.Add(_tlpCenterRows);

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

        // Add to parent container. WinForms docks children from highest index
        // down to index 0. BuildEmbeddedSuggestionPanel() (called next) will bring
        // _flowOrderedItems to index 0 so Fill docks LAST, after the Top-docked
        // suggestion panel and Bottom-docked cart actions have reserved space.
        _pnlOrderedItemsContainer.Controls.Add(pnlCartActions);     // Bottom
        _pnlOrderedItemsContainer.Controls.Add(_flowOrderedItems);  // Fill (moved to index 0 later)
        
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

        // Configure popup view columns explicitly so DevExpress does not auto-populate CustomerId (GUID)
        var popupView = _customerPicker.Properties.PopupView;
        if (popupView != null)
        {
            popupView.Columns.Clear();

            var colCode = popupView.Columns.AddVisible("CustomerCode", "Customer Code");
            colCode.Width = 120;
            colCode.MinWidth = 80;

            var colName = popupView.Columns.AddVisible("Name", "Name");
            colName.Width = 240;
            colName.MinWidth = 120;

            var colPhone = popupView.Columns.AddVisible("Phone", "Phone");
            colPhone.Width = 130;
            colPhone.MinWidth = 90;

            var colBalance = popupView.Columns.AddVisible("BalanceDisplay", "Balance");
            colBalance.Width = 110;
            colBalance.MinWidth = 80;
            colBalance.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            colBalance.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;

            if (popupView is DevExpress.XtraGrid.Views.Grid.GridView gv)
            {
                gv.OptionsView.ShowGroupPanel = false;
                gv.OptionsView.ShowIndicator = false;
            }
        }

        _customerPicker.Properties.DisplayMember = "Name";
        _customerPicker.Properties.ValueMember = "CustomerId";
        _customerPicker.Properties.NullText = "Select Customer...";
        _customerPicker.CustomDisplayText += (s, e) =>
        {
            if (e.Value is Guid custId && _customerPicker.Properties.DataSource is List<CustomerPickerRow> rows)
            {
                var match = rows.FirstOrDefault(r => r.CustomerId == custId);
                if (match != null)
                {
                    e.DisplayText = string.IsNullOrWhiteSpace(match.CustomerCode) || match.CustomerCode == "-"
                        ? match.Name
                        : $"[{match.CustomerCode}] {match.Name}";
                }
            }
        };

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
        SetDoubleBuffered(_tlpCenterRows);
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

        // Rush mode: no animation - collapse/expand instantly.
        if (!RushMode.AllowSidebarAnimation)
        {
            _activeOrdersExpanded = expand;
            Clovent.Desktop.Forms.Base.PosSettingsStore.SaveActiveOrdersCollapsed(!expand);
            ApplyActiveOrdersState();
            return;
        }

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
        if (InvokeRequired)
        {
            Invoke(() => RenderOrderedItemsList(lines));
            return;
        }

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

            // Prefetch summaries asynchronously before touching UI controls
            var orderSummaries = new Dictionary<Guid, OrderTotals>();
            foreach (var order in orders)
            {
                var summary = await _mediator.Send(new GetOrderSummaryQuery(order.OrderId));
                orderSummaries[order.OrderId] = summary;
            }

            void UpdateSidebarUi()
            {
                if (IsDisposed || !IsHandleCreated) return;

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
                        _ = RefreshOrderAsync();
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

                        var summary = orderSummaries.GetValueOrDefault(order.OrderId);
                        if (summary != null && card.Controls.Count > 0 && card.Controls[0] is TableLayoutPanel tlp)
                        {
                            var lblCount = tlp.GetControlFromPosition(0, 1) as LabelControl;
                            if (lblCount != null)
                            {
                                lblCount.Text = FormatSidebarCardLine2(order);
                                RegisterOrderHealthCard(order, lblCount);
                            }
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
                    if (orderSummaries.TryGetValue(order.OrderId, out var summary))
                    {
                        _sidebarOrdersFlow.Controls.Add(BuildSidebarOrderCard(order, tableCodes.GetValueOrDefault(order.TableId ?? Guid.Empty), summary));
                    }
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

            if (InvokeRequired)
            {
                Invoke(UpdateSidebarUi);
            }
            else
            {
                UpdateSidebarUi();
            }
        }
        finally
        {
            _isRefreshingRail = false;

            // Order health: drop entries whose card was disposed and keep the
            // 30s ticker running only while live orders are on the rail.
            try
            {
                var staleIds = _orderHealthCards.Where(kv => kv.Value.StatusLabel.IsDisposed).Select(kv => kv.Key).ToList();
                foreach (var staleId in staleIds)
                {
                    _orderHealthCards.Remove(staleId);
                }

                SyncOrderHealthTimer();
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                _logger?.LogError(ex, "Failed to sync the order health ticker.");
            }
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
        var line2 = Label(FormatSidebarCardLine2(order), 7.5F, FontStyle.Regular, Color.FromArgb(100, 116, 139));
        layout.Controls.Add(line2, 0, 1);
        layout.Controls.Add(Label(CurrencyDisplay.FormatPlain(totals.GrandTotal), 9F, FontStyle.Bold, Color.FromArgb(13, 148, 136), right: true), 1, 1);
        RegisterOrderHealthCard(order, line2);

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
        if (_currentOrder != null && _currentOrder.OrderId != order.OrderId && _currentOrder.Status == "Open" && _currentOrderLines.Count > 0)
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

        // Smart features: stop timers, cancel in-flight queries.
        // Cancel is guarded: a suggestion/search continuation that is still
        // unwinding can have disposed its CTS on another thread, and throwing
        // from Dispose would take down the whole test host.
        _orderHealthTimer?.Stop();
        _orderHealthTimer?.Dispose();
        CancelAndDisposeQuietly(ref _suggestionCts);
        CancelAndDisposeQuietly(ref _universalSearchCts);
        _searchDropdown?.Dispose();

        foreach (var image in _tileImagesByProductId.Values)
        {
            image.Dispose();
        }

        _operationsMenu?.Dispose();
    }

    /// <summary>
    /// Cancels and disposes a best-effort cancellation source without ever
    /// throwing: the suggestion/search pipelines replace their CTS on their
    /// own threads, so it may already be disposed by the time the form is.
    /// </summary>
    private static void CancelAndDisposeQuietly(ref CancellationTokenSource? cts)
    {
        var source = cts;
        cts = null;
        if (source is null)
        {
            return;
        }

        try
        {
            source.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        source.Dispose();
    }

    private Task? _initializationTask;

    private async void RestaurantPosForm_Load(object? sender, EventArgs e)
    {
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode || _currentSession is null)
            return;
        
        _logger?.LogInformation("POS_FORM_LOAD: RestaurantPosForm loaded event fired.");
        WindowState = FormWindowState.Maximized;

        try
        {
            _initializationTask = LoadAsync();
            await _initializationTask;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error during Restaurant POS Load.");
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

                    var qaAddItem = Environment.GetEnvironmentVariable("POS_QA_ADD_ITEM");
                    if (!string.IsNullOrWhiteSpace(qaAddItem))
                    {
                        await Task.Delay(600);
                        var variant = _variantsById.Values.FirstOrDefault(v =>
                            v.Name.Contains(qaAddItem, StringComparison.OrdinalIgnoreCase) ||
                            (_productNamesById.TryGetValue(v.ProductId, out var pName) && pName.Contains(qaAddItem, StringComparison.OrdinalIgnoreCase)));
                        if (variant != null)
                        {
                            await ProductTileTappedAsync(variant.ProductVariantId);
                        }

                        var qaOpenSug = Environment.GetEnvironmentVariable("POS_QA_OPEN_SUGGESTIONS");
                        if (!string.IsNullOrWhiteSpace(qaOpenSug) && bool.TryParse(qaOpenSug, out var openSug) && openSug)
                        {
                            await Task.Delay(4000);
                            _suggestionPopupEdit?.Focus();
                            _suggestionPopupEdit?.ShowPopup();
                        }

                        var qaCancel = Environment.GetEnvironmentVariable("POS_QA_CANCEL_ORDER");
                        if (!string.IsNullOrWhiteSpace(qaCancel) && bool.TryParse(qaCancel, out var cancelOrder) && cancelOrder)
                        {
                            await Task.Delay(7000);
                            if (_currentOrder != null)
                            {
                                await _mediator.Send(new CancelOrderCommand(_currentOrder.OrderId, "QA Test Cancel"));
                                _currentOrder = null;
                                _tablePicker.SelectId(null);
                                await ReloadTablesAsync();
                                await RefreshOrderAsync();
                            }
                        }
                    }
                }
            });
        }

        var qaOpenTablePicker = Environment.GetEnvironmentVariable("POS_QA_OPEN_TABLE_PICKER");
        if (!string.IsNullOrWhiteSpace(qaOpenTablePicker) && bool.TryParse(qaOpenTablePicker, out var openTp) && openTp)
        {
            BeginInvoke(async () =>
            {
                await Task.Delay(2000);
                _tablePicker.Focus();
                _tablePicker.ShowPopup();
            });
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (!Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
        {
            if (IsInitialLoadComplete)
            {
                _ = TryRunAsync(RefreshActiveOrdersAsync, "load active orders rail");
            }
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        _logger?.LogInformation("POS_FORM_HANDLE_CREATED: Thread={ThreadId}, Handle={Handle}", Environment.CurrentManagedThreadId, Handle);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);

        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;

        _logger?.LogInformation(
            "POS_FORM_CLOSING: RestaurantPosForm closing. Reason={CloseReason}, Initiator={Initiator}, Thread={ThreadId}",
            e.CloseReason,
            _closeInitiator,
            Environment.CurrentManagedThreadId);

        if (_closeInitiator == PosCloseInitiator.None && e.CloseReason == CloseReason.UserClosing)
        {
            if (HasInProgressOrder())
            {
                var prompt = XtraMessageBox.Show(
                    this,
                    "An order is currently in progress.\nClosing POS will lose unsaved items.\n\nDo you want to exit the application?",
                    "Confirm Exit",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (prompt != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

            _closeInitiator = PosCloseInitiator.UserWindowClose;
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        base.OnFormClosed(e);

        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;

        _logger?.LogInformation(
            "POS_FORM_CLOSED: RestaurantPosForm closed. Reason={CloseReason}, Initiator={Initiator}, Thread={ThreadId}",
            e.CloseReason,
            _closeInitiator,
            Environment.CurrentManagedThreadId);

        if (_closeInitiator == PosCloseInitiator.UserWindowClose &&
            _applicationModeNavigator is not null &&
            !_applicationModeNavigator.IsTransitioning)
        {
            if (_applicationModeNavigator.CurrentForm == this)
            {
                _applicationModeNavigator.ExitApplication("UserWindowClose");
            }
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
            await ReloadQuickOrderTemplatesAsync();
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
        if (_isRefreshingOrder)
        {
            // A genuine user click that landed inside a refresh window must not
            // leave the dropdown showing a table the order was never moved to -
            // snap the display back to the table the current order occupies. The
            // in-flight refresh's own SelectId then re-affirms it.
            if (_currentOrder?.TableId is { } tableId && _tablePicker.SelectedId != tableId)
            {
                _tablePicker.SelectId(tableId);
            }
            return;
        }

        await TryRunAsync(OnTableSelectedAsync, "select this table");
    }

    // Note: there is no "+ Dine In" header button any more. Dine-In orders are
    // started automatically by table selection (OnTableSelectedAsync ->
    // NewDineInAsync). NewDineInButton_Click was removed with the button.

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

    private void ProductSearchEdit_EditValueChanged(object? sender, EventArgs e)
    {
        ApplyProductFilter();
        ScheduleUniversalSearch();
    }

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
            if (_activeOrdersFilter == "TakeAway")
            {
                XtraMessageBox.Show(this, "Start a New Dine-In or New Take Away order first.", "No Order Started", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var tables = await _mediator.Send(new ListAllTablesQuery());
            var availableTable = TableSelectionDineInPolicy.FindFirstAvailableTable(tables, t => t.Status, t => t.OccupancyStatus);

            if (availableTable is null)
            {
                XtraMessageBox.Show(this,
                    "All tables are currently occupied. Please complete or clear an existing table order before starting a new Dine-In order.",
                    "No Table Available",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (_warehousePicker.SelectedId is not { } warehouseId)
            {
                XtraMessageBox.Show(this, "Select a location first.", "No Location Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Select table in picker and create Dine-In order
            _tablePicker.SelectId(availableTable.TableId);

            try
            {
                _currentOrder = await _mediator.Send(new CreateOrderCommand(OrderType.DineIn, warehouseId, availableTable.TableId));
                if (_defaultCustomerId is { } defaultCustId)
                {
                    _currentOrder = await _mediator.Send(new SetOrderCustomerCommand(_currentOrder.OrderId, defaultCustId));
                }
                _hasUnsavedEdits = true;
                await RefreshOrderAsync();
                await RefreshActiveOrdersAsync();
                await LogActivityAsync("New Order", $"{_currentOrder.OrderNumber} (Dine-In)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-start Dine-In order for table {TableCode}", availableTable.Code);
                XtraMessageBox.Show(this, $"Failed to start Dine-In order: {ex.Message}", "Order Start Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                await ReloadTablesAsync();
                return;
            }
        }

        await AddProductToCurrentOrder(variantId, _addQuantityEdit.Value);
    }

    private async Task AddProductToCurrentOrder(Guid variantId, decimal quantity, decimal? expectedUnitPrice = null)
    {
        await _orderMutationLock.WaitAsync();
        try
        {
            await AddProductToCurrentOrderCoreAsync(variantId, quantity, expectedUnitPrice);
            await RefreshOrderAsync();
        }
        finally
        {
            _orderMutationLock.Release();
        }
    }

    // Shared mutation for menu taps and suggestion batches. Caller owns the order lock.
    private async Task<OrderLineDto> AddProductToCurrentOrderCoreAsync(Guid variantId, decimal quantity, decimal? expectedUnitPrice = null)
    {
            await EnsureOrderResumedIfHeldAsync();
            _hasUnsavedEdits = true;

            var existingLine = _currentOrderLines.FirstOrDefault(l =>
                !l.IsVoided &&
                l.ProductVariantId == variantId &&
                string.IsNullOrEmpty(l.Notes));

            OrderLineDto lineDto;
            if (existingLine is not null)
            {
                lineDto = await _mediator.Send(new SetOrderLineQuantityCommand(existingLine.OrderLineId, existingLine.Quantity + quantity));
            }
            else
            {
                lineDto = await _mediator.Send(new AddOrderLineCommand(_currentOrder!.OrderId, variantId, quantity));
            }

            if (expectedUnitPrice.HasValue && lineDto.UnitPrice != expectedUnitPrice.Value)
            {
                lineDto = await _mediator.Send(new OverrideOrderLinePriceCommand(lineDto.OrderLineId, expectedUnitPrice.Value, "Quick Order Template Price", _currentSession.DisplayName ?? "System"));
            }

            return lineDto;
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
                _closeInitiator = PosCloseInitiator.ModeSwitchToBackOffice;
                if (_applicationModeNavigator is not null)
                {
                    await _applicationModeNavigator.OpenBackOfficeAsync();
                }
                else
                {
                    var shell = _scope.ServiceProvider.GetRequiredService<Clovent.Desktop.Forms.Shell.MainForm>();
                    var navigationService = _scope.ServiceProvider.GetRequiredService<Clovent.Desktop.Navigation.INavigationService>();
                    navigationService.NavigateTo("dashboard", "Dashboard");
                    shell.Show();
                    Close();
                }
                break;

            default:
                // Cancelled, or denied permission for both modules -
                // nothing left to run for a standalone POS window.
                _closeInitiator = PosCloseInitiator.SessionSignOut;
                if (_applicationModeNavigator is not null)
                {
                    _applicationModeNavigator.ExitApplication("SwitchUserCancelled");
                }
                else
                {
                    Close();
                }
                break;
        }
    }

    /// <summary>Indicates whether the initial asynchronous screen load has completed.</summary>
    public bool IsInitialLoadComplete { get; private set; }

    private void EnsurePosLoadingOverlay()
    {
    }

    private void SetPosLoading(bool loading, string? message = null)
    {
        if (IsDisposed || Disposing) return;

        if (loading)
        {
            if (_orderStatusLabel != null && !string.IsNullOrWhiteSpace(message))
            {
                _orderStatusLabel.Text = message;
            }
        }
    }

    private async Task LoadAsync()
    {
        _logger?.LogInformation("POS_INIT_STARTED: Starting Restaurant POS initialization...");
        UseWaitCursor = true;
        SetPosLoading(true, "Initializing POS...");
        try
        {
            await LoadCoreAsync();
            IsInitialLoadComplete = true;
            _logger?.LogInformation("POS_INIT_READY: Restaurant POS core initialization completed successfully.");
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            IsInitialLoadComplete = false;
            _logger?.LogError(ex, "POS_INIT_FAILED: Restaurant POS failed to load.");
            SetPosLoading(false);

            await HandleStartupFailureAsync(ex);
        }
        finally
        {
            UseWaitCursor = false;
            SetPosLoading(false);
        }
    }

    private async Task HandleStartupFailureAsync(Exception ex)
    {
        var message = $"Unable to load the Restaurant POS screen.\n\nReason:\n{FriendlyErrorText.Summarize(ex)}\n\nWould you like to retry initialization, or return to Back Office?";
        var result = XtraMessageBox.Show(
            this,
            message,
            "Restaurant POS Load Failed",
            MessageBoxButtons.RetryCancel,
            MessageBoxIcon.Warning);

        if (result == DialogResult.Retry)
        {
            _logger?.LogInformation("User chose to retry Restaurant POS initialization.");
            await LoadAsync();
        }
        else
        {
            _logger?.LogInformation("User cancelled or chose to return to Back Office after POS startup failure.");
            _closeInitiator = PosCloseInitiator.StartupFailureFallback;
            if (_applicationModeNavigator != null)
            {
                await _applicationModeNavigator.OpenBackOfficeAsync();
            }
            else
            {
                Close();
            }
        }
    }

    private async Task LoadCoreAsync()
    {
        _logger.LogInformation("POS_INIT_01: Starting Restaurant POS core initialization...");
        await CurrencyDisplayLoader.ConfigureAsync(_mediator);
        _logger.LogInformation("POS_INIT_02: Currency display configured.");

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
        _logger.LogInformation("POS_INIT_03: Warehouses loaded ({Count}).", warehouses.Count);

        // Once per screen, before the first RefreshOrderAsync below needs them.
        await UpdatePermissionsAsync();
        ApplySmartFeaturePermissions();
        _logger.LogInformation("POS_INIT_04: Permissions and smart features updated. CanAccessBackOffice={CanAccessBackOffice}", _canAccessBackOffice);

        await ReloadMenuItemsAsync();
        _logger.LogInformation("POS_INIT_05: Menu items loaded ({Count} variants).", _variantsById.Count);

        await ReloadQuickOrderTemplatesAsync();
        await ReloadTablesAsync();
        _logger.LogInformation("POS_INIT_06: Tables and quick order templates loaded.");

        await ReloadCustomersAsync();
        await RefreshOrderAsync();
        _logger.LogInformation("POS_INIT_07: Active order refreshed.");

        await RefreshActiveOrdersAsync();
        _logger.LogInformation("POS_INIT_08: Active orders rail refreshed.");

        AppearanceManager.Apply(this, "Restaurant", nameof(RestaurantPosForm));
        UpdateCategoryButtonSelection();
        _logger.LogInformation("POS_INIT_READY: Restaurant POS core initialization completed successfully.");
    }

    private async Task ReloadMenuItemsAsync()
    {
        var variants = await _mediator.Send(new ListProductVariantsQuery());
        _variantsById.Clear();
        foreach (var variant in variants)
        {
            _variantsById[variant.ProductVariantId] = variant;
        }
        _activeVariants = [.. variants.Where(v => v.Status == "Active" && (v.ProductStatus is null || v.ProductStatus == "Active")).OrderBy(v => v.SortOrder).ThenBy(v => v.Name)];

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

            // Rush mode skips tile image loading entirely (presentation only).
            if (RushMode.AllowTileImages
                && !_tileImagesByProductId.ContainsKey(variant.ProductId)
                && MenuItemImageStore.Load(variant.ProductId) is { } image)
            {
                _tileImagesByProductId[variant.ProductId] = image;
            }
        }

        var categories = await _mediator.Send(new ListProductCategoriesQuery());
        _loadedCategories = [.. categories.Where(c => c.Status == "Active")];
        BuildCategoryButtons();
        ApplyProductFilter();
    }

    private sealed record CategoryTag(Guid? CategoryId);

    private void BuildCategoryButtons()
    {
        if (_categoryButtonsPanel is null) return;

        if (_selectedCategoryId.HasValue && _selectedCategoryId.Value != Guid.Empty)
        {
            if (!_loadedCategories.Any(c => c.ProductCategoryId == _selectedCategoryId.Value))
            {
                _selectedCategoryId = null;
            }
        }

        _categoryButtonsPanel.SuspendLayout();
        _categoryButtonsPanel.Controls.Clear();

        int allCount = _activeVariants.Select(v => v.ProductId).Distinct().Count();
        var allCard = BuildCategoryCard(null, "All Menu", "🍔", allCount);
        _categoryButtonsPanel.Controls.Add(allCard);

        var ordered = _sortCategoriesByColor
            ? _loadedCategories.OrderBy(c => c.ColorHex ?? "zzz").ThenBy(c => c.SortOrder).ThenBy(c => c.Name)
            : _loadedCategories.OrderBy(c => c.SortOrder).ThenBy(c => c.Name);

        var distinctCategories = ordered
            .GroupBy(c => c.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        var activeCategoryIds = _loadedCategories.Select(c => c.ProductCategoryId).ToHashSet();

        foreach (var category in distinctCategories)
        {
            var matchingCategoryIds = _loadedCategories
                .Where(c => string.Equals(c.Name.Trim(), category.Name.Trim(), StringComparison.OrdinalIgnoreCase))
                .Select(c => c.ProductCategoryId)
                .ToHashSet();

            int count = _activeVariants
                .Where(v => v.ProductCategoryId.HasValue && matchingCategoryIds.Contains(v.ProductCategoryId.Value))
                .Select(v => v.ProductId)
                .Distinct()
                .Count();

            var card = BuildCategoryCard(category.ProductCategoryId, category.Name, CategoryIcon(category.Name), count);
            _categoryButtonsPanel.Controls.Add(card);
        }

        int uncategorizedCount = _activeVariants
            .Where(v => !v.ProductCategoryId.HasValue || !activeCategoryIds.Contains(v.ProductCategoryId.Value))
            .Select(v => v.ProductId)
            .Distinct()
            .Count();

        if (uncategorizedCount > 0)
        {
            var uncategorizedCard = BuildCategoryCard(Guid.Empty, "Uncategorized", "🏷️", uncategorizedCount);
            _categoryButtonsPanel.Controls.Add(uncategorizedCard);
        }

        _categoryButtonsPanel.ResumeLayout(true);
        _categoryButtonsPanel.PerformLayout();
        UpdateCategoryScrollButtons();
    }

    private Control BuildCategoryCard(Guid? categoryId, string name, string icon, int count)
    {
        var card = new DevExpress.XtraEditors.PanelControl
        {
            Width = 140,
            Height = 44,
            Padding = new Padding(4),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 8, 0),
            Tag = new CategoryTag(categoryId)
        };

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
        lblName.Appearance.Options.UseFont = true;
        lblName.Appearance.Options.UseForeColor = true;
        lblName.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Near;
        lblName.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lblName.Appearance.Options.UseTextOptions = true;

        var lblCount = new LabelControl
        {
            Text = $"{count} items",
            Dock = DockStyle.Fill,
            AutoSizeMode = LabelAutoSizeMode.None,
            Tag = "count"
        };
        lblCount.Appearance.Font = new Font("Segoe UI", 7.5F, FontStyle.Regular);
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

        bool isSelected = IsCategorySelected(categoryId);
        StyleCategoryCard(card, isSelected);

        void ClickAction()
        {
            SelectCategory(categoryId);
        }

        card.Click += (s, e) => ClickAction();
        lblIcon.Click += (s, e) => ClickAction();
        lblName.Click += (s, e) => ClickAction();
        lblCount.Click += (s, e) => ClickAction();

        return card;
    }

    private bool IsCategorySelected(Guid? categoryId)
    {
        if (_selectedCategoryId is null)
        {
            return categoryId is null;
        }

        return _selectedCategoryId == categoryId;
    }

    private void StyleCategoryCard(DevExpress.XtraEditors.PanelControl card, bool isSelected)
    {
        card.LookAndFeel.UseDefaultLookAndFeel = false;
        card.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Flat;

        var bg = isSelected ? AccentColor : Color.White;
        var border = isSelected ? Color.FromArgb(15, 118, 110) : Color.FromArgb(226, 232, 240);

        card.Appearance.BackColor = bg;
        card.Appearance.Options.UseBackColor = true;
        card.Appearance.BorderColor = border;
        card.Appearance.Options.UseBorderColor = true;
        card.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;

        if (card.Controls.Count > 0 && card.Controls[0] is TableLayoutPanel layout)
        {
            layout.BackColor = bg;
            foreach (Control child in layout.Controls)
            {
                if (child is LabelControl lbl)
                {
                    lbl.Appearance.Options.UseForeColor = true;
                    if (lbl.Tag is string role && role == "count")
                    {
                        lbl.Appearance.ForeColor = isSelected ? Color.FromArgb(204, 251, 241) : Color.FromArgb(100, 116, 139);
                    }
                    else
                    {
                        lbl.Appearance.ForeColor = isSelected ? Color.White : Color.FromArgb(15, 23, 42);
                    }
                }
            }
        }

        card.Invalidate();
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
        if (_categoryButtonsPanel is null) return;

        foreach (Control control in _categoryButtonsPanel.Controls)
        {
            if (control is DevExpress.XtraEditors.PanelControl card && card.Tag is CategoryTag tag)
            {
                StyleCategoryCard(card, IsCategorySelected(tag.CategoryId));
            }
            else if (control is SimpleButton btn && btn.Tag is Guid id)
            {
                SetCategoryButtonSelected(btn, _selectedCategoryId == id, id);
            }
        }

        if (_allCategoriesButton != null)
        {
            SetCategoryButtonSelected(_allCategoriesButton, _selectedCategoryId is null, null);
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

        IEnumerable<ProductVariantDto> filtered = _variantsById.Values.Where(v => v.Status == "Active" && (v.ProductStatus is null || v.ProductStatus == "Active"));

        if (_selectedCategoryId.HasValue)
        {
            if (_selectedCategoryId.Value == Guid.Empty)
            {
                var activeCategoryIds = _loadedCategories.Select(c => c.ProductCategoryId).ToHashSet();
                filtered = filtered.Where(v => !v.ProductCategoryId.HasValue || !activeCategoryIds.Contains(v.ProductCategoryId.Value));
            }
            else
            {
                filtered = filtered.Where(v => v.ProductCategoryId == _selectedCategoryId.Value);
            }
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
                if (_currentOrder != null && _currentOrder.Status == "Open" && _currentOrderLines.Count > 0)
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
            // Only Reserved/OutOfService block the transfer here. A persisted
            // "Occupied" flag with no live order is drift (the orders are the
            // authority - see CreateOrderCommandHandler); the transfer command
            // self-heals that flag, so blocking on it is what produced the
            // "Table Unavailable" flip-flop between picker and header.
            var tables = await _mediator.Send(new ListAllTablesQuery());
            var targetTable = tables.FirstOrDefault(t => t.TableId == tableId);
            if (targetTable != null && targetTable.OccupancyStatus is "Reserved" or "OutOfService")
            {
                XtraMessageBox.Show(this, $"Table {targetTable.Code} is {targetTable.OccupancyStatus} and cannot receive this order.", "Table Unavailable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _tablePicker.SelectId(_currentOrder.TableId);
                return;
            }

            try
            {
                _currentOrder = await _mediator.Send(new TransferOrderTableCommand(_currentOrder.OrderId, tableId));
            }
            catch (Exception ex)
            {
                // The command rejected the switch (occupied table, closed order,
                // out-of-service table). Nothing was persisted - snap the picker
                // back to the table the order still occupies so the header and
                // the dropdown cannot disagree.
                _logger.LogError(ex, "Table switch rejected for order {OrderId} to table {TableId}", _currentOrder.OrderId, tableId);
                XtraMessageBox.Show(this, ex.Message, "Table Switch Rejected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                await ReloadTablesAsync();
                _tablePicker.SelectId(_currentOrder.TableId);
                return;
            }

            await RefreshOrderAsync();
            return;
        }

        // No active order and the table is free: the table selection itself is
        // the cashier's explicit Dine-In intent. Start the working Dine-In
        // order immediately (same command path the "+ Dine In" button used)
        // so items can be added with no extra click.
        if (TableSelectionDineInPolicy.ShouldAutoStartDineIn(_currentOrder is not null, tableId))
        {
            await NewDineInAsync();
        }
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
        if (_defaultCustomerId is { } defaultCustId)
        {
            _currentOrder = await _mediator.Send(new SetOrderCustomerCommand(_currentOrder.OrderId, defaultCustId));
        }
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
        if (_defaultCustomerId is { } defaultCustId)
        {
            _currentOrder = await _mediator.Send(new SetOrderCustomerCommand(_currentOrder.OrderId, defaultCustId));
        }
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
        if (_currentOrder != null && _currentOrder.OrderId != targetOrder.OrderId && _currentOrder.Status == "Open" && _currentOrderLines.Count > 0)
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

        if (_currentOrder is { } order && TableSelectionDineInPolicy.ShouldCancelEmptyDraftOnClear(order.OrderType, _currentOrderLines.Count))
        {
            try
            {
                await _mediator.Send(new CancelOrderCommand(order.OrderId, "Cleared empty draft order"));
                await LogActivityAsync("Clear Order", $"Cancelled empty draft Dine-In order {order.OrderNumber}.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cancel empty draft order {OrderId} during Clear", order.OrderId);
            }
        }

        _hasUnsavedEdits = false;
        _currentOrder = null;
        _currentOrderLines = [];
        _tablePicker.SelectId(null);
        _addQuantityEdit.Value = 1;
        _amountEdit.Text = string.Empty;
        _amountEntryIsPreset = true;

        await ReloadTablesAsync();
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
        await ReloadTablesAsync();
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
            await ReloadTablesAsync();
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
            // RefreshOrderAsync ends with ReloadTablesAsync, which reloads the
            // picker's items (now showing the new occupancy) and selects the
            // order's new table - selecting here first would render against a
            // stale item list.
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
                void ApplyEmptyOrderUi()
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
                }

                if (InvokeRequired) Invoke(ApplyEmptyOrderUi); else ApplyEmptyOrderUi();
                await RefreshActiveOrdersAsync();
                return;
            }

            _currentOrder = await _mediator.Send(new GetOrderByIdQuery(_currentOrder.OrderId));

            var isEditable = _currentOrder.Status is "Open" or "Held";

            if (_currentOrder.CustomerId is { } custId)
            {
                var customer = await _mediator.Send(new GetCustomerByIdQuery(custId));
                void ApplyCustomerDetails()
                {
                    _customerPicker.Enabled = isEditable;
                    _newCustomerButton.Enabled = isEditable;
                    SetSelectedCustomerId(_currentOrder.CustomerId ?? Guid.Empty);
                    if (customer is not null)
                    {
                        var codePrefix = !string.IsNullOrWhiteSpace(customer.Code) ? $"[{customer.Code}] " : string.Empty;
                        _customerDetailsLabel.Text = $"{codePrefix}{customer.Name} • Outstanding: {CurrencyDisplay.FormatPlain(customer.OutstandingBalance)}";
                        _customerDetailsLabel.ForeColor = customer.OutstandingBalance > 0 ? Color.Red : Color.Green;
                    }
                    else
                    {
                        _customerDetailsLabel.Text = string.Empty;
                    }
                }
                if (InvokeRequired) Invoke(ApplyCustomerDetails); else ApplyCustomerDetails();
            }
            else
            {
                void ApplyWalkInCustomer()
                {
                    _customerPicker.Enabled = isEditable;
                    _newCustomerButton.Enabled = isEditable;
                    SetSelectedCustomerId(Guid.Empty);
                    _customerDetailsLabel.Text = $"Walk-in Customer • Outstanding: {CurrencyDisplay.FormatPlain(0m)}";
                    _customerDetailsLabel.ForeColor = Color.Green;
                }
                if (InvokeRequired) Invoke(ApplyWalkInCustomer); else ApplyWalkInCustomer();
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

            var tables = await _mediator.Send(new ListAllTablesQuery());
            var tableCodes = tables.ToDictionary(t => t.TableId, t => t.Code);
            var totals = await _mediator.Send(new GetOrderSummaryQuery(_currentOrder.OrderId));

            void ApplyActiveOrderUi()
            {
                _lineGrid.DataSource = rows;
                RestoreFocusedLine(focusedLineId, rows);
                RenderOrderedItemsList(lines);

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
                SetTotals(totals);

                _orderStatusLabel.Text = $"{_currentOrder.OrderNumber}  •  {_currentOrder.OrderType}  •  {_currentOrder.Status}";
                UpdateOrderStatusBadge();
            }

            if (InvokeRequired) Invoke(ApplyActiveOrderUi); else ApplyActiveOrderUi();

            await ReloadTablesAsync();
            await RefreshActiveOrdersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh order in POS.");
            throw;
        }
        finally
        {
            _isRefreshingOrder = false;
            void FinalizeRefresh()
            {
                UpdateButtonStates();
                ScheduleSuggestionRefresh();
            }
            if (InvokeRequired) Invoke(FinalizeRefresh); else FinalizeRefresh();
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
            _canAccessBackOffice = false;
            _canPerformRefund = false;
            BuildOperationsMenu();
            return;
        }

        string[] operations =
        [
            "create", "hold", "resume", "void", "cancel", "reopen", "sendtokitchen", "complete", "pay",
            "transfertable", "mergetables", "splitbill", "notes", "discount", "servicecharge", "additem", "editline",
            "priceoverride",
            "smartinsights", "quickorders", "restaurantpulse", "rushmode",
        ];

        _permissions = new Dictionary<string, bool>();
        try
        {
            var authService = _scope?.ServiceProvider.GetService<IAuthorizationService>();
            if (authService != null)
            {
                var codes = await authService.GetPermissionCodesAsync(userId);
                var codeSet = new HashSet<string>(codes, StringComparer.OrdinalIgnoreCase);

                foreach (var operation in operations)
                {
                    _permissions[operation] = codeSet.Contains($"feature.{FeatureCode}.{operation}");
                }

                _canAccessBackOffice = codeSet.Any(c => c.StartsWith("menu.", StringComparison.OrdinalIgnoreCase) && !c.Equals("menu.pos", StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                foreach (var operation in operations)
                {
                    _permissions[operation] = await _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}");
                }
                _canAccessBackOffice = true;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to evaluate permissions for user {UserId}", userId);
            _canAccessBackOffice = false;
        }

        // Refund / Return:
        // In CBOS currently, there is no end-to-end POS order refund domain workflow (only payment-level void exists in PaymentHistoryDialog).
        // Per requirement 28: keep hidden until functional rather than displaying a dead command.
        _canPerformRefund = false;

        BuildOperationsMenu();
    }

    /// <summary>
    /// Configures the Operations dropdown command menu based on canonical RBAC permissions
    /// and POS shift/day-close domain workflows.
    /// </summary>
    public void BuildOperationsMenu()
    {
        _operationsMenu.Items.Clear();

        // 1. Shift Submenu
        _shiftMenuItem = new ToolStripMenuItem("Shift");
        _openShiftMenuItem = new ToolStripMenuItem("Open Shift", null, async (s, e) => await HandleOpenShiftAsync());
        _currentShiftMenuItem = new ToolStripMenuItem("Current Shift / Shift Details", null, async (s, e) => await HandleCurrentShiftAsync());
        _closeShiftMenuItem = new ToolStripMenuItem("Close Shift", null, async (s, e) => await HandleCloseShiftAsync());

        _shiftMenuItem.DropDownItems.Add(_openShiftMenuItem);
        _shiftMenuItem.DropDownItems.Add(_currentShiftMenuItem);
        _shiftMenuItem.DropDownItems.Add(_closeShiftMenuItem);
        _operationsMenu.Items.Add(_shiftMenuItem);

        // 2. Cash Movement
        _cashMovementMenuItem = new ToolStripMenuItem("Cash Movement", null, async (s, e) => await HandleCashMovementAsync());
        _operationsMenu.Items.Add(_cashMovementMenuItem);

        // 3. Print Last Receipt
        _printLastReceiptMenuItem = new ToolStripMenuItem("Print Last Receipt", null, async (s, e) => await HandlePrintLastReceiptAsync());
        _operationsMenu.Items.Add(_printLastReceiptMenuItem);

        // Optional Refund (future domain workflow)
        if (_canPerformRefund)
        {
            var refundItem = new ToolStripMenuItem("Refund / Return", null, (s, e) => { });
            _operationsMenu.Items.Add(refundItem);
        }

        // Separator
        _operationsMenu.Items.Add(new ToolStripSeparator());

        // 4. End of Day
        _endOfDayMenuItem = new ToolStripMenuItem("End of Day", null, async (s, e) => await HandleEndOfDayAsync());
        _operationsMenu.Items.Add(_endOfDayMenuItem);

        // Separator
        _operationsMenu.Items.Add(new ToolStripSeparator());

        // 5. Back Office
        _backOfficeMenuItem = new ToolStripMenuItem("Back Office", null, async (s, e) =>
        {
            await RequestNavigateToBackOfficeAsync();
        });
        _operationsMenu.Items.Add(_backOfficeMenuItem);

        UpdateOperationsMenuState();

        if (_operationsButton != null)
        {
            StyleOperationsButton();
            _operationsButton.Visible = _operationsMenu.Items.Count > 0;
        }
    }

    /// <summary>
    /// Updates enabled/disabled states of Operations menu items based on shift presence and permissions.
    /// </summary>
    public void UpdateOperationsMenuState()
    {
        if (_shiftMenuItem == null) return;

        var hasShift = _activeShift != null;
        _openShiftMenuItem.Enabled = !hasShift;
        _currentShiftMenuItem.Enabled = hasShift;
        _closeShiftMenuItem.Enabled = hasShift;
        _cashMovementMenuItem.Enabled = hasShift;
        _printLastReceiptMenuItem.Enabled = hasShift;
        _endOfDayMenuItem.Enabled = true;
        _backOfficeMenuItem.Enabled = _canAccessBackOffice;
        _backOfficeMenuItem.Visible = _canAccessBackOffice;
    }

    /// <summary>Gets the Shift submenu item.</summary>
    public ToolStripMenuItem ShiftMenuItem => _shiftMenuItem;

    /// <summary>Gets the Open Shift menu item.</summary>
    public ToolStripMenuItem OpenShiftMenuItem => _openShiftMenuItem;

    /// <summary>Gets the Current Shift menu item.</summary>
    public ToolStripMenuItem CurrentShiftMenuItem => _currentShiftMenuItem;

    /// <summary>Gets the Close Shift menu item.</summary>
    public ToolStripMenuItem CloseShiftMenuItem => _closeShiftMenuItem;

    /// <summary>Gets the Cash Movement menu item.</summary>
    public ToolStripMenuItem CashMovementMenuItem => _cashMovementMenuItem;

    /// <summary>Gets the Print Last Receipt menu item.</summary>
    public ToolStripMenuItem PrintLastReceiptMenuItem => _printLastReceiptMenuItem;

    /// <summary>Gets the End of Day menu item.</summary>
    public ToolStripMenuItem EndOfDayMenuItem => _endOfDayMenuItem;

    /// <summary>Gets the Back Office menu item.</summary>
    public ToolStripMenuItem BackOfficeMenuItem => _backOfficeMenuItem;

    /// <summary>Gets the active POS shift, if one is currently open.</summary>
    public Clovent.Restaurant.Application.Shifts.Dtos.ShiftDto? ActiveShift => _activeShift;

    /// <summary>Sets the active POS shift and updates UI accordingly.</summary>
    public void SetActiveShift(Clovent.Restaurant.Application.Shifts.Dtos.ShiftDto? shift)
    {
        _activeShift = shift;
        if (_cashierLabel != null)
        {
            if (_activeShift != null)
            {
                var cashierName = _currentSession?.DisplayName ?? "Cashier";
                _cashierLabel.Text = $"Cashier: {cashierName} | Shift #{_activeShift.ShiftNumber}";
            }
            else
            {
                _cashierLabel.Text = _currentSession?.DisplayName is { } name ? $"Cashier: {name}" : "Cashier: Not signed in";
            }
        }
        UpdateOperationsMenuState();
    }

    private async Task HandleOpenShiftAsync()
    {
        if (_activeShift != null)
        {
            XtraMessageBox.Show(this, "A shift is already open on this terminal.", "Open Shift", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var branchId = Guid.Empty;
        var termId = Guid.Empty;
        var warehouseId = _warehousePicker.SelectedId ?? Guid.Empty;
        var termName = "Terminal";
        var branchName = "Branch";
        var businessDate = DateOnly.FromDateTime(DateTime.Today);

        try
        {
            var termResService = _scope?.ServiceProvider.GetService<Clovent.Desktop.Restaurant.Services.ITerminalResolutionService>();
            if (termResService != null)
            {
                var termRes = await termResService.ResolveCurrentTerminalAsync();
                if (termRes.IsConfigured && termRes.TerminalId.HasValue)
                {
                    termId = termRes.TerminalId.Value;
                    termName = termRes.TerminalName;
                    branchId = termRes.BranchId ?? Guid.Empty;
                    branchName = termRes.BranchName;
                    warehouseId = termRes.WarehouseId ?? warehouseId;
                }
            }

            var dateProvider = _scope?.ServiceProvider.GetService<Clovent.Restaurant.Application.Shifts.Services.IBusinessDateProvider>();
            if (dateProvider != null)
            {
                businessDate = dateProvider.GetCurrentBusinessDate();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to resolve terminal/business-date context for Open Shift.");
        }

        using var openDlg = new Clovent.Desktop.Restaurant.Shifts.OpenShiftDialog(_mediator, _currentSession, branchId, warehouseId, termId, termName, branchName, businessDate);
        var result = openDlg.ShowDialog(this);
        if (result == DialogResult.OK && openDlg.OpenedShift != null)
        {
            SetActiveShift(openDlg.OpenedShift);
        }
    }

    private async Task HandleCurrentShiftAsync()
    {
        if (_activeShift == null)
        {
            XtraMessageBox.Show(this, "No active shift is currently open.", "Shift Details", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var detailDlg = new Clovent.Desktop.Restaurant.Shifts.ShiftDetailDialog(_mediator, _activeShift.ShiftId);
        detailDlg.ShowDialog(this);
        await Task.CompletedTask;
    }

    private async Task HandleCloseShiftAsync()
    {
        if (_activeShift == null)
        {
            XtraMessageBox.Show(this, "No active shift is currently open.", "Close Shift", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (HasInProgressOrder())
        {
            XtraMessageBox.Show(
                this,
                "Cannot close shift while an order is currently in progress.\nPlease complete, hold, or cancel the active order first.",
                "Order In Progress",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        using var closeDlg = new Clovent.Desktop.Restaurant.Shifts.CloseShiftDialog(_mediator, _activeShift.ShiftId);
        var result = closeDlg.ShowDialog(this);
        if (result == DialogResult.OK && closeDlg.ClosedShiftSummary != null)
        {
            var summary = closeDlg.ClosedShiftSummary;
            SetActiveShift(null);

            XtraMessageBox.Show(
                this,
                $"Shift #{summary.Shift.ShiftNumber} has been closed successfully.\n\n" +
                $"Cash Sales: {CurrencyDisplay.Format(summary.CashSales)}\n" +
                $"Expected Cash: {CurrencyDisplay.Format(summary.ExpectedCash)}\n" +
                $"Counted Cash: {CurrencyDisplay.Format(summary.CountedCash)}\n" +
                $"Variance: {CurrencyDisplay.Format(summary.Variance)}\n\n" +
                "You will now be returned to the Login screen.",
                "Shift Closed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            if (_applicationModeNavigator != null)
            {
                _closeInitiator = PosCloseInitiator.SessionSignOut;
                await _applicationModeNavigator.OpenLoginAsync();
            }
        }
    }

    private async Task HandleCashMovementAsync()
    {
        if (_activeShift == null)
        {
            XtraMessageBox.Show(this, "No active shift is currently open.", "Cash Movement", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var movementDlg = new Clovent.Desktop.Restaurant.Shifts.CashMovementDialog(_mediator, _currentSession, _activeShift.ShiftId);
        var result = movementDlg.ShowDialog(this);
        if (result == DialogResult.OK && movementDlg.RecordedMovement != null)
        {
            var m = movementDlg.RecordedMovement;
            await LogActivityAsync("Cash Movement", $"{m.Type}: {m.Amount:C2} - {m.Reason}");

            try
            {
                var refreshed = await _mediator.Send(new Clovent.Restaurant.Application.Shifts.Queries.GetShiftByIdQuery(_activeShift.ShiftId));
                if (refreshed != null)
                {
                    _activeShift = refreshed;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to refresh shift after cash movement.");
            }
        }
    }

    private async Task HandlePrintLastReceiptAsync()
    {
        if (_activeShift == null)
        {
            XtraMessageBox.Show(this, "No active shift is associated with this terminal.", "Print Last Receipt", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            var lastOrder = await _mediator.Send(new Clovent.Restaurant.Application.Shifts.Queries.GetLastCompletedOrderForShiftQuery(_activeShift.ShiftId));
            if (lastOrder == null)
            {
                XtraMessageBox.Show(this, "No completed orders found for the current shift.", "Print Last Receipt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var receiptText = await ReceiptFormatter.FormatAsync(_mediator, lastOrder);
            using var preview = new ReceiptPreviewForm(receiptText);
            preview.ShowDialog(this);

            await LogActivityAsync("Receipt Reprinted", $"Order #{lastOrder.OrderNumber} reprinted for Shift #{_activeShift.ShiftNumber}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to print last receipt for shift {ShiftId}", _activeShift.ShiftId);
            XtraMessageBox.Show(this, $"Failed to reprint receipt: {ex.Message}", "Reprint Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task HandleEndOfDayAsync()
    {
        var branchId = _activeShift?.BranchId ?? Guid.Empty;
        var branchName = "Branch";
        var businessDate = DateOnly.FromDateTime(_activeShift?.OpenedAtUtc.LocalDateTime ?? DateTime.Today);

        try
        {
            if (branchId == Guid.Empty)
            {
                var termResService = _scope?.ServiceProvider.GetService<Clovent.Desktop.Restaurant.Services.ITerminalResolutionService>();
                if (termResService != null)
                {
                    var termRes = await termResService.ResolveCurrentTerminalAsync();
                    if (termRes.BranchId.HasValue)
                    {
                        branchId = termRes.BranchId.Value;
                        branchName = termRes.BranchName;
                    }
                }
            }

            var dateProvider = _scope?.ServiceProvider.GetService<Clovent.Restaurant.Application.Shifts.Services.IBusinessDateProvider>();
            if (dateProvider != null)
            {
                businessDate = dateProvider.GetCurrentBusinessDate();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to resolve branch or business date for End of Day.");
        }

        using var dialog = new Clovent.Desktop.Restaurant.EndOfDay.EndOfDayCloseDialog(_mediator, _currentSession, branchId, branchName, businessDate);
        dialog.ShowDialog(this);
    }

    /// <summary>
    /// Styles the Operations command button with CBOS signature teal prominence,
    /// matching the exact SimpleButton pattern used by More Actions.
    /// </summary>
    private void StyleOperationsButton()
    {
        if (_operationsButton == null) return;

        // Distinctive CBOS Primary Teal styling (#0D9488, Teal 600)
        var tealBg = Color.FromArgb(13, 148, 136);
        var tealHover = Color.FromArgb(15, 118, 110);   // #0F766E, Teal 700
        var tealPressed = Color.FromArgb(17, 94, 89);   // #115E59, Teal 800

        _operationsButton.Text = "Operations ▼";
        _operationsButton.ToolTip = "Additional POS operations";
        _operationsButton.Dock = DockStyle.Fill;
        _operationsButton.AutoSize = true;
        _operationsButton.Cursor = Cursors.Hand;
        _operationsButton.AllowFocus = false;

        // Border and LookAndFeel matching More Actions SimpleButton
        _operationsButton.ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
        _operationsButton.LookAndFeel.UseDefaultLookAndFeel = false;
        _operationsButton.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Flat;

        // Typography and Normal State
        var btnFont = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _operationsButton.Font = btnFont;
        _operationsButton.Appearance.Font = btnFont;
        _operationsButton.Appearance.BackColor = tealBg;
        _operationsButton.Appearance.ForeColor = Color.White;
        _operationsButton.Appearance.BorderColor = tealBg;
        _operationsButton.Appearance.Options.UseFont = true;
        _operationsButton.Appearance.Options.UseBackColor = true;
        _operationsButton.Appearance.Options.UseForeColor = true;
        _operationsButton.Appearance.Options.UseBorderColor = true;

        // Hover State
        _operationsButton.AppearanceHovered.Font = btnFont;
        _operationsButton.AppearanceHovered.BackColor = tealHover;
        _operationsButton.AppearanceHovered.ForeColor = Color.White;
        _operationsButton.AppearanceHovered.BorderColor = tealHover;
        _operationsButton.AppearanceHovered.Options.UseFont = true;
        _operationsButton.AppearanceHovered.Options.UseBackColor = true;
        _operationsButton.AppearanceHovered.Options.UseForeColor = true;
        _operationsButton.AppearanceHovered.Options.UseBorderColor = true;

        // Pressed State
        _operationsButton.AppearancePressed.Font = btnFont;
        _operationsButton.AppearancePressed.BackColor = tealPressed;
        _operationsButton.AppearancePressed.ForeColor = Color.White;
        _operationsButton.AppearancePressed.BorderColor = tealPressed;
        _operationsButton.AppearancePressed.Options.UseFont = true;
        _operationsButton.AppearancePressed.Options.UseBackColor = true;
        _operationsButton.AppearancePressed.Options.UseForeColor = true;
        // Compact professional vector SVG icon (Settings/Gear), unscaled (14x14) so it supports rather than dominates caption
        Clovent.Desktop.Forms.Base.DesktopIcons.Apply(_operationsButton, Clovent.Desktop.Forms.Base.DesktopIcons.Operations);
        _operationsButton.ImageOptions.SvgImageSize = new Size(14, 14);
        _operationsButton.ImageOptions.ImageToTextAlignment = DevExpress.XtraEditors.ImageAlignToText.LeftCenter;
        _operationsButton.ImageOptions.ImageToTextIndent = 5;
        _operationsButton.ImageOptions.AllowGlyphSkinning = DevExpress.Utils.DefaultBoolean.True;

        // High-DPI Spacing & Margins: Distinct separation from More and Logout
        _operationsButton.Margin = new Padding(
            Clovent.Desktop.Forms.Base.DesktopDpi.Scale(6, this),
            Clovent.Desktop.Forms.Base.DesktopDpi.Scale(4, this),
            Clovent.Desktop.Forms.Base.DesktopDpi.Scale(6, this),
            Clovent.Desktop.Forms.Base.DesktopDpi.Scale(4, this));

        _operationsButton.Padding = new Padding(12, 0, 12, 0);

        _operationsButton.Click -= OperationsButton_Click;
        _operationsButton.Click += OperationsButton_Click;
    }

    private void OperationsButton_Click(object? sender, EventArgs e) =>
        _operationsMenu.Show(_operationsButton, new Point(0, _operationsButton.Height));

    /// <summary>
    /// Checks whether the POS currently contains an unplaced / unheld order with items.
    /// </summary>
    public bool HasInProgressOrder()
    {
        return _currentOrder is not null &&
               string.Equals(_currentOrder.Status, "Open", StringComparison.OrdinalIgnoreCase) &&
               (_currentOrderLines.Count > 0 || _currentOrder.OrderLineIds.Count > 0);
    }

    /// <summary>
    /// Handles navigation request from POS to Back Office, validating transient cart state.
    /// If an order is in progress, prompts the cashier with a guard dialog offering
    /// "Hold &amp; Open Back Office" or "Stay in POS".
    /// </summary>
    public async Task RequestNavigateToBackOfficeAsync()
    {
        if (HasInProgressOrder())
        {
            using var guardDialog = new PosNavigationGuardDialog();
            var result = guardDialog.ShowDialog(this);
            if (result != DialogResult.Yes)
            {
                // Cashier chose "Stay in POS" or dismissed the dialog
                return;
            }

            // Cashier chose "Hold & Open Back Office": execute existing hold pipeline
            try
            {
                var heldOrder = await _mediator.Send(new HoldOrderCommand(_currentOrder!.OrderId));
                await LogActivityAsync("Hold Order", $"{heldOrder.OrderNumber}");

                _currentOrder = null;
                _tablePicker.SelectId(null);
                await RefreshOrderAsync();
                await RefreshActiveOrdersAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to hold order {OrderId} before navigating to Back Office.", _currentOrder?.OrderId);
                XtraMessageBox.Show(
                    this,
                    $"Failed to hold order: {ex.Message}\nNavigation to Back Office cancelled to protect order state.",
                    "Hold Order Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
        }

        if (_applicationModeNavigator is not null)
        {
            _closeInitiator = PosCloseInitiator.ModeSwitchToBackOffice;
            await _applicationModeNavigator.OpenBackOfficeAsync();
        }
    }

    /// <summary>Gets the Operations button on the POS header.</summary>
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public DevExpress.XtraEditors.SimpleButton OperationsButton => _operationsButton;

    /// <summary>Gets the Operations ContextMenuStrip attached to the button.</summary>
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public ContextMenuStrip OperationsMenu => _operationsMenu;

    /// <summary>Gets or sets whether the user can access Back Office (updates the Operations menu).</summary>
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool CanAccessBackOffice
    {
        get => _canAccessBackOffice;
        set
        {
            _canAccessBackOffice = value;
            BuildOperationsMenu();
        }
    }

    /// <summary>Gets or sets whether the user can perform Refund / Return (updates the Operations menu).</summary>
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool CanPerformRefund
    {
        get => _canPerformRefund;
        set
        {
            _canPerformRefund = value;
            BuildOperationsMenu();
        }
    }

    /// <summary>Sets the application mode navigator for testing.</summary>
    internal void SetApplicationModeNavigator(IApplicationModeNavigator navigator) => _applicationModeNavigator = navigator;

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

        // Smart entries: reorder needs a customer actually picked (not walk-in default).
        if (_moreRepeatLastOrderItem is not null)
        {
            _moreRepeatLastOrderItem.Enabled = canEdit && SelectedCustomerIdOrNull() is not null;
        }
    }

    /// <summary>
    /// Rebuilds the searchable customer dropdown's data source from active
    /// customer records, ensuring the designated default customer is resolved and
    /// placed first.
    /// </summary>
    private async Task ReloadCustomersAsync(Guid? selectCustomerId = null)
    {
        var customers = await _mediator.Send(new ListCustomersQuery());
        var activeCustomers = customers.Where(c => c.IsActive).ToList();

        var defaultCustomer = activeCustomers.FirstOrDefault(c => c.IsDefault) ?? activeCustomers.FirstOrDefault();
        _defaultCustomerId = defaultCustomer?.CustomerId;

        // Order with default customer first, then alphabetically by name
        var sortedCustomers = activeCustomers
            .OrderByDescending(c => c.IsDefault)
            .ThenBy(c => c.Name)
            .ToList();

        List<CustomerPickerRow> pickerItems = sortedCustomers
            .Select(c => new CustomerPickerRow(
                c.CustomerId,
                string.IsNullOrWhiteSpace(c.Code) ? "-" : c.Code,
                c.Name,
                c.MobileNumber ?? string.Empty,
                CurrencyDisplay.FormatPlain(c.OutstandingBalance)))
            .ToList();

        _customerPicker.Properties.DataSource = pickerItems;
        SetSelectedCustomerId(selectCustomerId ?? _currentOrder?.CustomerId ?? _defaultCustomerId ?? Guid.Empty);
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
                    form.PhoneValue,
                    form.IsDefaultValue));

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
            void ResetPaymentUi()
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
            }

            if (InvokeRequired)
            {
                try { Invoke(ResetPaymentUi); } catch { }
            }
            else
            {
                try { ResetPaymentUi(); } catch { }
            }
            return;
        }

        void EnablePaymentUi()
        {
            pnlAmountTendered.Enabled = true;
            pnlKeypad.Enabled = true;
            pnlQuickCash.Enabled = true;
            _recordButton.Enabled = true;
        }

        if (InvokeRequired)
        {
            try { Invoke(EnablePaymentUi); } catch { }
        }
        else
        {
            try { EnablePaymentUi(); } catch { }
        }

        var totals = await _mediator.Send(new GetOrderSummaryQuery(orderId));

        void UpdateTotalsUi()
        {
            _balance = Math.Max(totals.Balance, 0m);
            _paymentBalanceLabel.Text = CurrencyDisplay.FormatPlain(_balance);
            _amountEdit.Text = FormatPlain(_balance);
            _amountEntryIsPreset = true;
            UpdateChangeDisplay();
        }

        if (InvokeRequired)
        {
            try { Invoke(UpdateTotalsUi); } catch { }
        }
        else
        {
            try { UpdateTotalsUi(); } catch { }
        }
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

            if (!await EnsureShiftActiveOrPromptAsync())
            {
                return;
            }

            await _mediator.Send(new RecordPaymentCommand(orderId, paymentMethodId, applied, false, _activeShift?.ShiftId));
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

        if (!await EnsureShiftActiveOrPromptAsync())
        {
            return;
        }

        await _mediator.Send(new RecordPaymentCommand(orderId, paymentMethodId, applied, exceedCreditLimitApproved, _activeShift?.ShiftId));
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
    private sealed record CustomerPickerRow(Guid CustomerId, string CustomerCode, string Name, string Phone, string BalanceDisplay);

    private Clovent.Restaurant.Application.Shifts.Dtos.ShiftDto? _activeShift;

    private async Task<bool> EnsureShiftActiveOrPromptAsync()
    {
        if (_activeShift != null)
        {
            try
            {
                var shift = await _mediator.Send(new Clovent.Restaurant.Application.Shifts.Queries.GetShiftByIdQuery(_activeShift.ShiftId));
                if (shift == null || !string.Equals(shift.Status, "Open", StringComparison.OrdinalIgnoreCase))
                {
                    XtraMessageBox.Show(
                        this,
                        "Your shift has been closed externally.\nTransactions cannot be processed under a closed shift.",
                        "Shift Closed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    SetActiveShift(null);
                    if (_applicationModeNavigator != null)
                    {
                        _closeInitiator = PosCloseInitiator.ModeSwitchToBackOffice;
                        await _applicationModeNavigator.OpenBackOfficeAsync();
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to verify shift status with server");
            }
            return true;
        }

        var cashierId = _currentSession?.UserId ?? Guid.Parse("00000000-0000-0000-0000-000000000001");
        var active = await _mediator.Send(new Clovent.Restaurant.Application.Shifts.Queries.GetActiveShiftQuery(CashierId: cashierId));
        if (active != null)
        {
            SetActiveShift(active);
            return true;
        }

        await HandleOpenShiftAsync();
        return _activeShift != null;
    }

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

    // ==========================================
    // SMART POS FEATURES
    // ==========================================

    /// <summary>
    /// Builds every smart-feature control that lives inside panels created by
    /// <see cref="RestructureLayout"/>: the suggestion strip above the cart,
    /// the Quick Orders strip above the menu, the Rush Mode badge in the
    /// header, the order-health timer, and the smart More-menu entries.
    /// </summary>
    private void InitializeSmartFeatures()
    {
        try
        {
            BuildEmbeddedSuggestionPanel();
            BuildQuickOrdersStrip();
            BuildRushModeBadge();
            BuildOrderHealthTimer();
            BuildSmartMoreMenuItems();

            _productSearchEdit.KeyDown += ProductSearchEdit_SmartKeyDown;
            _productSearchEdit.LostFocus += (_, _) => BeginInvoke(new Action(() =>
            {
                if (_searchDropdown is { Visible: true })
                {
                    _searchDropdown.Hide();
                }
            }));

            RushMode.Changed += ApplyRushMode;
            RushMode.Enabled = Clovent.Desktop.Forms.Base.PosSettingsStore.LoadRushModeEnabled();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize the POS smart features.");
        }
    }

    // ---------- 1. Embedded Smart Upsell suggestion grid ----------

    private void BuildEmbeddedSuggestionPanel()
    {
        if (_suggestionPanel != null)
        {
            if (_pnlOrderedItemsContainer != null && !_pnlOrderedItemsContainer.Controls.Contains(_suggestionPanel))
            {
                _pnlOrderedItemsContainer.Controls.Add(_suggestionPanel);
                _flowOrderedItems?.BringToFront();
            }
            return;
        }

        // Compact 1-row strip docked at the TOP of the ordered-items viewport.
        // Takes approximately ONE normal control row (~28px) ONLY when suggestions exist.
        // When there are no recommendations: Height = 0 and Visible = false.
        // The actual suggestion GridControl lives inside _suggestionPopupControl (dropdown overlay),
        // completely avoiding permanently pushing the cart items downward.
        _suggestionPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 0,
            Visible = false,
            Margin = new Padding(0),
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(Clovent.Desktop.Forms.Base.DesktopDpi.Scale(8, this), Clovent.Desktop.Forms.Base.DesktopDpi.Scale(3, this), Clovent.Desktop.Forms.Base.DesktopDpi.Scale(8, this), Clovent.Desktop.Forms.Base.DesktopDpi.Scale(3, this))
        };

        // ── Left: [bulb icon] "Suggested Add-ons" ──
        var leftHeaderPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        _suggestionHeaderIcon = new PictureEdit
        {
            Width = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(22, this),
            Height = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(22, this),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(1, this), Clovent.Desktop.Forms.Base.DesktopDpi.Scale(4, this), 0)
        };
        _suggestionHeaderIcon.Properties.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Zoom;
        _suggestionHeaderIcon.Properties.ShowMenu = false;
        _suggestionHeaderIcon.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
        _suggestionHeaderIcon.Properties.Appearance.BackColor = Color.Transparent;
        _suggestionHeaderIcon.Properties.Appearance.Options.UseBackColor = true;
        try
        {
            // DevExpress built-in lightbulb SVG icon
            _suggestionHeaderIcon.SvgImage = DevExpress.Images.ImageResourceCache.Default.GetSvgImage(Clovent.Desktop.Forms.Base.DesktopIcons.Idea);
        }
        catch
        {
            // Fallback: no icon - the text label alone is sufficient.
        }

        _suggestionHeaderLabel = new LabelControl
        {
            Text = "Suggested Add-ons",
            AutoSizeMode = LabelAutoSizeMode.Horizontal,
            Margin = new Padding(0, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(3, this), 0, 0),
            Padding = new Padding(0),
            Cursor = Cursors.Hand
        };
        _suggestionHeaderLabel.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _suggestionHeaderLabel.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
        _suggestionHeaderLabel.Appearance.Options.UseFont = true;
        _suggestionHeaderLabel.Appearance.Options.UseForeColor = true;
        _suggestionHeaderLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _suggestionHeaderLabel.Appearance.Options.UseTextOptions = true;

        leftHeaderPanel.Controls.Add(_suggestionHeaderIcon);
        leftHeaderPanel.Controls.Add(_suggestionHeaderLabel);

        // ── Popup Container Control (the floating dropdown overlay) ──
        _suggestionPopupControl = new PopupContainerControl
        {
            Width = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(430, this),
            Height = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(180, this),
            BackColor = Color.White
        };

        // ── Right: [ 4 suggestions ▼ ] PopupContainerEdit ──
        _suggestionPopupEdit = new PopupContainerEdit
        {
            Dock = DockStyle.Right,
            Width = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(140, this),
            Cursor = Cursors.Hand
        };
        _suggestionPopupEdit.Properties.PopupControl = _suggestionPopupControl;
        _suggestionPopupEdit.Properties.ShowDropDown = DevExpress.XtraEditors.Controls.ShowDropDown.SingleClick;
        _suggestionPopupEdit.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _suggestionPopupEdit.Properties.NullText = "0 suggestions";
        _suggestionPopupEdit.Properties.ShowPopupCloseButton = false;
        _suggestionPopupEdit.Properties.PopupSizeable = false;
        _suggestionPopupEdit.Properties.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        _suggestionPopupEdit.Properties.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
        _suggestionPopupEdit.Properties.Appearance.Options.UseFont = true;
        _suggestionPopupEdit.Properties.Appearance.Options.UseForeColor = true;
        _suggestionPopupEdit.Properties.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        _suggestionPopupEdit.Properties.Appearance.Options.UseTextOptions = true;
        _suggestionPopupEdit.Properties.Buttons.Clear();
        _suggestionPopupEdit.Properties.Buttons.Add(new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo));

        // Clicking label or bulb icon opens the dropdown overlay
        _suggestionHeaderIcon.Click += (_, _) => _suggestionPopupEdit.ShowPopup();
        _suggestionHeaderLabel.Click += (_, _) => _suggestionPopupEdit.ShowPopup();

        // Update popup dimensions right before opening
        _suggestionPopupEdit.QueryPopUp += (_, _) => UpdateSuggestionPopupSize();

        _suggestionPanel.Controls.Add(_suggestionPopupEdit);
        _suggestionPanel.Controls.Add(leftHeaderPanel);

        // ── Dropdown Overlay Footer: [☐ Select All]  [Add Selected (0)] ──
        var footerHeight = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(36, this);
        var footerPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = footerHeight,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(8, 4, 8, 4)
        };

        var footerTlp = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
            BackColor = Color.Transparent
        };
        footerTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
        footerTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
        footerTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _suggestionSelectAllCheck = new CheckEdit
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0)
        };
        _suggestionSelectAllCheck.Properties.Caption = "Select All";
        _suggestionSelectAllCheck.Properties.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        _suggestionSelectAllCheck.Properties.Appearance.ForeColor = Color.FromArgb(71, 85, 105);
        _suggestionSelectAllCheck.Properties.Appearance.Options.UseFont = true;
        _suggestionSelectAllCheck.Properties.Appearance.Options.UseForeColor = true;
        _suggestionSelectAllCheck.CheckedChanged += (_, _) =>
        {
            if (!_suggestionSyncingSelection)
            {
                SetAllSuggestionsSelected(_suggestionSelectAllCheck.Checked);
            }
        };

        _suggestionAddSelectedButton = new SimpleButton
        {
            Text = SuggestedAddOnUiHelper.ComposeAddSelectedText(0),
            Enabled = false,
            Dock = DockStyle.Fill,
            Cursor = Cursors.Hand,
            AllowFocus = false,
            Margin = new Padding(4, 0, 0, 0),
            ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat
        };
        _suggestionAddSelectedButton.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        _suggestionAddSelectedButton.Appearance.BackColor = AccentColor;
        _suggestionAddSelectedButton.Appearance.ForeColor = Color.White;
        _suggestionAddSelectedButton.Appearance.Options.UseFont = true;
        _suggestionAddSelectedButton.Appearance.Options.UseBackColor = true;
        _suggestionAddSelectedButton.Appearance.Options.UseForeColor = true;
        _suggestionAddSelectedButton.Click += async (_, _) => await AddSelectedSuggestionsAsync();

        footerTlp.Controls.Add(_suggestionSelectAllCheck, 0, 0);
        footerTlp.Controls.Add(_suggestionAddSelectedButton, 1, 0);
        footerPanel.Controls.Add(footerTlp);

        var divider = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 1,
            BackColor = Color.FromArgb(226, 232, 240)
        };

        // ── Dropdown Overlay Grid: DevExpress checked GridControl ──
        _suggestionGrid = new GridControl
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0)
        };
        _suggestionGridView = new GridView();
        _suggestionGrid.MainView = _suggestionGridView;

        _suggestionGridView.OptionsBehavior.Editable = false;
        _suggestionGridView.OptionsSelection.EnableAppearanceFocusedCell = false;
        _suggestionGridView.OptionsView.ShowGroupPanel = false;
        _suggestionGridView.OptionsView.ShowIndicator = false;
        _suggestionGridView.OptionsView.ColumnAutoWidth = true;
        _suggestionGridView.OptionsView.RowAutoHeight = false;
        _suggestionGridView.OptionsView.ShowColumnHeaders = true;
        _suggestionGridView.ColumnPanelRowHeight = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(26, this);
        _suggestionGridView.RowHeight = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(32, this);
        _suggestionGridView.Appearance.HeaderPanel.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        _suggestionGridView.Appearance.HeaderPanel.ForeColor = Color.FromArgb(71, 85, 105);
        _suggestionGridView.Appearance.HeaderPanel.Options.UseFont = true;
        _suggestionGridView.Appearance.HeaderPanel.Options.UseForeColor = true;
        _suggestionGridView.Appearance.Row.Font = new Font("Segoe UI", 9.5F);
        _suggestionGridView.Appearance.Row.Options.UseFont = true;
        _suggestionGridView.OptionsCustomization.AllowFilter = false;
        _suggestionGridView.OptionsCustomization.AllowSort = false;
        _suggestionGridView.OptionsCustomization.AllowGroup = false;
        _suggestionGridView.OptionsCustomization.AllowColumnMoving = false;
        _suggestionGridView.OptionsCustomization.AllowQuickHideColumns = false;
        _suggestionGridView.OptionsView.EnableAppearanceEvenRow = true;
        _suggestionGridView.Appearance.EvenRow.BackColor = Color.FromArgb(248, 250, 252);
        _suggestionGridView.Appearance.EvenRow.Options.UseBackColor = true;

        var selectCol = new DevExpress.XtraGrid.Columns.GridColumn
        {
            FieldName = nameof(SuggestedAddOnSelectionRow.Selected),
            Caption = "✓",
            Width = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(38, this)
        };
        selectCol.OptionsColumn.FixedWidth = true;
        selectCol.OptionsColumn.AllowSize = false;
        selectCol.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        selectCol.AppearanceHeader.Options.UseTextOptions = true;
        var checkEditor = new RepositoryItemCheckEdit();
        _suggestionGrid.RepositoryItems.Add(checkEditor);
        selectCol.ColumnEdit = checkEditor;

        var portionCol = new DevExpress.XtraGrid.Columns.GridColumn
        {
            FieldName = nameof(SuggestedAddOnSelectionRow.PortionName),
            Caption = "Portion",
            Width = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(75, this)
        };
        portionCol.OptionsColumn.FixedWidth = true;
        portionCol.OptionsColumn.AllowSize = false;

        var priceCol = new DevExpress.XtraGrid.Columns.GridColumn
        {
            FieldName = nameof(SuggestedAddOnSelectionRow.PriceText),
            Caption = "Price",
            Width = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(85, this)
        };
        priceCol.OptionsColumn.FixedWidth = true;
        priceCol.OptionsColumn.AllowSize = false;
        priceCol.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        priceCol.AppearanceHeader.Options.UseTextOptions = true;
        priceCol.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
        priceCol.AppearanceCell.Options.UseTextOptions = true;

        var itemCol = new DevExpress.XtraGrid.Columns.GridColumn
        {
            FieldName = nameof(SuggestedAddOnSelectionRow.ItemName),
            Caption = "Item",
            MinWidth = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(180, this)
        };
        itemCol.AppearanceCell.TextOptions.Trimming = DevExpress.Utils.Trimming.EllipsisCharacter;
        itemCol.AppearanceCell.Options.UseTextOptions = true;

        _suggestionGridView.OptionsView.ColumnAutoWidth = true;
        _suggestionGridView.Columns.AddRange([selectCol, itemCol, portionCol, priceCol]);
        for (int i = 0; i < _suggestionGridView.Columns.Count; i++)
            _suggestionGridView.Columns[i].VisibleIndex = i;

        // Touch-friendly: clicking ANY cell in a row toggles its checkbox.
        _suggestionGridView.RowCellClick += (_, e) =>
        {
            if (e.Clicks > 1 || e.RowHandle < 0 || e.RowHandle >= _suggestionRows.Count) return;
            SetSuggestionRowSelected(e.RowHandle, !_suggestionRows[e.RowHandle].Selected);
        };
        _suggestionGridView.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Space && _suggestionGridView.FocusedRowHandle >= 0
                && _suggestionGridView.FocusedRowHandle < _suggestionRows.Count)
            {
                var idx = _suggestionGridView.FocusedRowHandle;
                SetSuggestionRowSelected(idx, !_suggestionRows[idx].Selected);
                e.Handled = true;
            }
        };

        _suggestionPopupControl.Controls.Add(_suggestionGrid);    // Fill
        _suggestionPopupControl.Controls.Add(divider);             // Bottom
        _suggestionPopupControl.Controls.Add(footerPanel);         // Bottom

        Controls.Add(_suggestionPopupControl);

        if (_pnlOrderedItemsContainer != null)
        {
            _pnlOrderedItemsContainer.Controls.Add(_suggestionPanel);
            _flowOrderedItems?.BringToFront();
        }
    }

    /// <summary>
    /// Called after every order refresh: debounces a GetBasketRecommendations
    /// query whenever the basket's product composition changed. Purely
    /// additive - failures only hide the strip, never block the POS.
    /// </summary>
    private void ScheduleSuggestionRefresh(bool forced = false)
    {
        if (_suggestionPanel is null || IsDisposed)
        {
            return;
        }

        var order = _currentOrder;
        var variantIds = _currentOrderLines
            .Where(l => !l.IsVoided)
            .Select(l => l.ProductVariantId)
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        var basketKey = order is null ? "-" : $"{order.OrderId}|{string.Join(",", variantIds)}";
        if (!forced && string.Equals(basketKey, _lastSuggestionBasketKey, StringComparison.Ordinal))
        {
            return;
        }

        _lastSuggestionBasketKey = basketKey;

        // A different order becoming current resets dismissals; a changed
        // product set (beyond quantity) re-admits previously dismissed items.
        if (order?.OrderId != _suggestionOrderId)
        {
            SuggestionTracker.Reset();
            _recordedOfferedVariantIds.Clear();
            _suggestionOrderId = order?.OrderId;
        }
        SuggestionTracker.OnBasketChanged(variantIds);

        if (order is null || variantIds.Count == 0)
        {
            HideSuggestionContent();
            return;
        }

        if (!forced && !RushMode.AllowSuggestionAutoPopup)
        {
            // Rush mode: no auto popups - still available via More > Suggestions.
            return;
        }

        _suggestionCts?.Cancel();
        _suggestionCts?.Dispose();
        _suggestionCts = new CancellationTokenSource();
        var ct = _suggestionCts.Token;

        _ = LoadSuggestionsAsync(order.OrderId, order.CustomerId, variantIds, ct);
    }

    private async Task LoadSuggestionsAsync(Guid orderId, Guid? customerId, List<Guid> variantIds, CancellationToken ct)
    {
        try
        {
            await Task.Delay(500, ct); // debounce

            var recommendations = await _mediator.Send(
                new GetBasketRecommendationsQuery(customerId, variantIds, DateTime.Now, int.MaxValue), ct);

            if (ct.IsCancellationRequested || IsDisposed || _currentOrder?.OrderId != orderId)
            {
                return;
            }

            var inBasket = variantIds.ToHashSet();
            var visible = recommendations
                .Where(r => !inBasket.Contains(r.VariantId) && !SuggestionTracker.IsDismissed(r.VariantId))
                .ToList();

            if (visible.Count == 0)
            {
                HideSuggestionContent();
                return;
            }

            _currentSuggestions = visible;
            RecordOfferedSuggestions(orderId, visible);
            ShowSuggestions(visible);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer basket - fine.
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            _logger?.LogError(ex, "Smart suggestion query failed.");
            HideSuggestionContent();
        }
    }

    private void ShowSuggestions(IReadOnlyList<BasketRecommendationDto> recommendations)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => ShowSuggestions(recommendations)));
            return;
        }

        if (IsDisposed || _suggestionPanel is null)
        {
            return;
        }

        _suggestionRows.Clear();
        foreach (var r in recommendations.DistinctBy(rec => rec.VariantId))
        {
            var (itemName, portionName) = SuggestedAddOnNaming.Resolve(r.ProductName, r.VariantName);
            var row = new SuggestedAddOnRow(r.VariantId, itemName, portionName, r.UnitPrice);
            _suggestionRows.Add(new SuggestedAddOnSelectionRow(row));
        }

        _suggestionGrid.DataSource = null;
        _suggestionGrid.DataSource = _suggestionRows;
        _suggestionGridView.RefreshData();

        UpdateSuggestionSelectionUi();
        UpdateSuggestionPanelHeight(_suggestionRows.Count);
    }

    /// <summary>
    /// Computes and sets the size of the floating dropdown overlay dynamically based
    /// on recommendation count.
    /// 1-4 suggestions: sized to fit rows without empty space.
    /// 5+ suggestions: capped at 4 rows with vertical scrolling.
    /// </summary>
    private void UpdateSuggestionPopupSize()
    {
        if (_suggestionPopupControl is null || IsDisposed) return;

        int rowCount = _suggestionRows.Count;
        int colHeaderH = _suggestionGridView?.ColumnPanelRowHeight > 0 
            ? _suggestionGridView.ColumnPanelRowHeight 
            : Clovent.Desktop.Forms.Base.DesktopDpi.Scale(26, this);
        int rowH = _suggestionGridView?.RowHeight > 0 
            ? _suggestionGridView.RowHeight 
            : Clovent.Desktop.Forms.Base.DesktopDpi.Scale(32, this);
        int footerH = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(36, this);
        int dividerH = 1;
        int visibleRows = Math.Clamp(rowCount, 1, 4);
        int totalH = colHeaderH + (visibleRows * rowH) + footerH + dividerH + Clovent.Desktop.Forms.Base.DesktopDpi.Scale(6, this);
        int popupW = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(430, this);

        _suggestionPopupControl.Size = new Size(popupW, totalH);
        if (_suggestionPopupEdit is not null)
        {
            _suggestionPopupEdit.Properties.PopupFormSize = new Size(popupW, totalH);
            _suggestionPopupEdit.Properties.PopupFormMinSize = new Size(popupW, totalH);
        }
    }

    /// <summary>
    /// Controls the visibility and compact height of the main POS suggestion trigger strip.
    /// 0 suggestions: section hidden entirely (Height = 0, Visible = false).
    /// 1+ suggestions: compact single row (Height = 28, Visible = true).
    /// The actual grid lives solely inside the temporary dropdown overlay.
    /// </summary>
    private void UpdateSuggestionPanelHeight(int rowCount)
    {
        if (_suggestionPanel is null || IsDisposed) return;

        if (rowCount <= 0)
        {
            _suggestionPanel.Visible = false;
            _suggestionPanel.Height = 0;
            if (_suggestionPopupEdit is not null)
            {
                _suggestionPopupEdit.EditValue = "0 suggestions";
            }
            return;
        }

        int compactHeight = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(32, this);
        _suggestionPanel.Height = compactHeight;
        _suggestionPanel.Visible = true;

        if (_suggestionHeaderIcon is not null)
        {
            int iconSize = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(22, this);
            _suggestionHeaderIcon.Size = new Size(iconSize, iconSize);
            _suggestionHeaderIcon.Margin = new Padding(0, Clovent.Desktop.Forms.Base.DesktopDpi.Scale(1, this), Clovent.Desktop.Forms.Base.DesktopDpi.Scale(4, this), 0);
            if (_suggestionHeaderIcon.SvgImage == null)
            {
                try
                {
                    _suggestionHeaderIcon.SvgImage = DevExpress.Images.ImageResourceCache.Default.GetSvgImage(Clovent.Desktop.Forms.Base.DesktopIcons.Idea);
                }
                catch {}
            }
            _suggestionHeaderIcon.Parent?.PerformLayout();
        }

        if (_suggestionPopupEdit is not null)
        {
            _suggestionPopupEdit.EditValue = $"{rowCount} suggestion{(rowCount == 1 ? "" : "s")}";
        }

        UpdateSuggestionPopupSize();
    }

    /// <summary>Selects or deselects all visible recommendation rows.</summary>
    private void SetAllSuggestionsSelected(bool selected)
    {
        if (_suggestionAdding) return;

        foreach (var row in _suggestionRows)
        {
            row.Selected = selected;
        }

        _suggestionGridView.RefreshData();
        UpdateSuggestionSelectionUi();
    }

    /// <summary>Selects or deselects a single suggestion row.</summary>
    private void SetSuggestionRowSelected(int rowIndex, bool selected)
    {
        if (_suggestionAdding || rowIndex < 0 || rowIndex >= _suggestionRows.Count) return;

        _suggestionRows[rowIndex].Selected = selected;
        _suggestionGridView.RefreshRow(rowIndex);
        UpdateSuggestionSelectionUi();
    }

    private void UpdateSuggestionSelectionUi()
    {
        var count = _suggestionRows.Count(r => r.Selected);
        _suggestionAddSelectedButton.Text = SuggestedAddOnUiHelper.ComposeAddSelectedText(count);
        _suggestionAddSelectedButton.Enabled = count > 0 && !_suggestionAdding;

        _suggestionSyncingSelection = true;
        _suggestionSelectAllCheck.Checked = count > 0 && count == _suggestionRows.Count;
        _suggestionSyncingSelection = false;
    }

    /// <summary>
    /// Adds all currently checked suggestions to the cart using the normal POS
    /// item-add pipeline (preserving duplicate merges, variant handling, pricing,
    /// tax, discounts, service charges, and totals), records Accepted SuggestionEvents,
    /// and refreshes the recommendations.
    /// </summary>
    private async Task AddSelectedSuggestionsAsync()
    {
        var selectedVariantIds = _suggestionRows
            .Where(r => r.Selected)
            .Select(r => r.Row.VariantId)
            .ToList();

        if (_suggestionAdding || selectedVariantIds.Count == 0 || _currentOrder is null)
        {
            return;
        }

        var orderId = _currentOrder.OrderId;

        _suggestionAdding = true;
        _suggestionAddSelectedButton.Enabled = false;
        _suggestionSelectAllCheck.Enabled = false;
        _suggestionGrid.Enabled = false;

        // Close dropdown overlay immediately upon action
        if (_suggestionPopupEdit is not null && _suggestionPopupEdit.IsPopupOpen)
        {
            _suggestionPopupEdit.ClosePopup();
        }

        try
        {
            await AddSuggestedItemsAsync(orderId, selectedVariantIds, committedId =>
            {
                foreach (var row in _suggestionRows.Where(r => r.Row.VariantId == committedId))
                {
                    row.Selected = false;
                }
            });
        }
        finally
        {
            _suggestionAdding = false;
            if (!IsDisposed && _suggestionPanel is not null)
            {
                _suggestionSelectAllCheck.Enabled = true;
                _suggestionGrid.Enabled = true;
                UpdateSuggestionSelectionUi();
            }
        }
    }

    private async Task AddSuggestedItemsAsync(Guid orderId, IReadOnlyList<Guid> variantIds, Action<Guid> committed)
    {
        await _orderMutationLock.WaitAsync();
        _suggestionCts?.Cancel();
        try
        {
            if (_currentOrder?.OrderId != orderId)
                throw new InvalidOperationException("The current order has changed. Refresh suggestions.");

            foreach (var variantId in variantIds.Distinct())
            {
                var line = await AddProductToCurrentOrderCoreAsync(variantId, 1);
                committed(variantId); // Confirmed persistence: never replay after a refresh failure.
                SuggestionTracker.UnDismiss(variantId);
                RecordSuggestionEvent(SuggestionEventKind.Accepted, orderId, variantId,
                    line.OrderLineId, 1m, line.UnitPrice);
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            _logger?.LogError(ex, "Could not finish adding selected suggestions to order {OrderId}.", orderId);
            throw;
        }
        finally
        {
            try { await RefreshOrderAsync(); }
            finally { _orderMutationLock.Release(); }
        }

        _suggestionCts?.Cancel();
        _suggestionCts?.Dispose();
        _suggestionCts = new CancellationTokenSource();
        await LoadSuggestionsAsync(orderId, _currentOrder?.CustomerId,
            _currentOrderLines.Where(l => !l.IsVoided).Select(l => l.ProductVariantId).Distinct().ToList(),
            _suggestionCts.Token);
    }

    private void HideSuggestionContent()
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(HideSuggestionContent));
            return;
        }

        _currentSuggestions = [];
        _suggestionRows.Clear();

        if (_suggestionPopupEdit is not null && _suggestionPopupEdit.IsPopupOpen)
        {
            _suggestionPopupEdit.ClosePopup();
        }

        if (_suggestionPanel is not null)
        {
            _suggestionPanel.Visible = false;
            _suggestionPanel.Height = 0;
        }

        if (_suggestionPopupEdit is not null)
        {
            _suggestionPopupEdit.EditValue = "0 suggestions";
        }

        if (_suggestionGrid is not null)
        {
            _suggestionGrid.DataSource = null;
        }
    }

    /// <summary>
    /// Fire-and-forget analytics write for one suggestion interaction. Never
    /// awaited by callers and never allowed to surface an error - analytics
    /// must not slow or break the cashier workflow.
    /// </summary>
    private void RecordSuggestionEvent(SuggestionEventKind kind, Guid orderId, Guid variantId, Guid? orderLineId = null, decimal quantity = 0m, decimal unitAmount = 0m)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _mediator.Send(new RecordSuggestionEventCommand(orderId, variantId, null, kind, orderLineId, quantity, unitAmount));
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                _logger?.LogError(ex, "Suggestion analytics write failed.");
            }
        });
    }

    /// <summary>Counts a variant as offered once per order (refreshes never double-count).</summary>
    private void RecordOfferedSuggestions(Guid orderId, IReadOnlyList<BasketRecommendationDto> recommendations)
    {
        foreach (var recommendation in recommendations)
        {
            if (_recordedOfferedVariantIds.Add(recommendation.VariantId))
            {
                RecordSuggestionEvent(SuggestionEventKind.Offered, orderId, recommendation.VariantId);
            }
        }
    }

    /// <summary>Attributes the just-added suggested line to the suggestion interaction.</summary>
    private void RecordAcceptedSuggestion(Guid orderId, Guid variantId)
    {
        var line = _currentOrderLines.FirstOrDefault(l => !l.IsVoided && l.ProductVariantId == variantId);
        if (line is not null)
        {
            RecordSuggestionEvent(SuggestionEventKind.Accepted, orderId, variantId, line.OrderLineId, 1m, line.UnitPrice);
            _recordedOfferedVariantIds.Remove(variantId);
        }
    }

    // ---------- 2. Quick Orders strip ----------

    private void BuildQuickOrdersStrip()
    {
        // Collapsible strip placed between the category bar and the product grid:
        // collapsed it is a compact single toggle line; expanded it shows the toggle
        // plus one deal button per active template in a clean horizontal strip.
        _quickOrdersStrip = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 2, 0, 4),
            BackColor = Color.FromArgb(248, 250, 252),
            Visible = false // shown once templates load (and permission allows)
        };

        var tlp = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
            BackColor = Color.Transparent
        };
        var toggleWidth = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(140, this);
        tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, toggleWidth));
        tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _quickOrdersToggle = new SimpleButton
        {
            Text = "⚡ Quick Orders ▸",
            Dock = DockStyle.Fill,
            Width = toggleWidth,
            Cursor = Cursors.Hand,
            AllowFocus = false,
            Margin = new Padding(0, 2, 8, 2),
            Padding = new Padding(8, 0, 8, 0),
            ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat
        };
        _quickOrdersToggle.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        _quickOrdersToggle.Appearance.BackColor = Color.White;
        _quickOrdersToggle.Appearance.ForeColor = Color.FromArgb(71, 85, 105);
        _quickOrdersToggle.Appearance.BorderColor = Color.FromArgb(203, 213, 225);
        _quickOrdersToggle.Appearance.Options.UseFont = true;
        _quickOrdersToggle.Appearance.Options.UseBackColor = true;
        _quickOrdersToggle.Appearance.Options.UseForeColor = true;
        _quickOrdersToggle.Appearance.Options.UseBorderColor = true;
        _quickOrdersToggle.Click += (_, _) => ToggleQuickOrdersStrip();

        _quickOrdersFlowPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            Margin = new Padding(0),
            BackColor = Color.Transparent,
            Visible = false
        };

        tlp.Controls.Add(_quickOrdersToggle, 0, 0);
        tlp.Controls.Add(_quickOrdersFlowPanel, 1, 0);
        _quickOrdersStrip.Controls.Add(tlp);

        // Add to row 2 of _tlpCenterRows (directly below category bar, above product grid)
        _tlpCenterRows.Controls.Add(_quickOrdersStrip, 0, 2);
    }

    private async void ToggleQuickOrdersStrip()
    {
        _quickOrdersExpanded = !_quickOrdersExpanded;
        if (_quickOrdersExpanded) await ReloadQuickOrderTemplatesAsync();
        ApplyQuickOrdersStripState();
    }

    private void ApplyQuickOrdersStripState()
    {
        if (_quickOrdersStrip is null)
        {
            return;
        }

        var isVisible = _quickOrdersStrip.Visible;
        int rowHeight = 0;
        if (isVisible)
        {
            rowHeight = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(_quickOrdersExpanded ? 44 : 28, this);
        }

        if (_tlpCenterRows != null && _tlpCenterRows.RowStyles.Count > 2)
        {
            _tlpCenterRows.RowStyles[2].Height = rowHeight;
            _tlpCenterRows.PerformLayout();
        }

        _quickOrdersToggle.Text = _quickOrdersExpanded ? "⚡ Quick Orders ▾" : "⚡ Quick Orders ▸";
        _quickOrdersFlowPanel.Visible = _quickOrdersExpanded;
        if (_moreQuickOrdersItem is not null)
        {
            _moreQuickOrdersItem.Checked = _quickOrdersExpanded;
        }
    }

    private async Task ReloadQuickOrderTemplatesAsync()
    {
        if (!Permit("quickorders"))
        {
            _quickOrderTemplates = [];
            _quickOrdersStrip.Visible = false;
            ApplyQuickOrdersStripState();
            return;
        }

        try
        {
            _quickOrderTemplates = [.. await _mediator.Send(new ListActiveQuickOrderTemplatesQuery(_currentOrder?.WarehouseId ?? _warehousePicker.SelectedId))];
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            _logger?.LogError(ex, "Quick order templates failed to load.");
            _quickOrderTemplates = [];
        }

        RebuildQuickOrderButtons();
    }

    private void RebuildQuickOrderButtons()
    {
        if (_quickOrdersFlowPanel is null)
        {
            return;
        }

        _quickOrdersFlowPanel.SuspendLayout();
        foreach (Control old in _quickOrdersFlowPanel.Controls)
        {
            old.Dispose();
        }
        _quickOrdersFlowPanel.Controls.Clear();

        foreach (var template in _quickOrderTemplates)
        {
            var captured = template;
            var button = new SimpleButton
            {
                Text = $"{template.Name} · {CurrencyDisplay.FormatPlain(template.TotalPrice)}",
                AutoSize = true,
                Height = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(32, this),
                Cursor = Cursors.Hand,
                AllowFocus = false,
                Margin = new Padding(2, 2, 6, 2),
                Padding = new Padding(10, 0, 10, 0),
                ToolTip = template.Description is { Length: > 0 } d ? d : $"{template.Items.Count} item(s) - click to preview",
                ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat
            };
            button.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            button.Appearance.BackColor = Color.White;
            button.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
            button.Appearance.BorderColor = Color.FromArgb(203, 213, 225);
            button.Appearance.Options.UseFont = true;
            button.Appearance.Options.UseBackColor = true;
            button.Appearance.Options.UseForeColor = true;
            button.Appearance.Options.UseBorderColor = true;
            // Clicking a deal previews its real configured contents first; the
            // deal is only added to the order when the cashier confirms in the
            // preview (Preview → Add Deal). Nothing is added on the first click.
            button.Click += async (_, _) =>
            {
                using var preview = new QuickOrderPreviewDialog(
                    captured,
                    this,
                    () => _currentOrder is not null && _currentOrder.Status is ("Open" or "Held"),
                    async () => await _mediator.Send(new ListAllTablesQuery()));

                if (preview.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                await HandleQuickOrderAddAsync(captured, preview.StartMode, preview.SelectedTableId);
            };
            _quickOrdersFlowPanel.Controls.Add(button);
        }

        _quickOrdersFlowPanel.ResumeLayout(true);

        var permitted = Permit("quickorders");
        _quickOrdersStrip.Visible = permitted && _quickOrderTemplates.Count > 0;
        _moreQuickOrdersItem.Visible = permitted;
        _moreQuickOrdersItem.Checked = _quickOrdersExpanded;
        ApplyQuickOrdersStripState();
    }

    private bool _isApplyingQuickOrder;

    private async Task HandleQuickOrderAddAsync(QuickOrderTemplateDto template, StartOrderChoice startMode, Guid? selectedTableId)
    {
        if (template.WarehouseId is { } scopeWarehouse && scopeWarehouse != (_currentOrder?.WarehouseId ?? _warehousePicker.SelectedId))
        {
            XtraMessageBox.Show(this, "This deal belongs to a different location. Refresh Quick Orders.", "Location changed");
            return;
        }
        if (_isApplyingQuickOrder) return;
        _isApplyingQuickOrder = true;

        Guid? newlyCreatedOrderId = null;
        try
        {
            if (_currentOrder is null || _currentOrder.Status is not ("Open" or "Held"))
            {
                var warehouseId = _warehousePicker.SelectedId;
                if (warehouseId is null)
                {
                    var warehouses = await _mediator.Send(new ListAllWarehousesQuery());
                    if (warehouses.Count > 0)
                    {
                        warehouseId = warehouses.First().WarehouseId;
                        _warehousePicker.SelectId(warehouseId);
                    }
                }

                if (warehouseId is not { } effectiveWarehouseId)
                {
                    XtraMessageBox.Show(this, "Select a location first.", "No Location Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (startMode == StartOrderChoice.TakeAway)
                {
                    var order = await _mediator.Send(new CreateOrderCommand(OrderType.TakeAway, effectiveWarehouseId));
                    if (_defaultCustomerId is { } defaultCustId)
                    {
                        order = await _mediator.Send(new SetOrderCustomerCommand(order.OrderId, defaultCustId));
                    }
                    _currentOrder = order;
                    _currentOrderLines = [];
                    newlyCreatedOrderId = order.OrderId;
                    _hasUnsavedEdits = true;
                    await LogActivityAsync("New Order", $"{_currentOrder.OrderNumber} (Take Away)");
                }
                else if (startMode == StartOrderChoice.DineIn)
                {
                    if (selectedTableId is not { } tableId)
                    {
                        return;
                    }

                    void SelectTable() => _tablePicker.SelectId(tableId);
                    if (InvokeRequired) Invoke(SelectTable); else SelectTable();
                    var order = await _mediator.Send(new CreateOrderCommand(OrderType.DineIn, effectiveWarehouseId, tableId));
                    if (_defaultCustomerId is { } defaultCustId)
                    {
                        order = await _mediator.Send(new SetOrderCustomerCommand(order.OrderId, defaultCustId));
                    }
                    _currentOrder = order;
                    _currentOrderLines = [];
                    newlyCreatedOrderId = order.OrderId;
                    _hasUnsavedEdits = true;
                    await LogActivityAsync("New Order", $"{_currentOrder.OrderNumber} (Dine-In)");
                }
                else
                {
                    return;
                }
            }

            try
            {
                await _orderMutationLock.WaitAsync();
                try
                {
                    foreach (var item in template.Items)
                    {
                        await AddProductToCurrentOrderCoreAsync(item.VariantId, item.Quantity, item.UnitPrice);
                    }
                }
                finally
                {
                    _orderMutationLock.Release();
                }

                await LogActivityAsync("Quick Order", $"{template.Name} ({template.Items.Count} items)");
                await RefreshOrderAsync();
                await RefreshActiveOrdersAsync();
                ScheduleSuggestionRefresh(forced: true);
            }
            catch (Exception ex)
            {
                var qaDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "qa", "quick_orders_deals_live");
                var tracePath = Path.Combine(qaDir, "qa_trace.log");
                try { File.AppendAllText(tracePath, $"[HANDLE_QUICK_ORDER_EXCEPTION] {ex}\r\n"); } catch { }
                _logger.LogError(ex, "Failed to apply quick order '{TemplateName}'.", template.Name);
                if (newlyCreatedOrderId is { } rollbackId)
                {
                    try
                    {
                        await _mediator.Send(new CancelOrderCommand(rollbackId, "Failed to apply quick order items"));
                    }
                    catch (Exception rbEx)
                    {
                        _logger.LogError(rbEx, "Failed to cancel order {OrderId} during rollback.", rollbackId);
                    }
                    _currentOrder = null;
                    void ResetTablePicker() => _tablePicker.SelectId(null);
                    if (InvokeRequired) Invoke(ResetTablePicker); else ResetTablePicker();
                    await ReloadTablesAsync();
                    await RefreshOrderAsync();
                    await RefreshActiveOrdersAsync();
                }
                throw;
            }
        }
        finally
        {
            _isApplyingQuickOrder = false;
        }
    }

    private async Task ApplyQuickOrderTemplateAsync(QuickOrderTemplateDto template)
    {
        if (_currentOrder is null || _currentOrder.Status is not ("Open" or "Held"))
        {
            using var choiceDialog = new StartOrderChoiceDialog(this);
            if (choiceDialog.ShowDialog(this) != DialogResult.OK || choiceDialog.Choice == StartOrderChoice.Cancel)
            {
                return;
            }

            if (choiceDialog.Choice == StartOrderChoice.TakeAway)
            {
                await HandleQuickOrderAddAsync(template, StartOrderChoice.TakeAway, null);
                return;
            }

            if (choiceDialog.Choice == StartOrderChoice.DineIn)
            {
                var tables = await _mediator.Send(new ListAllTablesQuery());
                using var tableDialog = new SelectTableDialog(tables, this);
                if (tableDialog.ShowDialog(this) != DialogResult.OK || tableDialog.SelectedTableId == null)
                {
                    return;
                }

                await HandleQuickOrderAddAsync(template, StartOrderChoice.DineIn, tableDialog.SelectedTableId);
                return;
            }

            return;
        }

        await HandleQuickOrderAddAsync(template, StartOrderChoice.Cancel, null);
    }

    // ---------- 3. Order health on the Active Orders rail ----------

    private void BuildOrderHealthTimer()
    {
        var (green, orange) = Clovent.Desktop.Forms.Base.PosSettingsStore.LoadOrderHealthThresholds();
        _orderHealthThresholds = new OrderHealthThresholds { GreenMinutes = green, OrangeMinutes = orange };

        _orderHealthTimer = new System.Windows.Forms.Timer { Interval = 30_000 };
        _orderHealthTimer.Tick += (_, _) => TickOrderHealth();
    }

    private static string OrderHealthGlyph(OrderHealthStatus status) => status switch
    {
        OrderHealthStatus.Green => "🟢",
        OrderHealthStatus.Orange => "🟠",
        _ => "🔴"
    };

    /// <summary>The second line of an Active Orders card, including the health pill for live orders.</summary>
    private string FormatSidebarCardLine2(OrderDto order)
    {
        var itemCount = order.OrderLineIds.Count;

        if (order.Status is "Open" or "Held")
        {
            try
            {
                var health = OrderHealthEvaluator.Evaluate(DateTimeOffset.UtcNow - order.CreatedAtUtc, _orderHealthThresholds);
                return $"{itemCount} items · {OrderHealthGlyph(health.Status)} {health.DisplayText}";
            }
            catch (ArgumentOutOfRangeException)
            {
                // Invalid persisted thresholds: fall through to the plain age.
            }
        }

        return $"{itemCount} items · {RelativeAge(order.CreatedAtUtc)}";
    }

    private void RegisterOrderHealthCard(OrderDto order, LabelControl statusLabel)
    {
        if (order.Status is "Open" or "Held")
        {
            _orderHealthCards[order.OrderId] = new OrderHealthCardState(statusLabel, order.CreatedAtUtc, IsLive: true);
        }
        else
        {
            _orderHealthCards.Remove(order.OrderId);
        }
    }

    private void SyncOrderHealthTimer()
    {
        if (_orderHealthTimer is null)
        {
            return;
        }

        if (_orderHealthCards.Count == 0)
        {
            _orderHealthTimer.Stop();
            return;
        }

        if (Visible && !_orderHealthTimer.Enabled)
        {
            _orderHealthTimer.Start();
        }
        else if (!Visible)
        {
            _orderHealthTimer.Stop();
        }
    }

    /// <summary>Timer tick: label text updates only - no queries, no control creation.</summary>
    private void TickOrderHealth()
    {
        try
        {
            if (IsDisposed || !Visible)
            {
                _orderHealthTimer?.Stop();
                return;
            }

            foreach (var state in _orderHealthCards.Values)
            {
                if (!state.IsLive || state.StatusLabel.IsDisposed)
                {
                    continue;
                }

                try
                {
                    var health = OrderHealthEvaluator.Evaluate(DateTimeOffset.UtcNow - state.CreatedAtUtc, _orderHealthThresholds);
                    state.StatusLabel.Text = ReplaceHealthSegment(state.StatusLabel.Text, health);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Bad thresholds: leave the text alone rather than crash.
                }
            }

            SyncOrderHealthTimer();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            _logger?.LogError(ex, "Order health tick failed.");
            _orderHealthTimer?.Stop();
        }
    }

    private static string ReplaceHealthSegment(string currentText, OrderHealthResult health)
    {
        // Card line 2 looks like "3 items · 🟢 Waiting 4 min"; replace from
        // the first emoji/age marker onward, preserving the item count.
        var separator = currentText.IndexOf("·", StringComparison.Ordinal);
        var prefix = separator >= 0 ? currentText[..(separator + 1)] : string.Empty;
        return $"{prefix} {OrderHealthGlyph(health.Status)} {health.DisplayText}";
    }

    // ---------- 4. Rush mode ----------

    private void BuildRushModeBadge()
    {
        _rushModeBadge = new LabelControl
        {
            Text = "⚡ RUSH MODE ON",
            Dock = DockStyle.Fill,
            AutoSizeMode = LabelAutoSizeMode.None,
            Margin = new Padding(4, 8, 4, 8),
            Visible = false
        };
        _rushModeBadge.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        _rushModeBadge.Appearance.ForeColor = Color.White;
        _rushModeBadge.Appearance.BackColor = Color.FromArgb(220, 38, 38);
        _rushModeBadge.Appearance.Options.UseFont = true;
        _rushModeBadge.Appearance.Options.UseForeColor = true;
        _rushModeBadge.Appearance.Options.UseBackColor = true;
        _rushModeBadge.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        _rushModeBadge.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _rushModeBadge.Appearance.Options.UseTextOptions = true;

        if (_headerTable is { } header)
        {
            header.Controls.Add(_rushModeBadge, header.ColumnCount, 0);
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        }
    }

    private void ApplyRushMode(object? sender, EventArgs e)
    {
        try
        {
            Clovent.Desktop.Forms.Base.PosSettingsStore.SaveRushModeEnabled(RushMode.Enabled);
            _rushModeBadge.Visible = RushMode.Enabled;
            if (_moreRushModeItem is not null)
            {
                _moreRushModeItem.Checked = RushMode.Enabled;
            }

            if (RushMode.Enabled)
            {
                // Kill any in-flight collapse animation and snap to the target.
                _sidebarAnimationTimer?.Stop();
                if (_activeOrdersExpanded)
                {
                    ApplyActiveOrdersState();
                }

                HideSuggestionContent();
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            _logger?.LogError(ex, "Failed to apply Rush Mode visuals.");
        }
    }

    private void ToggleRushMode()
    {
        RushMode.Enabled = !RushMode.Enabled;
    }

    // ---------- 5. Universal smart search ----------

    private void ScheduleUniversalSearch()
    {
        if (_productSearchEdit is null || IsDisposed)
        {
            return;
        }

        var term = _productSearchEdit.Text?.Trim() ?? string.Empty;
        if (term.Length < 2 || !Permit("smartinsights"))
        {
            CloseUniversalSearchDropdown();
            return;
        }

        _universalSearchCts?.Cancel();
        _universalSearchCts?.Dispose();
        _universalSearchCts = new CancellationTokenSource();
        var ct = _universalSearchCts.Token;

        _ = RunUniversalSearchAsync(term, ct);
    }

    private async Task RunUniversalSearchAsync(string term, CancellationToken ct)
    {
        try
        {
            await Task.Delay(350, ct); // debounce keystrokes

            if (ct.IsCancellationRequested)
            {
                return;
            }

            if (_universalSearchCache.TryGet(term, out var cached) && cached is not null)
            {
                ShowUniversalSearchResults(cached);
                return;
            }

            var results = await _mediator.Send(new UniversalPosSearchQuery(term, 4), ct);
            if (ct.IsCancellationRequested)
            {
                return;
            }

            _universalSearchCache.Set(term, results);
            ShowUniversalSearchResults(results);
        }
        catch (OperationCanceledException)
        {
            // Stale query - fine.
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            _logger?.LogError(ex, "Universal POS search failed for term '{Term}'.", term);
        }
    }

    private void ShowUniversalSearchResults(UniversalPosSearchResultsDto results)
    {
        if (_searchDropdown is null || _searchDropdown.IsDisposed)
        {
            _searchDropdown = new UniversalSearchDropdown();
            _searchDropdown.ItemInvoked += UniversalSearchItem_Invoked;
        }

        if (!_searchDropdown.SetResults(results))
        {
            _searchDropdown.Hide();
            return;
        }

        var origin = _productSearchEdit.PointToScreen(new Point(0, _productSearchEdit.Height));
        _searchDropdown.Location = origin;
        _searchDropdown.Show(this);
        // Keep typing in the search box - the dropdown is pointer/keyboard-driven.
        _productSearchEdit.Focus();
    }

    private void CloseUniversalSearchDropdown()
    {
        _universalSearchCts?.Cancel();
        if (_searchDropdown is { } dropdown && dropdown.Visible)
        {
            dropdown.Hide();
        }
    }

    private void ProductSearchEdit_SmartKeyDown(object? sender, KeyEventArgs e)
    {
        if (_searchDropdown is not { Visible: true })
        {
            return;
        }

        switch (e.KeyCode)
        {
            case Keys.Down:
                e.Handled = true;
                e.SuppressKeyPress = true;
                _searchDropdown.MoveSelection(1);
                break;
            case Keys.Up:
                e.Handled = true;
                e.SuppressKeyPress = true;
                _searchDropdown.MoveSelection(-1);
                break;
            case Keys.Enter:
                e.Handled = true;
                e.SuppressKeyPress = true;
                _searchDropdown.InvokeSelected();
                break;
            case Keys.Escape:
                e.Handled = true;
                e.SuppressKeyPress = true;
                _searchDropdown.Hide();
                break;
        }
    }

    private async void UniversalSearchItem_Invoked(UniversalSearchItem item)
    {
        try
        {
            CloseUniversalSearchDropdown();
            _productSearchEdit.Text = string.Empty; // triggers EditValueChanged -> ApplyProductFilter + close

            switch (item.Kind)
            {
                case UniversalSearchItemKind.Product:
                    if (_currentOrder is null || _currentOrder.Status is not ("Open" or "Held"))
                    {
                        XtraMessageBox.Show(this, "Start a New Dine-In or New Take Away order first.", "No Order Started", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    await TryRunAsync(() => AddProductToCurrentOrder(item.Id, 1), "add the searched item");
                    break;

                case UniversalSearchItemKind.Customer:
                    SetSelectedCustomerId(item.Id);
                    if (_currentOrder is { } order && order.Status is "Open" or "Held" && order.CustomerId != item.Id)
                    {
                        await TryRunAsync(
                            () => RunOrderActionAsync(new SetOrderCustomerCommand(order.OrderId, item.Id)),
                            "set the order's customer");
                    }
                    break;

                case UniversalSearchItemKind.Order:
                    await OpenSearchedOrderAsync(item);
                    break;

                case UniversalSearchItemKind.Table:
                    await OpenSearchedTableAsync(item);
                    break;
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            _logger?.LogError(ex, "Universal search action failed for {Kind} {Id}.", item.Kind, item.Id);
        }
    }

    private async Task OpenSearchedOrderAsync(UniversalSearchItem item)
    {
        var fresh = await _mediator.Send(new GetOrderByIdQuery(item.Id));
        if (fresh is null)
        {
            XtraMessageBox.Show(this, "This order is no longer available.", "Order Unavailable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (fresh.Status is "Open" or "Held")
        {
            // Same path the Active Orders rail uses (handles hold/resume conflicts).
            await TryRunAsync(() => LoadOrderFromRailAsync(fresh), "open this order");
            return;
        }

        var totals = await _mediator.Send(new GetOrderSummaryQuery(fresh.OrderId));
        XtraMessageBox.Show(this,
            $"Order {fresh.OrderNumber} is {fresh.Status}.\nTotal: {CurrencyDisplay.FormatPlain(totals.GrandTotal)}\nPaid: {CurrencyDisplay.FormatPlain(totals.PaidTotal)}",
            "Order", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task OpenSearchedTableAsync(UniversalSearchItem item)
    {
        var openOrders = (await _mediator.Send(new ListOpenOrdersQuery()))
            .Concat(await _mediator.Send(new ListHeldOrdersQuery()))
            .Where(o => o.TableId == item.Id)
            .ToList();

        if (openOrders.Count > 0)
        {
            await TryRunAsync(() => LoadOrderFromRailAsync(openOrders[0]), "open this table's order");
            return;
        }

        if (_currentOrder is { OrderType: "DineIn", Status: "Open" or "Held" })
        {
            // Existing table-picker set logic (moves/creates through OnTableSelectedAsync).
            _tablePicker.SelectId(item.Id);
            return;
        }

        XtraMessageBox.Show(this,
            $"Table '{item.Title}' has no open order. Start a dine-in order and select the table from the picker.",
            "Table", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // ---------- 6. One-click customer reorder ----------

    private Guid? SelectedCustomerIdOrNull()
    {
        if (_customerPicker?.EditValue is Guid guid && guid != Guid.Empty)
        {
            return guid;
        }

        return _currentOrder?.CustomerId;
    }

    private async void MoreRepeatLastOrder_Click(object? sender, EventArgs e)
    {
        try
        {
            if (SelectedCustomerIdOrNull() is not { } customerId)
            {
                XtraMessageBox.Show(this, "Select a customer on the order first.", "Repeat Last Order", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_currentOrder is null || _currentOrder.Status is not ("Open" or "Held"))
            {
                XtraMessageBox.Show(this, "Start a New Dine-In or New Take Away order first.", "No Order Started", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new CustomerReorderDialog(_mediator, _logger, customerId);
            if (dialog.ShowDialog(this) != DialogResult.OK || dialog.SelectedLines.Count == 0)
            {
                return;
            }

            if (_currentOrderLines.Count > 0)
            {
                var confirm = XtraMessageBox.Show(this,
                    $"The cart already has {_currentOrderLines.Count} item(s). Add the {dialog.SelectedLines.Count} item(s) from the last order on top?",
                    "Repeat Last Order",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes)
                {
                    return;
                }
            }

            var unavailable = dialog.UnavailableCount;
            await TryRunAsync(async () =>
            {
                foreach (var line in dialog.SelectedLines.Where(l => l.IsAvailable))
                {
                    await AddProductToCurrentOrder(line.VariantId, line.Quantity);
                }

                await LogActivityAsync("Repeat Last Order", $"{dialog.SelectedLines.Count} item(s) re-added");
            }, "repeat the last order");

            if (unavailable > 0)
            {
                XtraMessageBox.Show(this,
                    $"{unavailable} item(s) were unavailable and were skipped.",
                    "Repeat Last Order", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            _logger?.LogError(ex, "Repeat Last Order failed.");
        }
    }

    private void MoreCustomerInsights_Click(object? sender, EventArgs e)
    {
        if (SelectedCustomerIdOrNull() is not { } customerId)
        {
            XtraMessageBox.Show(this, "Select a customer on the order first.", "Customer Order Insights", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new CustomerInsightsDialog(_mediator, _logger, customerId);
        dialog.ShowDialog(this);
    }

    // ---------- 7. Restaurant Pulse ----------

    private void MoreRestaurantPulse_Click(object? sender, EventArgs e)
    {
        var pulse = new RestaurantPulseForm(_mediator, _logger);
        pulse.Show(this);
    }

    // ---------- 8. Smart More-menu entries + permissions ----------

    private void BuildSmartMoreMenuItems()
    {
        _moreMenuSmartSeparator = new ToolStripSeparator();

        _moreQuickOrdersItem = new ToolStripMenuItem("⚡ Quick Orders");
        _moreQuickOrdersItem.Click += (_, _) =>
        {
            _quickOrdersStrip.Visible = _quickOrderTemplates.Count > 0 && Permit("quickorders");
            ToggleQuickOrdersStrip();
        };

        _moreShowSuggestionsItem = new ToolStripMenuItem("💡 Suggestions");
        _moreShowSuggestionsItem.Click += (_, _) => ScheduleSuggestionRefresh(forced: true);

        _moreRepeatLastOrderItem = new ToolStripMenuItem("🔁 Repeat Last Order");
        _moreRepeatLastOrderItem.Click += MoreRepeatLastOrder_Click;

        _moreCustomerInsightsItem = new ToolStripMenuItem("📊 Customer Order Insights…");
        _moreCustomerInsightsItem.Click += MoreCustomerInsights_Click;

        _moreRestaurantPulseItem = new ToolStripMenuItem("📈 Restaurant Pulse");
        _moreRestaurantPulseItem.Click += MoreRestaurantPulse_Click;

        _moreRushModeItem = new ToolStripMenuItem("⚡ Rush Mode");
        _moreRushModeItem.Click += (_, _) => ToggleRushMode();

        _moreActionsMenu.Items.AddRange(new ToolStripItem[]
        {
            _moreMenuSmartSeparator,
            _moreQuickOrdersItem,
            _moreShowSuggestionsItem,
            _moreRepeatLastOrderItem,
            _moreCustomerInsightsItem,
            _moreRestaurantPulseItem,
            _moreRushModeItem
        });
    }

    /// <summary>Hides smart menu entries the current user has no permission for. Never throws.</summary>
    private void ApplySmartFeaturePermissions()
    {
        try
        {
            var smartInsights = Permit("smartinsights");
            var quickOrders = Permit("quickorders");
            var pulse = Permit("restaurantpulse");
            var rush = Permit("rushmode");

            _moreQuickOrdersItem.Visible = quickOrders;
            _moreShowSuggestionsItem.Visible = smartInsights;
            _moreRepeatLastOrderItem.Visible = smartInsights;
            _moreCustomerInsightsItem.Visible = smartInsights;
            _moreRestaurantPulseItem.Visible = pulse;
            _moreRushModeItem.Visible = rush;
            _moreMenuSmartSeparator.Visible = smartInsights || quickOrders || pulse || rush;

            if (!quickOrders && _quickOrdersStrip != null)
            {
                _quickOrdersStrip.Visible = false;
                ApplyQuickOrdersStripState();
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            _logger?.LogError(ex, "Failed to apply smart feature permissions.");
        }
    }
}



