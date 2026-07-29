using Clovent.Desktop.MasterData;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Restaurant.Application.DiningAreas.Queries;
using Clovent.Restaurant.Application.Tables.Commands;
using Clovent.Restaurant.Application.Tables.Dtos;
using Clovent.Restaurant.Application.Tables.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Restaurant.Tables;

/// <summary>
/// Table Management screen: search, filter, CRUD, activate/deactivate, and
/// floor-state control (Occupy/Vacate/Reserve/Set Out Of Service/Return To
/// Service) over the tables of a selected dining area. Feature-gated per
/// <c>tables.{create|edit|activate|deactivate|occupy|vacate|reserve|outofservice|returntoservice}</c>.
/// </summary>
public sealed class TableManagementView : XtraUserControl
{
    private const string FeatureCode = "tables";

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;
    private readonly EntityPicker _diningAreaPicker = new("Dining Area:");
    private readonly MasterDataListView<TableDto> _listView;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public TableManagementView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _featurePolicy = _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
        _currentSession = currentSession;

        Dock = DockStyle.Fill;

        _listView = new MasterDataListView<TableDto>(
        [
            new MasterDataColumn(nameof(TableDto.Code), "Code", 90),
            new MasterDataColumn(nameof(TableDto.Capacity), "Capacity", 80),
            new MasterDataColumn(nameof(TableDto.OccupancyStatus), "Occupancy", 100),
            new MasterDataColumn(nameof(TableDto.Status), "Status", 90),
            new MasterDataColumn(nameof(TableDto.CreatedAtUtc), "Created (UTC)", 160),
        ],
        [
            new MasterDataListAction<TableDto>("Occupy", dto => _mediator.Send(new OccupyTableCommand(dto.TableId)),
                dto => dto.OccupancyStatus is "Available" or "Reserved", "occupy"),
            new MasterDataListAction<TableDto>("Vacate", dto => _mediator.Send(new VacateTableCommand(dto.TableId)),
                dto => dto.OccupancyStatus is "Occupied", "vacate"),
            new MasterDataListAction<TableDto>("Reserve", dto => _mediator.Send(new ReserveTableCommand(dto.TableId)),
                dto => dto.OccupancyStatus is "Available", "reserve"),
            new MasterDataListAction<TableDto>("Out of Service", dto => _mediator.Send(new SetTableOutOfServiceCommand(dto.TableId)),
                dto => dto.OccupancyStatus is "Available", "outofservice"),
            new MasterDataListAction<TableDto>("Return to Service", dto => _mediator.Send(new ReturnTableToServiceCommand(dto.TableId)),
                dto => dto.OccupancyStatus is "OutOfService", "returntoservice"),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = dto => dto.Code,
            StatusSelector = dto => dto.Status,
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
            OnNew = CreateAsync,
            OnEdit = EditAsync,
            OnActivate = dto => _mediator.Send(new ActivateTableCommand(dto.TableId)),
            OnDeactivate = dto => _mediator.Send(new DeactivateTableCommand(dto.TableId)),
        };

        _diningAreaPicker.SelectionChanged += async (_, _) => await _listView.RefreshAsync();

        Controls.Add(_listView);
        Controls.Add(_diningAreaPicker);
        Load += async (_, _) => await LoadDiningAreasAsync();
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

    private async Task LoadDiningAreasAsync()
    {
        var areas = await _mediator.Send(new ListAllDiningAreasQuery());
        _diningAreaPicker.LoadItems([.. areas.Select(a => (a.DiningAreaId, a.Name))]);
    }

    private async Task<IReadOnlyList<TableDto>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        if (_diningAreaPicker.SelectedId is not { } diningAreaId)
        {
            return [];
        }

        var items = await _mediator.Send(new ListTablesByDiningAreaQuery(diningAreaId), cancellationToken);
        return [.. items];
    }

    private Task<bool> CanUseFeatureAsync(string operation) =>
        _currentSession.UserId is { } userId
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}")
            : Task.FromResult(false);

    private async Task CreateAsync()
    {
        if (_diningAreaPicker.SelectedId is not { } diningAreaId)
        {
            XtraMessageBox.Show(this, "Select a dining area first.", "No Dining Area Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var form = new TableEditForm("New Table");
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new CreateTableCommand(diningAreaId, form.CodeValue, form.CapacityValue));
        }
    }

    private async Task EditAsync(TableDto dto)
    {
        using var form = new TableEditForm("Edit Table", dto.Code, dto.Capacity, isNew: false);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new SetTableCapacityCommand(dto.TableId, form.CapacityValue));
        }
    }
}
