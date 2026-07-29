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
public sealed class UnitOfMeasureManagementView : XtraUserControl
{
    private const string FeatureCode = "units";

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;
    private readonly MasterDataListView<UnitOfMeasureDto> _listView;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public UnitOfMeasureManagementView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _featurePolicy = _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
        _currentSession = currentSession;

        Dock = DockStyle.Fill;

        _listView = new MasterDataListView<UnitOfMeasureDto>(
        [
            new MasterDataColumn(nameof(UnitOfMeasureDto.Code), "Code", 80),
            new MasterDataColumn(nameof(UnitOfMeasureDto.Name), "Name", 200),
            new MasterDataColumn(nameof(UnitOfMeasureDto.Status), "Status", 90),
            new MasterDataColumn(nameof(UnitOfMeasureDto.CreatedAtUtc), "Created (UTC)", 160),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = dto => $"{dto.Code} {dto.Name}",
            StatusSelector = dto => dto.Status,
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
            OnNew = CreateAsync,
            OnEdit = EditAsync,
            OnActivate = dto => _mediator.Send(new ActivateUnitOfMeasureCommand(dto.UnitOfMeasureId)),
            OnDeactivate = dto => _mediator.Send(new DeactivateUnitOfMeasureCommand(dto.UnitOfMeasureId)),
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
