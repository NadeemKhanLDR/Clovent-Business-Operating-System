using Clovent.Authentication.Application.Credentials.Commands;
using Clovent.Desktop.Identity.Users;
using Clovent.Desktop.Navigation;
using Clovent.Desktop.Notifications;
using Clovent.Desktop.Sessions;
using Clovent.Desktop.Theming;
using DevExpress.XtraBars;
using MediatR;
using DevExpress.XtraBars.Navigation;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Repository;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Shell;

/// <summary>
/// The application's main window. Milestone 7 kept this deliberately bare;
/// Milestone 11 ("Desktop Shell") fills it in: a Ribbon (profile menu,
/// recent companies/branches, theme/language switching, notifications), a
/// permission-aware navigation pane, the swappable workspace
/// (<see cref="IWorkspaceHost"/>, unchanged since Milestone 7), and a
/// Ribbon status bar. Built entirely in code (no designer-generated partial
/// class) - see the original Milestone 7 doc comment on this type for why;
/// that reasoning is unchanged.
/// </summary>
public sealed class ShellForm : RibbonForm, IWorkspaceHost
{
    private readonly INavigationService _navigationService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICurrentSession _currentSession;
    private readonly INotificationService _notificationService;
    private readonly IRecentItemsService _recentItemsService;

    private readonly RibbonControl _ribbon = new();
    private readonly PanelControl _workspacePanel = new() { Dock = DockStyle.Fill };
    private readonly AccordionControl _navigationAccordion = new()
    {
        Dock = DockStyle.Left,
        Width = 420,
        // Explicit, small font rather than whatever the active DevExpress
        // skin/DPI context would otherwise hand the accordion - on this
        // environment the ambient font measured far larger than its
        // declared point size at runtime, which combined with this panel's
        // width wrapped every label one character per line.
        Font = new Font("Segoe UI", 9F),
    };

    /// <summary>
    /// Human-readable menu labels for each <see cref="INavigationService"/>
    /// key. Display-only - each element's <c>Tag</c> still carries the real
    /// key, so <see cref="INavigationService.NavigateTo"/> is unaffected.
    /// Without this, the accordion showed the raw
    /// lowercase-no-spaces route key itself (e.g. "warehousestocks"), which
    /// is both unreadable and, at the panel's width, wraps one character per
    /// line instead of by word.
    /// </summary>
    private static readonly Dictionary<string, string> MenuLabels = new()
    {
        ["dashboard"] = "Dashboard",
        ["users"] = "Users",
        ["roles"] = "Roles",
        ["organizations"] = "Organizations",
        ["companies"] = "Companies",
        ["branches"] = "Branches",
        ["departments"] = "Departments",
        ["warehouses"] = "Warehouses",
        ["terminals"] = "Terminals",
        ["fiscalyears"] = "Fiscal Years",
        ["currencies"] = "Currencies",
        ["businesssettings"] = "Business Settings",
        ["categories"] = "Categories",
        ["brands"] = "Brands",
        ["units"] = "Units of Measure",
        ["products"] = "Products",
        ["variants"] = "Product Variants",
        ["barcodes"] = "Barcodes",
        ["prices"] = "Prices",
        ["warehousestocks"] = "Warehouse Stocks",
        ["stockadjustments"] = "Stock Adjustments",
        ["stocktransfers"] = "Stock Transfers",
        ["inventorytransactions"] = "Inventory Transactions",
        ["diningareas"] = "Dining Areas",
        ["tables"] = "Tables",
        ["pos"] = "Restaurant POS",
        ["runningorders"] = "Running Orders",
        ["holdorders"] = "Hold Orders",
        ["kitchentickets"] = "Kitchen Tickets",
        ["endofday"] = "End of Day",
    };
    private readonly BarStaticItem _statusLabel = new() { Caption = "Ready" };
    private readonly BarStaticItem _userStatusItem = new();
    private readonly BarButtonItem _notificationsButton = new();
    private readonly BarSubItem _profileMenu = new();
    private readonly BarSubItem _recentCompaniesMenu = new() { Caption = "Recent Companies" };
    private readonly BarSubItem _recentBranchesMenu = new() { Caption = "Recent Branches" };

    /// <summary>Builds the Shell window.</summary>
    public ShellForm(
        IThemeService themeService,
        INavigationService navigationService,
        IServiceScopeFactory scopeFactory,
        ICurrentSession currentSession,
        INotificationService notificationService,
        IRecentItemsService recentItemsService)
    {
        _navigationService = navigationService;
        _scopeFactory = scopeFactory;
        _currentSession = currentSession;
        _notificationService = notificationService;
        _recentItemsService = recentItemsService;

        Text = "Clovent Business Operating System";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 1280;
        Height = 800;
        MinimumSize = new Size(1024, 640);

        Ribbon = _ribbon;
        StatusBar = new RibbonStatusBar(_ribbon);

        BuildRibbon(themeService);
        BuildStatusBar();
        BuildNavigation();
        BuildLayout();

        _navigationService.Navigated += (_, key) => SetStatus($"Viewing: {key}");
        _notificationService.Changed += (_, _) => RefreshNotificationsButton();
        RefreshNotificationsButton();
        RefreshRecentMenus();

        Shown += async (_, _) => await RefreshNavigationAsync();
    }

    private void BuildRibbon(IThemeService themeService)
    {
        _ribbon.ApplicationButtonText = "&Menu";

        var homePage = new RibbonPage("Home");
        _ribbon.Pages.Add(homePage);

        var accountGroup = new RibbonPageGroup("Account");
        homePage.Groups.Add(accountGroup);

        _profileMenu.Caption = _currentSession.DisplayName ?? "Account";
        var changePasswordItem = new BarButtonItem { Caption = "Change Password" };
        changePasswordItem.ItemClick += async (_, _) => await ChangePasswordAsync();
        var signOutItem = new BarButtonItem { Caption = "Sign Out" };
        signOutItem.ItemClick += (_, _) => SignOut();
        _ribbon.Items.Add(_profileMenu);
        _ribbon.Items.Add(changePasswordItem);
        _ribbon.Items.Add(signOutItem);
        _profileMenu.AddItem(changePasswordItem);
        _profileMenu.AddItem(signOutItem);
        accountGroup.ItemLinks.Add(_profileMenu);

        var recentGroup = new RibbonPageGroup("Recent");
        homePage.Groups.Add(recentGroup);
        _ribbon.Items.Add(_recentCompaniesMenu);
        _ribbon.Items.Add(_recentBranchesMenu);
        recentGroup.ItemLinks.Add(_recentCompaniesMenu);
        recentGroup.ItemLinks.Add(_recentBranchesMenu);

        var appearanceGroup = new RibbonPageGroup("Appearance");
        homePage.Groups.Add(appearanceGroup);

        var themeCombo = new RepositoryItemComboBox();
        themeCombo.Items.AddRange([.. themeService.AvailableSkins]);
        var themeEditItem = new BarEditItem { Caption = "Theme", Edit = themeCombo, EditValue = themeService.CurrentSkin };
        themeEditItem.EditValueChanged += (_, _) =>
        {
            if (themeEditItem.EditValue is string skinName)
            {
                themeService.ApplySkin(skinName);
            }
        };
        _ribbon.Items.Add(themeEditItem);
        appearanceGroup.ItemLinks.Add(themeEditItem);

        var languageCombo = new RepositoryItemComboBox();
        languageCombo.Items.AddRange(["English (United States)", "Español", "Français"]);
        var languageEditItem = new BarEditItem { Caption = "Language", Edit = languageCombo, EditValue = "English (United States)" };
        _ribbon.Items.Add(languageEditItem);
        appearanceGroup.ItemLinks.Add(languageEditItem);

        var notificationsGroup = new RibbonPageGroup("Notifications");
        homePage.Groups.Add(notificationsGroup);
        _notificationsButton.ItemClick += (_, _) => ShowNotifications();
        _ribbon.Items.Add(_notificationsButton);
        notificationsGroup.ItemLinks.Add(_notificationsButton);
    }

    private void BuildStatusBar()
    {
        _userStatusItem.Caption = _currentSession.DisplayName is { } name ? $"Signed in as {name}" : "Not signed in";
        StatusBar.ItemLinks.Add(_userStatusItem);
        StatusBar.ItemLinks.Add(_statusLabel);
    }

    private void BuildNavigation()
    {
        _navigationAccordion.ElementClick += (_, e) =>
        {
            if (e.Element.Tag is string key)
            {
                _navigationService.NavigateTo(key);
            }
        };
    }

    private void BuildLayout()
    {
        Controls.Add(_workspacePanel);
        Controls.Add(_navigationAccordion);
    }

    private async Task RefreshNavigationAsync()
    {
        _navigationAccordion.Elements.Clear();

        if (_currentSession.UserId is not { } userId)
        {
            return;
        }

        // NavigationMenuBuilder depends on IMenuAuthorizationPolicy, which is
        // Scoped (it ultimately needs a Scoped repository/DbContext) - this
        // long-lived Singleton form cannot hold one directly, so it creates
        // a fresh scope each time navigation is (re)built, the same pattern
        // Login/LoginService uses for its own per-call scope.
        using var scope = _scopeFactory.CreateScope();
        var navigationMenuBuilder = scope.ServiceProvider.GetRequiredService<NavigationMenuBuilder>();

        var visibleKeys = await navigationMenuBuilder.GetVisibleMenuKeysAsync(userId);
        foreach (var key in visibleKeys)
        {
            var label = MenuLabels.GetValueOrDefault(key, key);
            _navigationAccordion.Elements.Add(new AccordionControlElement
            {
                Text = label,
                Tag = key,
                // These are flat, non-expandable menu entries, not
                // collapsible groups - without this, every element defaults
                // to ElementStyle.Group (a group-header's larger font plus
                // an expand chevron), which is both misleading (nothing to
                // expand) and, combined with this panel's width, wraps text
                // one character per line instead of by word.
                Style = ElementStyle.Item,
            });
        }
    }

    private void RefreshNotificationsButton() =>
        _notificationsButton.Caption = $"Notifications ({_notificationService.Notifications.Count})";

    private void RefreshRecentMenus()
    {
        _recentCompaniesMenu.ItemLinks.Clear();
        foreach (var company in _recentItemsService.RecentCompanies)
        {
            var item = new BarButtonItem { Caption = company };
            _ribbon.Items.Add(item);
            _recentCompaniesMenu.AddItem(item);
        }

        _recentBranchesMenu.ItemLinks.Clear();
        foreach (var branch in _recentItemsService.RecentBranches)
        {
            var item = new BarButtonItem { Caption = branch };
            _ribbon.Items.Add(item);
            _recentBranchesMenu.AddItem(item);
        }
    }

    private void ShowNotifications()
    {
        using var form = new NotificationsForm(_notificationService.Notifications);
        form.ShowDialog(this);
    }

    private void SignOut()
    {
        _currentSession.SignOut();
        Close();
    }

    /// <summary>
    /// Self-service password change, reachable from the profile menu -
    /// distinct from the admin Reset Password action on
    /// <see cref="UserListView"/>, which skips the current-password check
    /// this dialog requires.
    /// </summary>
    private async Task ChangePasswordAsync()
    {
        if (_currentSession.UserId is not { } userId)
        {
            return;
        }

        using var form = new PasswordPromptForm("Change Password", requireCurrentPassword: true);
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        try
        {
            await mediator.Send(new ChangePasswordCommand(userId, form.CurrentPassword, form.NewPassword));
            XtraMessageBox.Show(this, "Password changed.", "Change Password", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show(this, ex.Message, "Change Password", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    /// <summary>Updates the status bar's message text.</summary>
    public void SetStatus(string text) => _statusLabel.Caption = text;

    /// <inheritdoc/>
    public void SetContent(Control content)
    {
        ArgumentNullException.ThrowIfNull(content);

        _workspacePanel.Controls.Clear();
        content.Dock = DockStyle.Fill;
        _workspacePanel.Controls.Add(content);
    }
}
