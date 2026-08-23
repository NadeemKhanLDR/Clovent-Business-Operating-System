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
    private readonly Clovent.Desktop.MasterData.EntityPicker _tablePicker = new("Table:");

    private static readonly Color PageBackColor = Color.FromArgb(241, 245, 249);
    private static readonly Color HeaderBackColor = Color.White;
    private static readonly Color RailBackColor = Color.FromArgb(51, 65, 85);
    private static readonly Color RailSelectedBackColor = Color.FromArgb(37, 99, 235);
    private static readonly Color RailForeColor = Color.White;
    private static readonly Color TileBackColor = Color.White;
    private static readonly Color TileBorderColor = Color.FromArgb(226, 232, 240);
    private static readonly Color TilePriceColor = Color.FromArgb(37, 99, 235);
    private static readonly Color AccentColor = Color.FromArgb(37, 99, 235);
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

    private const int TileWidth = 160;
    private const int TileHeight = 150;

    private OrderDto? _currentOrder;
    private bool _isRefreshingOrder;
    private readonly Dictionary<Guid, ProductVariantDto> _variantsById = [];
    private readonly Dictionary<Guid, decimal> _sellingPricesByVariantId = [];
    private readonly Dictionary<Guid, Image> _tileImagesByProductId = [];
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
        _tablePicker.Margin = new Padding(8);
        _tablePicker.SelectionChanged += TablePicker_SelectionChanged;

        // Rows 2 and 3 of the order-context panel. They are NOT put in
        // _orderTypeButtonsFlow: that flow now lives in the header and has
        // WrapContents=false, so two 279px pickers in front of the buttons
        // pushed Dine In / Take Away off the right edge and out of view.
        _tablePicker.Dock = DockStyle.Fill;
        _warehousePicker.Dock = DockStyle.Fill;
        tlpOrderContext.Controls.Add(_warehousePicker, 0, 2);
        tlpOrderContext.Controls.Add(_tablePicker, 0, 3);
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
        _paymentBalanceLabel.Text = $"Balance Due: {CurrencyDisplay.FormatPlain(_balance)}";
        _amountEdit.Text = FormatPlain(_balance);
        _amountEntryIsPreset = true;
        BuildMethodButtons();
        UpdateChangeDisplay();
    }

    private void InitializeRuntime()
    {
        _changeNotifier.Changed += MenuItemsChangeNotifier_Changed;
        AppearanceManager.Changed += AppearanceManager_Changed;

        _cashierLabel.Text = _currentSession.DisplayName is { } name ? $"Cashier: {name}" : "Cashier: Not signed in";

        LocalizationHelper.LocalizeControl(this);

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
        try
        {
            await LoadAsync();
        }
        finally
        {
            _splashScreenService.Close();
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

    private async void HoldButton_Click(object? sender, EventArgs e) => await TryRunAsync(() => RunOrderActionAsync(new HoldOrderCommand(_currentOrder!.OrderId)), "hold this order");

    private async void ResumeButton_Click(object? sender, EventArgs e) => await TryRunAsync(() => RunOrderActionAsync(new ResumeOrderCommand(_currentOrder!.OrderId)), "resume this order");

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
        _categoryButtonsPanel.Controls.Clear();

        // Re-parented after every Clear() because the sort strip is a Designer
        // control living in this rail: Clear() drops it along with the
        // per-category buttons, and nothing else puts it back, so without this
        // line the two sort buttons vanish on the first rebuild.
        _categoryButtonsPanel.Controls.Add(_categorySortFlow);

        _allCategoriesButton.AutoSize = true;
        _allCategoriesButton.Margin = new Padding(0, 0, 6, 4);
        _categoryButtonsPanel.Controls.Add(_allCategoriesButton);

        var ordered = _sortCategoriesByColor
            ? _loadedCategories.OrderBy(c => c.ColorHex ?? "zzz").ThenBy(c => c.SortOrder).ThenBy(c => c.Name)
            : _loadedCategories.OrderBy(c => c.SortOrder).ThenBy(c => c.Name);

        foreach (var category in ordered)
        {
            var button = new SimpleButton
            {
                Text = $"{CategoryIcon(category.Name)}  {category.Name}",
                AutoSize = true,
                Tag = category.ProductCategoryId,
                Margin = new Padding(0, 0, 6, 4),
                Name = "CategoryButton",
            };
            StyleCategoryButton(button);
            button.Click += (_, _) => SelectCategory(category.ProductCategoryId);
            _categoryButtonsPanel.Controls.Add(button);
            AppearanceManager.Apply(button, "Restaurant", nameof(RestaurantPosForm));
        }

        UpdateCategoryButtonSelection();
        UpdateSortModeButtonSelection();
    }

    /// <summary>
    /// Applies the category-rail look to a button built at runtime from a menu
    /// category.
    /// </summary>
    /// <remarks>
    /// Lives here rather than in <c>RestaurantPosForm.Designer.cs</c>, where it
    /// used to: a helper in that file invites being called from
    /// <c>InitializeComponent</c>, which ADR-007 forbids because the Designer's
    /// parser cannot follow the call and silently drops everything after it.
    /// The one static "All Items" button is styled inline there instead; only
    /// the buttons created per category at runtime go through this.
    /// </remarks>
    private static void StyleCategoryButton(SimpleButton button)
    {
        button.Appearance.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        button.Appearance.BackColor = RailBackColor;
        button.Appearance.ForeColor = RailForeColor;
        button.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Near;
        button.Appearance.Options.UseFont = true;
        button.Appearance.Options.UseBackColor = true;
        button.Appearance.Options.UseForeColor = true;
        button.Appearance.Options.UseTextOptions = true;
        button.Margin = new Padding(0, 0, 0, 6);
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
            _ when name.Contains("drink") || name.Contains("beverage") || name.Contains("juice") => "🥤",
            _ when name.Contains("snack") || name.Contains("fries") || name.Contains("appetizer") => "🍟",
            _ when name.Contains("pizza") => "🍕",
            _ when name.Contains("burger") => "🍔",
            _ when name.Contains("seafood") || name.Contains("fish") => "🐟",
            _ when name.Contains("soup") => "🍲",
            _ when name.Contains("salad") => "🥗",
            _ => "🍽️",
        };
    }

    private void RestaurantPosForm_Resize(object? sender, EventArgs e) => ApplyColumnWidths();

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
        // foreach (SimpleButton button in ...) threw InvalidCastException when the
        // runtime reached that FlowLayoutPanel.  Iterate as Control and skip any
        // child that is not a SimpleButton so _categorySortFlow is silently ignored
        // while every category button still receives its selection state.
        foreach (Control control in _categoryButtonsPanel.Controls)
        {
            if (control is not SimpleButton button)
                continue;

            var isSelected = button.Tag is Guid categoryId && categoryId == _selectedCategoryId;
            var colorHex = button.Tag is Guid tagId ? _loadedCategories.FirstOrDefault(c => c.ProductCategoryId == tagId)?.ColorHex : null;
            SetCategoryButtonSelected(button, isSelected, colorHex);
        }
    }


    private static void SetCategoryButtonSelected(SimpleButton button, bool selected, string? colorHex)
    {
        button.Appearance.BackColor = selected ? RailSelectedBackColor : ParseColorHex(colorHex) ?? RailBackColor;
        button.Appearance.ForeColor = selected ? Color.White : RailForeColor;
        button.Appearance.Options.UseBackColor = true;
        button.Appearance.Options.UseForeColor = true;
    }

    private static Color? ParseColorHex(string? colorHex)
    {
        if (string.IsNullOrEmpty(colorHex))
        {
            return null;
        }

        try
        {
            return ColorTranslator.FromHtml(colorHex);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            return null;
        }
    }

    private void ApplyProductFilter()
    {
        var searchText = _productSearchEdit.Text.Trim();

        var filtered = _activeVariants.AsEnumerable();
        if (_selectedCategoryId is { } categoryId)
        {
            filtered = filtered.Where(v => v.ProductCategoryId == categoryId);
        }

        if (!string.IsNullOrEmpty(searchText))
        {
            filtered = filtered.Where(v =>
                v.Sku.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                v.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        RenderProductTiles([.. filtered]);
    }

    private void RenderProductTiles(IReadOnlyList<ProductVariantDto> variants)
    {
        _productTilesFlow.SuspendLayout();

        var oldTiles = _productTilesFlow.Controls.Cast<Control>().ToList();
        _productTilesFlow.Controls.Clear();
        foreach (var oldTile in oldTiles)
        {
            DisposeTile(oldTile);
        }

        foreach (var variant in variants)
        {
            _productTilesFlow.Controls.Add(BuildProductTile(variant));
        }

        _productTilesFlow.ResumeLayout();
        _productTilesFlow.Invalidate(true);

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

    private Control BuildProductTile(ProductVariantDto variant)
    {
        var price = _sellingPricesByVariantId.GetValueOrDefault(variant.ProductVariantId);
        var hasImage = _tileImagesByProductId.TryGetValue(variant.ProductId, out var image);

        var card = new DevExpress.XtraEditors.PanelControl
        {
            Size = new Size(TileWidth, TileHeight),
            Margin = new Padding(5),
            Cursor = Cursors.Hand,
        };
        card.Appearance.BackColor = TileBackColor;
        card.Appearance.Options.UseBackColor = true;
        card.Appearance.BorderColor = TileBorderColor;
        card.Appearance.Options.UseBorderColor = true;

        var priceLabel = new DevExpress.XtraEditors.LabelControl
        {
            Text = CurrencyDisplay.FormatPlain(price),
            Dock = DockStyle.Bottom,
            Height = 24,
            Padding = new Padding(6, 0, 6, 6),
            Name = "ProductTilePrice",
        };
        priceLabel.Appearance.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
        priceLabel.Appearance.ForeColor = TilePriceColor;
        priceLabel.Appearance.Options.UseFont = true;
        priceLabel.Appearance.Options.UseForeColor = true;
        priceLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        priceLabel.Appearance.Options.UseTextOptions = true;

        var nameLabel = new DevExpress.XtraEditors.LabelControl
        {
            Text = variant.Name,
            Dock = hasImage ? DockStyle.Bottom : DockStyle.Fill,
            Height = hasImage ? 34 : TileHeight,
            Padding = new Padding(6, 2, 6, 2),
            Name = "ProductTileName",
        };
        nameLabel.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        nameLabel.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
        nameLabel.Appearance.Options.UseFont = true;
        nameLabel.Appearance.Options.UseForeColor = true;
        nameLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        nameLabel.Appearance.TextOptions.VAlignment = hasImage ? DevExpress.Utils.VertAlignment.Top : DevExpress.Utils.VertAlignment.Center;
        nameLabel.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
        nameLabel.Appearance.Options.UseTextOptions = true;

        PictureBox? imageBox = null;
        if (hasImage)
        {
            imageBox = new PictureBox { Dock = DockStyle.Fill, Image = image, SizeMode = PictureBoxSizeMode.Zoom, BackColor = TileBackColor };
        }

        card.Controls.Add(imageBox is not null ? imageBox : nameLabel);
        if (imageBox is not null)
        {
            card.Controls.Add(nameLabel);
        }
        card.Controls.Add(priceLabel);

        foreach (Control tappable in new Control?[] { card, imageBox, nameLabel, priceLabel }.OfType<Control>())
        {
            WireTileClick(tappable, variant.ProductVariantId);
        }

        AppearanceManager.Apply(card, "Restaurant", nameof(RestaurantPosForm));

        return card;
    }

    private void WireTileClick(Control control, Guid variantId)
    {
        control.Cursor = Cursors.Hand;
        control.Click += async (_, _) => await TryRunAsync(() => ProductTileTappedAsync(variantId), "add this item");
    }

    private async Task ReloadTablesAsync()
    {
        var selectedTableId = _tablePicker.SelectedId;
        var tables = await _mediator.Send(new ListAllTablesQuery());
        _tablePicker.LoadItems([.. tables.Select(t => (t.TableId, $"{t.Code} ({t.OccupancyStatus})"))]);
        _tablePicker.SelectId(selectedTableId);
    }

    private async Task OnTableSelectedAsync()
    {
        if (_tablePicker.SelectedId is not { } tableId)
        {
            _currentOrder = null;
            await RefreshOrderAsync();
            return;
        }

        _currentOrder = await _mediator.Send(new GetOpenOrHeldOrderByTableQuery(tableId));
        await RefreshOrderAsync();
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
            XtraMessageBox.Show(this, "Select a table first.", "No Table Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _currentOrder = await _mediator.Send(new CreateOrderCommand(OrderType.DineIn, warehouseId, tableId));
        await RefreshOrderAsync();
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
        await RefreshOrderAsync();
        await LogActivityAsync("New Order", $"{_currentOrder.OrderNumber} (Take Away)");
    }

    private async Task RunOrderActionAsync(IRequest<OrderDto> command)
    {
        _currentOrder = await _mediator.Send(command);
        await RefreshOrderAsync();
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
        using var form = new TextPromptForm("Clear Bill", "Reason:", "Started by mistake", required: true);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _currentOrder = await _mediator.Send(new CancelOrderCommand(_currentOrder!.OrderId, form.Value!));
            await RefreshOrderAsync();
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

        await _mediator.Send(new VoidOrderLineCommand(row.OrderLineId));
        await RefreshOrderAsync();
    }

    private async Task RemoveLineAsync()
    {
        if (_lineGridView.GetFocusedRow() is not OrderLineRow row)
        {
            return;
        }

        await _mediator.Send(new RemoveOrderLineCommand(_currentOrder!.OrderId, row.OrderLineId));
        await RefreshOrderAsync();
        await LogActivityAsync("Remove Line", $"{_currentOrder.OrderNumber}: {row.Name}");
    }

    private async Task RefreshOrderAsync()
    {
        // Rebinding the grid resets focus to the first row; remember the
        // focused line's identity so it can be restored after the rebind.
        var focusedLineId = _lineGridView.GetFocusedRow() is OrderLineRow focused ? focused.OrderLineId : (Guid?)null;

        _isRefreshingOrder = true;
        try
        {
            // Permissions are loaded once by LoadCoreAsync, not here: this
            // method runs after every till interaction (29 call sites) and
            // UpdatePermissionsAsync costs 18 sequential, serialized
            // CanUseFeatureAsync round trips. A signed-in cashier's rights
            // cannot change mid-session, so re-querying them per keystroke
            // was the dominant cost of every POS action.
            await BindOrderPaymentAsync(_currentOrder?.OrderId, _currentSession.DisplayName ?? "Unknown");

            if (_currentOrder is null)
            {
                _currentOrderLines = [];
                _lineGrid.DataSource = null;
                UpdateBillEmptyState(isEmpty: true);
                SetTotals(null);
                _orderStatusLabel.Text = PosStrings.NoOrderSelected;
                UpdateOrderStatusBadge();
                UpdateButtonStates();

                SetSelectedCustomerId(Guid.Empty);
                _customerPicker.Enabled = false;
                _newCustomerButton.Enabled = false;
                _customerDetailsLabel.Text = string.Empty;
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
            // Voided lines stay in the database for audit/order history - the
            // active cart shows only live lines (totals already exclude voided
            // lines, see OrderTotalsCalculator).
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
            UpdateBillEmptyState(isEmpty: lines.Count == 0);

            var totals = await _mediator.Send(new GetOrderSummaryQuery(_currentOrder.OrderId));
            SetTotals(totals);

            _orderStatusLabel.Text = $"{_currentOrder.OrderNumber}  •  {_currentOrder.OrderType}  •  {_currentOrder.Status}";
            UpdateOrderStatusBadge();

            await ReloadTablesAsync();
        }
        finally
        {
            _isRefreshingOrder = false;

            // Runs even when a later refresh step (tables reload, customer
            // lookup) threw, so Print/History state always matches the
            // currently-loaded order instead of staying stale-disabled.
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

    private string ResolveVariantName(Guid variantId) => _variantsById.TryGetValue(variantId, out var v) ? v.Name : "(unknown)";

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

    private void UpdateButtonStates()
    {
        var status = _currentOrder?.Status;
        var isOpen = status == "Open";
        var isHeld = status == "Held";
        var isDineIn = _currentOrder?.OrderType == "DineIn";
        var canReopen = status is "Voided" or "Cancelled";
        var hasOrder = _currentOrder is not null;

        _newDineInButton.Enabled = !hasOrder && Permit("create");
        _newTakeAwayButton.Enabled = !hasOrder && Permit("create");

        _holdButton.Enabled = isOpen && Permit("hold");
        _resumeButton.Enabled = isHeld && Permit("resume");
        _voidOrderButton.Enabled = (isOpen || isHeld) && Permit("void");
        _cancelOrderButton.Enabled = (isOpen || isHeld) && Permit("cancel");
        _reopenButton.Enabled = canReopen && Permit("reopen");
        _sendToKitchenButton.Enabled = isOpen && Permit("sendtokitchen");
        _completeButton.Enabled = isOpen && Permit("complete");

        // Enable or disable the payment entry controls (excluding payment methods):
        var canPay = isOpen && Permit("pay");
        pnlAmountTendered.Enabled = canPay;
        pnlKeypad.Enabled = canPay;
        pnlQuickCash.Enabled = canPay;
        _recordButton.Enabled = canPay;
        _splitPaymentButton.Enabled = canPay;

        _paymentHistoryButton.Enabled = hasOrder;
        _transferTableButton.Enabled = isOpen && isDineIn && Permit("transfertable");
        _mergeTablesButton.Enabled = isOpen && isDineIn && Permit("mergetables");
        _splitBillButton.Enabled = isOpen && isDineIn && Permit("splitbill");
        _orderNotesButton.Enabled = (isOpen || isHeld) && Permit("notes");
        _customerNotesButton.Enabled = (isOpen || isHeld) && Permit("notes");
        _addDiscountButton.Enabled = isOpen && Permit("discount");
        _removeDiscountButton.Enabled = isOpen && Permit("discount");
        _addServiceChargeButton.Enabled = isOpen && Permit("servicecharge");
        _removeServiceChargeButton.Enabled = isOpen && Permit("servicecharge");

        _addByBarcodeButton.Enabled = isOpen && Permit("additem");
        _decreaseQuantityButton.Enabled = isOpen && Permit("editline");
        _increaseQuantityButton.Enabled = isOpen && Permit("editline");
        _editQuantityButton.Enabled = isOpen && Permit("editline");
        _editLineNotesButton.Enabled = isOpen && Permit("editline");
        _voidLineButton.Enabled = isOpen && Permit("editline");
        _removeLineButton.Enabled = isOpen && Permit("editline");
        _overridePriceButton.Enabled = isOpen && Permit("priceoverride");
        // The in-place Price-cell edit uses the same "priceoverride" permission
        // as the button; without it the column stays read-only.
        _lineGridColumnUnitPrice.OptionsColumn.ReadOnly = !(isOpen && Permit("priceoverride"));
        // The in-place Qty-cell edit uses the same "editline" permission as
        // the +/- buttons and Edit Quantity dialog.
        _lineGridColumnQuantity.OptionsColumn.ReadOnly = !(isOpen && Permit("editline"));

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
        AppearanceManager.Apply(this, "Restaurant", "PaymentPanel");

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
            _paymentBalanceLabel.Text = "Balance Due: -";
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
        _paymentBalanceLabel.Text = $"Balance Due: {CurrencyDisplay.FormatPlain(_balance)}";
        _amountEdit.Text = FormatPlain(_balance);
        _amountEntryIsPreset = true;
        UpdateChangeDisplay();
    }

    private void BuildMethodButtons()
    {
        _methodButtonsFlow.Controls.Clear();

        foreach (var (id, name) in _paymentMethods)
        {
            var button = new SimpleButton { Text = name, AutoSize = true, Padding = new Padding(8, 4, 8, 4), MinimumSize = new Size(70, 32), Margin = new Padding(0, 0, 4, 4), Name = "PaymentMethodButton" };
            button.Tag = id;
            button.LookAndFeel.UseDefaultLookAndFeel = false;
            button.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Flat;
            button.Appearance.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            button.Appearance.Options.UseFont = true;
            button.Click += (_, _) => SelectPaymentMethod(id);
            _methodButtonsFlow.Controls.Add(button);
            AppearanceManager.Apply(button, "Restaurant", "PaymentPanel");
        }

        if (_selectedPaymentMethodId is null && _paymentMethods.Count > 0)
        {
            // Pre-select the configured default (the method the cashier last
            // used) when a new order starts; the cashier can still pick any
            // other method manually.
            var preferredId = _paymentMethods.FirstOrDefault(m => m.PaymentMethodId == PosPaymentMethodPreferenceStore.Load()).PaymentMethodId;
            if (preferredId == Guid.Empty)
            {
                preferredId = _paymentMethods[0].PaymentMethodId;
            }
            SelectPaymentMethod(preferredId);
        }
        else
        {
            UpdateMethodButtonSelection();
        }
    }

    private void SelectPaymentMethod(Guid paymentMethodId)
    {
        _selectedPaymentMethodId = paymentMethodId;
        PosPaymentMethodPreferenceStore.Save(paymentMethodId);
        UpdateMethodButtonSelection();
    }

    private void UpdateMethodButtonSelection()
    {
        foreach (SimpleButton button in _methodButtonsFlow.Controls)
        {
            if (button.Tag is not Guid methodId) continue;
            var isSelected = methodId == _selectedPaymentMethodId;
            var method = _paymentMethods.FirstOrDefault(m => m.PaymentMethodId == methodId);
            var baseName = method.Name ?? "Unknown";
            var baseColor = ResolveMethodColor(baseName);

            // Selection is carried by fill inversion + a leading glyph + weight, not by hue
            // alone, so it stays legible for colour-blind cashiers and on washed-out terminals.
            button.Text = isSelected ? $"✓ {baseName}" : baseName;
            button.Appearance.Font = new Font("Segoe UI", 8.5F, isSelected ? FontStyle.Bold : FontStyle.Regular);
            button.Appearance.Options.UseFont = true;

            switch (PosPaymentRules.ResolveButtonState(button.Enabled, isSelected))
            {
                case PaymentMethodButtonState.Unavailable:
                    button.Appearance.BackColor = UnavailableMethodFill;
                    button.Appearance.ForeColor = UnavailableMethodText;
                    button.Appearance.BorderColor = UnavailableMethodText;
                    break;
                case PaymentMethodButtonState.Selected:
                    button.Appearance.BackColor = baseColor;
                    button.Appearance.ForeColor = Color.White;
                    button.Appearance.BorderColor = SelectedMethodBorder;
                    break;
                default:
                    button.Appearance.BackColor = UnselectedMethodFill;
                    button.Appearance.ForeColor = baseColor;
                    button.Appearance.BorderColor = baseColor;
                    break;
            }

            // Simple in both states: a permanent outline reads as a persistent selection,
            // whereas toggling the border on click reads as a transient "pressed" flash.
            button.ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;
            button.Appearance.Options.UseBackColor = true;
            button.Appearance.Options.UseForeColor = true;
            button.Appearance.Options.UseBorderColor = true;
        }
    }

    private static Color ResolveMethodColor(string methodName)
    {
        var name = methodName.ToLowerInvariant();

        return name switch
        {
            _ when name.Contains("cash back") => Color.FromArgb(37, 99, 235),
            _ when name.Contains("cash") => Color.FromArgb(22, 163, 74),
            _ when name.Contains("credit") => Color.FromArgb(217, 119, 6),
            _ when name.Contains("debit") => Color.FromArgb(37, 99, 235),
            _ when name.Contains("card") => Color.FromArgb(37, 99, 235),
            _ when name.Contains("gift") || name.Contains("prepaid") => Color.FromArgb(217, 119, 6),
            _ when name.Contains("wallet") || name.Contains("online") || name.Contains("qr") => Color.FromArgb(124, 58, 237),
            _ => Color.FromArgb(71, 85, 105),
        };
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
