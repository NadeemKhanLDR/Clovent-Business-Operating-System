using Clovent.Platform.Execution;
using Xunit;

namespace Clovent.Platform.Tests.Execution;

public sealed class AuthoritativeScopeResolverTests
{
    private readonly FakeTenancyHierarchyProvider _hierarchy = new();

    [Fact]
    public async Task ResolveScope_AuthorizedConsistentScope_SucceedsWithContext()
    {
        var orgId = Guid.NewGuid();
        var compId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var termId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _hierarchy.AddOrganization(orgId, true);
        _hierarchy.AddCompany(compId, orgId, true);
        _hierarchy.AddBranch(branchId, compId, true);
        _hierarchy.AddTerminal(termId, branchId, true);
        _hierarchy.AddUser(userId, compId, branchId, true, false);

        var resolver = new AuthoritativeScopeResolver(_hierarchy);
        var request = new ScopeResolutionRequest(userId, termId, branchId, compId, orgId);

        var result = await resolver.ResolveAndValidateScopeAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Context);
        Assert.Equal(userId, result.Context.UserId);
        Assert.Equal(orgId, result.Context.OrganizationId);
        Assert.Equal(compId, result.Context.CompanyId);
        Assert.Equal(branchId, result.Context.BranchId);
    }

    [Fact]
    public async Task ResolveScope_UserNotFound_Fails()
    {
        var resolver = new AuthoritativeScopeResolver(_hierarchy);
        var request = new ScopeResolutionRequest(Guid.NewGuid());

        var result = await resolver.ResolveAndValidateScopeAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.ErrorMessage);
    }

    [Fact]
    public async Task ResolveScope_InactiveUser_Fails()
    {
        var userId = Guid.NewGuid();
        _hierarchy.AddUser(userId, null, null, false, false);

        var resolver = new AuthoritativeScopeResolver(_hierarchy);
        var request = new ScopeResolutionRequest(userId);

        var result = await resolver.ResolveAndValidateScopeAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Contains("inactive or locked", result.ErrorMessage);
    }

    [Fact]
    public async Task ResolveScope_InconsistentTerminalBranch_Fails()
    {
        var orgId = Guid.NewGuid();
        var compId = Guid.NewGuid();
        var branch1 = Guid.NewGuid();
        var branch2 = Guid.NewGuid();
        var termId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _hierarchy.AddOrganization(orgId, true);
        _hierarchy.AddCompany(compId, orgId, true);
        _hierarchy.AddBranch(branch1, compId, true);
        _hierarchy.AddBranch(branch2, compId, true);
        _hierarchy.AddTerminal(termId, branch1, true); // Terminal belongs to branch1
        _hierarchy.AddUser(userId, compId, branch2, true, false);

        var resolver = new AuthoritativeScopeResolver(_hierarchy);
        // Request specifies branch2, but terminal belongs to branch1
        var request = new ScopeResolutionRequest(userId, termId, branch2, compId, orgId);

        var result = await resolver.ResolveAndValidateScopeAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Contains("Hierarchy inconsistency", result.ErrorMessage);
        Assert.Contains("belongs to Branch", result.ErrorMessage);
    }

    [Fact]
    public async Task ResolveScope_InconsistentBranchCompany_Fails()
    {
        var orgId = Guid.NewGuid();
        var comp1 = Guid.NewGuid();
        var comp2 = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _hierarchy.AddOrganization(orgId, true);
        _hierarchy.AddCompany(comp1, orgId, true);
        _hierarchy.AddCompany(comp2, orgId, true);
        _hierarchy.AddBranch(branchId, comp1, true); // Branch belongs to comp1
        _hierarchy.AddUser(userId, comp2, branchId, true, false);

        var resolver = new AuthoritativeScopeResolver(_hierarchy);
        // Request specifies comp2, but branch belongs to comp1
        var request = new ScopeResolutionRequest(userId, null, branchId, comp2, orgId);

        var result = await resolver.ResolveAndValidateScopeAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Contains("Hierarchy inconsistency", result.ErrorMessage);
        Assert.Contains("belongs to Company", result.ErrorMessage);
    }

    [Fact]
    public async Task ResolveScope_InconsistentCompanyOrganization_Fails()
    {
        var org1 = Guid.NewGuid();
        var org2 = Guid.NewGuid();
        var compId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _hierarchy.AddOrganization(org1, true);
        _hierarchy.AddOrganization(org2, true);
        _hierarchy.AddCompany(compId, org1, true); // Company belongs to org1
        _hierarchy.AddBranch(branchId, compId, true);
        _hierarchy.AddUser(userId, compId, branchId, true, false);

        var resolver = new AuthoritativeScopeResolver(_hierarchy);
        // Request specifies org2, but company belongs to org1
        var request = new ScopeResolutionRequest(userId, null, branchId, compId, org2);

        var result = await resolver.ResolveAndValidateScopeAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Contains("Hierarchy inconsistency", result.ErrorMessage);
        Assert.Contains("belongs to Organization", result.ErrorMessage);
    }

    [Fact]
    public async Task ResolveScope_CallerCompanyMismatch_Unauthorized()
    {
        var orgId = Guid.NewGuid();
        var comp1 = Guid.NewGuid();
        var comp2 = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _hierarchy.AddOrganization(orgId, true);
        _hierarchy.AddCompany(comp1, orgId, true);
        _hierarchy.AddCompany(comp2, orgId, true);
        _hierarchy.AddBranch(branchId, comp1, true);
        // User assigned to comp2
        _hierarchy.AddUser(userId, comp2, null, true, false);

        var resolver = new AuthoritativeScopeResolver(_hierarchy);
        var request = new ScopeResolutionRequest(userId, null, branchId, comp1, orgId);

        var result = await resolver.ResolveAndValidateScopeAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Contains("cannot operate in Company", result.ErrorMessage);
    }

    [Fact]
    public async Task ResolveScope_CallerBranchMismatch_NonAdmin_Unauthorized()
    {
        var orgId = Guid.NewGuid();
        var compId = Guid.NewGuid();
        var branch1 = Guid.NewGuid();
        var branch2 = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _hierarchy.AddOrganization(orgId, true);
        _hierarchy.AddCompany(compId, orgId, true);
        _hierarchy.AddBranch(branch1, compId, true);
        _hierarchy.AddBranch(branch2, compId, true);
        // User assigned to branch1
        _hierarchy.AddUser(userId, compId, branch1, true, false);

        var resolver = new AuthoritativeScopeResolver(_hierarchy);
        // Request targets branch2
        var request = new ScopeResolutionRequest(userId, null, branch2, compId, orgId);

        var result = await resolver.ResolveAndValidateScopeAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Contains("cannot operate in Branch", result.ErrorMessage);
    }

    [Fact]
    public async Task ResolveScope_UnassignedUser_NonAdmin_DeniedGlobalAccess()
    {
        var orgId = Guid.NewGuid();
        var compId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _hierarchy.AddOrganization(orgId, true);
        _hierarchy.AddCompany(compId, orgId, true);
        _hierarchy.AddBranch(branchId, compId, true);
        // Unassigned non-admin user (CompanyId = null, BranchId = null)
        _hierarchy.AddUser(userId, null, null, true, false);

        var resolver = new AuthoritativeScopeResolver(_hierarchy);
        var request = new ScopeResolutionRequest(userId, null, branchId, compId, orgId);

        var result = await resolver.ResolveAndValidateScopeAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Contains("unresolved company authorization", result.ErrorMessage);
        Assert.Contains("Global access is denied", result.ErrorMessage);
    }

    [Fact]
    public async Task ResolveScope_ExplicitAdministratorTargetAuthorization_Succeeds()
    {
        var orgId = Guid.NewGuid();
        var compId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();

        _hierarchy.AddOrganization(orgId, true);
        _hierarchy.AddCompany(compId, orgId, true);
        _hierarchy.AddBranch(branchId, compId, true);
        // Unassigned administrator
        _hierarchy.AddUser(adminUserId, null, null, true, true);

        var resolver = new AuthoritativeScopeResolver(_hierarchy);
        var request = new ScopeResolutionRequest(
            adminUserId,
            null,
            branchId,
            compId,
            orgId,
            IsAdministratorElevation: true);

        var result = await resolver.ResolveAndValidateScopeAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Context);
        Assert.Equal(adminUserId, result.Context.UserId);
        Assert.Equal(orgId, result.Context.OrganizationId);
        Assert.Equal(compId, result.Context.CompanyId);
        Assert.Equal(branchId, result.Context.BranchId);
    }

    [Fact]
    public async Task ResolveScope_AdministratorWithoutElevationFlag_Fails()
    {
        var orgId = Guid.NewGuid();
        var compId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();

        _hierarchy.AddOrganization(orgId, true);
        _hierarchy.AddCompany(compId, orgId, true);
        _hierarchy.AddBranch(branchId, compId, true);
        _hierarchy.AddUser(adminUserId, null, null, true, true);

        var resolver = new AuthoritativeScopeResolver(_hierarchy);
        // Elevation is false
        var request = new ScopeResolutionRequest(
            adminUserId,
            null,
            branchId,
            compId,
            orgId,
            IsAdministratorElevation: false);

        var result = await resolver.ResolveAndValidateScopeAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Contains("unresolved company authorization", result.ErrorMessage);
    }

    [Fact]
    public async Task ResolveScope_CompanyOnlyScope_DoesNotRequireBranch()
    {
        var orgId = Guid.NewGuid();
        var compId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _hierarchy.AddOrganization(orgId, true);
        _hierarchy.AddCompany(compId, orgId, true);
        _hierarchy.AddUser(userId, compId, null, true, false);

        var resolver = new AuthoritativeScopeResolver(_hierarchy);
        var request = new ScopeResolutionRequest(userId, null, null, compId, orgId);

        var result = await resolver.ResolveAndValidateScopeAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Context);
        Assert.Equal(compId, result.Context.CompanyId);
        Assert.Equal(orgId, result.Context.OrganizationId);
        Assert.Null(result.Context.BranchId);
    }

    private sealed class FakeTenancyHierarchyProvider : ITenancyHierarchyProvider
    {
        private readonly Dictionary<Guid, OrganizationScopeInfo> _orgs = new();
        private readonly Dictionary<Guid, CompanyScopeInfo> _companies = new();
        private readonly Dictionary<Guid, BranchScopeInfo> _branches = new();
        private readonly Dictionary<Guid, TerminalScopeInfo> _terminals = new();
        private readonly Dictionary<Guid, UserScopeInfo> _users = new();

        public void AddOrganization(Guid id, bool isActive) => _orgs[id] = new OrganizationScopeInfo(id, isActive);
        public void AddCompany(Guid id, Guid orgId, bool isActive) => _companies[id] = new CompanyScopeInfo(id, orgId, isActive);
        public void AddBranch(Guid id, Guid compId, bool isActive) => _branches[id] = new BranchScopeInfo(id, compId, isActive);
        public void AddTerminal(Guid id, Guid branchId, bool isActive) => _terminals[id] = new TerminalScopeInfo(id, branchId, isActive);
        public void AddUser(Guid id, Guid? compId, Guid? branchId, bool isActive, bool isAdmin) =>
            _users[id] = new UserScopeInfo(id, compId, branchId, isActive, isAdmin);

        public Task<OrganizationScopeInfo?> GetOrganizationAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_orgs.GetValueOrDefault(id));

        public Task<CompanyScopeInfo?> GetCompanyAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_companies.GetValueOrDefault(id));

        public Task<BranchScopeInfo?> GetBranchAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_branches.GetValueOrDefault(id));

        public Task<TerminalScopeInfo?> GetTerminalAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_terminals.GetValueOrDefault(id));

        public Task<UserScopeInfo?> GetUserAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_users.GetValueOrDefault(id));
    }
}
