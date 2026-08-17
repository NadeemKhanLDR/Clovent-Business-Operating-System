using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.MasterData.Application;
using Clovent.MasterData.Application.Currencies.Dtos;
using Clovent.MasterData.Application.Currencies.Queries;
using Clovent.MasterData.Application.FiscalYears.Dtos;
using Clovent.MasterData.Application.FiscalYears.Queries;
using Clovent.MasterData.Application.Languages.Dtos;
using Clovent.MasterData.Application.Languages.Queries;
using Clovent.MasterData.Application.Settings.Commands;
using Clovent.MasterData.Application.Settings.Dtos;
using Clovent.MasterData.Application.Settings.Queries;
using Clovent.MasterData.Application.TimeZones.Dtos;
using Clovent.MasterData.Application.TimeZones.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.MasterData.Settings;

/// <summary>
/// Business Settings screen: one record per organization (its default
/// currency/language/time zone/fiscal year and date format). Unlike every
/// other Milestone 13 screen this is a single-record form, not a grid - see
/// <c>Clovent.MasterData.Settings.BusinessSettings</c>'s doc comment for why
/// exactly one record exists per organization. Language and Time Zone have
/// no dedicated management screens of their own (per this milestone's
/// scope), so their catalogs are surfaced here only as read-only lookups.
/// Feature-gated per <c>businesssettings.edit</c>.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class BusinessSettingsManagementView : XtraUserControl
{
    private const string FeatureCode = "businesssettings";

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;

    private readonly Dictionary<string, Guid> _currenciesByDisplay = [];
    private readonly Dictionary<string, Guid> _languagesByDisplay = [];
    private readonly Dictionary<string, Guid> _timeZonesByDisplay = [];
    private readonly Dictionary<string, Guid> _fiscalYearsByDisplay = [];
    private const string NoFiscalYear = "(none)";

    private BusinessSettingsDto? _existingSettings;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public BusinessSettingsManagementView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _featurePolicy = _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
        _currentSession = currentSession;

        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _scope.Dispose();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private async Task LoadLookupsAsync()
    {
        var currencies = await _mediator.Send(new ListCurrenciesQuery());
        PopulateCombo(_currencyCombo, _currenciesByDisplay, currencies, c => $"{c.Code} - {c.Name}", c => c.CurrencyId);

        var languages = await _mediator.Send(new ListLanguagesQuery());
        PopulateCombo(_languageCombo, _languagesByDisplay, languages, l => $"{l.Code} - {l.Name}", l => l.LanguageId);

        var timeZones = await _mediator.Send(new ListTimeZoneEntriesQuery());
        PopulateCombo(_timeZoneCombo, _timeZonesByDisplay, timeZones, t => t.DisplayName, t => t.TimeZoneEntryId);
    }

    private static void PopulateCombo<T>(
        ComboBoxEdit combo, Dictionary<string, Guid> byDisplay, IReadOnlyCollection<T> items, Func<T, string> display, Func<T, Guid> id)
    {
        byDisplay.Clear();
        combo.Properties.Items.Clear();
        foreach (var item in items)
        {
            var text = display(item);
            byDisplay[text] = id(item);
            combo.Properties.Items.Add(text);
        }

        if (combo.Properties.Items.Count > 0)
        {
            combo.SelectedIndex = 0;
        }
    }

    private async Task LoadForSelectedOrganizationAsync()
    {
        if (_selector.SelectedOrganizationId is not { } organizationId)
        {
            _existingSettings = null;
            _statusLabel.Text = string.Empty;
            return;
        }

        var fiscalYears = await _mediator.Send(new ListFiscalYearsByOrganizationQuery(organizationId));
        _fiscalYearsByDisplay.Clear();
        _fiscalYearCombo.Properties.Items.Clear();
        _fiscalYearCombo.Properties.Items.Add(NoFiscalYear);
        foreach (var fiscalYear in fiscalYears)
        {
            _fiscalYearsByDisplay[fiscalYear.Name] = fiscalYear.FiscalYearId;
            _fiscalYearCombo.Properties.Items.Add(fiscalYear.Name);
        }

        _fiscalYearCombo.SelectedIndex = 0;

        try
        {
            _existingSettings = await _mediator.Send(new GetBusinessSettingsByOrganizationQuery(organizationId));
            SelectComboValue(_currencyCombo, _currenciesByDisplay, _existingSettings.DefaultCurrencyId);
            SelectComboValue(_languageCombo, _languagesByDisplay, _existingSettings.DefaultLanguageId);
            SelectComboValue(_timeZoneCombo, _timeZonesByDisplay, _existingSettings.DefaultTimeZoneId);
            if (_existingSettings.DefaultFiscalYearId is { } fiscalYearId)
            {
                SelectComboValue(_fiscalYearCombo, _fiscalYearsByDisplay, fiscalYearId);
            }

            _dateFormatEdit.Text = _existingSettings.DateFormat;
            _statusLabel.Text = "Loaded existing settings.";
        }
        catch (NotFoundException)
        {
            _existingSettings = null;
            _dateFormatEdit.Text = "MM/dd/yyyy";
            _statusLabel.Text = "No settings yet for this organization - Save to create them.";
        }
    }

    private static void SelectComboValue(ComboBoxEdit combo, Dictionary<string, Guid> byDisplay, Guid value)
    {
        foreach (var pair in byDisplay)
        {
            if (pair.Value == value)
            {
                combo.SelectedItem = pair.Key;
                return;
            }
        }
    }

    private Task<bool> CanUseFeatureAsync(string operation) =>
        _currentSession.UserId is { } userId
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}")
            : Task.FromResult(false);

    private async Task SaveAsync()
    {
        if (_selector.SelectedOrganizationId is not { } organizationId)
        {
            XtraMessageBox.Show(this, "Select an organization first.", "No Organization Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!await CanUseFeatureAsync("edit"))
        {
            XtraMessageBox.Show(this, "You do not have permission to edit business settings.", "Not Authorized", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_currencyCombo.SelectedItem is not string currencyDisplay || !_currenciesByDisplay.TryGetValue(currencyDisplay, out var currencyId) ||
            _languageCombo.SelectedItem is not string languageDisplay || !_languagesByDisplay.TryGetValue(languageDisplay, out var languageId) ||
            _timeZoneCombo.SelectedItem is not string timeZoneDisplay || !_timeZonesByDisplay.TryGetValue(timeZoneDisplay, out var timeZoneId))
        {
            XtraMessageBox.Show(this, "Select a currency, language, and time zone.", "Incomplete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Guid? fiscalYearId = _fiscalYearCombo.SelectedItem is string fiscalYearDisplay && _fiscalYearsByDisplay.TryGetValue(fiscalYearDisplay, out var fyId)
            ? fyId
            : null;

        var dateFormat = _dateFormatEdit.Text.Trim();

        if (_existingSettings is null)
        {
            _existingSettings = await _mediator.Send(new CreateBusinessSettingsCommand(organizationId, currencyId, languageId, timeZoneId, dateFormat));
            if (fiscalYearId is not null)
            {
                _existingSettings = await _mediator.Send(new UpdateBusinessSettingsCommand(
                    _existingSettings.BusinessSettingsId, currencyId, languageId, timeZoneId, fiscalYearId, dateFormat));
            }
        }
        else
        {
            _existingSettings = await _mediator.Send(new UpdateBusinessSettingsCommand(
                _existingSettings.BusinessSettingsId, currencyId, languageId, timeZoneId, fiscalYearId, dateFormat));
        }

        _statusLabel.Text = "Saved.";
    }

    private async void Selector_SelectionChanged(object? sender, EventArgs e) => await LoadForSelectedOrganizationAsync();

    private async void SaveButton_Click(object? sender, EventArgs e) => await SaveAsync();

    private async void BusinessSettingsManagementView_Load(object? sender, EventArgs e)
    {
        await LoadLookupsAsync();
        await _selector.LoadOrganizationsAsync();
    }
}
