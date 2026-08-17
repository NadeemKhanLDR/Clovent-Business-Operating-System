using Clovent.Desktop.Forms.Base;
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
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class TableManagementView : XtraUserControl
{
    private const string FeatureCode = "tables";

    private readonly IServiceScope _scope;
    private readonly ScreenOperationGate _gate = new();
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public TableManagementView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = new SerializedMediator(_scope.ServiceProvider.GetRequiredService<IMediator>(), _gate);
        _featurePolicy = new SerializedFeatureAuthorizationPolicy(_scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>(), _gate);
        _currentSession = currentSession;

        InitializeComponent();
    }

    private async void DiningAreaPicker_SelectionChanged(object? sender, EventArgs e) => await _listView.RefreshAsync();

    private async void TableManagementView_Load(object? sender, EventArgs e) => await LoadDiningAreasAsync();

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            _scope.Dispose();
            _gate.Dispose();
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
