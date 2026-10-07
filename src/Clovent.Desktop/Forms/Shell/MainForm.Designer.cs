using Clovent.Desktop.Navigation;
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
/// <see cref="TabbedView"/> document host, and the status bar.
/// Uses centralized <see cref="NavigationRegistry"/> for all page, group, icon, and button definitions.
/// </summary>
public sealed partial class MainForm
{
    private readonly RibbonControl _ribbon = new();

    /// <summary>
    /// The non-MDI document host. <see cref="_tabbedView"/> is DevExpress's
    /// "Tabbed View" - deliberately not "MDI Tabbed View"/"Native MDI Tabbed
    /// View", the two options in the same toolbox family that do use a real
    /// MDI parent/child relationship under the hood.
    /// </summary>
    private readonly DocumentManager _documentManager = new();

    private readonly TabbedView _tabbedView = new();

    private readonly BarStaticItem _statusLabel = new() { Caption = "Ready" };
    private readonly BarStaticItem _userStatusItem = new();
    private readonly BarStaticItem _attendanceStatusItem = new();
    private readonly BarButtonItem _punchInOutButton = new();
    private readonly BarButtonItem _notificationsButton = new();
    private readonly BarSubItem _profileMenu = new();
    private readonly BarSubItem _recentCompaniesMenu = new() { Caption = "Recent Companies" };
    private readonly BarSubItem _recentBranchesMenu = new() { Caption = "Recent Branches" };
    private readonly BarEditItem _themeEditItem = new();
    private DevExpress.XtraEditors.Repository.RepositoryItemComboBox themeCombo = null!;

    private RibbonPage _mastersRibbonPage = null!;
    private RibbonPage _inventoryRibbonPage = null!;
    private RibbonPage _purchasesRibbonPage = null!;
    private RibbonPage _posRibbonPage = null!;
    private RibbonPage _managerPanelRibbonPage = null!;
    private RibbonPage _usersRibbonPage = null!;
    private RibbonPage _reportsRibbonPage = null!;
    private RibbonPage _settingsRibbonPage = null!;

    private readonly Dictionary<string, RibbonPage> _pagesByName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Every navigation <see cref="BarButtonItem"/> paired with its navigation key for permission gating.</summary>
    private readonly List<(string Key, BarButtonItem Button)> _allNavigationButtons = [];

    /// <summary>Ribbon page groups built from <see cref="NavigationRegistry"/>, keyed by <c>"{Page}_{Group}"</c>.</summary>
    private readonly Dictionary<string, RibbonPageGroup> _navigationGroupsByKey = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Builds the Ribbon, document host, and status bar. Called once from the constructor in <c>MainForm.cs</c>.</summary>
    private void InitializeComponent()
    {
        SuspendLayout();

        Ribbon = _ribbon;
        StatusBar = new RibbonStatusBar(_ribbon);

        Clovent.Desktop.Forms.Base.DesktopStyle.ApplyRibbonTypography(_ribbon, StatusBar);

        _ribbon.ShowApplicationButton = DevExpress.Utils.DefaultBoolean.False;
        _ribbon.ApplicationButtonText = string.Empty;

        BuildBusinessAreaPages();
        BuildShellExtras();
        BuildStatusBar();
        BuildDocumentHost();

        _ribbon.SelectedPage = _mastersRibbonPage;

        // Assigning the `Ribbon`/`StatusBar` RibbonForm properties does not
        // parent those controls by itself - without adding them to
        // Controls too, neither ever gets a Bounds/paints at all.
        Controls.Add(StatusBar);
        Controls.Add(_ribbon);

        ResumeLayout(false);
    }

    /// <summary>
    /// Builds the top-level RibbonPages in strictly ordered sequence:
    /// Masters, Inventory, POS, Manager Panel, Users, Reports, Settings.
    /// Purchases is hidden until its procurement module is ready.
    /// Groups and items are populated directly from <see cref="NavigationRegistry.AllItems"/>.
    /// </summary>
    private void BuildBusinessAreaPages()
    {
        _pagesByName.Clear();
        _allNavigationButtons.Clear();
        _navigationGroupsByKey.Clear();

        // Create the top-level RibbonPages in canonical order (skipping empty Purchases)
        foreach (var pageName in NavigationPage.OrderedPages)
        {
            if (string.Equals(pageName, NavigationPage.Purchases, StringComparison.OrdinalIgnoreCase))
            {
                continue; // Hidden until purchasing/procurement module is implemented
            }

            var page = new RibbonPage(pageName);
            _ribbon.Pages.Add(page);
            _pagesByName[pageName] = page;
        }

        // Add groups and buttons from NavigationRegistry
        foreach (var item in NavigationRegistry.AllItems)
        {
            if (!_pagesByName.TryGetValue(item.RibbonPage, out var page))
            {
                continue;
            }

            var groupKey = $"{item.RibbonPage}_{item.RibbonGroup}";
            if (!_navigationGroupsByKey.TryGetValue(groupKey, out var group))
            {
                group = new RibbonPageGroup(item.RibbonGroup)
                {
                    AllowTextClipping = false
                };
                page.Groups.Add(group);
                _navigationGroupsByKey[groupKey] = group;
            }

            var button = new BarButtonItem
            {
                Caption = item.Caption,
                Tag = item.Key,
                RibbonStyle = item.IsPrimaryAction ? RibbonItemStyles.Large : RibbonItemStyles.SmallWithText
            };

            var svg = DevExpress.Images.ImageResourceCache.Default.GetSvgImage(item.IconUri);
            if (svg != null)
            {
                button.ImageOptions.SvgImage = svg;
            }

            button.ItemClick += NavigationButtonItem_ItemClick;
            _ribbon.Items.Add(button);
            group.ItemLinks.Add(button);
            _allNavigationButtons.Add((item.Key, button));
        }

        _mastersRibbonPage = _pagesByName[NavigationPage.Masters];
        _inventoryRibbonPage = _pagesByName[NavigationPage.Inventory];
        _purchasesRibbonPage = _pagesByName.GetValueOrDefault(NavigationPage.Purchases)!;
        _posRibbonPage = _pagesByName[NavigationPage.Pos];
        _managerPanelRibbonPage = _pagesByName[NavigationPage.ManagerPanel];
        _usersRibbonPage = _pagesByName[NavigationPage.Users];
        _reportsRibbonPage = _pagesByName[NavigationPage.Reports];
        _settingsRibbonPage = _pagesByName[NavigationPage.Settings];
    }

    /// <summary>
    /// Adds Masters non-navigation groups: Session (profile menu with Change Password/Sign Out
    /// plus standalone dynamic Punch In/Out button), Recent, and Notifications.
    /// Adds Settings Appearance group (theme and language pickers).
    /// </summary>
    private void BuildShellExtras()
    {
        // 1. Session Group on Masters
        var sessionGroup = new RibbonPageGroup("Session");

        _profileMenu.Caption = _currentSession?.DisplayName ?? "Administrator";
        _profileMenu.ImageOptions.SvgImage = DevExpress.Images.ImageResourceCache.Default.GetSvgImage("svgimages/business%20objects/bo_user.svg");

        var changePasswordItem = new BarButtonItem { Caption = "Change Password" };
        changePasswordItem.ImageOptions.SvgImage = DevExpress.Images.ImageResourceCache.Default.GetSvgImage("svgimages/business%20objects/bo_role.svg");
        changePasswordItem.ItemClick += ChangePasswordItem_ItemClick;

        var signOutItem = new BarButtonItem { Caption = "Sign Out" };
        signOutItem.ImageOptions.SvgImage = DevExpress.Images.ImageResourceCache.Default.GetSvgImage("devav/actions/close.svg");
        signOutItem.ItemClick += SignOutItem_ItemClick;

        _punchInOutButton.Caption = "Punch In";
        _punchInOutButton.ImageOptions.SvgImage = DevExpress.Images.ImageResourceCache.Default.GetSvgImage("svgimages/scheduling/time.svg");
        _punchInOutButton.ItemClick += PunchInOutButton_ItemClick;

        _ribbon.Items.Add(_profileMenu);
        _ribbon.Items.Add(changePasswordItem);
        _ribbon.Items.Add(signOutItem);
        _ribbon.Items.Add(_punchInOutButton);

        _profileMenu.AddItem(changePasswordItem);
        _profileMenu.AddItem(signOutItem);

        sessionGroup.ItemLinks.Add(_profileMenu);
        sessionGroup.ItemLinks.Add(_punchInOutButton);

        // 2. Recent Group on Masters
        var recentGroup = new RibbonPageGroup("Recent");
        _recentCompaniesMenu.ImageOptions.SvgImage = DevExpress.Images.ImageResourceCache.Default.GetSvgImage("svgimages/business%20objects/bo_organization.svg");
        _recentBranchesMenu.ImageOptions.SvgImage = DevExpress.Images.ImageResourceCache.Default.GetSvgImage("svgimages/business%20objects/bo_department.svg");
        _ribbon.Items.Add(_recentCompaniesMenu);
        _ribbon.Items.Add(_recentBranchesMenu);
        recentGroup.ItemLinks.Add(_recentCompaniesMenu);
        recentGroup.ItemLinks.Add(_recentBranchesMenu);

        // 3. Notifications Group on Masters
        var notificationsGroup = new RibbonPageGroup("Notifications");
        _notificationsButton.ImageOptions.SvgImage = DevExpress.Images.ImageResourceCache.Default.GetSvgImage("svgimages/business%20objects/bo_report.svg");
        _notificationsButton.ItemClick += NotificationsButtonItem_ItemClick;
        _ribbon.Items.Add(_notificationsButton);
        notificationsGroup.ItemLinks.Add(_notificationsButton);

        // Insert Session, Recent, Notifications right after Workspace (index 0) in Masters
        int insertIdx = _mastersRibbonPage.Groups.Count > 0 ? 1 : 0;
        _mastersRibbonPage.Groups.Insert(insertIdx, sessionGroup);
        _mastersRibbonPage.Groups.Insert(insertIdx + 1, recentGroup);
        _mastersRibbonPage.Groups.Insert(insertIdx + 2, notificationsGroup);

        // 4. Appearance Group on Settings (Theme & Language)
        var appearanceGroup = new RibbonPageGroup("Appearance");

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

        _settingsRibbonPage.Groups.Add(appearanceGroup);

        // 5. System & Registration Group on Settings
        var systemGroup = new RibbonPageGroup("System & Registration");
        var dbSettingsButton = new BarButtonItem { Caption = "Database Settings" };
        var dbSvg = DevExpress.Images.ImageResourceCache.Default.GetSvgImage("svgimages/data/database.svg")
            ?? DevExpress.Images.ImageResourceCache.Default.GetSvgImage("svgimages/actions/properties.svg");
        if (dbSvg != null) dbSettingsButton.ImageOptions.SvgImage = dbSvg;
        dbSettingsButton.ItemClick += DatabaseSettingsButton_ItemClick;

        var licenseButton = new BarButtonItem { Caption = "Registration & License" };
        var licSvg = DevExpress.Images.ImageResourceCache.Default.GetSvgImage("svgimages/business%20objects/bo_security_permission.svg")
            ?? DevExpress.Images.ImageResourceCache.Default.GetSvgImage("svgimages/actions/about.svg");
        if (licSvg != null) licenseButton.ImageOptions.SvgImage = licSvg;
        licenseButton.ItemClick += LicenseButton_ItemClick;

        var diagnosticsButton = new BarButtonItem { Caption = "System Diagnostics" };
        var diagSvg = DevExpress.Images.ImageResourceCache.Default.GetSvgImage("svgimages/actions/support.svg")
            ?? DevExpress.Images.ImageResourceCache.Default.GetSvgImage("svgimages/actions/about.svg");
        if (diagSvg != null) diagnosticsButton.ImageOptions.SvgImage = diagSvg;
        diagnosticsButton.ItemClick += DiagnosticsButton_ItemClick;

        _ribbon.Items.Add(dbSettingsButton);
        _ribbon.Items.Add(licenseButton);
        _ribbon.Items.Add(diagnosticsButton);
        systemGroup.ItemLinks.Add(dbSettingsButton);
        systemGroup.ItemLinks.Add(licenseButton);
        systemGroup.ItemLinks.Add(diagnosticsButton);
        _settingsRibbonPage.Groups.Add(systemGroup);
    }

    private void BuildStatusBar()
    {
        _userStatusItem.Caption = _currentSession?.DisplayName is { } name ? $"Signed in as {name}" : "Not signed in";
        _attendanceStatusItem.Caption = "○ Not Punched In";
        _attendanceStatusItem.ItemClick += AttendanceStatusItem_ItemClick;
        StatusBar.ItemLinks.Add(_userStatusItem);
        StatusBar.ItemLinks.Add(_attendanceStatusItem);
        StatusBar.ItemLinks.Add(_statusLabel);
    }

    /// <summary>
    /// Wires the <see cref="DocumentManager"/> to this form via
    /// <see cref="DocumentManager.ContainerControl"/> - never
    /// <see cref="DocumentManager.MdiParent"/>, which this application never
    /// sets anywhere.
    /// </summary>
    private void BuildDocumentHost()
    {
        _documentManager.ContainerControl = this;
        _documentManager.MenuManager = _ribbon;
        _documentManager.View = _tabbedView;
        _documentManager.ViewCollection.AddRange([_tabbedView]);
    }
}
