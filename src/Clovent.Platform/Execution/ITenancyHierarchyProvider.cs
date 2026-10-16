namespace Clovent.Platform.Execution;

/// <summary>Summary information for an organization's tenancy scope.</summary>
public sealed record OrganizationScopeInfo(Guid Id, bool IsActive);

/// <summary>Summary information for a company's tenancy scope and parent organization.</summary>
public sealed record CompanyScopeInfo(Guid Id, Guid OrganizationId, bool IsActive);

/// <summary>Summary information for a branch's tenancy scope and parent company.</summary>
public sealed record BranchScopeInfo(Guid Id, Guid CompanyId, bool IsActive);

/// <summary>Summary information for a terminal's tenancy scope and parent branch.</summary>
public sealed record TerminalScopeInfo(Guid Id, Guid BranchId, bool IsActive);

/// <summary>Summary information for an authenticated user's assignment and privileges.</summary>
public sealed record UserScopeInfo(Guid Id, Guid? CompanyId, Guid? BranchId, bool IsActive, bool IsAdministrator);

/// <summary>
/// Abstraction providing authoritative verification of the Organization -> Company -> Branch -> Terminal
/// ownership hierarchy and user assignments, decoupled from specific entity models.
/// </summary>
public interface ITenancyHierarchyProvider
{
    /// <summary>Retrieves organization scope metadata by identity.</summary>
    Task<OrganizationScopeInfo?> GetOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves company scope metadata and its owning organization by identity.</summary>
    Task<CompanyScopeInfo?> GetCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves branch scope metadata and its owning company by identity.</summary>
    Task<BranchScopeInfo?> GetBranchAsync(Guid branchId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves terminal scope metadata and its owning branch by identity.</summary>
    Task<TerminalScopeInfo?> GetTerminalAsync(Guid terminalId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves user assignment metadata and administrative status by identity.</summary>
    Task<UserScopeInfo?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
