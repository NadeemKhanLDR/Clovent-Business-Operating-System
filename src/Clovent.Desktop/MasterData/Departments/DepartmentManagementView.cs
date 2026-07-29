using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.MasterData.Application.Departments.Commands;
using Clovent.MasterData.Application.Departments.Dtos;
using Clovent.MasterData.Application.Departments.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.MasterData.Departments;

/// <summary>
/// Department Management screen: search, filter, CRUD, activate/deactivate
/// over the departments belonging to a selected branch (under a selected
/// organization and company). Feature-gated per <c>departments.{create|edit|activate|deactivate}</c>.
/// </summary>
public sealed class DepartmentManagementView : XtraUserControl
{
    private const string FeatureCode = "departments";

    private readonly IServiceScope _scope;
    private readonly MediatR.IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;
    private readonly OrganizationHierarchySelector _selector;
    private readonly MasterDataListView<DepartmentDto> _listView;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public DepartmentManagementView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
        _featurePolicy = _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
        _currentSession = currentSession;

        Dock = DockStyle.Fill;

        _listView = new MasterDataListView<DepartmentDto>(
        [
            new MasterDataColumn(nameof(DepartmentDto.Name), "Name", 220),
            new MasterDataColumn(nameof(DepartmentDto.Status), "Status", 90),
            new MasterDataColumn(nameof(DepartmentDto.CreatedAtUtc), "Created (UTC)", 160),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = dto => dto.Name,
            StatusSelector = dto => dto.Status,
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
            OnNew = CreateAsync,
            OnEdit = EditAsync,
            OnActivate = dto => _mediator.Send(new ActivateDepartmentCommand(dto.DepartmentId)),
            OnDeactivate = dto => _mediator.Send(new DeactivateDepartmentCommand(dto.DepartmentId)),
        };

        _selector = new OrganizationHierarchySelector(_mediator, showCompany: true, showBranch: true);
        _selector.SelectionChanged += async (_, _) => await _listView.RefreshAsync();

        Controls.Add(_listView);
        Controls.Add(_selector);
        Load += async (_, _) => await _selector.LoadOrganizationsAsync();
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

    private async Task<IReadOnlyList<DepartmentDto>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        if (_selector.SelectedBranchId is not { } branchId)
        {
            return [];
        }

        var items = await _mediator.Send(new ListDepartmentsByBranchQuery(branchId), cancellationToken);
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

        using var form = new DepartmentEditForm("New Department");
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new CreateDepartmentCommand(branchId, form.DepartmentNameValue));
        }
    }

    private async Task EditAsync(DepartmentDto dto)
    {
        using var form = new DepartmentEditForm("Edit Department", dto.Name);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new RenameDepartmentCommand(dto.DepartmentId, form.DepartmentNameValue));
        }
    }
}
