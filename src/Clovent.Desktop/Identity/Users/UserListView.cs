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
using Clovent.Authentication.Application.Credentials.Commands;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Identity.Users;

/// <summary>
/// User Administration screen: search, filter, CRUD, activate/deactivate,
/// reset password, unlock, assign company/branch/role. Built on the shared
/// <see cref="MasterDataListView{TDto}"/> chrome, same as every other
/// master-data screen. Feature-gated per <c>users.{operation}</c>.
/// </summary>
public sealed class UserListView : XtraUserControl
{
    private const string FeatureCode = "users";

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;
    private readonly MasterDataListView<UserRow> _listView;

    private IReadOnlyList<(Guid Id, string Name)> _companies = [];
    private IReadOnlyList<(Guid Id, string Name)> _branches = [];
    private IReadOnlyList<(Guid Id, string Name)> _roles = [];

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public UserListView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _featurePolicy = _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
        _currentSession = currentSession;

        Dock = DockStyle.Fill;

        _listView = new MasterDataListView<UserRow>(
        [
            new MasterDataColumn(nameof(UserRow.UserName), "Username", 130),
            new MasterDataColumn(nameof(UserRow.DisplayName), "Display Name", 180),
            new MasterDataColumn(nameof(UserRow.Email), "Email", 200),
            new MasterDataColumn(nameof(UserRow.Status), "Status", 100),
            new MasterDataColumn(nameof(UserRow.CompanyName), "Company", 150),
            new MasterDataColumn(nameof(UserRow.BranchName), "Branch", 150),
        ],
        [
            new MasterDataListAction<UserRow>("Reset Password", ResetPasswordAsync, FeatureOperation: "resetpassword"),
            new MasterDataListAction<UserRow>("Unlock", UnlockAsync, row => row.Status == "Locked", FeatureOperation: "unlock"),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = row => $"{row.UserName} {row.DisplayName} {row.Email}",
            StatusSelector = row => row.Status,
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
            OnNew = CreateAsync,
            OnEdit = EditAsync,
            OnActivate = row => _mediator.Send(new ActivateUserCommand(row.UserId)),
            OnDeactivate = row => _mediator.Send(new DeactivateUserCommand(row.UserId)),
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
    /// build flat lookup lists - the same shape
    /// <see cref="MasterData.OrganizationHierarchySelector"/> already walks,
    /// just flattened instead of cascading, since a user's Company/Branch
    /// picker isn't scoped to one organization at a time.
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

    private Task UnlockAsync(UserRow row) => _mediator.Send(new UnlockUserCommand(row.UserId));

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
