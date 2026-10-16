namespace Clovent.Platform.Execution;

/// <summary>
/// Service enabling background workers and queue processors to establish verified,
/// isolated execution scopes for background jobs and outbox messages.
/// </summary>
public interface IBackgroundExecutionScopeService
{
    /// <summary>
    /// Validates message tenancy ownership and begins an ambient execution scope.
    /// Missing legacy scope throws a visible, actionable <see cref="ScopeUnresolvedException"/>
    /// rather than guessing a default.
    /// </summary>
    /// <param name="organizationId">Mandatory organization tenancy ID from message/job metadata.</param>
    /// <param name="companyId">Optional company ID within the organization.</param>
    /// <param name="branchId">Optional branch ID within the company.</param>
    /// <param name="userId">Optional user on whose behalf the operation is performed.</param>
    /// <param name="jobOrMessageType">Descriptive name of the message or job for diagnostics.</param>
    /// <returns>A disposable scope that restores ambient context upon completion.</returns>
    /// <exception cref="ScopeUnresolvedException">Thrown if organization scope is missing or empty.</exception>
    IDisposable EstablishScope(
        Guid? organizationId,
        Guid? companyId,
        Guid? branchId,
        Guid? userId,
        string jobOrMessageType);
}

/// <summary>
/// Default implementation of <see cref="IBackgroundExecutionScopeService"/>.
/// Validates persisted ownership and rejects missing scope with actionable exceptions.
/// </summary>
public sealed class BackgroundExecutionScopeService : IBackgroundExecutionScopeService
{
    private readonly IExecutionContextAccessor _accessor;
    private readonly IInteractiveScopeProvider? _interactiveScopeProvider;

    /// <summary>Initializes a new instance of <see cref="BackgroundExecutionScopeService"/>.</summary>
    /// <param name="accessor">The ambient execution context accessor.</param>
    /// <param name="interactiveScopeProvider">Optional interactive scope provider to suppress during background execution.</param>
    public BackgroundExecutionScopeService(
        IExecutionContextAccessor accessor,
        IInteractiveScopeProvider? interactiveScopeProvider = null)
    {
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
        _interactiveScopeProvider = interactiveScopeProvider;
    }

    /// <inheritdoc />
    public IDisposable EstablishScope(
        Guid? organizationId,
        Guid? companyId,
        Guid? branchId,
        Guid? userId,
        string jobOrMessageType)
    {
        if (!organizationId.HasValue || organizationId.Value == Guid.Empty)
        {
            throw new ScopeUnresolvedException(
                $"Background execution for '{jobOrMessageType}' failed: Message/job lacks required Organization tenancy scope. " +
                "Unscoped background execution is strictly forbidden and cannot guess a default.");
        }

        var context = new PlatformExecutionContext
        {
            UserId = userId,
            OrganizationId = organizationId.Value,
            CompanyId = companyId,
            BranchId = branchId,
            RequestId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            ExecutionTimestamp = DateTimeOffset.UtcNow
        };

        var scope = _accessor.BeginScope(context);
        var suppression = _interactiveScopeProvider?.Suppress();

        return new BackgroundScopeDisposable(scope, suppression);
    }

    private sealed class BackgroundScopeDisposable(IDisposable scope, IDisposable? suppression) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                suppression?.Dispose();
                scope.Dispose();
            }
        }
    }
}
