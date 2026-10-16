using Clovent.Identity.Branches;
using Clovent.Identity.Companies;
using Clovent.Identity.Organizations;
using Clovent.Identity.Users;
using Clovent.Identity.Application.Authorization;
using Clovent.MasterData.Shared;
using Clovent.MasterData.Terminals;
using Clovent.Platform.Execution;

namespace Clovent.Desktop.Security;

/// <summary>
/// Adapts CBOS domain repositories to the <see cref="ITenancyHierarchyProvider"/> abstraction,
/// enabling authoritative validation of Organization -> Company -> Branch -> Terminal ownership.
/// </summary>
public sealed class TenancyHierarchyProviderAdapter : ITenancyHierarchyProvider
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly ITerminalRepository _terminalRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuthorizationService _authorizationService;

    /// <summary>Initializes a new instance of <see cref="TenancyHierarchyProviderAdapter"/>.</summary>
    public TenancyHierarchyProviderAdapter(
        IOrganizationRepository organizationRepository,
        ICompanyRepository companyRepository,
        IBranchRepository branchRepository,
        ITerminalRepository terminalRepository,
        IUserRepository userRepository,
        IAuthorizationService authorizationService)
    {
        _organizationRepository = organizationRepository ?? throw new ArgumentNullException(nameof(organizationRepository));
        _companyRepository = companyRepository ?? throw new ArgumentNullException(nameof(companyRepository));
        _branchRepository = branchRepository ?? throw new ArgumentNullException(nameof(branchRepository));
        _terminalRepository = terminalRepository ?? throw new ArgumentNullException(nameof(terminalRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
    }

    /// <inheritdoc />
    public async Task<OrganizationScopeInfo?> GetOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var org = await _organizationRepository.GetByIdAsync(new OrganizationId(organizationId), cancellationToken);
        if (org == null) return null;

        return new OrganizationScopeInfo(org.Id.Value, org.Status == OrganizationStatus.Active);
    }

    /// <inheritdoc />
    public async Task<CompanyScopeInfo?> GetCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var company = await _companyRepository.GetByIdAsync(new CompanyId(companyId), cancellationToken);
        if (company == null) return null;

        return new CompanyScopeInfo(company.Id.Value, company.OrganizationId.Value, company.Status == CompanyStatus.Active);
    }

    /// <inheritdoc />
    public async Task<BranchScopeInfo?> GetBranchAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        var branch = await _branchRepository.GetByIdAsync(new BranchId(branchId), cancellationToken);
        if (branch == null) return null;

        return new BranchScopeInfo(branch.Id.Value, branch.CompanyId.Value, branch.Status == BranchStatus.Active);
    }

    /// <inheritdoc />
    public async Task<TerminalScopeInfo?> GetTerminalAsync(Guid terminalId, CancellationToken cancellationToken = default)
    {
        var terminal = await _terminalRepository.GetByIdAsync(new TerminalId(terminalId), cancellationToken);
        if (terminal == null) return null;

        return new TerminalScopeInfo(terminal.Id.Value, terminal.BranchId.Value, terminal.Status == MasterDataStatus.Active);
    }

    /// <inheritdoc />
    public async Task<UserScopeInfo?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(new UserId(userId), cancellationToken);
        if (user == null) return null;

        var isAdmin = await _authorizationService.HasRoleAsync(userId, "Administrator", cancellationToken) ||
                      await _authorizationService.HasRoleAsync(userId, "Admin", cancellationToken) ||
                      await _authorizationService.HasPermissionAsync(userId, "system.admin", cancellationToken);

        return new UserScopeInfo(
            user.Id.Value,
            user.CompanyId?.Value,
            user.BranchId?.Value,
            user.Status == UserStatus.Active,
            isAdmin);
    }
}
