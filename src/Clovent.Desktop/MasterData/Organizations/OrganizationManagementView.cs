using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Identity.Application.Organizations.Commands;
using Clovent.Identity.Application.Organizations.Dtos;
using Clovent.Identity.Application.Organizations.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.MasterData.Organizations;

/// <summary>
/// Organization Management screen: search, filter, CRUD, activate/deactivate
/// over every organization in the system - the top of the tenant hierarchy.
/// Built entirely on the shared <see cref="MasterDataListView{TDto}"/>
/// chrome. Feature-gated per <c>organizations.{create|edit|activate|deactivate}</c>.
/// </summary>
public sealed class OrganizationManagementView : XtraUserControl
{
    private const string FeatureCode = "organizations";

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;
    private readonly MasterDataListView<OrganizationDto> _listView;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public OrganizationManagementView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _featurePolicy = _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
        _currentSession = currentSession;

        Dock = DockStyle.Fill;

        _listView = new MasterDataListView<OrganizationDto>(
        [
            new MasterDataColumn(nameof(OrganizationDto.Name), "Name", 220),
            new MasterDataColumn(nameof(OrganizationDto.TaxId), "Tax Id", 140),
            new MasterDataColumn(nameof(OrganizationDto.Status), "Status", 90),
            new MasterDataColumn(nameof(OrganizationDto.CreatedAtUtc), "Created (UTC)", 160),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = dto => dto.Name,
            StatusSelector = dto => dto.Status,
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
            OnNew = CreateAsync,
            OnEdit = EditAsync,
            OnActivate = dto => _mediator.Send(new ActivateOrganizationCommand(dto.OrganizationId)),
            OnDeactivate = dto => _mediator.Send(new DeactivateOrganizationCommand(dto.OrganizationId)),
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

    private async Task<IReadOnlyList<OrganizationDto>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        var items = await _mediator.Send(new ListOrganizationsQuery(), cancellationToken);
        return [.. items];
    }

    private Task<bool> CanUseFeatureAsync(string operation) =>
        _currentSession.UserId is { } userId
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}")
            : Task.FromResult(false);

    private async Task CreateAsync()
    {
        using var form = new OrganizationEditForm("New Organization");
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new CreateOrganizationCommand(form.OrganizationName, form.TaxId));
        }
    }

    private async Task EditAsync(OrganizationDto dto)
    {
        using var form = new OrganizationEditForm("Edit Organization", dto.Name, dto.TaxId);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new RenameOrganizationCommand(dto.OrganizationId, form.OrganizationName));
            await _mediator.Send(new SetOrganizationTaxIdCommand(dto.OrganizationId, form.TaxId));
        }
    }
}
