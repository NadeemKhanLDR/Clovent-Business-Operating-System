using Clovent.Authentication.Application.Credentials.Commands;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Identity;
using Clovent.Desktop.Identity.Users;
using Clovent.Desktop.Navigation;
using Clovent.Desktop.Notifications;
using Clovent.Desktop.Sessions;
using Clovent.Desktop.Shell;
using Clovent.Desktop.Theming;
using Clovent.Restaurant.Application.ActivityLogs.Commands;
using DevExpress.XtraBars;
using DevExpress.XtraBars.Docking2010.Views;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Forms.Shell;

/// <summary>
/// The application's main window: a professional DevExpress
/// <see cref="RibbonControl"/> + non-MDI <see cref="DevExpress.XtraBars.Docking2010.DocumentManager"/>/
/// <see cref="DevExpress.XtraBars.Docking2010.Views.Tabbed.TabbedView"/>
/// shell (see <c>MainForm.Designer.cs</c> for the Ribbon's page/group/button
/// structure and how the document host is wired up). Every screen opens as a
/// document tab - multiple can stay open at once, and re-navigating to an
/// already-open key activates its existing tab instead of rebuilding it (see
/// <see cref="ShowDocument"/>). Every navigation command routes through one
/// handler, <see cref="NavigationButtonItem_ItemClick"/>, which reads its
/// destination key off <see cref="BarItem.Tag"/> and calls
/// <see cref="INavigationService.NavigateTo"/> - the Control -> Event ->
/// Handler -> Service chain documented in
/// <c>docs/architecture/DesktopShellArchitecture.md</c>. Authorization is
/// unchanged from before this rebuild: <see cref="NavigationMenuBuilder"/>
/// still filters <see cref="INavigationService.RegisteredKeys"/> through
/// <c>IMenuAuthorizationPolicy</c>; this class only decides how to *display*
/// the result (show/hide Ribbon buttons and collapse empty groups/pages).
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class MainForm : RibbonForm, IWorkspaceHost
{
    private readonly INavigationService _navigationService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICurrentSession _currentSession;
    private readonly INotificationService _notificationService;
    private readonly IRecentItemsService _recentItemsService;
    private readonly IThemeService _themeService;

    /// <summary>Every open document, keyed by its navigation key - how <see cref="ShowDocument"/> decides "activate the existing tab" vs. "build a new one".</summary>
    private readonly Dictionary<string, BaseDocument> _openDocumentsByKey = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The active document's <see cref="BaseForm"/>, if any - subscribed to <see cref="BaseForm.StatusTextChanged"/> so the status bar tracks whichever tab is active.</summary>
    private BaseForm? _activeStatusSource;
    /// <summary>Design-time-only constructor for the Visual Studio WinForms Designer - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public MainForm()
    {
        _navigationService = null!;
        _scopeFactory = null!;
        _currentSession = null!;
        _notificationService = null!;
        _recentItemsService = null!;
        _themeService = null!;

        InitializeComponent();
    }

    /// <summary>Builds the Shell window.</summary>
    public MainForm(
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
        _themeService = themeService;

        Text = "Clovent Business Operating System";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 1366;
        Height = 800;
        MinimumSize = new Size(1024, 640);

        InitializeComponent();

        if (DesignModeHelper.IsInDesignMode)
            return;

        InitializeRuntime();
    }

    private void InitializeRuntime()
    {
        _tabbedView.DocumentClosing += TabbedView_DocumentClosing;
        _tabbedView.DocumentClosed += TabbedView_DocumentClosed;
        _tabbedView.DocumentActivated += TabbedView_DocumentActivated;

        _notificationService.Changed += NotificationService_Changed;
        RefreshNotificationsButton();
        RefreshRecentMenus();

        if (_themeService is not null)
        {
            themeCombo.Items.Clear();
            foreach (var skin in _themeService.AvailableSkins)
            {
                themeCombo.Items.Add(skin);
            }
            _themeEditItem.EditValue = _themeService.CurrentSkin;
        }

        Shown += ShellForm_Shown;
    }

    private async void ShellForm_Shown(object? sender, EventArgs e)
    {
        if (DesignModeHelper.IsInDesignMode)
            return;
        await RefreshNavigationAsync();
    }

    private void NotificationService_Changed(object? sender, EventArgs e) => RefreshNotificationsButton();

    /// <summary>
    /// Shared handler for every navigation <see cref="BarButtonItem"/> built
    /// from <c>MainForm.Designer.cs</c>'s <c>NavigationItems</c> table - the
    /// destination key travels on <see cref="BarItem.Tag"/>, the tab caption
    /// travels on the clicked item's own <see cref="BarItem.Caption"/> (set
    /// from the same table row). Unlike the old single-panel Shell, clicking
    /// the page already open is not a no-op here - <see cref="ShowDocument"/>
    /// itself decides "activate the existing tab" vs. "build a new one" per
    /// key, so there is nothing left for this handler to guard against.
    /// </summary>
    private void NavigationButtonItem_ItemClick(object? sender, ItemClickEventArgs e)
    {
        if (e.Item.Tag is not string key)
        {
            return;
        }

        _navigationService.NavigateTo(key, e.Item.Caption);
    }

    private void NotificationsButtonItem_ItemClick(object? sender, ItemClickEventArgs e)
    {
        using var form = new NotificationsForm(_notificationService.Notifications);
        form.ShowDialog(this);
    }

    private async void ChangePasswordItem_ItemClick(object? sender, ItemClickEventArgs e) => await ChangePasswordAsync();

    private async void SignOutItem_ItemClick(object? sender, ItemClickEventArgs e) => await SignOutAsync();

    private void ThemeEditItem_EditValueChanged(object? sender, EventArgs e)
    {
        if (sender is DevExpress.XtraBars.BarEditItem { EditValue: string skinName })
        {
            _themeService.ApplySkin(skinName);
        }
    }

    /// <summary>
    /// Rebuilds the Ribbon's navigation visibility for the signed-in user:
    /// every <see cref="BarButtonItem"/> in <c>_navigationButtonsByKey</c>
    /// is shown if <see cref="NavigationMenuBuilder"/> says the user can
    /// view that key, hidden otherwise; a business-area page in
    /// <c>PermissionGatedPages</c> collapses itself once none of its groups
    /// have a visible button left, so a user with zero Catalog permissions
    /// never sees an empty "Catalog" ribbon tab.
    /// </summary>
    private async Task RefreshNavigationAsync()
    {
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

        var visibleKeys = new HashSet<string>(await navigationMenuBuilder.GetVisibleMenuKeysAsync(userId), StringComparer.OrdinalIgnoreCase);

        foreach (var (key, button) in _navigationButtonsByKey)
        {
            button.Visibility = visibleKeys.Contains(key) ? BarItemVisibility.Always : BarItemVisibility.Never;
        }

        foreach (var group in _navigationGroupsByKey.Values)
        {
            group.Visible = group.ItemLinks.Cast<BarItemLink>().Any(link => link.Item.Visibility == BarItemVisibility.Always);
        }

        foreach (var pageName in PermissionGatedPages)
        {
            var page = _ribbon.Pages[pageName];
            page.Visible = page.Groups.Cast<RibbonPageGroup>().Any(g => g.Visible);
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

    /// <summary>
    /// Signs out and returns to the login screen for a different user to
    /// sign in, without exiting the process. <see cref="MainForm"/> is a
    /// DI singleton (required - <c>NavigationService</c> resolves
    /// <see cref="IWorkspaceHost"/>, which maps to this same instance, on
    /// every navigation, not just once), and <c>Program.cs</c>'s startup
    /// sequence only ever calls <c>Application.Run</c> on it once. Calling
    /// <see cref="Form.Close"/> here would therefore end the whole
    /// application's message loop and exit the process instead of returning
    /// to login. Hiding instead of closing, then showing a fresh
    /// <see cref="LoginForm"/> as an owned modal dialog (ordinary modal
    /// dialog usage, unrelated to MDI), keeps this same Shell instance alive
    /// for the next session; only a genuinely cancelled re-login (closing
    /// that dialog without signing in) closes the Shell for real. Open
    /// document tabs are deliberately left as they are across a sign-out -
    /// re-signing in returns to the same tabs rather than losing them.
    /// </summary>
    private async Task SignOutAsync()
    {
        var outgoingUserDisplayName = _currentSession.DisplayName ?? "Unknown";

        using (var logScope = _scopeFactory.CreateScope())
        {
            try
            {
                var mediator = logScope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new RecordActivityCommand("Logout", null, outgoingUserDisplayName, Environment.MachineName));
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                // Best-effort - see RestaurantPosView.LogActivityAsync's
                // identical reasoning; a logging hiccup shouldn't block
                // signing out.
            }
        }

        _currentSession.SignOut();
        Hide();

        using var scope = _scopeFactory.CreateScope();
        var loginForm = scope.ServiceProvider.GetRequiredService<LoginForm>();
        var signedIn = false;
        loginForm.LoginSucceeded += (_, _) => signedIn = true;
        loginForm.ShowDialog(this);

        if (!signedIn)
        {
            Close();
            return;
        }

        _profileMenu.Caption = _currentSession.DisplayName ?? "Account";
        _userStatusItem.Caption = _currentSession.DisplayName is { } name ? $"Signed in as {name}" : "Not signed in";
        await RefreshNavigationAsync();
        RefreshRecentMenus();
        RefreshNotificationsButton();
        _navigationService.NavigateTo("dashboard", "Dashboard");
        Show();
    }

    /// <summary>
    /// Self-service password change, reachable from the profile menu -
    /// distinct from the admin Reset Password action on
    /// <see cref="Identity.Users.UsersForm"/>, which skips the
    /// current-password check this dialog requires.
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

    /// <summary>
    /// Opens a document tab for <paramref name="contentFactory"/>'s control,
    /// captioned <paramref name="caption"/>. <paramref name="key"/> identifies
    /// the document for de-duplication: unless
    /// <paramref name="allowMultipleInstances"/> is <see langword="true"/>, an
    /// already-open tab with the same key is activated instead of building a
    /// new one (see <see cref="_openDocumentsByKey"/>). As a special case, a
    /// <paramref name="key"/> of <c>"pos"</c> whose content is a
    /// <see cref="Clovent.Desktop.Restaurant.Orders.RestaurantPosForm"/> is
    /// hosted in its own top-level <see cref="Clovent.Desktop.Restaurant.Orders.RestaurantPosForm"/>
    /// instead of a Shell tab.
    /// </summary>
    public void ShowDocument(string key, string caption, Func<Control> contentFactory, bool allowMultipleInstances = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(contentFactory);

        if (string.Equals(key, "pos", StringComparison.OrdinalIgnoreCase))
        {
            var contentControl = contentFactory();
            if (contentControl is Clovent.Desktop.Restaurant.Orders.RestaurantPosForm posForm)
            {
                posForm.Show(this);
                return;
            }
        }

        if (!allowMultipleInstances && _openDocumentsByKey.TryGetValue(key, out var existing))
        {
            _tabbedView.ActivateDocument(existing.Control);
            return;
        }

        var content = contentFactory();
        content.Dock = DockStyle.Fill;
        var document = _tabbedView.AddDocument(content, caption);

        if (!allowMultipleInstances)
        {
            _openDocumentsByKey[key] = document;
        }

        _tabbedView.ActivateDocument(document.Control);

        if (content is BaseForm baseForm)
        {
            _ = RunInitialRefreshAsync(baseForm);
        }
    }

    private static async Task RunInitialRefreshAsync(BaseForm baseForm)
    {
        try
        {
            await baseForm.RefreshAsync();
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show(baseForm, ex.Message, "Refresh", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    /// <summary>
    /// Cancels a tab close when its content is a dirty <see cref="BaseForm"/>
    /// that declines to close (see <see cref="BaseForm.ConfirmClose"/>).
    /// </summary>
    private void TabbedView_DocumentClosing(object? sender, DocumentCancelEventArgs e)
    {
        if (e.Document.Control is BaseForm baseForm && !baseForm.ConfirmClose())
        {
            e.Cancel = true;
        }
    }

    /// <summary>Drops the closed document from <see cref="_openDocumentsByKey"/> so its key can be reopened fresh.</summary>
    private void TabbedView_DocumentClosed(object? sender, DocumentEventArgs e)
    {
        var key = _openDocumentsByKey.FirstOrDefault(pair => ReferenceEquals(pair.Value, e.Document)).Key;
        if (key is not null)
        {
            _openDocumentsByKey.Remove(key);
        }
    }

    /// <summary>
    /// Tracks the active tab's status text: <see cref="BaseForm"/> documents
    /// push their own <see cref="BaseForm.StatusText"/> (subscribed here,
    /// unsubscribed from whichever document was active before); anything
    /// else (a not-yet-converted screen) falls back to its tab caption.
    /// </summary>
    private void TabbedView_DocumentActivated(object? sender, DocumentEventArgs e)
    {
        if (_activeStatusSource is not null)
        {
            _activeStatusSource.StatusTextChanged -= ActiveDocument_StatusTextChanged;
            _activeStatusSource = null;
        }

        if (e.Document.Control is BaseForm baseForm)
        {
            _activeStatusSource = baseForm;
            baseForm.StatusTextChanged += ActiveDocument_StatusTextChanged;
            SetStatus(baseForm.StatusText);
        }
        else
        {
            SetStatus($"Viewing: {e.Document.Caption}");
        }
    }

    private void ActiveDocument_StatusTextChanged(object? sender, EventArgs e)
    {
        if (sender is BaseForm baseForm)
        {
            SetStatus(baseForm.StatusText);
        }
    }

    /// <summary>
    /// Ctrl+S/F5 are handled here, not by each document, because a hosted
    /// <see cref="Control"/> (every <see cref="BaseForm"/> is a
    /// <c>UserControl</c>, never a <c>Form</c> - see that class's doc
    /// comment) does not get its own command-key routing the way a
    /// top-level <see cref="Form"/> does; this Shell is the only place that
    /// can intercept them application-wide and forward to whichever
    /// document is active.
    /// </summary>
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (_tabbedView.ActiveDocument?.Control is BaseForm activeForm)
        {
            if (keyData == (Keys.Control | Keys.S))
            {
                _ = RunSaveShortcutAsync(activeForm);
                return true;
            }

            if (keyData == Keys.F5)
            {
                _ = RunInitialRefreshAsync(activeForm);
                return true;
            }
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private static async Task RunSaveShortcutAsync(BaseForm baseForm)
    {
        try
        {
            await baseForm.SaveAsync();
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show(baseForm, ex.Message, "Save", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
