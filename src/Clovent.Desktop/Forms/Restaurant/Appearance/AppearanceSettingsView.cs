using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using DevExpress.XtraEditors;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Forms.Restaurant.Appearance;

/// <summary>
/// Restaurant Setup -&gt; Appearance -&gt; Fonts: lets an owner define
/// font/color rules (<see cref="AppearanceRule"/>) scoped to the entire
/// system, one module, one screen, one control type, or one named control -
/// no code change needed for a font/color to apply anywhere in CBOS (the
/// design brief's own "AppearanceManager.Apply(this)" ask -
/// see <see cref="AppearanceManager"/>). Saving reloads
/// <see cref="AppearanceManager"/>'s cache and raises
/// <see cref="AppearanceManager.Changed"/>, which every open Restaurant
/// screen already subscribes to (see e.g.
/// <c>Restaurant.Orders.RestaurantPosView</c>'s own
/// <c>AppearanceManager_Changed</c> handler) - so a saved change applies to
/// already-open screens immediately, no restart needed. Feature-gated per
/// <c>appearance.{create|edit|delete}</c>.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class AppearanceSettingsView : XtraUserControl
{
    private const string FeatureCode = "appearance";

    private readonly IServiceScope _scope;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public AppearanceSettingsView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _featurePolicy = _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
        _currentSession = currentSession;

        InitializeComponent();
    }

    private void AppearanceManager_Changed(object? sender, EventArgs e) => AppearanceManager.Apply(this, "Restaurant", nameof(AppearanceSettingsView));

    private async void AppearanceSettingsView_Load(object? sender, EventArgs e) => await RefreshAsync();

    private void GridView_FocusedRowChanged(object? sender, EventArgs e) => UpdateButtonStates();

    private async void GridView_DoubleClick(object? sender, EventArgs e) => await EditAsync();

    private async void NewButton_Click(object? sender, EventArgs e) => await CreateAsync();

    private async void EditButton_Click(object? sender, EventArgs e) => await EditAsync();

    private async void DeleteButton_Click(object? sender, EventArgs e) => await DeleteAsync();

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            AppearanceManager.Changed -= AppearanceManager_Changed;
            _scope.Dispose();
        }

        base.Dispose(disposing);
    }

    private async Task RefreshAsync()
    {
        AppearanceManager.Apply(this, "Restaurant", nameof(AppearanceSettingsView));

        _grid.DataSource = AppearanceManager.Rules.ToList();
        await UpdatePermissionsAsync();
        UpdateButtonStates();
    }

    private Task<bool> CanUseFeatureAsync(string operation) =>
        _currentSession.UserId is { } userId
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}")
            : Task.FromResult(false);

    private async Task UpdatePermissionsAsync()
    {
        _newButton.Enabled = await CanUseFeatureAsync("create");
        _editButton.Tag = await CanUseFeatureAsync("edit");
        _deleteButton.Tag = await CanUseFeatureAsync("delete");
    }

    private void UpdateButtonStates()
    {
        var hasFocusedRow = _gridView.GetFocusedRow() is AppearanceRule;
        _editButton.Enabled = hasFocusedRow && (_editButton.Tag as bool? ?? false);
        _deleteButton.Enabled = hasFocusedRow && (_deleteButton.Tag as bool? ?? false);
    }

    private async Task CreateAsync()
    {
        using var form = new AppearanceRuleEditForm("New Appearance Rule");
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var rules = AppearanceManager.Rules.ToList();
        rules.Add(form.BuildRule());
        AppearanceManager.Save(rules);

        await RefreshAsync();
    }

    private async Task EditAsync()
    {
        if (_gridView.GetFocusedRow() is not AppearanceRule existing)
        {
            return;
        }

        using var form = new AppearanceRuleEditForm("Edit Appearance Rule", existing);
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var rules = AppearanceManager.Rules.Where(r => r.RuleId != existing.RuleId).ToList();
        rules.Add(form.BuildRule());
        AppearanceManager.Save(rules);

        await RefreshAsync();
    }

    private async Task DeleteAsync()
    {
        if (_gridView.GetFocusedRow() is not AppearanceRule existing)
        {
            return;
        }

        var confirm = XtraMessageBox.Show(this, $"Delete the appearance rule for \"{existing.ScopeDescription}\"?", "Delete Rule", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        var rules = AppearanceManager.Rules.Where(r => r.RuleId != existing.RuleId).ToList();
        AppearanceManager.Save(rules);

        await RefreshAsync();
    }
}
