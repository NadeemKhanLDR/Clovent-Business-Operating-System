using Clovent.Authentication.Application.Credentials.Commands;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Identity.Users;
using Clovent.Desktop.MasterData;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Identity.Application.Branches.Queries;
using Clovent.Identity.Application.Companies.Queries;
using Clovent.Identity.Application.Organizations.Queries;
using Clovent.Identity.Application.Roles.Queries;
using Clovent.Identity.Application.Users.Commands;
using Clovent.Identity.Application.Users.Dtos;
using Clovent.Identity.Application.Users.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Forms.Identity.Users;

/// <summary>
/// User Administration screen: search, filter, CRUD, activate/deactivate,
/// reset password, unlock, assign company/branch/role. Feature-gated per
/// <c>users.{operation}</c>. Replaces the old <c>UserListView</c>'s generic
/// <see cref="MasterDataListView{TDto}"/> chrome with a hand-authored
/// grid+toolbar (see <c>UsersForm.Designer.cs</c>) - every <c>IMediator</c>
/// call and handler body below is unchanged from that predecessor, only
/// where the controls are declared changed, per this app's "no
/// runtime-constructed UI" policy for converted screens (see
/// <c>docs/architecture/DesktopShellArchitecture.md</c>).
/// </summary>
public sealed partial class UsersForm : BaseForm
{
    private const string FeatureCode = "users";

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;

    private IReadOnlyList<UserRow> _allItems = [];
    private IReadOnlyList<(Guid Id, string Name)> _companies = [];
    private IReadOnlyList<(Guid Id, string Name)> _branches = [];
    private IReadOnlyList<(Guid Id, string Name)> _roles = [];

    /// <summary>Design-time-only constructor for the Visual Studio WinForms Designer - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public UsersForm()
    {
        _scope = null!;
        _mediator = null!;
        _featurePolicy = null!;
        _currentSession = null!;

        InitializeComponent();
    }

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public UsersForm(IServiceScopeFactory scopeFactory, ICurrentSession currentSession) : base()
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

    private async void BtnActivate_Click(object? sender, EventArgs e)
    {
        if (GetFocusedItem() is { } item)
        {
            await _mediator.Send(new ActivateUserCommand(item.UserId));
            await RefreshAsync();
        }
    }

    private async void BtnDeactivate_Click(object? sender, EventArgs e)
    {
        if (GetFocusedItem() is { } item)
        {
            await _mediator.Send(new DeactivateUserCommand(item.UserId));
            await RefreshAsync();
        }
    }

    private async void BtnResetPassword_Click(object? sender, EventArgs e)
    {
        if (GetFocusedItem() is { } item)
        {
            await ResetPasswordAsync(item);
        }
    }

    private async void BtnUnlock_Click(object? sender, EventArgs e)
    {
        if (GetFocusedItem() is { } item)
        {
            await _mediator.Send(new UnlockUserCommand(item.UserId));
            await RefreshAsync();
        }
    }

    private async void BtnRefresh_Click(object? sender, EventArgs e) => await RefreshAsync();

    private async Task<IReadOnlyList<UserRow>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        await LoadLookupsAsync(cancellationToken);

        var users = await _mediator.Send(new SearchUsersQuery(), cancellationToken);
        var companyNames = _companies.ToDictionary(c => c.Id, c => c.Name);
        var branchNames = _branches.ToDictionary(b => b.Id, b => b.Name);

        return
        [
            .. users.Select(u => new UserRow(
                u.UserId,
                u.UserName,
                u.DisplayName,
                u.Email,
                u.Status,
                u.CompanyId is { } companyId && companyNames.TryGetValue(companyId, out var companyName) ? companyName : string.Empty,
                u.BranchId is { } branchId && branchNames.TryGetValue(branchId, out var branchName) ? branchName : string.Empty,
                u)),
        ];
    }

    /// <summary>
    /// Walks every Organization -&gt; Company -&gt; Branch once per refresh to
    /// build flat lookup lists - a user's Company/Branch picker isn't scoped
    /// to one organization at a time, unlike <see cref="MasterData.OrganizationHierarchySelector"/>.
    /// </summary>
    private async Task LoadLookupsAsync(CancellationToken cancellationToken)
    {
        var organizations = await _mediator.Send(new ListOrganizationsQuery(), cancellationToken);

        var companies = new List<(Guid Id, string Name)>();
        foreach (var organization in organizations)
        {
            var orgCompanies = await _mediator.Send(new ListCompaniesByOrganizationQuery(organization.OrganizationId), cancellationToken);
            companies.AddRange(orgCompanies.Select(c => (c.CompanyId, c.Name)));
        }

        var branches = new List<(Guid Id, string Name)>();
        foreach (var (companyId, _) in companies)
        {
            var companyBranches = await _mediator.Send(new ListBranchesByCompanyQuery(companyId), cancellationToken);
            branches.AddRange(companyBranches.Select(b => (b.BranchId, b.Name)));
        }

        _companies = companies;
        _branches = branches;
        _roles = [.. (await _mediator.Send(new ListRolesQuery(), cancellationToken)).Select(r => (r.RoleId, r.Name))];
    }

    private Task<bool> CanUseFeatureAsync(string operation) =>
        _currentSession.UserId is { } userId
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}")
            : Task.FromResult(false);

    private async Task CreateAsync()
    {
        using var form = new UserEditForm("New User", isNew: true);
        form.LoadCompanies(_companies, null);
        form.LoadBranches(_branches, null);
        form.LoadRoles(_roles, []);

        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var created = await _mediator.Send(new CreateUserCommand(form.EmailValue, form.UserNameValue, form.DisplayNameValue));
        await ApplyAssignmentsAsync(created.UserId, [], form);
    }

    private async Task EditAsync(UserRow row)
    {
        using var form = new UserEditForm("Edit User", isNew: false, row.Email, row.UserName, row.DisplayName);
        form.LoadCompanies(_companies, row.Source.CompanyId);
        form.LoadBranches(_branches, row.Source.BranchId);
        form.LoadRoles(_roles, row.Source.RoleIds);

        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        await _mediator.Send(new UpdateUserCommand(row.UserId, form.DisplayNameValue));
        await ApplyAssignmentsAsync(row.UserId, row.Source.RoleIds, form);
    }

    private async Task ApplyAssignmentsAsync(Guid userId, IReadOnlyCollection<Guid> previousRoleIds, UserEditForm form)
    {
        if (form.SelectedCompanyId is { } companyId)
        {
            await _mediator.Send(new AssignUserCompanyCommand(userId, companyId));
        }

        if (form.SelectedBranchId is { } branchId)
        {
            await _mediator.Send(new AssignUserBranchCommand(userId, branchId));
        }

        foreach (var roleId in form.SelectedRoleIds.Except(previousRoleIds))
        {
            await _mediator.Send(new AssignUserToRoleCommand(userId, roleId));
        }

        foreach (var roleId in previousRoleIds.Except(form.SelectedRoleIds))
        {
            await _mediator.Send(new RemoveUserFromRoleCommand(userId, roleId));
        }
    }

    private async Task ResetPasswordAsync(UserRow row)
    {
        using var form = new PasswordPromptForm($"Reset Password - {row.UserName}", requireCurrentPassword: false);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new ResetPasswordCommand(row.UserId, form.NewPassword));
            XtraMessageBox.Show(this, "Password has been reset.", "Reset Password", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void ApplyFilter()
    {
        var visible = MasterDataFilter.Apply(_allItems, txtSearch.Text, row => $"{row.UserName} {row.DisplayName} {row.Email}");
        gridControl.DataSource = visible.ToList();
        StatusText = $"{visible.Count} of {_allItems.Count} record(s)";
        UpdateButtonStates();
    }

    private async Task UpdateFeaturePermissionsAsync()
    {
        btnNew.Enabled = await CanUseFeatureAsync("create");
        btnEdit.Tag = await CanUseFeatureAsync("edit");
        btnActivate.Tag = await CanUseFeatureAsync("activate");
        btnDeactivate.Tag = await CanUseFeatureAsync("deactivate");
        btnResetPassword.Tag = await CanUseFeatureAsync("resetpassword");
        btnUnlock.Tag = await CanUseFeatureAsync("unlock");
    }

    private void UpdateButtonStates()
    {
        var focused = GetFocusedItem();
        var hasFocusedRow = focused is not null;
        var status = focused?.Status;

        btnEdit.Enabled = MasterDataFilter.CanEdit(hasFocusedRow, btnEdit.Tag as bool?, true);
        btnActivate.Enabled = MasterDataFilter.CanActivate(hasFocusedRow, btnActivate.Tag as bool?, status, true);
        btnDeactivate.Enabled = MasterDataFilter.CanDeactivate(hasFocusedRow, btnDeactivate.Tag as bool?, status, true);
        btnResetPassword.Enabled = hasFocusedRow && (btnResetPassword.Tag as bool? ?? true);
        btnUnlock.Enabled = hasFocusedRow && (btnUnlock.Tag as bool? ?? true) && status == "Locked";
    }

    private UserRow? GetFocusedItem() => gridView.GetFocusedRow() as UserRow;

    private sealed record UserRow(
        Guid UserId,
        string UserName,
        string DisplayName,
        string Email,
        string Status,
        string CompanyName,
        string BranchName,
        UserDto Source);
}
