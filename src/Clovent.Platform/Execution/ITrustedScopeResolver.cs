namespace Clovent.Platform.Execution;

/// <summary>
/// Parameters for resolving and authoritatively validating an execution scope.
/// </summary>
public sealed record ScopeResolutionRequest(
    Guid UserId,
    Guid? TerminalId = null,
    Guid? BranchId = null,
    Guid? CompanyId = null,
    Guid? OrganizationId = null,
    bool IsAdministratorElevation = false);

/// <summary>
/// Result of an authoritative scope resolution and verification attempt.
/// </summary>
public sealed record ScopeResolutionResult
{
    /// <summary>Indicates whether scope resolution succeeded.</summary>
    public bool IsSuccess { get; init; }

    /// <summary>The verified execution context if successful; otherwise <see langword="null"/>.</summary>
    public IExecutionContext? Context { get; init; }

    /// <summary>Diagnostic error message describing why scope resolution failed.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Creates a successful scope resolution result with the given verified context.</summary>
    /// <param name="context">The verified execution context.</param>
    public static ScopeResolutionResult Success(IExecutionContext context) => new()
    {
        IsSuccess = true,
        Context = context
    };

    /// <summary>Creates a failed scope resolution result with an error explanation.</summary>
    /// <param name="errorMessage">The reason resolution failed.</param>
    public static ScopeResolutionResult Failed(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// Authoritative resolver that validates Organization -> Company -> Branch -> Terminal
/// ownership consistency and the caller's access.
/// </summary>
public interface ITrustedScopeResolver
{
    /// <summary>
    /// Resolves and validates execution scope against the authoritative ownership hierarchy.
    /// </summary>
    Task<ScopeResolutionResult> ResolveAndValidateScopeAsync(
        ScopeResolutionRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Default implementation of <see cref="ITrustedScopeResolver"/>.
/// Validates ownership chains and asserts caller access, rejecting broken hierarchies,
/// unassigned user wildcards, and unverified parameters.
/// </summary>
public sealed class AuthoritativeScopeResolver : ITrustedScopeResolver
{
    private readonly ITenancyHierarchyProvider _hierarchyProvider;

    /// <summary>Initializes a new instance of <see cref="AuthoritativeScopeResolver"/>.</summary>
    /// <param name="hierarchyProvider">The provider for hierarchy and user metadata.</param>
    public AuthoritativeScopeResolver(ITenancyHierarchyProvider hierarchyProvider)
    {
        _hierarchyProvider = hierarchyProvider ?? throw new ArgumentNullException(nameof(hierarchyProvider));
    }

    /// <inheritdoc />
    public async Task<ScopeResolutionResult> ResolveAndValidateScopeAsync(
        ScopeResolutionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Verify User existence and active status
        var user = await _hierarchyProvider.GetUserAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return ScopeResolutionResult.Failed($"Authenticated user '{request.UserId}' was not found.");
        }

        if (!user.IsActive)
        {
            return ScopeResolutionResult.Failed($"User '{request.UserId}' is inactive or locked.");
        }

        Guid? resolvedBranchId = null;
        Guid? resolvedCompanyId = null;
        Guid? resolvedOrgId = null;

        // 2. Resolve & Validate Terminal -> Branch consistency
        if (request.TerminalId.HasValue && request.TerminalId.Value != Guid.Empty)
        {
            var terminal = await _hierarchyProvider.GetTerminalAsync(request.TerminalId.Value, cancellationToken);
            if (terminal == null || !terminal.IsActive)
            {
                return ScopeResolutionResult.Failed($"Terminal '{request.TerminalId.Value}' was not found or is inactive.");
            }

            if (request.BranchId.HasValue && request.BranchId.Value != Guid.Empty && terminal.BranchId != request.BranchId.Value)
            {
                return ScopeResolutionResult.Failed(
                    $"Hierarchy inconsistency: Terminal '{terminal.Id}' belongs to Branch '{terminal.BranchId}', not requested Branch '{request.BranchId.Value}'.");
            }

            resolvedBranchId = terminal.BranchId;
        }
        else if (request.BranchId.HasValue && request.BranchId.Value != Guid.Empty)
        {
            resolvedBranchId = request.BranchId.Value;
        }

        // 3. Resolve & Validate Branch -> Company consistency
        if (resolvedBranchId.HasValue)
        {
            var branch = await _hierarchyProvider.GetBranchAsync(resolvedBranchId.Value, cancellationToken);
            if (branch == null || !branch.IsActive)
            {
                return ScopeResolutionResult.Failed($"Branch '{resolvedBranchId.Value}' was not found or is inactive.");
            }

            if (request.CompanyId.HasValue && request.CompanyId.Value != Guid.Empty && branch.CompanyId != request.CompanyId.Value)
            {
                return ScopeResolutionResult.Failed(
                    $"Hierarchy inconsistency: Branch '{branch.Id}' belongs to Company '{branch.CompanyId}', not requested Company '{request.CompanyId.Value}'.");
            }

            resolvedCompanyId = branch.CompanyId;
        }
        else if (request.CompanyId.HasValue && request.CompanyId.Value != Guid.Empty)
        {
            resolvedCompanyId = request.CompanyId.Value;
        }

        // 4. Resolve & Validate Company -> Organization consistency
        if (resolvedCompanyId.HasValue)
        {
            var company = await _hierarchyProvider.GetCompanyAsync(resolvedCompanyId.Value, cancellationToken);
            if (company == null || !company.IsActive)
            {
                return ScopeResolutionResult.Failed($"Company '{resolvedCompanyId.Value}' was not found or is inactive.");
            }

            if (request.OrganizationId.HasValue && request.OrganizationId.Value != Guid.Empty && company.OrganizationId != request.OrganizationId.Value)
            {
                return ScopeResolutionResult.Failed(
                    $"Hierarchy inconsistency: Company '{company.Id}' belongs to Organization '{company.OrganizationId}', not requested Organization '{request.OrganizationId.Value}'.");
            }

            resolvedOrgId = company.OrganizationId;
        }
        else if (request.OrganizationId.HasValue && request.OrganizationId.Value != Guid.Empty)
        {
            resolvedOrgId = request.OrganizationId.Value;
        }

        // 5. Verify Organization active status
        if (resolvedOrgId.HasValue)
        {
            var org = await _hierarchyProvider.GetOrganizationAsync(resolvedOrgId.Value, cancellationToken);
            if (org == null || !org.IsActive)
            {
                return ScopeResolutionResult.Failed($"Organization '{resolvedOrgId.Value}' was not found or is inactive.");
            }
        }
        else
        {
            return ScopeResolutionResult.Failed("Unable to resolve required Organization scope from requested hierarchy.");
        }

        // 6. Verify Caller Access & Assignment Permissions
        // 6.1. Company matching
        if (user.CompanyId.HasValue && user.CompanyId.Value != Guid.Empty)
        {
            if (resolvedCompanyId.HasValue && user.CompanyId.Value != resolvedCompanyId.Value)
            {
                return ScopeResolutionResult.Failed(
                    $"Unauthorized: User '{user.Id}' is assigned to Company '{user.CompanyId.Value}' and cannot operate in Company '{resolvedCompanyId.Value}'.");
            }
        }
        else if (!user.IsAdministrator || !request.IsAdministratorElevation)
        {
            // Null user CompanyId means unresolved authorization, not unrestricted access
            return ScopeResolutionResult.Failed(
                $"Unassigned user '{user.Id}' has unresolved company authorization. Global access is denied.");
        }

        // 6.2. Branch matching
        if (resolvedBranchId.HasValue)
        {
            if (user.BranchId.HasValue && user.BranchId.Value != Guid.Empty)
            {
                if (user.BranchId.Value != resolvedBranchId.Value)
                {
                    if (!user.IsAdministrator || !request.IsAdministratorElevation)
                    {
                        return ScopeResolutionResult.Failed(
                            $"Unauthorized: User '{user.Id}' is assigned to Branch '{user.BranchId.Value}' and cannot operate in Branch '{resolvedBranchId.Value}'.");
                    }
                }
            }
            else if (!user.IsAdministrator || !request.IsAdministratorElevation)
            {
                // Null user BranchId means unresolved branch authorization
                return ScopeResolutionResult.Failed(
                    $"Unassigned user '{user.Id}' has unresolved branch authorization for Branch '{resolvedBranchId.Value}'. Global access is denied.");
            }
        }

        var context = new PlatformExecutionContext
        {
            UserId = user.Id,
            OrganizationId = resolvedOrgId,
            CompanyId = resolvedCompanyId,
            BranchId = resolvedBranchId
        };

        return ScopeResolutionResult.Success(context);
    }
}
