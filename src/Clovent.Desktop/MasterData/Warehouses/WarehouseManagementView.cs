using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.MasterData.Application.Warehouses.Commands;
using Clovent.MasterData.Application.Warehouses.Dtos;
using Clovent.MasterData.Application.Warehouses.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.MasterData.Warehouses;

/// <summary>
/// Warehouse Management screen: search, filter, CRUD, activate/deactivate
/// over the warehouses belonging to a selected branch. Feature-gated per
/// <c>warehouses.{create|edit|activate|deactivate}</c>.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class WarehouseManagementView : XtraUserControl
{
    private const string FeatureCode = "warehouses";

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public WarehouseManagementView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
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

    private async Task<IReadOnlyList<WarehouseDto>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        if (_selector.SelectedBranchId is not { } branchId)
        {
            return [];
        }

        var items = await _mediator.Send(new ListWarehousesByBranchQuery(branchId), cancellationToken);
        return [.. items];
    }

    private Task<bool> CanUseFeatureAsync(string operation) =>
        _currentSession.UserId is { } userId
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}")
            : Task.FromResult(false);

    private async Task CreateAsync()
    {
        if (_selector.SelectedBranchId is not { } branchId)
        {
            XtraMessageBox.Show(this, "Select a branch first.", "No Branch Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var form = new WarehouseEditForm("New Warehouse");
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new CreateWarehouseCommand(branchId, form.WarehouseNameValue, form.CodeValue));
        }
    }

    private async Task EditAsync(WarehouseDto dto)
    {
        using var form = new WarehouseEditForm("Edit Warehouse", dto.Name, dto.Code, isNew: false);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new RenameWarehouseCommand(dto.WarehouseId, form.WarehouseNameValue));
        }
    }

    private async void Selector_SelectionChanged(object? sender, EventArgs e) => await _listView.RefreshAsync();

    private async void WarehouseManagementView_Load(object? sender, EventArgs e) => await _selector.LoadOrganizationsAsync();
}
