using Clovent.Catalog.Application.UnitsOfMeasure.Commands;
using Clovent.Catalog.Application.UnitsOfMeasure.Dtos;
using Clovent.Catalog.Application.UnitsOfMeasure.Queries;
using Clovent.Desktop.MasterData;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Catalog.UnitsOfMeasure;

/// <summary>
/// Unit Management screen: search, filter, CRUD, activate/deactivate over
/// the shared unit-of-measure catalog. Feature-gated per
/// <c>units.{create|edit|activate|deactivate}</c>.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class UnitOfMeasureManagementView : XtraUserControl
{
    private const string FeatureCode = "units";

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public UnitOfMeasureManagementView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _featurePolicy = _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
        _currentSession = currentSession;

        InitializeComponent();
    }

    private async void UnitOfMeasureManagementView_Load(object? sender, EventArgs e) => await _listView.RefreshAsync();

    private async Task<IReadOnlyList<UnitOfMeasureDto>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        var items = await _mediator.Send(new ListUnitsOfMeasureQuery(), cancellationToken);
        return [.. items];
    }

    private Task<bool> CanUseFeatureAsync(string operation) =>
        _currentSession.UserId is { } userId
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}")
            : Task.FromResult(false);

    private async Task CreateAsync()
    {
        using var form = new UnitOfMeasureEditForm("New Unit of Measure");
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new CreateUnitOfMeasureCommand(form.CodeValue, form.NameValue));
        }
    }

    private async Task EditAsync(UnitOfMeasureDto dto)
    {
        using var form = new UnitOfMeasureEditForm("Edit Unit of Measure", dto.Code, dto.Name, isNew: false);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new RenameUnitOfMeasureCommand(dto.UnitOfMeasureId, form.NameValue));
        }
    }
}
