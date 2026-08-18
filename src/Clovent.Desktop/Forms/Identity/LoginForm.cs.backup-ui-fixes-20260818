using Clovent.Desktop.Login;
using Clovent.Desktop.Navigation;
using Clovent.Desktop.Sessions;
using Clovent.Desktop.Theming;
using DevExpress.XtraEditors;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Forms.Identity;

/// <summary>
/// The application's sign-in screen - and, since module selection was folded
/// into it, also where the user picks POS or Back Office. There is no
/// separate "choose a module" screen: <see cref="CardPos_Click"/>/
/// <see cref="CardBackOffice_Click"/> both authenticate first (via
/// <see cref="ILoginService"/>, the same single credential path either
/// button uses - neither duplicates it) and only then, on success, check
/// whether the now-signed-in user may use that specific module before
/// setting <see cref="SelectedModuleKey"/> and closing. The control tree
/// (every label/edit/button, its Location/Size/TabIndex, and every event
/// subscription) is declared in <c>LoginForm.Designer.cs</c>, exactly as
/// Visual Studio's WinForms Designer generates it - this file holds behavior
/// only: validation, credential submission, post-login permission checks,
/// and theme/language population.
/// </summary>
/// <remarks>
/// <para><b>Visual Studio Designer compatibility.</b> <c>InitializeComponent</c>
/// (the visual structure, in the Designer partial) never touches
/// <see cref="_loginService"/>/<see cref="_themeService"/>/<see cref="_scopeFactory"/>/
/// <see cref="_currentSession"/> - only <see cref="LoginForm_Load"/> and the
/// module button handlers do, and <see cref="LoginForm_Load"/> returns
/// immediately when <c>DesignMode</c> is <see langword="true"/>, before any
/// of them are dereferenced. That is what makes the parameterless
/// constructor below safe: it passes <see langword="null"/>! for every
/// dependency, and the Designer never runs far enough into this class's
/// logic to notice.</para>
/// <para><b>Why permission is checked after authentication, not before.</b>
/// <see cref="Navigation.NavigationMenuBuilder"/>/<c>IMenuAuthorizationPolicy</c>
/// (the same mechanism <c>Forms.Shell.MainForm.RefreshNavigationAsync</c>
/// already uses for Ribbon visibility) answers "can user X view key Y?" - it
/// has no notion of "can whoever eventually types this username view Y?"
/// before that username has even been verified. There is no anonymous/
/// pre-auth permission lookup anywhere in this system, and inventing one
/// would be a new permission system, not reuse of the existing one. So both
/// module buttons stay enabled before sign-in (identity is unknown until
/// then); the permission actually gated on the clicked module's key is
/// checked the instant <see cref="ILoginService.LoginAsync"/> succeeds
/// (<see cref="ICurrentSession.UserId"/> is populated by then - see
/// <see cref="LoginService"/>'s doc comment), and a denied user is signed
/// back out immediately, before this form closes, so no module ever opens
/// for someone not permitted to use it.</para>
/// </remarks>
public sealed partial class LoginForm : XtraForm
{
    private readonly ILoginService _loginService;
    private readonly IThemeService _themeService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICurrentSession _currentSession;

    /// <summary>
    /// Raised once both <see cref="ILoginService.LoginAsync"/> AND the
    /// clicked module's permission check succeed - i.e. exactly when this
    /// form is about to close leaving the user validly signed in.
    /// <c>Forms.Shell.MainForm.SignOutAsync</c>'s re-sign-in dialog is the
    /// only other consumer: it uses this event as its sole "did closing this
    /// dialog leave me signed in" signal, never which button was clicked
    /// (it always returns to the Back Office dashboard regardless -
    /// <see cref="SelectedModuleKey"/> is irrelevant there). A user who
    /// authenticates successfully but is then denied the specific module
    /// they clicked is signed back out before this event would fire - see
    /// <see cref="AuthenticateAndSelectModuleAsync"/> - so that dialog is
    /// never left thinking a denied re-authentication succeeded.
    /// </summary>
    public event EventHandler? LoginSucceeded;

    /// <summary>
    /// The module the signed-in user chose and is permitted to use ("pos" or
    /// "backoffice"), or <see langword="null"/> if this form closed without
    /// completing a permitted sign-in (Cancel, or a permission denial).
    /// <see cref="Program"/> reads this once <see cref="Application.Run(Form)"/>
    /// returns to decide what to open next.
    /// </summary>
    public string? SelectedModuleKey { get; private set; }

    /// <summary>Builds the sign-in screen.</summary>
    public LoginForm(ILoginService loginService, IThemeService themeService, IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _loginService = loginService;
        _themeService = themeService;
        _scopeFactory = scopeFactory;
        _currentSession = currentSession;

        InitializeComponent();
    }

    /// <summary>
    /// Design-time-only constructor - required for the Visual Studio
    /// WinForms Designer to host this form: the Designer instantiates the
    /// type being designed via a public parameterless constructor and
    /// cannot supply the constructor above's DI dependencies (it never
    /// starts the application's DI container at all). Never used at
    /// runtime: <c>services.TryAddTransient&lt;LoginForm&gt;()</c> resolves
    /// the constructor above instead, since the built-in DI container
    /// always prefers the constructor whose parameters can all be
    /// satisfied by registered services, and every one of them is
    /// registered - this parameterless overload is strictly less
    /// resolvable, so it is never chosen outside the Designer.
    /// </summary>
    public LoginForm() : this(null!, null!, null!, null!)
    {
    }

    /// <summary>
    /// Populates the language/theme selectors and applies the
    /// DevExpress <c>UseSystemPasswordChar</c> workaround (see the inline
    /// comment below) - deferred to <c>Load</c>, and skipped entirely in
    /// <c>DesignMode</c>, so the Visual Studio Designer never needs a
    /// working <see cref="ILoginService"/>/<see cref="IThemeService"/> to
    /// host this form. Real theme/language data appearing only at runtime
    /// is expected - the Designer only needs the static control tree
    /// <c>InitializeComponent</c> builds, not populated combo items.
    /// </summary>
    private void LoginForm_Load(object? sender, EventArgs e)
    {
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
        {
            return;
        }

        ApplyContentDrivenMinimumSize();
        PopulateLanguages();
        PopulateThemes();

        // DevExpress's TextEdit.UseSystemPasswordChar defers its native
        // mask-box update via a marshaled callback that assumes the
        // control's window handle already exists; setting it any earlier
        // (e.g. in the constructor, before this form is ever shown) throws
        // a NullReferenceException from inside DevExpress's own internals
        // once that deferred callback runs. By Load, every child control's
        // handle has been created along with the form's own, which avoids it.
        txtPassword.Properties.UseSystemPasswordChar = true;
        txtPin.Properties.UseSystemPasswordChar = true;
    }

    /// <summary>
    /// Grows the window - never shrinks it - when <c>tlpForm</c>'s actual
    /// field-group height (title through the module cards, at whatever
    /// font/DPI this machine renders) exceeds the Designer's 600px
    /// design-time <see cref="Control.ClientSize"/>. <c>tlpForm</c>'s two
    /// Percent(100) spacer rows (see <c>LoginForm.Designer.cs</c>'s own
    /// remarks) only have leftover space to share for vertical centering
    /// once the window is at least as tall as every AutoSize/Absolute row's
    /// real height combined - a plain hardcoded 600 silently forced those
    /// AutoSize rows to compress below their preferred height instead,
    /// which is what was clipping Username/Password/PIN. This is the same
    /// "measure real content, then size the chrome around it" technique
    /// <c>MasterDataEditFormBase</c>'s own <c>Load</c> handler already uses
    /// for edit dialogs, applied here instead of a second hardcoded guess.
    /// </summary>
    private void ApplyContentDrivenMinimumSize()
    {
        tlpForm.PerformLayout();
        var requiredContentHeight = tlpForm.GetPreferredSize(new Size(tlpForm.Width, 0)).Height;
        var requiredClientHeight = requiredContentHeight + 32;

        if (ClientSize.Height < requiredClientHeight)
        {
            var chromeHeight = Height - ClientSize.Height;
            MinimumSize = new Size(MinimumSize.Width, requiredClientHeight + chromeHeight);
            ClientSize = new Size(ClientSize.Width, requiredClientHeight);
        }
    }

    private void PopulateLanguages()
    {
        cmbLanguage.Properties.Items.AddRange(["English (United States)", "Español", "Français"]);
        cmbLanguage.SelectedIndex = 0;
    }

    private void PopulateThemes()
    {
        cmbTheme.Properties.Items.AddRange([.. _themeService.AvailableSkins]);
        var currentIndex = cmbTheme.Properties.Items.IndexOf(_themeService.CurrentSkin);
        cmbTheme.SelectedIndex = currentIndex >= 0 ? currentIndex : 0;
    }

    private void CmbTheme_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (cmbTheme.SelectedItem is string skinName)
        {
            _themeService.ApplySkin(skinName);
        }
    }

    private bool ValidateInput()
    {
        var isValid = true;

        txtUsername.ErrorText = string.IsNullOrWhiteSpace(txtUsername.Text) ? "Username is required." : string.Empty;
        isValid &= string.IsNullOrEmpty(txtUsername.ErrorText);

        var hasPassword = !string.IsNullOrWhiteSpace(txtPassword.Text);
        var hasPin = !string.IsNullOrWhiteSpace(txtPin.Text);
        var credentialError = hasPassword || hasPin ? string.Empty : "Enter your password or PIN.";
        txtPassword.ErrorText = credentialError;
        txtPin.ErrorText = credentialError;
        isValid &= string.IsNullOrEmpty(credentialError);

        return isValid;
    }

    private async void CardPos_Click(object? sender, EventArgs e) => await AuthenticateAndSelectModuleAsync("pos", "Restaurant POS");

    private async void CardBackOffice_Click(object? sender, EventArgs e) => await AuthenticateAndSelectModuleAsync("backoffice", "Back Office");

    /// <summary>Space/Enter activates a focused module card - <see cref="PanelControl"/> has no built-in "activate on keypress" the way a real button does, since it isn't a button; wired to both cards' <c>KeyDown</c> in <c>LoginForm.Designer.cs</c>.</summary>
    private void Card_KeyDown(object? sender, KeyEventArgs e)
    {
        if ((e.KeyCode is Keys.Enter or Keys.Space) && sender is Control card)
        {
            e.Handled = true;
            if (ReferenceEquals(card, cardPos))
            {
                CardPos_Click(sender, EventArgs.Empty);
            }
            else if (ReferenceEquals(card, cardBackOffice))
            {
                CardBackOffice_Click(sender, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// The one credential path both module buttons share: authenticate,
    /// then - only on success - check whether the now-signed-in user may
    /// use <paramref name="moduleKey"/> before ever setting
    /// <see cref="SelectedModuleKey"/>. A denied user is signed back out
    /// immediately and shown an error, exactly like a failed password would
    /// be; this form stays open either way.
    /// </summary>
    private async Task AuthenticateAndSelectModuleAsync(string moduleKey, string moduleDisplayName)
    {
        lblError.Visible = false;

        if (!ValidateInput())
        {
            return;
        }

        SetLoading(true);
        try
        {
            var request = new LoginRequest(
                txtUsername.Text.Trim(),
                string.IsNullOrWhiteSpace(txtPassword.Text) ? null : txtPassword.Text,
                string.IsNullOrWhiteSpace(txtPin.Text) ? null : txtPin.Text,
                chkRememberMe.Checked);

            var result = await _loginService.LoginAsync(request);

            if (!result.Succeeded)
            {
                lblError.Text = result.ErrorMessage ?? "Login failed.";
                lblError.Visible = true;
                return;
            }

            if (!await CanUseModuleAsync(moduleKey))
            {
                _currentSession.SignOut();
                lblError.Text = $"You do not have permission to access {moduleDisplayName}.";
                lblError.Visible = true;
                return;
            }

            // Raised only once both authentication AND the permission check
            // above have succeeded - Forms.Shell.MainForm.SignOutAsync's
            // re-sign-in dialog uses this event as its sole signal that
            // closing this form left the user validly signed in. Raising it
            // any earlier (right after a successful LoginAsync, before the
            // permission check) would break that: a user who authenticates
            // successfully but is then denied this specific module gets
            // signed back out again below, and SignOutAsync must not mistake
            // that for a valid re-sign-in.
            LoginSucceeded?.Invoke(this, EventArgs.Empty);

            SelectedModuleKey = moduleKey;
            Close();
        }
        finally
        {
            SetLoading(false);
        }
    }

    /// <summary>
    /// Whether the just-signed-in user may open <paramref name="moduleKey"/> -
    /// "pos" needs the "pos" navigation key visible; "backoffice" needs any
    /// visible key other than "pos" (Back Office is every other registered
    /// screen, not one specific key). Reuses <see cref="NavigationMenuBuilder"/>/
    /// <c>IMenuAuthorizationPolicy</c>, the same permission mechanism
    /// <c>Forms.Shell.MainForm.RefreshNavigationAsync</c> already gates the
    /// Ribbon with - see this class's remarks for why this can only run
    /// after authentication, not before.
    /// </summary>
    private async Task<bool> CanUseModuleAsync(string moduleKey)
    {
        if (_currentSession.UserId is not { } userId)
        {
            return false;
        }

        using var scope = _scopeFactory.CreateScope();
        var navigationMenuBuilder = scope.ServiceProvider.GetRequiredService<NavigationMenuBuilder>();
        var visibleKeys = await navigationMenuBuilder.GetVisibleMenuKeysAsync(userId);

        return moduleKey == "pos"
            ? visibleKeys.Contains("pos", StringComparer.OrdinalIgnoreCase)
            : visibleKeys.Any(key => !string.Equals(key, "pos", StringComparison.OrdinalIgnoreCase));
    }

    private void BtnCancelHidden_Click(object? sender, EventArgs e) => Close();

    /// <summary>Keeps a module card's rounded-corner clipping region matched to its live size - a <see cref="Control.Region"/> set once at Designer time would go stale the moment the card resizes with its column. Shared by both cards' <c>Resize</c> event (wired in <c>LoginForm.Designer.cs</c>).</summary>
    private void Card_Resize(object? sender, EventArgs e)
    {
        if (sender is not Control card)
        {
            return;
        }

        const int radius = 14;
        var rect = new Rectangle(0, 0, card.Width, card.Height);
        using var path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
        path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
        path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
        path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
        path.CloseFigure();
        card.Region = new Region(path);
    }

    /// <summary>Lightens a card on hover - shared by the card panel and its icon/title/subtitle labels, since a child label's own <c>MouseEnter</c>/<c>MouseLeave</c> fire independently of its parent panel's (see <see cref="Card_MouseLeave"/>).</summary>
    private void Card_MouseEnter(object? sender, EventArgs e)
    {
        if (GetCard(sender) is { } card && card.Enabled)
        {
            SetCardBackColor(card, ControlPaint.Light(GetAccentColor(card), 0.2f));
        }
    }

    /// <summary>
    /// Reverts a card's hover/pressed color - but only once the pointer has
    /// truly left the whole card, not just crossed from the panel's own
    /// background onto one of its child labels (which independently raises
    /// the panel's <c>MouseLeave</c> the instant the pointer enters the
    /// child's window). Checking the live cursor position against the
    /// card's own client bounds is what tells these two cases apart.
    /// </summary>
    private void Card_MouseLeave(object? sender, EventArgs e)
    {
        if (GetCard(sender) is not { } card || !card.Enabled)
        {
            return;
        }

        var stillInsideCard = card.ClientRectangle.Contains(card.PointToClient(Cursor.Position));
        if (!stillInsideCard)
        {
            SetCardBackColor(card, GetAccentColor(card));
        }
    }

    /// <summary>Darkens a card while the mouse button is held down on it - the "pressed" state, reverted on <see cref="Card_MouseUp"/> or whenever the pointer leaves (<see cref="Card_MouseLeave"/> already reverts to the resting color, which is correct even mid-press since releasing outside the card is a cancelled click anyway).</summary>
    private void Card_MouseDown(object? sender, MouseEventArgs e)
    {
        if (GetCard(sender) is { } card && card.Enabled)
        {
            SetCardBackColor(card, ControlPaint.Dark(GetAccentColor(card), 0.1f));
        }
    }

    private void Card_MouseUp(object? sender, MouseEventArgs e)
    {
        if (GetCard(sender) is { } card && card.Enabled)
        {
            var stillInsideCard = card.ClientRectangle.Contains(card.PointToClient(Cursor.Position));
            SetCardBackColor(card, stillInsideCard ? ControlPaint.Light(GetAccentColor(card), 0.2f) : GetAccentColor(card));
        }
    }

    /// <summary>Draws a keyboard-focus ring around a card - <see cref="PanelControl"/> gives no focus-rectangle of its own (it isn't a button), so <c>TabStop</c>/<c>Enter</c>/<c>Leave</c> (wired in <c>LoginForm.Designer.cs</c>) are what make Tab-navigating to a card visible at all.</summary>
    private void Card_Enter(object? sender, EventArgs e)
    {
        if (sender is not PanelControl card)
        {
            return;
        }

        card.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;
        card.Appearance.BorderColor = Color.White;
        card.Appearance.Options.UseBorderColor = true;
    }

    private void Card_Leave(object? sender, EventArgs e)
    {
        if (sender is not PanelControl card)
        {
            return;
        }

        card.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
    }

    /// <summary>Resolves the card a mouse/keyboard event belongs to - <paramref name="sender"/> is already the card itself when the event came from the card panel directly, or a child icon/title/subtitle label whose <c>Tag</c> was set to its owning card in <c>LoginForm.Designer.cs</c>.</summary>
    private static PanelControl? GetCard(object? sender) => sender as PanelControl ?? (sender as Control)?.Tag as PanelControl;

    private Color GetAccentColor(PanelControl card) => ReferenceEquals(card, cardPos) ? PosAccentColor : BackOfficeAccentColor;

    private static void SetCardBackColor(PanelControl card, Color color)
    {
        card.Appearance.BackColor = color;
        card.Appearance.Options.UseBackColor = true;
    }

    /// <summary>Grays a card out and stops it responding to input while <paramref name="enabled"/> is <see langword="false"/> - <see cref="PanelControl"/> doesn't automatically dim the way a real button does when <see cref="Control.Enabled"/> changes, so this drives the same visual by hand.</summary>
    private static void SetCardEnabled(PanelControl card, LabelControl subtitle, bool enabled, Color accentColor)
    {
        card.Enabled = enabled;
        card.TabStop = enabled;
        SetCardBackColor(card, enabled ? accentColor : Color.FromArgb(200, 200, 200));
        subtitle.Appearance.ForeColor = enabled ? Color.FromArgb(225, 235, 255) : Color.FromArgb(140, 140, 140);
        subtitle.Appearance.Options.UseForeColor = true;
    }

    private void SetLoading(bool isLoading)
    {
        prgLoading.Visible = isLoading;
        SetCardEnabled(cardPos, lblPosSubtitle, !isLoading, PosAccentColor);
        SetCardEnabled(cardBackOffice, lblBackOfficeSubtitle, !isLoading, BackOfficeAccentColor);
        txtUsername.Enabled = !isLoading;
        txtPassword.Enabled = !isLoading;
        txtPin.Enabled = !isLoading;
        chkRememberMe.Enabled = !isLoading;
    }
}
