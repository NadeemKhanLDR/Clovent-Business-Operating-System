using Clovent.Desktop.MasterData;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Restaurant.Application.DiningAreas.Commands;
using Clovent.Restaurant.Application.DiningAreas.Dtos;
using Clovent.Restaurant.Application.DiningAreas.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Restaurant.DiningAreas;

/// <summary>
/// Dining Area Management screen: search, filter, CRUD, activate/deactivate
/// over the dining areas belonging to a selected branch - the parent every
/// <see cref="Clovent.Restaurant.Tables.Table"/> is grouped under (e.g.
/// "Main Hall", "Patio", "Bar"). Feature-gated per
/// <c>diningareas.{create|edit|activate|deactivate}</c>.
/// </summary>
public sealed class DiningAreaManagementView : XtraUserControl
{
    private const string FeatureCode = "diningareas";

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;
    private readonly OrganizationHierarchySelector _selector;
    private readonly MasterDataListView<DiningAreaDto> _listView;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public DiningAreaManagementView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _featurePolicy = _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
        _currentSession = currentSession;

        Dock = DockStyle.Fill;

        _listView = new MasterDataListView<DiningAreaDto>(
        [
            new MasterDataColumn(nameof(DiningAreaDto.Name), "Name", 220),
            new MasterDataColumn(nameof(DiningAreaDto.Status), "Status", 90),
            new MasterDataColumn(nameof(DiningAreaDto.CreatedAtUtc), "Created (UTC)", 160),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = dto => dto.Name,
            StatusSelector = dto => dto.Status,
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
            OnNew = CreateAsync,
            OnEdit = EditAsync,
            OnActivate = dto => _mediator.Send(new ActivateDiningAreaCommand(dto.DiningAreaId)),
            OnDeactivate = dto => _mediator.Send(new DeactivateDiningAreaCommand(dto.DiningAreaId)),
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

    private async Task<IReadOnlyList<DiningAreaDto>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        if (_selector.SelectedBranchId is not { } branchId)
        {
            return [];
        }

        var items = await _mediator.Send(new ListDiningAreasByBranchQuery(branchId), cancellationToken);
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

        using var form = new DiningAreaEditForm("New Dining Area");
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new CreateDiningAreaCommand(branchId, form.NameValue));
        }
    }

    private async Task EditAsync(DiningAreaDto dto)
    {
        using var form = new DiningAreaEditForm("Edit Dining Area", dto.Name);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new RenameDiningAreaCommand(dto.DiningAreaId, form.NameValue));
        }
    }
}
