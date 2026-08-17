using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.MasterData;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Identity.Application.Permissions.Queries;
using Clovent.Identity.Application.Roles.Commands;
using Clovent.Identity.Application.Roles.Dtos;
using Clovent.Identity.Application.Roles.Queries;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Forms.Identity.Roles;

/// <summary>
/// Role Editor screen: create/rename roles and assign permissions to them
/// (via <see cref="RoleEditForm"/>'s embedded checklist) - also the
/// "Permission Assignment" screen, since permissions are only ever granted
/// at the role level in this domain model. Feature-gated per
/// <c>roles.{operation}</c>. Activate/Deactivate has no meaning for a Role
/// (it has no lifecycle beyond existing), so neither button exists on this
/// screen's toolbar. Replaces the old <c>RoleEditorView</c>'s generic
/// <see cref="MasterDataListView{TDto}"/> chrome with a hand-authored
/// grid+toolbar (see <c>RolesForm.Designer.cs</c>) - every <c>IMediator</c>
/// call and handler body below is unchanged from that predecessor.
/// </summary>
public sealed partial class RolesForm : BaseForm
{
    private const string FeatureCode = "roles";

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;

    private IReadOnlyList<RoleDto> _allItems = [];
    private IReadOnlyList<(Guid Id, string Code, string Description)> _permissions = [];

    /// <summary>Design-time-only constructor for the Visual Studio WinForms Designer - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public RolesForm()
    {
        _scope = null!;
        _mediator = null!;
        _featurePolicy = null!;
        _currentSession = null!;

        InitializeComponent();
    }

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public RolesForm(IServiceScopeFactory scopeFactory, ICurrentSession currentSession) : base()
    {
        InitializeComponent();

        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
        {
            _scope = null!;
            _mediator = null!;
            _featurePolicy = null!;
            _currentSession = null!;
            return;
        }

        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _featurePolicy = _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
        _currentSession = currentSession;
    }

    /// <inheritdoc/>
    public override string? PermissionKey => FeatureCode;

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _scope.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>Reloads every row, re-applies the current search filter, and re-evaluates feature permissions. Called once by <c>MainForm</c> when this document opens, and again on F5/the Refresh button.</summary>
    public override async Task RefreshAsync()
    {
        await RunBusyAsync(async () =>
        {
            _allItems = await LoadItemsAsync(CancellationToken.None);
            ApplyFilter();
            await UpdateFeaturePermissionsAsync();
            UpdateButtonStates();
        });
    }

    private void TxtSearch_EditValueChanged(object? sender, EventArgs e) => ApplyFilter();

    private void GridView_FocusedRowChanged(object? sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e) => UpdateButtonStates();

    private async void BtnNew_Click(object? sender, EventArgs e)
    {
        await CreateAsync();
        await RefreshAsync();
    }

    private async void BtnEdit_Click(object? sender, EventArgs e)
    {
        if (GetFocusedItem() is { } item)
        {
            await EditAsync(item);
            await RefreshAsync();
        }
    }

    private async void BtnRefresh_Click(object? sender, EventArgs e) => await RefreshAsync();

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

    private void ApplyFilter()
    {
        var visible = MasterDataFilter.Apply(_allItems, txtSearch.Text, dto => dto.Name);
        gridControl.DataSource = visible.ToList();
        StatusText = $"{visible.Count} of {_allItems.Count} record(s)";
        UpdateButtonStates();
    }

    private async Task UpdateFeaturePermissionsAsync()
    {
        btnNew.Enabled = await CanUseFeatureAsync("create");
        btnEdit.Tag = await CanUseFeatureAsync("edit");
    }

    private void UpdateButtonStates()
    {
        var focused = GetFocusedItem();
        var hasFocusedRow = focused is not null;

        btnEdit.Enabled = MasterDataFilter.CanEdit(hasFocusedRow, btnEdit.Tag as bool?, true);
    }

    private RoleDto? GetFocusedItem() => gridView.GetFocusedRow() as RoleDto;
}
