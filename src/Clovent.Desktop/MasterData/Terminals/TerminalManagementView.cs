using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.MasterData.Application.Terminals.Commands;
using Clovent.MasterData.Application.Terminals.Dtos;
using Clovent.MasterData.Application.Terminals.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.MasterData.Terminals;

/// <summary>
/// Terminal Management screen: search, filter, CRUD, activate/deactivate
/// over the terminals belonging to a selected branch. Feature-gated per
/// <c>terminals.{create|edit|activate|deactivate}</c>.
/// </summary>
public sealed class TerminalManagementView : XtraUserControl
{
    private const string FeatureCode = "terminals";

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;
    private readonly OrganizationHierarchySelector _selector;
    private readonly MasterDataListView<TerminalDto> _listView;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public TerminalManagementView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _featurePolicy = _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
        _currentSession = currentSession;

        Dock = DockStyle.Fill;

        _listView = new MasterDataListView<TerminalDto>(
        [
            new MasterDataColumn(nameof(TerminalDto.Name), "Name", 200),
            new MasterDataColumn(nameof(TerminalDto.Code), "Code", 100),
            new MasterDataColumn(nameof(TerminalDto.Status), "Status", 90),
            new MasterDataColumn(nameof(TerminalDto.CreatedAtUtc), "Created (UTC)", 160),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = dto => dto.Name,
            StatusSelector = dto => dto.Status,
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
            OnNew = CreateAsync,
            OnEdit = EditAsync,
            OnActivate = dto => _mediator.Send(new ActivateTerminalCommand(dto.TerminalId)),
            OnDeactivate = dto => _mediator.Send(new DeactivateTerminalCommand(dto.TerminalId)),
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

    private async Task<IReadOnlyList<TerminalDto>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        if (_selector.SelectedBranchId is not { } branchId)
        {
            return [];
        }

        var items = await _mediator.Send(new ListTerminalsByBranchQuery(branchId), cancellationToken);
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

        using var form = new TerminalEditForm("New Terminal");
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new CreateTerminalCommand(branchId, form.TerminalNameValue, form.CodeValue));
        }
    }

    private async Task EditAsync(TerminalDto dto)
    {
        using var form = new TerminalEditForm("Edit Terminal", dto.Name, dto.Code, isNew: false);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new RenameTerminalCommand(dto.TerminalId, form.TerminalNameValue));
        }
    }
}
