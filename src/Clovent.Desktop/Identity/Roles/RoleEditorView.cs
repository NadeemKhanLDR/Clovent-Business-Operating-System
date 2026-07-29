using Clovent.Desktop.MasterData;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Identity.Application.Permissions.Queries;
using Clovent.Identity.Application.Roles.Commands;
using Clovent.Identity.Application.Roles.Dtos;
using Clovent.Identity.Application.Roles.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Identity.Roles;

/// <summary>
/// Role Editor screen: create/rename roles and assign permissions to them
/// (via <see cref="RoleEditForm"/>'s embedded checklist) - this is also the
/// brief's "Permission Assignment" screen, since permissions are only ever
/// granted at the role level in this domain model. Feature-gated per
/// <c>roles.{operation}</c>. Activate/Deactivate has no meaning for a Role
/// (it has no lifecycle beyond existing), so those buttons stay hidden by
/// leaving <c>OnActivate</c>/<c>OnDeactivate</c> unset.
/// </summary>
public sealed class RoleEditorView : XtraUserControl
{
    private const string FeatureCode = "roles";

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;
    private readonly MasterDataListView<RoleDto> _listView;

    private IReadOnlyList<(Guid Id, string Code, string Description)> _permissions = [];

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public RoleEditorView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _featurePolicy = _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
        _currentSession = currentSession;

        Dock = DockStyle.Fill;

        _listView = new MasterDataListView<RoleDto>(
        [
            new MasterDataColumn(nameof(RoleDto.Name), "Name", 220),
            new MasterDataColumn(nameof(RoleDto.CreatedAtUtc), "Created (UTC)", 160),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = dto => dto.Name,
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
            OnNew = CreateAsync,
            OnEdit = EditAsync,
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

    private async Task<IReadOnlyList<RoleDto>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        var permissions = await _mediator.Send(new ListPermissionsQuery(), cancellationToken);
        _permissions = [.. permissions.Select(p => (p.PermissionId, p.Code, p.Description))];

        var roles = await _mediator.Send(new ListRolesQuery(), cancellationToken);
        return [.. roles];
    }

    private Task<bool> CanUseFeatureAsync(string operation) =>
        _currentSession.UserId is { } userId
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}")
            : Task.FromResult(false);

    private async Task CreateAsync()
    {
        using var form = new RoleEditForm("New Role");
        form.LoadPermissions(_permissions, []);

        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var created = await _mediator.Send(new CreateRoleCommand(form.RoleNameValue));
        foreach (var permissionId in form.SelectedPermissionIds)
        {
            await _mediator.Send(new AssignPermissionToRoleCommand(created.RoleId, permissionId));
        }
    }

    private async Task EditAsync(RoleDto dto)
    {
        using var form = new RoleEditForm("Edit Role", dto.Name);
        form.LoadPermissions(_permissions, dto.PermissionIds);

        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        await _mediator.Send(new RenameRoleCommand(dto.RoleId, form.RoleNameValue));

        foreach (var permissionId in form.SelectedPermissionIds.Except(dto.PermissionIds))
        {
            await _mediator.Send(new AssignPermissionToRoleCommand(dto.RoleId, permissionId));
        }

        foreach (var permissionId in dto.PermissionIds.Except(form.SelectedPermissionIds))
        {
            await _mediator.Send(new RemovePermissionFromRoleCommand(dto.RoleId, permissionId));
        }
    }
}
