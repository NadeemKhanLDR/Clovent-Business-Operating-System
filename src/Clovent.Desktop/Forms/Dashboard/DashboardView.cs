using Clovent.Authentication.LoginAttempts;
using Clovent.Authentication.Sessions;
using Clovent.Catalog.Application.Prices.Queries;
using Clovent.Catalog.Application.Products.Queries;
using Clovent.Desktop.Dashboard;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Navigation;
using Clovent.Desktop.Notifications;
using Clovent.Desktop.Sessions;
using Clovent.Desktop.Shell;
using Clovent.Identity.Application.Organizations.Queries;
using Clovent.Identity.Application.Companies.Queries;
using Clovent.Identity.Application.Branches.Queries;
using Clovent.Identity.Users;
using Clovent.Inventory.Application.Transactions.Queries;
using Clovent.Inventory.Application.WarehouseStocks.Queries;
using Clovent.MasterData.Application.FiscalYears.Queries;
using Clovent.MasterData.Application.Settings.Queries;
using Clovent.MasterData.Application.TimeZones.Queries;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Restaurant.Application.Customers.Queries;
using Clovent.Restaurant.Application.EndOfDay.Queries;
using Clovent.Restaurant.Application.KitchenTickets.Queries;
using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.Application.OrderLines.Queries;
using Clovent.Restaurant.Application.Orders.Queries;
using Clovent.Restaurant.Application.Tables.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Forms.Dashboard;

/// <summary>
/// The default view <see cref="Program"/> navigates to immediately after
/// showing <see cref="Shell.MainForm"/>. Every number/list shown is real
/// data (the current user's own active sessions and recent login attempts,
/// the Shell's own notifications and recent-companies/branches, and live
/// Catalog/Inventory/Restaurant query results) - never a hardcoded
/// placeholder. Since this system has no tenant-switcher UI, "current"
/// organization/company/branch resolves to the first one in the system (in
/// practice the one <c>DevelopmentMasterDataSeedStartupTask</c> seeds); see
/// <c>Forms/Shell/MainForm.cs</c>'s class doc comment for why the Ribbon's
/// own "Recent" group doesn't attempt the same resolution. The control tree
/// (every label/panel/card/list, its Location/Size, and every event
/// subscription) is declared in <c>DashboardView.Designer.cs</c>, exactly
/// as Visual Studio's WinForms Designer generates it - this file holds
/// behavior only: data loading and event handler bodies. Inherits
/// <see cref="BaseForm"/> - its own controls live inside
/// <see cref="BaseForm.ContentPanel"/>, added by
/// <c>DashboardView.Designer.cs</c>.
/// </summary>
/// <remarks>
/// <b>Visual Studio Designer compatibility.</b> The DI-facing constructor
/// used to create this control's <see cref="IServiceScope"/> and resolve
/// every Scoped repository/<see cref="IMediator"/> eagerly, in the
/// constructor - which needed a real, working <see cref="IServiceScopeFactory"/>
/// and, transitively, this application's whole composition root, neither of
/// which the Designer ever starts. That resolution now happens lazily, in
/// <see cref="EnsureServicesResolved"/>, called only from
/// <see cref="RefreshAsync"/> - so the parameterless constructor below can leave
/// <see cref="_scopeFactory"/> <see langword="null"/> and the Designer never
/// notices, since it never calls <c>RefreshAsync</c>.
/// </remarks>
public sealed partial class DashboardView : BaseForm
{
    private readonly IServiceScopeFactory? _scopeFactory;
    private IServiceScope? _scope;
    private ISessionRepository _sessionRepository = null!;
    private ILoginAttemptRepository _loginAttemptRepository = null!;
    private IMediator _mediator = null!;
    private readonly ICurrentSession _currentSession;
    private readonly INotificationService _notificationService;
    private readonly IRecentItemsService _recentItemsService;
    private readonly INavigationService? _navigationService;

    /// <summary>
    /// Builds the dashboard. The Scoped repositories/mediator it queries are
    /// resolved lazily, on first <see cref="RefreshAsync"/> (see
    /// <see cref="EnsureServicesResolved"/>) rather than here - see this
    /// class's remarks for why.
    /// </summary>
    public DashboardView(
        IServiceScopeFactory scopeFactory,
        ICurrentSession currentSession,
        INotificationService notificationService,
        IRecentItemsService recentItemsService,
        INavigationService? navigationService = null)
    {
        _scopeFactory = scopeFactory;
        _currentSession = currentSession;
        _notificationService = notificationService;
        _recentItemsService = recentItemsService;
        _navigationService = navigationService;

        InitializeComponent();
        WireDrilldowns();
    }

    /// <summary>
    /// Design-time-only constructor - required for the Visual Studio
    /// WinForms Designer to host this control: the Designer instantiates
    /// the type being designed via a public parameterless constructor and
    /// never starts this application's DI container, so it cannot supply
    /// the constructor above's dependencies. Never used at runtime: the
    /// built-in DI container (<c>services.TryAddTransient&lt;DashboardView&gt;()</c>)
    /// always prefers the constructor whose parameters can all be
    /// satisfied by registered services, which is strictly this
    /// overload's - it is never chosen outside the Designer.
    /// </summary>
    public DashboardView()
    {
        _currentSession = null!;
        _notificationService = null!;
        _recentItemsService = null!;

        InitializeComponent();
        WireDrilldowns();
    }

    private void WireDrilldowns()
    {
        pnlTotalReceivables.Cursor = Cursors.Hand;
        lblTotalReceivablesValue.Cursor = Cursors.Hand;
        lblTotalReceivablesCaption.Cursor = Cursors.Hand;
        void OpenReceivables(object? s, EventArgs e) => _navigationService?.NavigateTo("customerreceivables", "Customer Receivables");
        pnlTotalReceivables.Click += OpenReceivables;
        lblTotalReceivablesValue.Click += OpenReceivables;
        lblTotalReceivablesCaption.Click += OpenReceivables;

        pnlDeliveryOrders.Cursor = Cursors.Hand;
        lblDeliveryOrdersValue.Cursor = Cursors.Hand;
        lblDeliveryOrdersCaption.Cursor = Cursors.Hand;
        void OpenRunningOrders(object? s, EventArgs e) => _navigationService?.NavigateTo("runningorders", "Running Orders");
        pnlDeliveryOrders.Click += OpenRunningOrders;
        lblDeliveryOrdersValue.Click += OpenRunningOrders;
        lblDeliveryOrdersCaption.Click += OpenRunningOrders;

        lblBranchCaption.Cursor = Cursors.Hand;
        lblBranchValue.Cursor = Cursors.Hand;
        void OpenBranchSync(object? s, EventArgs e) => _navigationService?.NavigateTo("branchsync", "Branch Sync");
        lblBranchCaption.Click += OpenBranchSync;
        lblBranchValue.Click += OpenBranchSync;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            _scope?.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Resolves this view's Scoped repositories/mediator from a fresh DI
    /// scope, once - deferred from the constructor to here (see the class
    /// remarks) so the Designer never needs a working
    /// <see cref="IServiceScopeFactory"/> to host this control. Skipped
    /// entirely at design time: real data is not required for the Designer,
    /// only the static control tree <c>InitializeComponent</c> builds.
    /// </summary>
    private void EnsureServicesResolved()
    {
        if (_scope is not null)
        {
            return;
        }

        _scope = _scopeFactory!.CreateScope();
        _sessionRepository = _scope.ServiceProvider.GetRequiredService<ISessionRepository>();
        _loginAttemptRepository = _scope.ServiceProvider.GetRequiredService<ILoginAttemptRepository>();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
    }

    /// <summary>
    /// Loads the dashboard's data. Called once by <c>MainForm</c> right
    /// after this document is first created (replacing the old
    /// <c>Load += DashboardView_Load</c> event-handler wiring pre-Shell-rebuild
    /// - the Designer itself never calls this, so no <c>DesignMode</c> guard
    /// is necessary), and again on F5 or the Refresh button.
    /// </summary>
    public override async Task RefreshAsync()
    {
        EnsureServicesResolved();
        await LoadAsync();
    }

    private async void BtnRefresh_Click(object? sender, EventArgs e) => await RefreshAsync();

    private void BtnViewNotifications_Click(object? sender, EventArgs e)
    {
        using var form = new NotificationsForm(_notificationService.Notifications);
        form.ShowDialog(this);
    }

    private void CmbCompany_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (cmbCompany.SelectedItem is string company)
        {
            _recentItemsService.RecordCompanySelected(company);
        }
    }

    private void CmbBranch_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (cmbBranch.SelectedItem is string branch)
        {
            _recentItemsService.RecordBranchSelected(branch);
        }
    }

    private async Task LoadAsync()
    {
        SetLoading(true);
        try
        {
            PopulateSelectors();
            await LoadBusinessContextAsync();
            await LoadCatalogInventoryContextAsync();
            await LoadRestaurantContextAsync();

            if (_currentSession.UserId is not { } rawUserId)
            {
                return;
            }

            var userId = new UserId(rawUserId);
            var activeSessions = await _sessionRepository.GetActiveByUserIdAsync(userId);
            var recentAttempts = await _loginAttemptRepository.GetRecentByUserIdAsync(userId, DateTimeOffset.UtcNow.AddDays(-7));

            lblActiveSessionsValue.Text = activeSessions.Count.ToString();
            lblRecentLoginsValue.Text = recentAttempts.Count.ToString();
            lblNotificationsCountValue.Text = _notificationService.Notifications.Count.ToString();

            PopulateList(lstRecentActivity, [.. recentAttempts
                .OrderByDescending(a => a.OccurredAtUtc)
                .Select(a => $"{a.OccurredAtUtc:g}  -  {a.Outcome}")], "No recent activity.");

            PopulateList(lstNotifications, [.. _notificationService.Notifications
                .Select(n => $"{n.TimestampUtc:g}  -  {n.Title}: {n.Message}")], "No notifications yet.");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async Task LoadBusinessContextAsync()
    {
        lblCurrentUserValue.Text = _currentSession.DisplayName ?? "Not signed in";

        var organizations = await _mediator.Send(new ListOrganizationsQuery());
        var organization = organizations.FirstOrDefault();
        if (organization is null)
        {
            lblOrganizationValue.Text = "None configured";
            lblCompanyValue.Text = "-";
            lblBranchValue.Text = "-";
            lblFiscalYearValue.Text = "-";
            return;
        }

        lblOrganizationValue.Text = organization.Name;

        var companies = await _mediator.Send(new ListCompaniesByOrganizationQuery(organization.OrganizationId));
        var company = companies.FirstOrDefault();
        lblCompanyValue.Text = company?.Name ?? "None configured";

        if (company is not null)
        {
            var branches = await _mediator.Send(new ListBranchesByCompanyQuery(company.CompanyId));
            lblBranchValue.Text = branches.FirstOrDefault()?.Name ?? "None configured";
        }
        else
        {
            lblBranchValue.Text = "-";
        }

        try
        {
            var settings = await _mediator.Send(new GetBusinessSettingsByOrganizationQuery(organization.OrganizationId));
            if (settings.DefaultFiscalYearId is { } fiscalYearId)
            {
                var fiscalYear = await _mediator.Send(new GetFiscalYearByIdQuery(fiscalYearId));
                lblFiscalYearValue.Text = $"{fiscalYear.Name} ({fiscalYear.Status})";
            }
            else
            {
                lblFiscalYearValue.Text = "Not set";
            }
        }
        catch (Clovent.MasterData.Application.NotFoundException)
        {
            lblFiscalYearValue.Text = "Not configured";
        }
    }

    /// <summary>
    /// Loads the Total Products, Low Stock, Out of Stock, Inventory Value,
    /// and Recent Stock Movements widgets. Inventory Value looks up each
    /// distinct variant's current cost price individually (there is no flat
    /// "list every price" query) - fine at this demo scale, not a design
    /// meant to scale to a catalog with thousands of variants.
    /// </summary>
    private async Task LoadCatalogInventoryContextAsync()
    {
        var products = await _mediator.Send(new ListProductsQuery());
        lblTotalProductsValue.Text = products.Count.ToString();

        var stocks = await _mediator.Send(new ListWarehouseStocksQuery());
        lblLowStockValue.Text = CatalogDashboardCalculations.CountLowStock(stocks).ToString();
        lblOutOfStockValue.Text = CatalogDashboardCalculations.CountOutOfStock(stocks).ToString();

        var costsByVariantId = new Dictionary<Guid, decimal>();
        foreach (var variantId in stocks.Select(s => s.ProductVariantId).Distinct())
        {
            var prices = await _mediator.Send(new ListProductPricesByVariantQuery(variantId));
            var costPrice = prices
                .Where(p => p.Status == "Active" && p.PriceType == "Cost")
                .OrderByDescending(p => p.EffectiveFromUtc)
                .FirstOrDefault();
            costsByVariantId[variantId] = costPrice?.Amount ?? 0m;
        }

        var inventoryValue = CatalogDashboardCalculations.CalculateInventoryValue(stocks, id => costsByVariantId.GetValueOrDefault(id));
        lblInventoryValueValue.Text = inventoryValue.ToString("N" + CurrencyDisplay.DecimalPlaces);

        var recentTransactions = await _mediator.Send(new ListRecentInventoryTransactionsQuery(10));
        PopulateList(lstStockMovements, [.. recentTransactions
            .OrderByDescending(t => t.OccurredAtUtc)
            .Select(t => $"{t.OccurredAtUtc:g}  -  {t.TransactionType} {t.Quantity}")], "No recent stock movements.");
    }

    /// <summary>
    /// Loads the restaurant widgets: Open Tables, Running Orders, Kitchen Queue,
    /// Delivery Orders, Today's Sales, Top Selling Items, Customer Receivables,
    /// Customer Advances, and Today's Collections.
    /// </summary>
    private async Task LoadRestaurantContextAsync()
    {
        // Determine business date from organization default timezone
        var businessToday = DateOnly.FromDateTime(DateTime.UtcNow);
        try
        {
            var organizations = await _mediator.Send(new ListOrganizationsQuery());
            var org = organizations.FirstOrDefault();
            if (org is not null)
            {
                var settings = await _mediator.Send(new GetBusinessSettingsByOrganizationQuery(org.OrganizationId));
                var tz = await _mediator.Send(new GetTimeZoneEntryByIdQuery(settings.DefaultTimeZoneId));
                var offset = TimeSpan.FromMinutes(tz.UtcOffsetMinutes);
                businessToday = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(offset).DateTime);
            }
        }
        catch
        {
            businessToday = DateOnly.FromDateTime(DateTime.UtcNow);
        }

        var tables = await _mediator.Send(new ListAllTablesQuery());
        lblOpenTablesValue.Text = RestaurantDashboardCalculations.CountOccupiedTables(tables).ToString();

        var openOrders = await _mediator.Send(new ListOpenOrdersQuery());
        lblRunningOrdersValue.Text = openOrders.Count.ToString();

        var activeTickets = await _mediator.Send(new ListActiveKitchenTicketsQuery());
        lblKitchenQueueValue.Text = activeTickets.Count.ToString();

        var allOrders = await _mediator.Send(new ListAllOrdersQuery());
        var completedToday = RestaurantDashboardCalculations.FilterCompletedOn(allOrders, businessToday);

        // Delivery orders placed today (business date)
        var deliveryCount = RestaurantDashboardCalculations.CountDeliveryOrdersOn(allOrders, businessToday);
        lblDeliveryOrdersValue.Text = deliveryCount.ToString();

        decimal todaysSales = 0m;
        var allLines = new List<OrderLineDto>();
        foreach (var order in completedToday)
        {
            var totals = await _mediator.Send(new GetOrderSummaryQuery(order.OrderId));
            todaysSales += totals.PaidTotal;

            var lines = await _mediator.Send(new ListOrderLinesByOrderQuery(order.OrderId));
            allLines.AddRange(lines);
        }
        lblTodaysSalesValue.Text = todaysSales.ToString("N" + CurrencyDisplay.DecimalPlaces);

        var topSelling = RestaurantDashboardCalculations.TopSellingItems(allLines);
        var topSellingDisplay = new List<string>();
        foreach (var (variantId, quantity) in topSelling)
        {
            var variant = await _mediator.Send(new GetProductVariantByIdQuery(variantId));
            topSellingDisplay.Add($"{variant.Sku} {variant.Name}  -  {quantity:N2} sold");
        }
        PopulateList(lstTopSellingItems, [.. topSellingDisplay], "No sales completed today.");

        // Customer Receivables & Advances metrics
        try
        {
            var asOfDate = businessToday.ToDateTime(TimeOnly.MinValue);
            var receivablesReport = await _mediator.Send(new GetCustomerReceivablesReportQuery(asOfDate));
            lblTotalReceivablesValue.Text = receivablesReport.TotalReceivables.ToString("N" + CurrencyDisplay.DecimalPlaces);
            lblCustomersWithBalanceValue.Text = receivablesReport.ActiveAccountsWithBalanceCount.ToString();
            lblCustomerAdvancesValue.Text = receivablesReport.TotalAdvances.ToString("N" + CurrencyDisplay.DecimalPlaces);
            lblNetReceivablesValue.Text = receivablesReport.NetPosition.ToString("N" + CurrencyDisplay.DecimalPlaces);

            var warehouses = await _mediator.Send(new Clovent.MasterData.Application.Warehouses.Queries.ListAllWarehousesQuery());
            var warehouse = warehouses.FirstOrDefault();
            if (warehouse is not null)
            {
                var summary = await _mediator.Send(new GetExpandedSalesSummaryQuery(warehouse.WarehouseId, businessToday, businessToday));
                lblTodaysOnAccountValue.Text = summary.Kpis.OnAccountCreated.ToString("N" + CurrencyDisplay.DecimalPlaces);
                lblTodaysCollectionsValue.Text = summary.Kpis.CustomerPaymentsCollected.ToString("N" + CurrencyDisplay.DecimalPlaces);
            }
        }
        catch
        {
            lblTotalReceivablesValue.Text = "0.00";
            lblCustomersWithBalanceValue.Text = "0";
            lblCustomerAdvancesValue.Text = "0.00";
            lblTodaysOnAccountValue.Text = "0.00";
            lblTodaysCollectionsValue.Text = "0.00";
            lblNetReceivablesValue.Text = "0.00";
        }
    }

    private void PopulateSelectors()
    {
        PopulateCombo(cmbCompany, _recentItemsService.RecentCompanies, "No recent companies");
        PopulateCombo(cmbBranch, _recentItemsService.RecentBranches, "No recent branches");
    }

    private static void PopulateCombo(ComboBoxEdit combo, IReadOnlyList<string> items, string emptyText)
    {
        combo.Properties.Items.Clear();
        if (items.Count == 0)
        {
            combo.Properties.Items.Add(emptyText);
            combo.Enabled = false;
        }
        else
        {
            combo.Properties.Items.AddRange([.. items]);
            combo.Enabled = true;
        }

        combo.SelectedIndex = 0;
    }

    private static void PopulateList(ListBoxControl list, string[] items, string emptyText)
    {
        list.Items.Clear();
        list.Items.AddRange(items.Length == 0 ? [emptyText] : items);
    }

    private void SetLoading(bool isLoading)
    {
        prgLoading.Visible = isLoading;
        btnRefresh.Enabled = !isLoading;
    }
}
