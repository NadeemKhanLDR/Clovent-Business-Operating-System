using Clovent.Desktop.Theming;
using DevExpress.LookAndFeel;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraLayout;

namespace Clovent.Desktop.Login;

/// <summary>
/// The application's sign-in screen. Contains no credential-checking logic -
/// the Log In button calls <see cref="ILoginService"/> only (see that
/// interface's doc comment). Built entirely in code; see
/// <see cref="Shell.ShellForm"/>'s doc comment for why no
/// designer-generated partial class is used anywhere in this project.
/// </summary>
public sealed class LoginForm : XtraForm
{
    private readonly ILoginService _loginService;
    private readonly IThemeService _themeService;

    private readonly TextEdit _usernameEdit = new();
    private readonly TextEdit _passwordEdit = new();
    private readonly TextEdit _pinEdit = new();
    private readonly CheckEdit _rememberMeCheck = new() { Text = "Remember me" };
    private readonly ComboBoxEdit _languageCombo = new();
    private readonly ComboBoxEdit _themeCombo = new();
    private readonly SimpleButton _loginButton = new() { Text = "Log In" };
    private readonly LabelControl _failureLabel = new() { Visible = false };
    private readonly ProgressBarControl _loadingIndicator = new() { Visible = false };

    /// <summary>Raised once <see cref="ILoginService.LoginAsync"/> reports success, immediately before this form closes itself.</summary>
    public event EventHandler? LoginSucceeded;

    /// <summary>Builds the sign-in screen.</summary>
    public LoginForm(ILoginService loginService, IThemeService themeService)
    {
        _loginService = loginService;
        _themeService = themeService;

        Text = "Sign in - Clovent Business Operating System";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 900;
        Height = 560;
        MinimumSize = new Size(720, 480);
        MaximizeBox = false;
        KeyPreview = true;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        root.Controls.Add(BuildBrandingPanel(), 0, 0);
        root.Controls.Add(BuildFormPanel(), 1, 0);

        Controls.Add(root);

        AcceptButton = _loginButton;
        CancelButton = CreateEscapeButton();

        _loginButton.Click += OnLoginClicked;
        _themeCombo.SelectedIndexChanged += OnThemeSelected;

        PopulateLanguages();
        PopulateThemes();

        // DevExpress's TextEdit.UseSystemPasswordChar defers its native mask-box
        // update via a marshaled callback that assumes the control's window
        // handle already exists; setting it before the handle exists (i.e.
        // here in the constructor, before this form is ever shown) throws a
        // NullReferenceException from inside DevExpress's own internals once
        // that deferred callback runs. Deferring the assignment to Load - by
        // which point every child control's handle has been created along
        // with the form's own - avoids it.
        Load += (_, _) =>
        {
            _passwordEdit.Properties.UseSystemPasswordChar = true;
            _pinEdit.Properties.UseSystemPasswordChar = true;
        };
    }

    private static PanelControl BuildBrandingPanel()
    {
        var panel = new PanelControl
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyles.NoBorder,
            LookAndFeel = { UseDefaultLookAndFeel = false, Style = LookAndFeelStyle.Flat },
        };
        panel.Appearance.BackColor = Color.FromArgb(32, 45, 74);
        panel.Appearance.Options.UseBackColor = true;

        var wordmark = new LabelControl
        {
            Text = "CBOS",
            Dock = DockStyle.Top,
            Height = 80,
            Padding = new Padding(0, 48, 0, 0),
            AccessibleName = "Clovent Business Operating System logo",
        };
        wordmark.Appearance.Font = new Font("Segoe UI", 28F, FontStyle.Bold);
        wordmark.Appearance.ForeColor = Color.White;
        wordmark.Appearance.Options.UseFont = true;
        wordmark.Appearance.Options.UseForeColor = true;
        wordmark.Appearance.TextOptions.HAlignment = HorzAlignment.Center;
        wordmark.Appearance.Options.UseTextOptions = true;

        var tagline = new LabelControl
        {
            Text = "Clovent Business Operating System",
            Dock = DockStyle.Top,
            Height = 40,
        };
        tagline.Appearance.Font = new Font("Segoe UI", 10F);
        tagline.Appearance.ForeColor = Color.FromArgb(200, 210, 230);
        tagline.Appearance.Options.UseFont = true;
        tagline.Appearance.Options.UseForeColor = true;
        tagline.Appearance.TextOptions.HAlignment = HorzAlignment.Center;
        tagline.Appearance.Options.UseTextOptions = true;

        panel.Controls.Add(tagline);
        panel.Controls.Add(wordmark);
        return panel;
    }

    private Control BuildFormPanel()
    {
        var outer = new PanelControl { Dock = DockStyle.Fill, BorderStyle = BorderStyles.NoBorder };

        var layoutControl = new LayoutControl { Dock = DockStyle.Fill, Padding = new Padding(32) };
        layoutControl.BeginUpdate();
        try
        {
            layoutControl.Root.TextVisible = false;
            layoutControl.Root.Padding = new DevExpress.XtraLayout.Utils.Padding(8);

            _usernameEdit.AccessibleName = "Username";
            _passwordEdit.AccessibleName = "Password";
            _pinEdit.AccessibleName = "PIN";
            _pinEdit.Properties.MaxLength = 8;

            var titleItem = layoutControl.AddItem("", CreateTitleLabel());
            titleItem.TextVisible = false;

            var usernameItem = layoutControl.AddItem("&Username", _usernameEdit);
            var passwordItem = layoutControl.AddItem("&Password", _passwordEdit);
            var pinItem = layoutControl.AddItem("P&IN", _pinEdit);
            var rememberItem = layoutControl.AddItem("", _rememberMeCheck);
            rememberItem.TextVisible = false;

            var languageItem = layoutControl.AddItem("&Language", _languageCombo);
            var themeItem = layoutControl.AddItem("&Theme", _themeCombo);

            var failureItem = layoutControl.AddItem("", _failureLabel);
            failureItem.TextVisible = false;
            _failureLabel.Appearance.ForeColor = Color.Firebrick;
            _failureLabel.Appearance.Options.UseForeColor = true;

            var loadingItem = layoutControl.AddItem("", _loadingIndicator);
            loadingItem.TextVisible = false;

            var buttonItem = layoutControl.AddItem("", _loginButton);
            buttonItem.TextVisible = false;

            foreach (var item in new[] { usernameItem, passwordItem, pinItem, rememberItem, languageItem, themeItem, failureItem, loadingItem, buttonItem })
            {
                item.TextToControlDistance = 8;
            }
        }
        finally
        {
            layoutControl.EndUpdate();
        }

        outer.Controls.Add(layoutControl);
        return outer;
    }

    private static LabelControl CreateTitleLabel()
    {
        var label = new LabelControl { Text = "Welcome back", AutoSizeMode = LabelAutoSizeMode.Vertical };
        label.Appearance.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        label.Appearance.Options.UseFont = true;
        return label;
    }

    private SimpleButton CreateEscapeButton()
    {
        var hidden = new SimpleButton { Visible = false, DialogResult = DialogResult.Cancel };
        hidden.Click += (_, _) => Close();
        Controls.Add(hidden);
        return hidden;
    }

    private void PopulateLanguages()
    {
        _languageCombo.Properties.Items.AddRange(["English (United States)", "Español", "Français"]);
        _languageCombo.SelectedIndex = 0;
        _languageCombo.AccessibleName = "Language";
    }

    private void PopulateThemes()
    {
        _themeCombo.Properties.Items.AddRange([.. _themeService.AvailableSkins]);
        var currentIndex = _themeCombo.Properties.Items.IndexOf(_themeService.CurrentSkin);
        _themeCombo.SelectedIndex = currentIndex >= 0 ? currentIndex : 0;
        _themeCombo.AccessibleName = "Theme";
    }

    private void OnThemeSelected(object? sender, EventArgs e)
    {
        if (_themeCombo.SelectedItem is string skinName)
        {
            _themeService.ApplySkin(skinName);
        }
    }

    private bool ValidateInput()
    {
        var isValid = true;

        _usernameEdit.ErrorText = string.IsNullOrWhiteSpace(_usernameEdit.Text) ? "Username is required." : string.Empty;
        isValid &= string.IsNullOrEmpty(_usernameEdit.ErrorText);

        var hasPassword = !string.IsNullOrWhiteSpace(_passwordEdit.Text);
        var hasPin = !string.IsNullOrWhiteSpace(_pinEdit.Text);
        var credentialError = hasPassword || hasPin ? string.Empty : "Enter your password or PIN.";
        _passwordEdit.ErrorText = credentialError;
        _pinEdit.ErrorText = credentialError;
        isValid &= string.IsNullOrEmpty(credentialError);

        return isValid;
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        _failureLabel.Visible = false;

        if (!ValidateInput())
        {
            return;
        }

        SetLoading(true);
        try
        {
            var request = new LoginRequest(
                _usernameEdit.Text.Trim(),
                string.IsNullOrWhiteSpace(_passwordEdit.Text) ? null : _passwordEdit.Text,
                string.IsNullOrWhiteSpace(_pinEdit.Text) ? null : _pinEdit.Text,
                _rememberMeCheck.Checked);

            var result = await _loginService.LoginAsync(request);

            if (!result.Succeeded)
            {
                _failureLabel.Text = result.ErrorMessage ?? "Login failed.";
                _failureLabel.Visible = true;
                return;
            }

            LoginSucceeded?.Invoke(this, EventArgs.Empty);
            Close();
        }
        finally
        {
            SetLoading(false);
        }
    }

    private void SetLoading(bool isLoading)
    {
        _loadingIndicator.Visible = isLoading;
        _loginButton.Enabled = !isLoading;
        _usernameEdit.Enabled = !isLoading;
        _passwordEdit.Enabled = !isLoading;
        _pinEdit.Enabled = !isLoading;
        _rememberMeCheck.Enabled = !isLoading;
        _loginButton.Text = isLoading ? "Logging in..." : "Log In";
    }
}
