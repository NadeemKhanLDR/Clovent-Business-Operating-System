using MediatR;
using Microsoft.Extensions.Logging;

namespace Clovent.Platform.Execution;

/// <summary>
/// MediatR pipeline behavior that enforces trusted execution scope across all dispatched commands and queries.
/// Binds interactive scope from <see cref="IInteractiveScopeProvider"/> at the request-dispatch boundary if
/// an ambient scope is not already present, propagates across child async operations, and reliably restores/disposes
/// the scope on completion or exception.
/// </summary>
public sealed class ScopeEnforcementBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IExecutionContextAccessor _accessor;
    private readonly IInteractiveScopeProvider? _interactiveScopeProvider;
    private readonly ILogger<ScopeEnforcementBehavior<TRequest, TResponse>>? _logger;

    /// <summary>Initializes a new instance of <see cref="ScopeEnforcementBehavior{TRequest, TResponse}"/>.</summary>
    /// <param name="accessor">The ambient execution context accessor.</param>
    /// <param name="interactiveScopeProvider">Optional interactive scope provider for desktop sessions.</param>
    /// <param name="logger">Optional diagnostic logger.</param>
    public ScopeEnforcementBehavior(
        IExecutionContextAccessor accessor,
        IInteractiveScopeProvider? interactiveScopeProvider = null,
        ILogger<ScopeEnforcementBehavior<TRequest, TResponse>>? logger = null)
    {
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
        _interactiveScopeProvider = interactiveScopeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        IDisposable? boundaryScope = null;

        try
        {
            // 1. Bind interactive scope at request-dispatch boundary if no ambient context exists
            if (_accessor.Current == null && _interactiveScopeProvider != null)
            {
                var interactiveContext = _interactiveScopeProvider.GetCurrentContext();
                if (interactiveContext != null)
                {
                    boundaryScope = _accessor.BeginScope(interactiveContext);
                }
            }

            // 2. Bypass if request explicitly allows anonymous scope
            if (request is IAllowAnonymousScope)
            {
                return await next();
            }

            // 3. Enforce Branch-level scope (requires BranchId, CompanyId, OrganizationId)
            if (request is IRequireBranchScope)
            {
                var current = _accessor.Current;
                if (current == null || !current.BranchId.HasValue || !current.CompanyId.HasValue || !current.OrganizationId.HasValue)
                {
                    throw new ScopeUnresolvedException(
                        $"Request '{typeof(TRequest).Name}' requires valid Branch, Company, and Organization scope, " +
                        $"but current execution context is incomplete or null (Org: {current?.OrganizationId}, Company: {current?.CompanyId}, Branch: {current?.BranchId}). " +
                        "Null scope never grants global access.");
                }

                if (request is ITargetBranchScoped targetBranch && current.BranchId.Value != targetBranch.TargetBranchId)
                {
                    throw new ScopeMismatchException(
                        $"Request '{typeof(TRequest).Name}' target branch '{targetBranch.TargetBranchId}' " +
                        $"does not match caller's verified active branch '{current.BranchId.Value}'.");
                }
            }
            // 4. Enforce Company-level scope (requires CompanyId, OrganizationId; BranchId NOT required)
            else if (request is IRequireCompanyScope)
            {
                var current = _accessor.Current;
                if (current == null || !current.CompanyId.HasValue || !current.OrganizationId.HasValue)
                {
                    throw new ScopeUnresolvedException(
                        $"Request '{typeof(TRequest).Name}' requires valid Company and Organization scope, " +
                        $"but current execution context is incomplete or null (Org: {current?.OrganizationId}, Company: {current?.CompanyId}). " +
                        "Null scope never grants global access.");
                }

                if (request is ITargetCompanyScoped targetCompany && current.CompanyId.Value != targetCompany.TargetCompanyId)
                {
                    throw new ScopeMismatchException(
                        $"Request '{typeof(TRequest).Name}' target company '{targetCompany.TargetCompanyId}' " +
                        $"does not match caller's verified active company '{current.CompanyId.Value}'.");
                }
            }
            // 5. Enforce Organization-level scope (requires OrganizationId; CompanyId/BranchId NOT required)
            else if (request is IRequireOrganizationScope)
            {
                var current = _accessor.Current;
                if (current == null || !current.OrganizationId.HasValue)
                {
                    throw new ScopeUnresolvedException(
                        $"Request '{typeof(TRequest).Name}' requires valid Organization scope, " +
                        "but current execution context is incomplete or null. Null scope never grants global access.");
                }

                if (request is ITargetOrganizationScoped targetOrg && current.OrganizationId.Value != targetOrg.TargetOrganizationId)
                {
                    throw new ScopeMismatchException(
                        $"Request '{typeof(TRequest).Name}' target organization '{targetOrg.TargetOrganizationId}' " +
                        $"does not match caller's verified active organization '{current.OrganizationId.Value}'.");
                }
            }

            return await next();
        }
        finally
        {
            boundaryScope?.Dispose();
        }
    }
}
