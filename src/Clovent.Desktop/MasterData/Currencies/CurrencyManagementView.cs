using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Identity.Application.Organizations.Queries;
using Clovent.MasterData.Application.Currencies.Commands;
using Clovent.MasterData.Application.Currencies.Dtos;
using Clovent.MasterData.Application.Currencies.Queries;
using Clovent.MasterData.Application.Languages.Queries;
using Clovent.MasterData.Application.Settings.Commands;
using Clovent.MasterData.Application.Settings.Queries;
using Clovent.MasterData.Application.TimeZones.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.MasterData.Currencies;

/// <summary>
/// Currency Management screen: search, filter, create, activate/deactivate
/// over the shared currency catalog - reference data, not scoped to any one
/// organization. Feature-gated per <c>currencies.{create|activate|deactivate}</c>.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class CurrencyManagementView : XtraUserControl
{
    private const string FeatureCode = "currencies";

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;
    private Guid? _defaultCurrencyId;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public CurrencyManagementView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _featurePolicy = _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
        _currentSession = currentSession;

        InitializeComponent();
        _listView.GridView.CustomColumnDisplayText += GridView_CustomColumnDisplayText;
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

    private void GridView_CustomColumnDisplayText(object? sender, DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventArgs e)
    {
        if (e.Column.FieldName == "IsDefault")
        {
            var row = _listView.GridView.GetRow(e.ListSourceRowIndex) as CurrencyDto;
            if (row != null && row.CurrencyId == _defaultCurrencyId)
            {
                e.DisplayText = "✓";
            }
            else
            {
                e.DisplayText = string.Empty;
            }
        }
    }

    private async Task<Guid?> TryGetDefaultCurrencyIdAsync()
    {
        try
        {
            var organizations = await _mediator.Send(new ListOrganizationsQuery());
            if (organizations.Count == 0) return null;
            var settings = await _mediator.Send(new GetBusinessSettingsByOrganizationQuery(organizations.First().OrganizationId));
            return settings.DefaultCurrencyId;
        }
        catch (Clovent.MasterData.Application.NotFoundException)
        {
            return null;
        }
    }

    private async Task<IReadOnlyList<CurrencyDto>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        var items = await _mediator.Send(new ListCurrenciesQuery(), cancellationToken);
        _defaultCurrencyId = await TryGetDefaultCurrencyIdAsync();
        return [.. items];
    }

    private Task<bool> CanUseFeatureAsync(string operation) =>
        _currentSession.UserId is { } userId
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}")
            : Task.FromResult(false);

    private async Task CreateAsync()
    {
        using var form = new CurrencyCreateForm();
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new CreateCurrencyCommand(form.Code, form.CurrencyNameValue, form.Symbol, form.DecimalPlaces));
        }
    }

    /// <summary>
    /// Makes the focused currency the organization's default via the
    /// existing <c>BusinessSettings.DefaultCurrencyId</c> mechanism (the same
    /// one the Business Settings screen's currency combo writes), so every
    /// monetary display - POS, receipts, End-of-Day - picks it up. If the
    /// organization has no settings record yet, one is created with defaults
    /// rather than blocking the administrator.
    /// </summary>
    private async Task SetAsDefaultAsync(CurrencyDto currency)
    {
        var organizations = await _mediator.Send(new ListOrganizationsQuery());
        if (organizations.Count == 0)
        {
            XtraMessageBox.Show(this, "No organization exists yet, so a default currency cannot be set.", "Set as Default", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var organizationId = organizations.First().OrganizationId;
        try
        {
            var settings = await _mediator.Send(new GetBusinessSettingsByOrganizationQuery(organizationId));
            await _mediator.Send(new UpdateBusinessSettingsCommand(
                settings.BusinessSettingsId,
                currency.CurrencyId,
                settings.DefaultLanguageId,
                settings.DefaultTimeZoneId,
                settings.DefaultFiscalYearId,
                settings.DateFormat));
        }
        catch (Clovent.MasterData.Application.NotFoundException)
        {
            var language = (await _mediator.Send(new ListLanguagesQuery())).First();
            var timeZone = (await _mediator.Send(new ListTimeZoneEntriesQuery())).First();
            await _mediator.Send(new CreateBusinessSettingsCommand(
                organizationId,
                currency.CurrencyId,
                language.LanguageId,
                timeZone.TimeZoneEntryId,
                "MM/dd/yyyy"));
        }

        // Re-configure display formatting immediately so already-open screens
        // (including this one) don't keep rendering the previous currency.
        await CurrencyDisplayLoader.ConfigureAsync(_mediator);
        _defaultCurrencyId = currency.CurrencyId;
        _listView.GridView.RefreshData();

        XtraMessageBox.Show(this, $"{currency.Code} ({currency.Name}) is now the default currency.", "Set as Default", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async void CurrencyManagementView_Load(object? sender, EventArgs e) => await _listView.RefreshAsync();
}
