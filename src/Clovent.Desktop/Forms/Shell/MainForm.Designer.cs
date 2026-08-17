using Clovent.Desktop.Theming;
using DevExpress.XtraBars;
using DevExpress.XtraBars.Docking2010;
using DevExpress.XtraBars.Docking2010.Views.Tabbed;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors.Repository;

namespace Clovent.Desktop.Forms.Shell;

/// <summary>
/// Visual structure of <see cref="MainForm"/>: the <see cref="RibbonControl"/>
/// (pages/groups/buttons), the non-MDI <see cref="DocumentManager"/>/
/// <see cref="TabbedView"/> document host, and the status bar. Behavior
/// (event handler implementations, navigation, sign-out, password change)
/// lives in <c>MainForm.cs</c> - see that file's class doc comment for the
/// full Control -> Event -> Handler -> Service story this split is meant to
/// make traceable.
/// </summary>
public sealed partial class MainForm
{
    /// <summary>
    /// The Ribbon's business-area navigation, one row per menu item: which
    /// <see cref="Navigation.INavigationService"/> key it opens, which
    /// <see cref="RibbonPage"/>/<see cref="RibbonPageGroup"/> it lives in,
    /// and its button caption. Declared as data rather than thirty
    /// hand-written field-plus-ItemClick-handler pairs - every button here
    /// shares one handler (<see cref="NavigationButtonItem_ItemClick"/>)
    /// that reads the key back off <see cref="BarItem.Tag"/>, so the
    /// control->event->handler->service chain is still one hop, not thirty
    /// near-identical ones. This table is Shell chrome (permission-gated
    /// per signed-in user by <c>RefreshNavigationAsync</c>), not a business
    /// screen, which is why it stays data-driven rather than one hand-authored
    /// field per button - see <c>docs/architecture/DesktopShellArchitecture.md</c>
    /// for why that's a deliberate, bounded exception to "no dynamic
    /// construction". Page/group creation order follows this array's order
    /// (first appearance of a page/group name wins).
    /// </summary>
    private static readonly (string Key, string Page, string Group, string Caption)[] NavigationItems =
    [
        ("dashboard", "Home", "Navigation", "Dashboard"),

        ("users", "Administration", "Security", "Users"),
        ("roles", "Administration", "Security", "Roles"),
        ("organizations", "Administration", "Organization", "Organizations"),
        ("companies", "Administration", "Organization", "Companies"),
        ("branches", "Administration", "Organization", "Branches"),
        ("departments", "Administration", "Organization", "Departments"),
        ("warehouses", "Administration", "Operations Setup", "Warehouses"),
        ("terminals", "Administration", "Operations Setup", "Terminals"),
        ("fiscalyears", "Administration", "Financial Setup", "Fiscal Years"),
        ("currencies", "Administration", "Financial Setup", "Currencies"),
        ("businesssettings", "Administration", "Configuration", "Business Settings"),

        ("categories", "Catalog", "Product Setup", "Categories"),
        ("brands", "Catalog", "Product Setup", "Brands"),
        ("units", "Catalog", "Product Setup", "Units of Measure"),
        ("products", "Catalog", "Products", "Products"),
        ("variants", "Catalog", "Products", "Product Variants"),
        ("barcodes", "Catalog", "Identification", "Barcodes"),
        ("prices", "Catalog", "Pricing", "Prices"),

        ("warehousestocks", "Inventory", "Stock", "Warehouse Stocks"),
        ("inventorytransactions", "Inventory", "Stock", "Inventory Transactions"),
        ("stockadjustments", "Inventory", "Operations", "Stock Adjustments"),
        ("stocktransfers", "Inventory", "Operations", "Stock Transfers"),

        ("menuitems", "Restaurant", "Menu", "Menu Items"),
        ("pos", "Restaurant", "POS", "Restaurant POS"),
        ("diningareas", "Restaurant", "Dining", "Dining Areas"),
        ("tables", "Restaurant", "Dining", "Tables"),
        ("customers", "Restaurant", "Customers", "Customers"),
        ("runningorders", "Restaurant", "Orders", "Running Orders"),
        ("holdorders", "Restaurant", "Orders", "Held Orders"),
        ("orderhistory", "Restaurant", "Orders", "Order History"),
        ("kitchentickets", "Restaurant", "Kitchen", "Kitchen Tickets"),
        ("endofday", "Restaurant", "Closing", "Sales Summary"),
        ("restaurantsetup", "Restaurant", "Setup", "Restaurant Setup"),
        ("paymentmethods", "Restaurant", "Setup", "Payment Methods"),
        ("activitylog", "Restaurant", "Closing", "Activity Log"),
        ("appearance", "Restaurant", "Setup", "Appearance"),
    ];

    /// <summary>Business-area pages that collapse themselves when the signed-in user has no visible item on any of their groups - never <c>Home</c>, which always carries session/appearance/notifications regardless of menu permissions.</summary>
    private static readonly string[] PermissionGatedPages = ["Administration", "Catalog", "Inventory", "Restaurant"];

    private readonly RibbonControl _ribbon = new();

    /// <summary>
    /// The non-MDI document host. <see cref="_tabbedView"/> is DevExpress's
    /// "Tabbed View" - deliberately not "MDI Tabbed View"/"Native MDI Tabbed
    /// View", the two options in the same toolbox family that do use a real
    /// MDI parent/child relationship under the hood. See
    /// <see cref="BuildDocumentHost"/> for how it's wired to this form
    /// without ever touching <see cref="DocumentManager.MdiParent"/>.
    /// </summary>
    private readonly DocumentManager _documentManager = new();

    private readonly TabbedView _tabbedView = new();

    private readonly BarStaticItem _statusLabel = new() { Caption = "Ready" };
    private readonly BarStaticItem _userStatusItem = new();
    private readonly BarButtonItem _notificationsButton = new();
    private readonly BarSubItem _profileMenu = new();
    private readonly BarSubItem _recentCompaniesMenu = new() { Caption = "Recent Companies" };
    private readonly BarSubItem _recentBranchesMenu = new() { Caption = "Recent Branches" };
    private readonly BarEditItem _themeEditItem = new();
    private DevExpress.XtraEditors.Repository.RepositoryItemComboBox themeCombo;

    private RibbonPage _homeRibbonPage = null!;
    private RibbonPage _administrationRibbonPage = null!;
    private RibbonPage _catalogRibbonPage = null!;
    private RibbonPage _inventoryRibbonPage = null!;
    private RibbonPage _restaurantRibbonPage = null!;

    /// <summary>Every navigation <see cref="BarButtonItem"/> built from <see cref="NavigationItems"/>, keyed by its navigation key - how <c>RefreshNavigationAsync</c> (in <c>MainForm.cs</c>) shows/hides commands per the signed-in user's menu permissions.</summary>
    private readonly Dictionary<string, BarButtonItem> _navigationButtonsByKey = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Ribbon page groups built from <see cref="NavigationItems"/>, keyed by <c>"{Page}{Group}"</c> - lets <c>RefreshNavigationAsync</c> collapse a group once every button in it is hidden.</summary>
    private readonly Dictionary<string, RibbonPageGroup> _navigationGroupsByKey = [];

    /// <summary>Builds the Ribbon, document host, and status bar. Called once from the constructor in <c>MainForm.cs</c>.</summary>
    private void InitializeComponent()
    {
        SuspendLayout();

        Ribbon = _ribbon;
        StatusBar = new RibbonStatusBar(_ribbon);

        _ribbon.ApplicationButtonText = "&Menu";

        BuildBusinessAreaPages();
        BuildHomePageExtras();
        BuildStatusBar();
        BuildDocumentHost();

        // Assigning the `Ribbon`/`StatusBar` RibbonForm properties does not
        // parent those controls by itself - without adding them to
        // Controls too, neither ever gets a Bounds/paints at all.
        Controls.Add(StatusBar);
        Controls.Add(_ribbon);

        ResumeLayout(false);
    }

    /// <summary>
    /// Builds the Administration/Catalog/Inventory/Restaurant pages (plus
    /// Home's own "Navigation" group, just the Dashboard button) entirely
    /// from <see cref="NavigationItems"/>: one <see cref="RibbonPage"/> per
    /// distinct <c>Page</c>, one <see cref="RibbonPageGroup"/> per distinct
    /// <c>Group</c> within it, one <see cref="BarButtonItem"/> per row - all
    /// three keyed off first appearance in the array, so this method's
    /// output order matches the table declared above.
    /// </summary>
    private void BuildBusinessAreaPages()
    {
        var pagesByName = new Dictionary<string, RibbonPage>();

        foreach (var (key, pageName, groupName, caption) in NavigationItems)
        {
            if (!pagesByName.TryGetValue(pageName, out var page))
            {
                page = new RibbonPage(pageName);
                _ribbon.Pages.Add(page);
                pagesByName[pageName] = page;
            }

            var groupKey = $"{pageName}{groupName}";
            if (!_navigationGroupsByKey.TryGetValue(groupKey, out var group))
            {
                group = new RibbonPageGroup(groupName);
                page.Groups.Add(group);
                _navigationGroupsByKey[groupKey] = group;
            }

            var button = new BarButtonItem { Caption = caption, Tag = key };
            button.ItemClick += NavigationButtonItem_ItemClick;
            _ribbon.Items.Add(button);
            group.ItemLinks.Add(button);
            _navigationButtonsByKey[key] = button;
        }

        _homeRibbonPage = pagesByName["Home"];
        _administrationRibbonPage = pagesByName["Administration"];
        _catalogRibbonPage = pagesByName["Catalog"];
        _inventoryRibbonPage = pagesByName["Inventory"];
        _restaurantRibbonPage = pagesByName["Restaurant"];
    }

    /// <summary>
    /// Adds Home's non-navigation groups: Session (profile menu - current
    /// user name plus Change Password/Sign Out), Recent (recent
    /// companies/branches), Appearance (theme/language), and Notifications.
    /// None of these are permission gated - they are always available to any
    /// signed-in user.
    /// </summary>
    private void BuildHomePageExtras()
    {
        var sessionGroup = new RibbonPageGroup("Session");
        _homeRibbonPage.Groups.Add(sessionGroup);

        _profileMenu.Caption = _currentSession?.DisplayName ?? "Account";
        var changePasswordItem = new BarButtonItem { Caption = "Change Password" };
        changePasswordItem.ItemClick += ChangePasswordItem_ItemClick;
        var signOutItem = new BarButtonItem { Caption = "Sign Out" };
        signOutItem.ItemClick += SignOutItem_ItemClick;
        _ribbon.Items.Add(_profileMenu);
        _ribbon.Items.Add(changePasswordItem);
        _ribbon.Items.Add(signOutItem);
        _profileMenu.AddItem(changePasswordItem);
        _profileMenu.AddItem(signOutItem);
        sessionGroup.ItemLinks.Add(_profileMenu);

        var recentGroup = new RibbonPageGroup("Recent");
        _homeRibbonPage.Groups.Add(recentGroup);
        _ribbon.Items.Add(_recentCompaniesMenu);
        _ribbon.Items.Add(_recentBranchesMenu);
        recentGroup.ItemLinks.Add(_recentCompaniesMenu);
        recentGroup.ItemLinks.Add(_recentBranchesMenu);

        var appearanceGroup = new RibbonPageGroup("Appearance");
        _homeRibbonPage.Groups.Add(appearanceGroup);

        themeCombo = new DevExpress.XtraEditors.Repository.RepositoryItemComboBox();
        themeCombo.Items.AddRange(new object[] { "Office 2019 Colorful", "Basic", "The Bezier" });
        _themeEditItem.Caption = "Theme";
        _themeEditItem.Edit = themeCombo;
        _themeEditItem.EditValue = "The Bezier";
        _themeEditItem.EditValueChanged += ThemeEditItem_EditValueChanged;
        _ribbon.Items.Add(_themeEditItem);
        appearanceGroup.ItemLinks.Add(_themeEditItem);

        var languageCombo = new RepositoryItemComboBox();
        languageCombo.Items.AddRange(["English (United States)", "Español", "Français"]);
        var languageEditItem = new BarEditItem { Caption = "Language", Edit = languageCombo, EditValue = "English (United States)" };
        _ribbon.Items.Add(languageEditItem);
        appearanceGroup.ItemLinks.Add(languageEditItem);

        var notificationsGroup = new RibbonPageGroup("Notifications");
        _homeRibbonPage.Groups.Add(notificationsGroup);
        _notificationsButton.ItemClick += NotificationsButtonItem_ItemClick;
        _ribbon.Items.Add(_notificationsButton);
        notificationsGroup.ItemLinks.Add(_notificationsButton);
    }

    private void BuildStatusBar()
    {
        _userStatusItem.Caption = _currentSession?.DisplayName is { } name ? $"Signed in as {name}" : "Not signed in";
        StatusBar.ItemLinks.Add(_userStatusItem);
        StatusBar.ItemLinks.Add(_statusLabel);
    }

    /// <summary>
    /// Wires the <see cref="DocumentManager"/> to this form via
    /// <see cref="DocumentManager.ContainerControl"/> - never
    /// <see cref="DocumentManager.MdiParent"/>, which this application never
    /// sets anywhere. This is the same "let the manager own the surface"
    /// relationship <see cref="RibbonControl"/> itself is built on; no
    /// separate <c>Controls.Add</c> is needed for the tab surface the way it
    /// is for <see cref="_ribbon"/>/<c>StatusBar</c>, because the
    /// <see cref="DocumentManager"/> renders directly into its
    /// <c>ContainerControl</c> once assigned. <see cref="DocumentManager.MenuManager"/>
    /// is set to <see cref="_ribbon"/> so a document could merge its own
    /// Ribbon commands in later if one ever needs to; nothing does yet.
    /// </summary>
    private void BuildDocumentHost()
    {
        _documentManager.ContainerControl = this;
        _documentManager.MenuManager = _ribbon;
        _documentManager.View = _tabbedView;
        _documentManager.ViewCollection.AddRange([_tabbedView]);
    }
}
