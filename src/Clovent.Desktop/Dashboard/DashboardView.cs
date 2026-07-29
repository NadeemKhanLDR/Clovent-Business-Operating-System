using Clovent.Authentication.LoginAttempts;
using Clovent.Authentication.Sessions;
using Clovent.Catalog.Application.Prices.Queries;
using Clovent.Catalog.Application.Products.Queries;
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
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Restaurant.Application.KitchenTickets.Queries;
using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.Application.OrderLines.Queries;
using Clovent.Restaurant.Application.Orders.Queries;
using Clovent.Restaurant.Application.Tables.Queries;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Dashboard;

/// <summary>
/// The "Dashboard shell" deliverable - the default view <see cref="Program"/>
/// navigates to immediately after showing <see cref="Shell.ShellForm"/>.
/// Every number/list shown is real data from what earlier milestones
/// actually built (the current user's own active sessions and recent login
/// attempts, the Shell's own notifications and recent-companies/branches).
/// Milestone 13 ("Organization &amp; Master Data Foundation") added the
/// Current Organization/Company/Branch/Fiscal Year/User tiles - since this
/// system has no tenant-switcher UI yet, "current" resolves to the first
/// organization in the system (in practice the one <c>DevelopmentMasterDataSeedStartupTask</c>
/// seeds) and its first company/branch, the same honest simplification
/// already applied to <c>IRecentItemsService</c>'s company/branch combos.
/// </summary>
public sealed class DashboardView : XtraUserControl
{
    private readonly IServiceScope _scope;
    private readonly ISessionRepository _sessionRepository;
    private readonly ILoginAttemptRepository _loginAttemptRepository;
    private readonly IMediator _mediator;
    private readonly ICurrentSession _currentSession;
    private readonly INotificationService _notificationService;
    private readonly IRecentItemsService _recentItemsService;

    private readonly LabelControl _activeSessionsValue = new();
    private readonly LabelControl _recentLoginsValue = new();
    private readonly LabelControl _notificationsValue = new();
    private readonly LabelControl _currentOrganizationValue = new();
    private readonly LabelControl _currentCompanyValue = new();
    private readonly LabelControl _currentBranchValue = new();
    private readonly LabelControl _currentFiscalYearValue = new();
    private readonly LabelControl _currentUserValue = new();
    private readonly LabelControl _totalProductsValue = new();
    private readonly LabelControl _lowStockValue = new();
    private readonly LabelControl _outOfStockValue = new();
    private readonly LabelControl _inventoryValueValue = new();
    private readonly LabelControl _todaysSalesValue = new();
    private readonly LabelControl _openTablesValue = new();
    private readonly LabelControl _runningOrdersValue = new();
    private readonly LabelControl _kitchenQueueValue = new();
    private readonly ListBoxControl _recentActivityList = new();
    private readonly ListBoxControl _notificationsList = new();
    private readonly ListBoxControl _recentStockMovementsList = new();
    private readonly ListBoxControl _topSellingItemsList = new();
    private readonly ComboBoxEdit _companyCombo = new();
    private readonly ComboBoxEdit _branchCombo = new();
    private readonly ProgressBarControl _loadingIndicator = new() { Visible = false };
    private readonly SimpleButton _refreshButton = new() { Text = "Refresh" };
    private readonly SimpleButton _viewNotificationsButton = new() { Text = "View All Notifications" };

    /// <summary>
    /// Shows each KPI/context card's full, untruncated text on hover - the
    /// safety net for <see cref="Trimming.EllipsisCharacter"/> captions and
    /// values that don't fit a card's width at narrower window sizes.
    /// </summary>
    private readonly ToolTip _cardToolTip = new();

    private const int KpiCardHeight = 150;

    /// <summary>Builds the dashboard and starts its own DI scope for the Scoped repositories it queries.</summary>
    public DashboardView(
        IServiceScopeFactory scopeFactory,
        ICurrentSession currentSession,
        INotificationService notificationService,
        IRecentItemsService recentItemsService)
    {
        _scope = scopeFactory.CreateScope();
        _sessionRepository = _scope.ServiceProvider.GetRequiredService<ISessionRepository>();
        _loginAttemptRepository = _scope.ServiceProvider.GetRequiredService<ILoginAttemptRepository>();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _currentSession = currentSession;
        _notificationService = notificationService;
        _recentItemsService = recentItemsService;

        BuildLayout();

        _refreshButton.Click += async (_, _) => await LoadAsync();
        _viewNotificationsButton.Click += (_, _) =>
        {
            using var form = new NotificationsForm(_notificationService.Notifications);
            form.ShowDialog(this);
        };
        _companyCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_companyCombo.SelectedItem is string company)
            {
                _recentItemsService.RecordCompanySelected(company);
            }
        };
        _branchCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_branchCombo.SelectedItem is string branch)
            {
                _recentItemsService.RecordBranchSelected(branch);
            }
        };

        Load += async (_, _) => await LoadAsync();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _scope.Dispose();
        }

        base.Dispose(disposing);
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

            _activeSessionsValue.Text = activeSessions.Count.ToString();
            _recentLoginsValue.Text = recentAttempts.Count.ToString();
            _notificationsValue.Text = _notificationService.Notifications.Count.ToString();

            PopulateList(_recentActivityList, [.. recentAttempts
                .OrderByDescending(a => a.OccurredAtUtc)
                .Select(a => $"{a.OccurredAtUtc:g}  -  {a.Outcome}")], "No recent activity.");

            PopulateList(_notificationsList, [.. _notificationService.Notifications
                .Select(n => $"{n.TimestampUtc:g}  -  {n.Title}: {n.Message}")], "No notifications yet.");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async Task LoadBusinessContextAsync()
    {
        _currentUserValue.Text = _currentSession.DisplayName ?? "Not signed in";

        var organizations = await _mediator.Send(new ListOrganizationsQuery());
        var organization = organizations.FirstOrDefault();
        if (organization is null)
        {
            _currentOrganizationValue.Text = "None configured";
            _currentCompanyValue.Text = "-";
            _currentBranchValue.Text = "-";
            _currentFiscalYearValue.Text = "-";
            return;
        }

        _currentOrganizationValue.Text = organization.Name;

        var companies = await _mediator.Send(new ListCompaniesByOrganizationQuery(organization.OrganizationId));
        var company = companies.FirstOrDefault();
        _currentCompanyValue.Text = company?.Name ?? "None configured";

        if (company is not null)
        {
            var branches = await _mediator.Send(new ListBranchesByCompanyQuery(company.CompanyId));
            _currentBranchValue.Text = branches.FirstOrDefault()?.Name ?? "None configured";
        }
        else
        {
            _currentBranchValue.Text = "-";
        }

        try
        {
            var settings = await _mediator.Send(new GetBusinessSettingsByOrganizationQuery(organization.OrganizationId));
            if (settings.DefaultFiscalYearId is { } fiscalYearId)
            {
                var fiscalYear = await _mediator.Send(new GetFiscalYearByIdQuery(fiscalYearId));
                _currentFiscalYearValue.Text = $"{fiscalYear.Name} ({fiscalYear.Status})";
            }
            else
            {
                _currentFiscalYearValue.Text = "Not set";
            }
        }
        catch (Clovent.MasterData.Application.NotFoundException)
        {
            _currentFiscalYearValue.Text = "Not configured";
        }
    }

    /// <summary>
    /// Loads the Milestone 14 ("Product Catalog &amp; Inventory Foundation")
    /// widgets: Total Products, Low Stock, Out of Stock, Inventory Value, and
    /// Recent Stock Movements. Inventory Value looks up each distinct
    /// variant's current cost price individually (there is no flat
    /// "list every price" query) - fine at this demo scale, not a design
    /// meant to scale to a catalog with thousands of variants.
    /// </summary>
    private async Task LoadCatalogInventoryContextAsync()
    {
        var products = await _mediator.Send(new ListProductsQuery());
        _totalProductsValue.Text = products.Count.ToString();

        var stocks = await _mediator.Send(new ListWarehouseStocksQuery());
        _lowStockValue.Text = CatalogDashboardCalculations.CountLowStock(stocks).ToString();
        _outOfStockValue.Text = CatalogDashboardCalculations.CountOutOfStock(stocks).ToString();

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
        _inventoryValueValue.Text = inventoryValue.ToString("N2");

        var recentTransactions = await _mediator.Send(new ListRecentInventoryTransactionsQuery(10));
        PopulateList(_recentStockMovementsList, [.. recentTransactions
            .OrderByDescending(t => t.OccurredAtUtc)
            .Select(t => $"{t.OccurredAtUtc:g}  -  {t.TransactionType} {t.Quantity}")], "No recent stock movements.");
    }

    /// <summary>
    /// Loads the Milestone 15 ("Restaurant POS Core") widgets: Today's Sales,
    /// Open Tables, Running Orders, Kitchen Queue, and Top Selling Items.
    /// Today's Sales and Top Selling Items both walk every completed order's
    /// lines/payments individually (there is no flat "sales report" query
    /// yet) - the same "fine at this demo scale" honest simplification
    /// <see cref="LoadCatalogInventoryContextAsync"/> already applies to
    /// Inventory Value.
    /// </summary>
    private async Task LoadRestaurantContextAsync()
    {
        var tables = await _mediator.Send(new ListAllTablesQuery());
        _openTablesValue.Text = RestaurantDashboardCalculations.CountOccupiedTables(tables).ToString();

        var openOrders = await _mediator.Send(new ListOpenOrdersQuery());
        _runningOrdersValue.Text = openOrders.Count.ToString();

        var activeTickets = await _mediator.Send(new ListActiveKitchenTicketsQuery());
        _kitchenQueueValue.Text = activeTickets.Count.ToString();

        var allOrders = await _mediator.Send(new ListAllOrdersQuery());
        var completedToday = RestaurantDashboardCalculations.FilterCompletedOn(allOrders, DateOnly.FromDateTime(DateTime.UtcNow));

        decimal todaysSales = 0m;
        var allLines = new List<OrderLineDto>();
        foreach (var order in completedToday)
        {
            var totals = await _mediator.Send(new GetOrderSummaryQuery(order.OrderId));
            todaysSales += totals.PaidTotal;

            var lines = await _mediator.Send(new ListOrderLinesByOrderQuery(order.OrderId));
            allLines.AddRange(lines);
        }
        _todaysSalesValue.Text = todaysSales.ToString("N2");

        var topSelling = RestaurantDashboardCalculations.TopSellingItems(allLines);
        var topSellingDisplay = new List<string>();
        foreach (var (variantId, quantity) in topSelling)
        {
            var variant = await _mediator.Send(new GetProductVariantByIdQuery(variantId));
            topSellingDisplay.Add($"{variant.Sku} {variant.Name}  -  {quantity:N2} sold");
        }
        PopulateList(_topSellingItemsList, [.. topSellingDisplay], "No sales completed today.");
    }

    private void PopulateSelectors()
    {
        PopulateCombo(_companyCombo, _recentItemsService.RecentCompanies, "No recent companies");
        PopulateCombo(_branchCombo, _recentItemsService.RecentBranches, "No recent branches");
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
        _loadingIndicator.Visible = isLoading;
        _refreshButton.Enabled = !isLoading;
    }

    private void BuildLayout()
    {
        Dock = DockStyle.Fill;
        AutoScroll = true;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildBusinessContextRow(), 0, 1);
        root.Controls.Add(BuildKpiCardsSection(), 0, 2);
        root.Controls.Add(BuildContentRow(), 0, 3);
        root.Controls.Add(BuildQuickActionsRow(), 0, 4);

        Controls.Add(root);
    }

    private Control BuildHeader()
    {
        var panel = new PanelControl
        {
            Dock = DockStyle.Top,
            Height = 64,
            BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder,
            Padding = new Padding(16, 0, 16, 0),
        };

        var title = new LabelControl { Text = "Dashboard", Dock = DockStyle.Left };
        title.Appearance.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
        title.Appearance.Options.UseFont = true;
        title.Appearance.TextOptions.VAlignment = VertAlignment.Center;
        title.Appearance.Options.UseTextOptions = true;

        _loadingIndicator.Dock = DockStyle.Right;
        _loadingIndicator.Width = 160;

        panel.Controls.Add(_loadingIndicator);
        panel.Controls.Add(title);
        return panel;
    }

    /// <summary>
    /// The "Top Section" - Current Organization/Company/Branch/Fiscal
    /// Year/User. A <see cref="FlowLayoutPanel"/> of auto-sized items rather
    /// than fixed-percentage columns, since these values (a company name, a
    /// fiscal year label) vary widely in length and a fixed-width card was
    /// what clipped them before; each item here simply takes the width its
    /// own text needs and wraps to a second line if the window is too
    /// narrow for all five.
    /// </summary>
    private Control BuildBusinessContextRow()
    {
        var strip = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            Padding = new Padding(16, 12, 16, 12),
        };

        strip.Controls.Add(BuildContextItem("Current Organization", _currentOrganizationValue));
        strip.Controls.Add(BuildContextItem("Current Company", _currentCompanyValue));
        strip.Controls.Add(BuildContextItem("Current Branch", _currentBranchValue));
        strip.Controls.Add(BuildContextItem("Current Fiscal Year", _currentFiscalYearValue));
        strip.Controls.Add(BuildContextItem("Current User", _currentUserValue));

        return strip;
    }

    private static Control BuildContextItem(string caption, LabelControl valueLabel)
    {
        var stack = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0, 0, 32, 8),
        };
        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var captionLabel = new LabelControl { Text = caption, Margin = new Padding(0, 0, 0, 2) };
        captionLabel.Appearance.Font = new Font("Segoe UI", 9F);
        captionLabel.Appearance.ForeColor = Color.Gray;
        captionLabel.Appearance.Options.UseFont = true;
        captionLabel.Appearance.Options.UseForeColor = true;

        valueLabel.Text = "-";
        valueLabel.Appearance.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        valueLabel.Appearance.Options.UseFont = true;

        stack.Controls.Add(captionLabel, 0, 0);
        stack.Controls.Add(valueLabel, 0, 1);
        return stack;
    }

    /// <summary>
    /// Every KPI tile (Sessions/Logins/Notifications, then Milestone 14's
    /// Catalog/Inventory tiles, then Milestone 15's Restaurant tiles) in one
    /// grid, 4 equal-width/equal-height cards per row, so the columns line
    /// up consistently down the page instead of three separately-sized
    /// 3/4/4-column rows.
    /// </summary>
    private Control BuildKpiCardsSection()
    {
        (string Caption, LabelControl Value)[] items =
        [
            ("Active Sessions", _activeSessionsValue),
            ("Logins (7 days)", _recentLoginsValue),
            ("Notifications", _notificationsValue),
            ("Total Products", _totalProductsValue),
            ("Low Stock", _lowStockValue),
            ("Out of Stock", _outOfStockValue),
            ("Inventory Value", _inventoryValueValue),
            ("Today's Sales", _todaysSalesValue),
            ("Open Tables", _openTablesValue),
            ("Running Orders", _runningOrdersValue),
            ("Kitchen Queue", _kitchenQueueValue),
        ];

        const int columns = 4;
        var rowCount = (int)Math.Ceiling(items.Length / (double)columns);

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = rowCount * KpiCardHeight,
            ColumnCount = columns,
            RowCount = rowCount,
        };
        for (var c = 0; c < columns; c++)
        {
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / columns));
        }
        for (var r = 0; r < rowCount; r++)
        {
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, KpiCardHeight));
        }

        for (var i = 0; i < items.Length; i++)
        {
            grid.Controls.Add(BuildStatCard(items[i].Caption, items[i].Value), i % columns, i / columns);
        }

        return grid;
    }

    private PanelControl BuildStatCard(string caption, LabelControl valueLabel)
    {
        var card = new PanelControl { Dock = DockStyle.Fill, Margin = new Padding(8), Padding = new Padding(16, 8, 16, 12) };

        valueLabel.Text = "0";
        valueLabel.Dock = DockStyle.Fill;
        valueLabel.Appearance.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
        valueLabel.Appearance.Options.UseFont = true;
        valueLabel.Appearance.TextOptions.HAlignment = HorzAlignment.Center;
        valueLabel.Appearance.TextOptions.VAlignment = VertAlignment.Center;
        valueLabel.Appearance.TextOptions.Trimming = Trimming.EllipsisCharacter;
        valueLabel.Appearance.Options.UseTextOptions = true;
        valueLabel.TextChanged += (_, _) => _cardToolTip.SetToolTip(valueLabel, valueLabel.Text);

        var captionLabel = new LabelControl { Text = caption, Dock = DockStyle.Top, Height = 22 };
        captionLabel.Appearance.Font = new Font("Segoe UI", 9.5F);
        captionLabel.Appearance.Options.UseFont = true;
        captionLabel.Appearance.TextOptions.HAlignment = HorzAlignment.Center;
        captionLabel.Appearance.TextOptions.Trimming = Trimming.EllipsisCharacter;
        captionLabel.Appearance.Options.UseTextOptions = true;
        _cardToolTip.SetToolTip(captionLabel, caption);

        card.Controls.Add(valueLabel);
        card.Controls.Add(captionLabel);
        return card;
    }

    private Control BuildContentRow()
    {
        var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1 };
        for (var i = 0; i < 4; i++)
        {
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        }

        row.Controls.Add(BuildListSection("Recent Activity", _recentActivityList), 0, 0);
        row.Controls.Add(BuildListSection("Notifications", _notificationsList), 1, 0);
        row.Controls.Add(BuildListSection("Recent Stock Movements", _recentStockMovementsList), 2, 0);
        row.Controls.Add(BuildListSection("Top Selling Items", _topSellingItemsList), 3, 0);

        return row;
    }

    private PanelControl BuildListSection(string caption, ListBoxControl list)
    {
        var panel = new PanelControl { Dock = DockStyle.Fill, Margin = new Padding(8), Padding = new Padding(4) };
        var captionLabel = new LabelControl { Text = caption, Dock = DockStyle.Top, Height = 24, Padding = new Padding(0, 0, 0, 8) };
        captionLabel.Appearance.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        captionLabel.Appearance.Options.UseFont = true;
        captionLabel.Appearance.TextOptions.Trimming = Trimming.EllipsisCharacter;
        captionLabel.Appearance.Options.UseTextOptions = true;
        _cardToolTip.SetToolTip(captionLabel, caption);

        list.Dock = DockStyle.Fill;

        panel.Controls.Add(list);
        panel.Controls.Add(captionLabel);
        return panel;
    }

    private void InitializeComponent()
    {

    }

    private Control BuildQuickActionsRow()
    {
        var panel = new PanelControl
        {
            Dock = DockStyle.Bottom,
            Height = 100,
            BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder,
            Padding = new Padding(16, 12, 16, 12),
        };

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var companyLabel = new LabelControl { Text = "Company", Dock = DockStyle.Bottom, Padding = new Padding(0, 0, 0, 4) };
        var branchLabel = new LabelControl { Text = "Branch", Dock = DockStyle.Bottom, Padding = new Padding(0, 0, 0, 4) };

        _companyCombo.Dock = DockStyle.Fill;
        _companyCombo.Margin = new Padding(0, 4, 16, 0);
        _branchCombo.Dock = DockStyle.Fill;
        _branchCombo.Margin = new Padding(0, 4, 16, 0);
        _refreshButton.Dock = DockStyle.Fill;
        _refreshButton.Margin = new Padding(0, 4, 8, 0);
        _viewNotificationsButton.Dock = DockStyle.Fill;
        _viewNotificationsButton.Margin = new Padding(0, 4, 0, 0);

        layout.Controls.Add(companyLabel, 0, 0);
        layout.Controls.Add(_companyCombo, 0, 1);
        layout.Controls.Add(branchLabel, 1, 0);
        layout.Controls.Add(_branchCombo, 1, 1);
        layout.Controls.Add(_refreshButton, 2, 1);
        layout.Controls.Add(_viewNotificationsButton, 3, 1);

        panel.Controls.Add(layout);
        return panel;
    }
}
