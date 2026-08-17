using System.Globalization;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Desktop.Forms.Base.Localization;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Restaurant.Application.ActivityLogs.Commands;
using Clovent.Restaurant.Application.Orders.Commands;
using Clovent.Restaurant.Application.Orders.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Forms.Restaurant.Setup;

/// <summary>
/// Restaurant Setup: lets an owner configure the order-number prefix and
/// next number the Restaurant POS assigns to every new order (e.g. "ORD-"
/// starting at 3453), replacing the earlier opaque timestamp-derived
/// number. A single-record form like <c>BusinessSettingsManagementView</c>,
/// simpler still - there is exactly one, restaurant-wide sequence (see
/// <c>Clovent.Restaurant.Orders.OrderNumberSequence</c>'s doc comment), no
/// per-organization selector needed. Feature-gated per
/// <c>restaurantsetup.edit</c>. Control tree lives in
/// <c>RestaurantSetupView.Designer.cs</c>; this file holds behavior only.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class RestaurantSetupView : DevExpress.XtraEditors.XtraUserControl
{
    private const string FeatureCode = "restaurantsetup";

    private readonly IServiceScope _scope;
    private readonly ScreenOperationGate _gate = new();
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;

    // "Display Language" - a separate section/Save action from the order-
    // number sequence above, since the two are unrelated settings that just
    // happen to share this one Restaurant Setup screen.
    private static readonly (string CultureCode, string Display)[] LanguageOptions = [("en", "English"), ("ur", "Ø§Ø±Ø¯Ùˆ (Urdu)")];

    /// <summary>Design-time-only constructor for the Visual Studio WinForms Designer - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public RestaurantSetupView()
    {
        _scope = null!;
        _mediator = null!;
        _featurePolicy = null!;
        _currentSession = null!;

        InitializeComponent();
        InitializeLanguageOptions();
    }

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public RestaurantSetupView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession) : base()
    {
        InitializeComponent();
        InitializeLanguageOptions();

        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
        {
            _scope = null!;
            _mediator = null!;
            _featurePolicy = null!;
            _currentSession = null!;
            return;
        }

        _scope = scopeFactory.CreateScope();
        _mediator = new SerializedMediator(_scope.ServiceProvider.GetRequiredService<IMediator>(), _gate);
        _featurePolicy = new SerializedFeatureAuthorizationPolicy(_scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>(), _gate);
        _currentSession = currentSession;

        InitializeRuntime();
    }

    private void InitializeRuntime()
    {
        AppearanceManager.Changed += AppearanceManager_Changed;
    }

    private void InitializeLanguageOptions()
    {
        _languageCombo.Properties.Items.Clear();
        foreach (var option in LanguageOptions)
        {
            _languageCombo.Properties.Items.Add(option.Display);
        }
    }

    private void AppearanceManager_Changed(object? sender, EventArgs e) => AppearanceManager.Apply(this, "Restaurant", nameof(RestaurantSetupView));

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            AppearanceManager.Changed -= AppearanceManager_Changed;
            _scope.Dispose();
            _gate.Dispose();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private async void SaveButton_Click(object? sender, EventArgs e) => await SaveAsync();

    private void PrefixEdit_EditValueChanged(object? sender, EventArgs e) => UpdatePreview();

    private void StartingNumberEdit_EditValueChanged(object? sender, EventArgs e) => UpdatePreview();

    private void SaveLanguageButton_Click(object? sender, EventArgs e) => SaveLanguage();
    private async void RestaurantSetupView_Load(object? sender, EventArgs e)
    {
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;
        await LoadAsync();
    }
    private void UpdatePreview() => _previewLabel.Text = $"{_prefixEdit.Text}{(int)_startingNumberEdit.Value}";

    private async Task LoadAsync()
    {
        AppearanceManager.Apply(this, "Restaurant", nameof(RestaurantSetupView));

        var sequence = await _mediator.Send(new GetOrderNumberSequenceQuery());
        _prefixEdit.Text = sequence.Prefix;
        _startingNumberEdit.Value = sequence.NextNumber;
        UpdatePreview();
        _statusLabel.Text = string.Empty;

        var currentCultureCode = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
        var selectedIndex = Array.FindIndex(LanguageOptions, o => o.CultureCode == currentCultureCode);
        _languageCombo.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
        _languageStatusLabel.Text = string.Empty;
    }

    private Task<bool> CanUseFeatureAsync(string operation) =>
        _currentSession.UserId is { } userId
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}")
            : Task.FromResult(false);

    private async Task SaveAsync()
    {
        if (!await CanUseFeatureAsync("edit"))
        {
            XtraMessageBox.Show(this, "You do not have permission to edit the Restaurant Setup.", "Not Authorized", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var prefix = _prefixEdit.Text.Trim();
        if (string.IsNullOrEmpty(prefix))
        {
            XtraMessageBox.Show(this, "Enter an order number prefix.", "Prefix Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var sequence = await _mediator.Send(new ConfigureOrderNumberSequenceCommand(prefix, (int)_startingNumberEdit.Value));
            _prefixEdit.Text = sequence.Prefix;
            _startingNumberEdit.Value = sequence.NextNumber;
            UpdatePreview();
            _statusLabel.Text = "Saved.";

            try
            {
                await _mediator.Send(new RecordActivityCommand("Setup Changes", $"Order number prefix set to \"{sequence.Prefix}\", next number {sequence.NextNumber}", _currentSession.DisplayName ?? "Unknown", Environment.MachineName));
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                // See RestaurantPosView.LogActivityAsync - a logging hiccup
                // should not make an already-saved setup change look like it
                // failed.
            }
        }
        catch (Clovent.Restaurant.RestaurantDomainException ex)
        {
            XtraMessageBox.Show(this, ex.Message, "Invalid Setup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    /// <summary>
    /// Persists the chosen display language and applies it immediately to
    /// this process (<see cref="Thread.CurrentUICulture"/>/
    /// <see cref="CultureInfo.DefaultThreadCurrentUICulture"/>, the latter so
    /// every *new* thread/scope created from here on also picks it up) -
    /// already-open document tabs keep whatever language they were built
    /// with (Transient screens read <c>PosStrings</c> at construction time,
    /// not on a timer), consistent with this same limitation already
    /// documented for Menu Items' auto-refresh.
    /// </summary>
    private void SaveLanguage()
    {
        if (_languageCombo.SelectedIndex < 0)
        {
            return;
        }

        var cultureCode = LanguageOptions[_languageCombo.SelectedIndex].CultureCode;
        LanguagePreferenceStore.Save(cultureCode);

        var culture = CultureInfo.GetCultureInfo(cultureCode);
        Thread.CurrentThread.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        _languageStatusLabel.Text = "Saved.";
    }
}
