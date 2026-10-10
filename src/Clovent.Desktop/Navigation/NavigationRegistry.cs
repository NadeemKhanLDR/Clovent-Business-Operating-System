using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Clovent.Desktop.Catalog.Barcodes;
using Clovent.Desktop.Catalog.Brands;
using Clovent.Desktop.Catalog.Categories;
using Clovent.Desktop.Catalog.Prices;
using Clovent.Desktop.Catalog.UnitsOfMeasure;
using Clovent.Desktop.Catalog.Variants;
using Clovent.Desktop.Forms.Catalog.Products;
using Clovent.Desktop.Forms.Dashboard;
using Clovent.Desktop.Forms.Identity.Roles;
using Clovent.Desktop.Forms.Identity.Users;
using Clovent.Desktop.Forms.Restaurant.ActivityLog;
using Clovent.Desktop.Forms.Restaurant.Appearance;
using Clovent.Desktop.Forms.Restaurant.MenuItems;
using Clovent.Desktop.Forms.Restaurant.Setup;
using Clovent.Desktop.Inventory.Adjustments;
using Clovent.Desktop.Inventory.Transactions;
using Clovent.Desktop.Inventory.Transfers;
using Clovent.Desktop.Inventory.WarehouseStocks;
using Clovent.Desktop.MasterData.Branches;
using Clovent.Desktop.MasterData.Companies;
using Clovent.Desktop.MasterData.Currencies;
using Clovent.Desktop.MasterData.Departments;
using Clovent.Desktop.MasterData.FiscalYears;
using Clovent.Desktop.MasterData.Organizations;
using Clovent.Desktop.MasterData.Settings;
using Clovent.Desktop.MasterData.Terminals;
using Clovent.Desktop.MasterData.Warehouses;
using Clovent.Desktop.Restaurant.Customers;
using Clovent.Desktop.Restaurant.DiningAreas;
using Clovent.Desktop.Restaurant.EndOfDay;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.Desktop.Restaurant.Shifts;
using Clovent.Desktop.Restaurant.SmartPos;
using Clovent.Desktop.Restaurant.Tables;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Navigation;

/// <summary>
/// Central registry of navigation metadata for Clovent Business Operating System (CBOS).
/// Serves as the single authoritative source of truth for:
/// - Top-level Ribbon page and group taxonomy
/// - Button visual prominence (primary large vs. secondary compact)
/// - DevExpress SVG icon mappings
/// - Feature keys and RBAC permission codes
/// - Desktop view registrations
/// </summary>
public static class NavigationRegistry
{
    private static readonly List<NavigationItemDefinition> _items =
    [
        // ==========================================
        // 1. MASTERS PAGE
        // ==========================================
        new(
            Key: "dashboard",
            Caption: "Dashboard",
            RibbonPage: NavigationPage.Masters,
            RibbonGroup: "Workspace",
            IconUri: "svgimages/dashboards/chart.svg",
            Permission: "menu.dashboard",
            Order: 10,
            IsPrimaryAction: true,
            Description: "Operational KPI metrics and quick business pulse"),

        new(
            Key: "customers",
            Caption: "Customers",
            RibbonPage: NavigationPage.Masters,
            RibbonGroup: "Customer Masters",
            IconUri: "svgimages/business%20objects/bo_customer.svg",
            Permission: "menu.customers",
            Order: 100,
            IsPrimaryAction: true,
            Description: "Customer master records, contact info, credit limits"),

        new(
            Key: "categories",
            Caption: "Categories",
            RibbonPage: NavigationPage.Masters,
            RibbonGroup: "Menu Masters",
            IconUri: "svgimages/business%20objects/bo_category.svg",
            Permission: "menu.categories",
            Order: 110,
            IsPrimaryAction: false,
            Description: "Menu and product hierarchy groupings"),

        new(
            Key: "menuitems",
            Caption: "Menu Items",
            RibbonPage: NavigationPage.Masters,
            RibbonGroup: "Menu Masters",
            IconUri: "svgimages/business%20objects/bo_product.svg",
            Permission: "menu.menuitems",
            Order: 120,
            IsPrimaryAction: true,
            Description: "Restaurant dish catalogue, pricing, and stations"),

        new(
            Key: "products",
            Caption: "Products",
            RibbonPage: NavigationPage.Masters,
            RibbonGroup: "Product Catalog",
            IconUri: "svgimages/business%20objects/bo_product.svg",
            Permission: "menu.products",
            Order: 130,
            IsPrimaryAction: false,
            Description: "Master product catalog definitions"),

        new(
            Key: "variants",
            Caption: "Product Variants",
            RibbonPage: NavigationPage.Masters,
            RibbonGroup: "Product Catalog",
            IconUri: "svgimages/business%20objects/bo_product_group.svg",
            Permission: "menu.variants",
            Order: 140,
            IsPrimaryAction: false,
            Description: "Product size, color, and portion variants"),

        new(
            Key: "brands",
            Caption: "Brands",
            RibbonPage: NavigationPage.Masters,
            RibbonGroup: "Product Catalog",
            IconUri: "svgimages/icon%20builder/business_idea.svg",
            Permission: "menu.brands",
            Order: 150,
            IsPrimaryAction: false,
            Description: "Supplier and manufacturer brand references"),

        new(
            Key: "units",
            Caption: "Units of Measure",
            RibbonPage: NavigationPage.Masters,
            RibbonGroup: "Product Catalog",
            IconUri: "svgimages/setup/properties.svg",
            Permission: "menu.units",
            Order: 160,
            IsPrimaryAction: false,
            Description: "Measurement units and packaging conversions"),

        new(
            Key: "barcodes",
            Caption: "Barcodes",
            RibbonPage: NavigationPage.Masters,
            RibbonGroup: "Product Catalog",
            IconUri: "svgimages/business%20objects/bo_order.svg",
            Permission: "menu.barcodes",
            Order: 170,
            IsPrimaryAction: false,
            Description: "UPC / EAN / SKU barcode management"),

        new(
            Key: "prices",
            Caption: "Prices",
            RibbonPage: NavigationPage.Masters,
            RibbonGroup: "Product Catalog",
            IconUri: "svgimages/business%20objects/bo_sale.svg",
            Permission: "menu.prices",
            Order: 180,
            IsPrimaryAction: false,
            Description: "Price lists, schedules, and tiered pricing"),

        new(
            Key: "diningareas",
            Caption: "Dining Areas",
            RibbonPage: NavigationPage.Masters,
            RibbonGroup: "Dining Masters",
            IconUri: "svgimages/business%20objects/bo_organization.svg",
            Permission: "menu.diningareas",
            Order: 190,
            IsPrimaryAction: false,
            Description: "Restaurant seating zones, halls, terraces"),

        new(
            Key: "tables",
            Caption: "Tables",
            RibbonPage: NavigationPage.Masters,
            RibbonGroup: "Dining Masters",
            IconUri: "svgimages/business%20objects/bo_department.svg",
            Permission: "menu.tables",
            Order: 200,
            IsPrimaryAction: false,
            Description: "Dining table configurations and capacities"),

        new(
            Key: "organizations",
            Caption: "Organizations",
            RibbonPage: NavigationPage.Masters,
            RibbonGroup: "Organization Masters",
            IconUri: "svgimages/business%20objects/bo_organization.svg",
            Permission: "menu.organizations",
            Order: 210,
            IsPrimaryAction: false,
            Description: "Top-level corporate organization entities"),

        new(
            Key: "companies",
            Caption: "Companies",
            RibbonPage: NavigationPage.Masters,
            RibbonGroup: "Organization Masters",
            IconUri: "svgimages/business%20objects/bo_organization.svg",
            Permission: "menu.companies",
            Order: 220,
            IsPrimaryAction: false,
            Description: "Legal company entities within the organization"),

        new(
            Key: "branches",
            Caption: "Branches",
            RibbonPage: NavigationPage.Masters,
            RibbonGroup: "Organization Masters",
            IconUri: "svgimages/business%20objects/bo_department.svg",
            Permission: "menu.branches",
            Order: 230,
            IsPrimaryAction: false,
            Description: "Physical operating branches and outlets"),

        new(
            Key: "departments",
            Caption: "Departments",
            RibbonPage: NavigationPage.Masters,
            RibbonGroup: "Organization Masters",
            IconUri: "svgimages/business%20objects/bo_department.svg",
            Permission: "menu.departments",
            Order: 240,
            IsPrimaryAction: false,
            Description: "Functional business departments"),

        // ==========================================
        // 2. INVENTORY PAGE
        // ==========================================
        new(
            Key: "warehouses",
            Caption: "Warehouses",
            RibbonPage: NavigationPage.Inventory,
            RibbonGroup: "Warehousing",
            IconUri: "svgimages/business%20objects/bo_order.svg",
            Permission: "menu.warehouses",
            Order: 300,
            IsPrimaryAction: true,
            Description: "Storage facilities, central stores, and stock locations"),

        new(
            Key: "warehousestocks",
            Caption: "Stock On Hand",
            RibbonPage: NavigationPage.Inventory,
            RibbonGroup: "Stock",
            IconUri: "svgimages/business%20objects/bo_order.svg",
            Permission: "menu.warehousestocks",
            Order: 310,
            IsPrimaryAction: true,
            Description: "Current on-hand warehouse inventory balances"),

        new(
            Key: "stockadjustments",
            Caption: "Stock Adjustments",
            RibbonPage: NavigationPage.Inventory,
            RibbonGroup: "Movements",
            IconUri: "devav/actions/edit.svg",
            Permission: "menu.stockadjustments",
            Order: 320,
            IsPrimaryAction: false,
            Description: "Physical count discrepancies, damage, and adjustments"),

        new(
            Key: "stocktransfers",
            Caption: "Stock Transfers",
            RibbonPage: NavigationPage.Inventory,
            RibbonGroup: "Movements",
            IconUri: "svgimages/business%20objects/bo_order.svg",
            Permission: "menu.stocktransfers",
            Order: 330,
            IsPrimaryAction: false,
            Description: "Inter-warehouse stock transfer requisitions and dispatches"),

        new(
            Key: "inventorytransactions",
            Caption: "Inventory Movements",
            RibbonPage: NavigationPage.Inventory,
            RibbonGroup: "Movements",
            IconUri: "svgimages/business%20objects/bo_order.svg",
            Permission: "menu.inventorytransactions",
            Order: 340,
            IsPrimaryAction: false,
            Description: "Chronological audit trail of all inventory movement entries"),

        // ==========================================
        // 3. PURCHASES PAGE (Future module architecture - hidden until ready)
        // ==========================================

        // ==========================================
        // 4. POS PAGE
        // ==========================================
        new(
            Key: "pos",
            Caption: "Restaurant POS",
            RibbonPage: NavigationPage.Pos,
            RibbonGroup: "Operations",
            IconUri: "svgimages/business%20objects/bo_sale.svg",
            Permission: "menu.pos",
            Order: 500,
            IsPrimaryAction: true,
            Description: "High-performance front-counter POS and table ordering"),

        new(
            Key: "runningorders",
            Caption: "Running Orders",
            RibbonPage: NavigationPage.Pos,
            RibbonGroup: "Orders",
            IconUri: "svgimages/scheduling/time.svg",
            Permission: "menu.runningorders",
            Order: 510,
            IsPrimaryAction: false,
            Description: "Active open table orders and pending guest bills"),

        new(
            Key: "holdorders",
            Caption: "Held Orders",
            RibbonPage: NavigationPage.Pos,
            RibbonGroup: "Orders",
            IconUri: "devav/actions/close.svg",
            Permission: "menu.holdorders",
            Order: 520,
            IsPrimaryAction: false,
            Description: "Paused takeaway and drive-through orders awaiting recall"),

        new(
            Key: "orderhistory",
            Caption: "Order History",
            RibbonPage: NavigationPage.Pos,
            RibbonGroup: "Orders",
            IconUri: "svgimages/business%20objects/bo_report.svg",
            Permission: "menu.orderhistory",
            Order: 530,
            IsPrimaryAction: false,
            Description: "Historical sales transactions, receipts, and order lookup"),

        new(
            Key: "kitchentickets",
            Caption: "Kitchen Tickets",
            RibbonPage: NavigationPage.Pos,
            RibbonGroup: "Kitchen",
            IconUri: "svgimages/business%20objects/bo_order.svg",
            Permission: "menu.kitchentickets",
            Order: 540,
            IsPrimaryAction: false,
            Description: "KOT production status and kitchen preparation monitor"),

        // ==========================================
        // 5. MANAGER PANEL
        // ==========================================
        new(
            Key: "customerreceivables",
            Caption: "Customer Receivables",
            RibbonPage: NavigationPage.ManagerPanel,
            RibbonGroup: "Financial / A/R",
            IconUri: "svgimages/business%20objects/bo_sale.svg",
            Permission: "menu.customerreceivables",
            Order: 600,
            IsPrimaryAction: true,
            Description: "A/R aging, credit balances, payment collection, statements"),

        new(
            Key: "quickordertemplates",
            Caption: "Quick Order Templates",
            RibbonPage: NavigationPage.ManagerPanel,
            RibbonGroup: "Operations / Controls",
            IconUri: "devav/actions/add.svg",
            Permission: "menu.quickordertemplates",
            Order: 610,
            IsPrimaryAction: false,
            Description: "Front-counter quick order templates and fast keys"),

        new(
            Key: "smartcombos",
            Caption: "Smart Combo Builder",
            RibbonPage: NavigationPage.ManagerPanel,
            RibbonGroup: "Operations / Controls",
            IconUri: "svgimages/icon%20builder/business_idea.svg",
            Permission: "menu.smartcombos",
            Order: 620,
            IsPrimaryAction: false,
            Description: "Dynamic bundle deals, meal combos, and package pricing"),

        new(
            Key: "recommendationrules",
            Caption: "Recommendation Rules",
            RibbonPage: NavigationPage.ManagerPanel,
            RibbonGroup: "Operations / Controls",
            IconUri: "svgimages/setup/properties.svg",
            Permission: "menu.recommendationrules",
            Order: 630,
            IsPrimaryAction: false,
            Description: "Upsell, cross-sell, and basket opportunity rules"),

        new(
            Key: "branchsync",
            Caption: "Branch Sync",
            RibbonPage: NavigationPage.ManagerPanel,
            RibbonGroup: "Operations / Controls",
            IconUri: "svgimages/setup/properties.svg",
            Permission: "menu.branchsync",
            Order: 640,
            IsPrimaryAction: false,
            Description: "Multi-terminal replication status, fleet health, and manager conflict review"),

        new(
            Key: "quickbooks",
            Caption: "QuickBooks Sync",
            RibbonPage: NavigationPage.ManagerPanel,
            RibbonGroup: "Operations / Controls",
            IconUri: "svgimages/business%20objects/bo_order.svg",
            Permission: "menu.quickbooks",
            Order: 650,
            IsPrimaryAction: false,
            Description: "QuickBooks accounting synchronization, outbox reconciliation, and manager override"),

        new(
            Key: "printerhealth",
            Caption: "Printer Health",
            RibbonPage: NavigationPage.ManagerPanel,
            RibbonGroup: "Operations / Controls",
            IconUri: "svgimages/print/print.svg",
            Permission: "menu.printerhealth",
            Order: 660,
            IsPrimaryAction: false,
            Description: "Peripheral hardware health, live spooler queue depths, and quarantined receipt jobs recovery"),

        new(
            Key: "auditanalytics",
            Caption: "Audit Analytics",
            RibbonPage: NavigationPage.ManagerPanel,
            RibbonGroup: "Operations / Controls",
            IconUri: "svgimages/dashboards/chart.svg",
            Permission: "menu.audit.analytics",
            Order: 670,
            IsPrimaryAction: true,
            Description: "Cashier behavior anomaly detection, risk scoring, and predictive inventory burn rates"),

        // ==========================================
        // 6. USERS PAGE
        // ==========================================
        new(
            Key: "users",
            Caption: "Users",
            RibbonPage: NavigationPage.Users,
            RibbonGroup: "Security",
            IconUri: "svgimages/business%20objects/bo_user.svg",
            Permission: "menu.users",
            Order: 700,
            IsPrimaryAction: true,
            Description: "Employee logins, credentials, and branch access"),

        new(
            Key: "roles",
            Caption: "Roles",
            RibbonPage: NavigationPage.Users,
            RibbonGroup: "Security",
            IconUri: "svgimages/business%20objects/bo_role.svg",
            Permission: "menu.roles",
            Order: 710,
            IsPrimaryAction: false,
            Description: "Security roles, functional scopes, and privilege sets"),

        new(
            Key: "activitylog",
            Caption: "Activity Log",
            RibbonPage: NavigationPage.Users,
            RibbonGroup: "Audit",
            IconUri: "svgimages/business%20objects/bo_report.svg",
            Permission: "menu.activitylog",
            Order: 720,
            IsPrimaryAction: false,
            Description: "Security audit trail, overrides, logins, and system events"),

        // ==========================================
        // 7. REPORTS PAGE
        // ==========================================
        new(
            Key: "endofday",
            Caption: "Sales Summary",
            RibbonPage: NavigationPage.Reports,
            RibbonGroup: "Sales",
            IconUri: "svgimages/business%20objects/bo_report.svg",
            Permission: "menu.endofday",
            Order: 800,
            IsPrimaryAction: true,
            Description: "Comprehensive sales analytics, breakdown by bill/item/type"),

        new(
            Key: "shifts",
            Caption: "Shift History",
            RibbonPage: NavigationPage.Reports,
            RibbonGroup: "Operations",
            IconUri: "svgimages/scheduling/time.svg",
            Permission: "menu.shifts",
            Order: 810,
            IsPrimaryAction: false,
            Description: "Shift revenue and cash reconciliation report"),

        new(
            Key: "upsellperformance",
            Caption: "Upsell Performance",
            RibbonPage: NavigationPage.Reports,
            RibbonGroup: "Analytics",
            IconUri: "svgimages/dashboards/chart.svg",
            Permission: "menu.upsellperformance",
            Order: 820,
            IsPrimaryAction: false,
            Description: "Smart recommendation conversion rates and revenue uplift"),

        // ==========================================
        // 8. SETTINGS PAGE
        // ==========================================
        new(
            Key: "businesssettings",
            Caption: "Business Settings",
            RibbonPage: NavigationPage.Settings,
            RibbonGroup: "Business",
            IconUri: "svgimages/setup/properties.svg",
            Permission: "menu.businesssettings",
            Order: 900,
            IsPrimaryAction: true,
            Description: "Business profile, timezone, tax, and working hours"),

        new(
            Key: "fiscalyears",
            Caption: "Fiscal Years",
            RibbonPage: NavigationPage.Settings,
            RibbonGroup: "Financial",
            IconUri: "svgimages/scheduling/time.svg",
            Permission: "menu.fiscalyears",
            Order: 910,
            IsPrimaryAction: false,
            Description: "Accounting periods and fiscal year definitions"),

        new(
            Key: "currencies",
            Caption: "Currencies",
            RibbonPage: NavigationPage.Settings,
            RibbonGroup: "Financial",
            IconUri: "svgimages/business%20objects/bo_sale.svg",
            Permission: "menu.currencies",
            Order: 920,
            IsPrimaryAction: false,
            Description: "Operational currencies, exchange rates, and decimal formats"),

        new(
            Key: "restaurantsetup",
            Caption: "POS Setup",
            RibbonPage: NavigationPage.Settings,
            RibbonGroup: "POS Configuration",
            IconUri: "svgimages/setup/properties.svg",
            Permission: "menu.restaurantsetup",
            Order: 930,
            IsPrimaryAction: false,
            Description: "Dining modes, service charges, delivery parameters"),

        new(
            Key: "paymentmethods",
            Caption: "Payment Methods",
            RibbonPage: NavigationPage.Settings,
            RibbonGroup: "POS Configuration",
            IconUri: "svgimages/business%20objects/bo_sale.svg",
            Permission: "menu.paymentmethods",
            Order: 940,
            IsPrimaryAction: false,
            Description: "Cash, cards, mobile wallets, and receivable terms"),

        new(
            Key: "terminals",
            Caption: "Terminals",
            RibbonPage: NavigationPage.Settings,
            RibbonGroup: "Terminals",
            IconUri: "svgimages/setup/properties.svg",
            Permission: "menu.terminals",
            Order: 950,
            IsPrimaryAction: false,
            Description: "POS terminal machines, printers, and hardware mapping"),

        new(
            Key: "appearance",
            Caption: "Appearance",
            RibbonPage: NavigationPage.Settings,
            RibbonGroup: "Display",
            IconUri: "svgimages/setup/properties.svg",
            Permission: "menu.appearance",
            Order: 960,
            IsPrimaryAction: false,
            Description: "Look-and-feel themes, high-contrast skins, font sizing")
    ];

    /// <summary>All navigation item definitions configured in the system.</summary>
    public static IReadOnlyList<NavigationItemDefinition> AllItems => _items;

    /// <summary>Retrieves the DevExpress SVG image URI for a navigation key.</summary>
    public static string GetIconUri(string key)
    {
        var item = _items.FirstOrDefault(i => string.Equals(i.Key, key, StringComparison.OrdinalIgnoreCase));
        return item?.IconUri ?? "svgimages/business%20objects/bo_order.svg";
    }

    /// <summary>
    /// Gets the canonical primary <see cref="NavigationItemDefinition.RibbonPage"/> for a given navigation key.
    /// Non-shortcut items take precedence over shortcuts.
    /// </summary>
    public static string? GetCanonicalPageForKey(string key)
    {
        var canonical = _items.FirstOrDefault(i => string.Equals(i.Key, key, StringComparison.OrdinalIgnoreCase) && !i.IsShortcut);
        return canonical?.RibbonPage ?? _items.FirstOrDefault(i => string.Equals(i.Key, key, StringComparison.OrdinalIgnoreCase))?.RibbonPage;
    }

    /// <summary>
    /// Registers all 43 desktop view factories into <paramref name="navigationService"/>.
    /// Resolves each view transiently on navigation via <paramref name="serviceProvider"/>.
    /// </summary>
    public static void RegisterAllViews(INavigationService navigationService, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(navigationService);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        // Shell & Identity
        navigationService.Register("dashboard", () => serviceProvider.GetRequiredService<DashboardView>());
        navigationService.Register("users", () => serviceProvider.GetRequiredService<UsersForm>());
        navigationService.Register("roles", () => serviceProvider.GetRequiredService<RolesForm>());

        // Organization & Master Data
        navigationService.Register("organizations", () => serviceProvider.GetRequiredService<OrganizationManagementView>());
        navigationService.Register("companies", () => serviceProvider.GetRequiredService<CompanyManagementView>());
        navigationService.Register("branches", () => serviceProvider.GetRequiredService<BranchManagementView>());
        navigationService.Register("departments", () => serviceProvider.GetRequiredService<DepartmentManagementView>());
        navigationService.Register("warehouses", () => serviceProvider.GetRequiredService<WarehouseManagementView>());
        navigationService.Register("terminals", () => serviceProvider.GetRequiredService<TerminalManagementView>());
        navigationService.Register("fiscalyears", () => serviceProvider.GetRequiredService<FiscalYearManagementView>());
        navigationService.Register("currencies", () => serviceProvider.GetRequiredService<CurrencyManagementView>());
        navigationService.Register("businesssettings", () => serviceProvider.GetRequiredService<BusinessSettingsManagementView>());

        // Catalog & Inventory
        navigationService.Register("categories", () => serviceProvider.GetRequiredService<ProductCategoryManagementView>());
        navigationService.Register("brands", () => serviceProvider.GetRequiredService<BrandManagementView>());
        navigationService.Register("units", () => serviceProvider.GetRequiredService<UnitOfMeasureManagementView>());
        navigationService.Register("products", () => serviceProvider.GetRequiredService<ProductsForm>());
        navigationService.Register("variants", () => serviceProvider.GetRequiredService<ProductVariantManagementView>());
        navigationService.Register("barcodes", () => serviceProvider.GetRequiredService<BarcodeManagementView>());
        navigationService.Register("prices", () => serviceProvider.GetRequiredService<ProductPriceManagementView>());
        navigationService.Register("warehousestocks", () => serviceProvider.GetRequiredService<WarehouseStockManagementView>());
        navigationService.Register("stockadjustments", () => serviceProvider.GetRequiredService<StockAdjustmentManagementView>());
        navigationService.Register("stocktransfers", () => serviceProvider.GetRequiredService<StockTransferManagementView>());
        navigationService.Register("inventorytransactions", () => serviceProvider.GetRequiredService<InventoryTransactionsView>());

        // Dining & Restaurant POS
        navigationService.Register("diningareas", () => serviceProvider.GetRequiredService<DiningAreaManagementView>());
        navigationService.Register("tables", () => serviceProvider.GetRequiredService<TableManagementView>());
        navigationService.Register("menuitems", () => serviceProvider.GetRequiredService<MenuItemsForm>());
        navigationService.Register("pos", () => serviceProvider.GetRequiredService<RestaurantPosForm>());
        navigationService.Register("runningorders", () => serviceProvider.GetRequiredService<RunningOrdersView>());
        navigationService.Register("holdorders", () => serviceProvider.GetRequiredService<HoldOrdersView>());
        navigationService.Register("orderhistory", () => serviceProvider.GetRequiredService<OrderHistoryView>());
        navigationService.Register("kitchentickets", () => serviceProvider.GetRequiredService<KitchenTicketViewerView>());
        navigationService.Register("customers", () => serviceProvider.GetRequiredService<CustomersView>());
        navigationService.Register("customerreceivables", () => serviceProvider.GetRequiredService<CustomerReceivablesReportView>());

        // Smart POS & Analytics
        navigationService.Register("recommendationrules", () => serviceProvider.GetRequiredService<RecommendationRulesView>());
        navigationService.Register("smartcombos", () => serviceProvider.GetRequiredService<SmartComboBuilderView>());
        navigationService.Register("quickordertemplates", () => serviceProvider.GetRequiredService<QuickOrderTemplatesView>());
        navigationService.Register("upsellperformance", () => serviceProvider.GetRequiredService<UpsellPerformanceView>());

        // Closing, Control, Setup & Audit
        navigationService.Register("endofday", () => serviceProvider.GetRequiredService<EndOfDayReportView>());
        navigationService.Register("restaurantsetup", () => serviceProvider.GetRequiredService<RestaurantSetupView>());
        navigationService.Register("paymentmethods", () => serviceProvider.GetRequiredService<PaymentMethodsView>());
        navigationService.Register("activitylog", () => serviceProvider.GetRequiredService<ActivityLogView>());
        navigationService.Register("appearance", () => serviceProvider.GetRequiredService<AppearanceSettingsView>());
        navigationService.Register("shifts", () => serviceProvider.GetRequiredService<ShiftHistoryView>());
        navigationService.Register("branchsync", () => serviceProvider.GetRequiredService<Clovent.Desktop.Sync.BranchSyncStatusView>());
        navigationService.Register("quickbooks", () => serviceProvider.GetRequiredService<Clovent.Desktop.QuickBooks.QuickBooksSyncLogView>());
        navigationService.Register("printerhealth", () => serviceProvider.GetRequiredService<Clovent.Desktop.Printing.PrinterHardwareHealthView>());
        navigationService.Register("auditanalytics", () => serviceProvider.GetRequiredService<Clovent.Desktop.Restaurant.Audit.CashierAuditAnalyticsControl>());
    }
}
