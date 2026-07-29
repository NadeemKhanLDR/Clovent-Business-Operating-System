using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.MasterData.Application.Currencies.Commands;
using Clovent.MasterData.Application.Currencies.Dtos;
using Clovent.MasterData.Application.Currencies.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.MasterData.Currencies;

/// <summary>
/// Currency Management screen: search, filter, create, activate/deactivate
/// over the shared currency catalog - reference data, not scoped to any one
/// organization. Feature-gated per <c>currencies.{create|activate|deactivate}</c>.
/// </summary>
public sealed class CurrencyManagementView : XtraUserControl
{
    private const string FeatureCode = "currencies";

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;
    private readonly MasterDataListView<CurrencyDto> _listView;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public CurrencyManagementView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _featurePolicy = _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
        _currentSession = currentSession;

        Dock = DockStyle.Fill;

        _listView = new MasterDataListView<CurrencyDto>(
        [
            new MasterDataColumn(nameof(CurrencyDto.Code), "Code", 80),
            new MasterDataColumn(nameof(CurrencyDto.Name), "Name", 180),
            new MasterDataColumn(nameof(CurrencyDto.Symbol), "Symbol", 70),
            new MasterDataColumn(nameof(CurrencyDto.DecimalPlaces), "Decimals", 80),
            new MasterDataColumn(nameof(CurrencyDto.Status), "Status", 90),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = dto => $"{dto.Code} {dto.Name}",
            StatusSelector = dto => dto.Status,
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
            OnNew = CreateAsync,
            OnActivate = dto => _mediator.Send(new ActivateCurrencyCommand(dto.CurrencyId)),
            OnDeactivate = dto => _mediator.Send(new DeactivateCurrencyCommand(dto.CurrencyId)),
        };

        Controls.Add(_listView);
        Load += async (_, _) => await _listView.RefreshAsync();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _scope.Dispose();
        }

        base.Dispose(disposing);
    }

    private async Task<IReadOnlyList<CurrencyDto>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        var items = await _mediator.Send(new ListCurrenciesQuery(), cancellationToken);
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
}
