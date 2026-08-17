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
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class DepartmentManagementView : XtraUserControl
{
    private const string FeatureCode = "departments";

    private readonly IServiceScope _scope;
    private readonly MediatR.IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public DepartmentManagementView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
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

    private async void Selector_SelectionChanged(object? sender, EventArgs e) => await _listView.RefreshAsync();

    private async void DepartmentManagementView_Load(object? sender, EventArgs e) => await _selector.LoadOrganizationsAsync();
}
