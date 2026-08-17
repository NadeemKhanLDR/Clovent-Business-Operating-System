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
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class CurrencyManagementView : XtraUserControl
{
    private const string FeatureCode = "currencies";

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public CurrencyManagementView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
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

    private async void CurrencyManagementView_Load(object? sender, EventArgs e) => await _listView.RefreshAsync();
}
