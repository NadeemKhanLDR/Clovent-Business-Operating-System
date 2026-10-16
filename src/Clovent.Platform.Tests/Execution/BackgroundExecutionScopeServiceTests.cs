using Clovent.Platform.Execution;
using Xunit;

namespace Clovent.Platform.Tests.Execution;

public sealed class BackgroundExecutionScopeServiceTests
{
    private readonly IExecutionContextAccessor _accessor = new ExecutionContextAccessor();

    [Fact]
    public void EstablishScope_ValidOrganization_SetsAmbientContext_AndRestoresOnDispose()
    {
        var service = new BackgroundExecutionScopeService(_accessor);
        var orgId = Guid.NewGuid();
        var compId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        Assert.Null(_accessor.Current);

        using (service.EstablishScope(orgId, compId, branchId, userId, "InventoryRestockJob"))
        {
            Assert.NotNull(_accessor.Current);
            Assert.Equal(orgId, _accessor.Current.OrganizationId);
            Assert.Equal(compId, _accessor.Current.CompanyId);
            Assert.Equal(branchId, _accessor.Current.BranchId);
            Assert.Equal(userId, _accessor.Current.UserId);
        }

        Assert.Null(_accessor.Current);
    }

    [Fact]
    public void EstablishScope_MissingNullOrganization_ThrowsScopeUnresolvedException_NoGuessing()
    {
        var service = new BackgroundExecutionScopeService(_accessor);

        var ex = Assert.Throws<ScopeUnresolvedException>(() =>
            service.EstablishScope(null, Guid.NewGuid(), Guid.NewGuid(), null, "LegacyOutboxSync"));

        Assert.Contains("LegacyOutboxSync", ex.Message);
        Assert.Contains("lacks required Organization tenancy scope", ex.Message);
        Assert.Contains("strictly forbidden", ex.Message);
    }

    [Fact]
    public void EstablishScope_EmptyGuidOrganization_ThrowsScopeUnresolvedException_NoGuessing()
    {
        var service = new BackgroundExecutionScopeService(_accessor);

        var ex = Assert.Throws<ScopeUnresolvedException>(() =>
            service.EstablishScope(Guid.Empty, null, null, null, "LegacyDeltaReplication"));

        Assert.Contains("LegacyDeltaReplication", ex.Message);
        Assert.Contains("lacks required Organization tenancy scope", ex.Message);
    }

    [Fact]
    public void EstablishScope_WhileInteractiveSessionActive_SuppressesInteractiveScopeAndRestoresOnDispose()
    {
        var fakeInteractive = new FakeInteractiveScopeProvider();
        var cashierContext = new PlatformExecutionContext
        {
            UserId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            BranchId = Guid.NewGuid()
        };
        fakeInteractive.SetCurrentContext(cashierContext);

        Assert.NotNull(fakeInteractive.GetCurrentContext());

        var service = new BackgroundExecutionScopeService(_accessor, fakeInteractive);
        var workerOrg = Guid.NewGuid();

        using (service.EstablishScope(workerOrg, null, null, null, "BackgroundWorkerJob"))
        {
            // Inside the background scope, interactive scope is suppressed
            Assert.Null(fakeInteractive.GetCurrentContext());
            Assert.Equal(workerOrg, _accessor.Current?.OrganizationId);
        }

        // After disposing background scope, interactive session scope is restored
        Assert.NotNull(fakeInteractive.GetCurrentContext());
        Assert.Equal(cashierContext.UserId, fakeInteractive.GetCurrentContext()?.UserId);
    }

    private sealed class FakeInteractiveScopeProvider : IInteractiveScopeProvider
    {
        private readonly AsyncLocal<bool> _suppressed = new();
        private IExecutionContext? _context;

        public IExecutionContext? GetCurrentContext() => _suppressed.Value ? null : _context;
        public void SetCurrentContext(IExecutionContext context) => _context = context;
        public void Clear() => _context = null;

        public IDisposable Suppress()
        {
            _suppressed.Value = true;
            return new SuppressionScope(this);
        }

        private sealed class SuppressionScope(FakeInteractiveScopeProvider parent) : IDisposable
        {
            public void Dispose() => parent._suppressed.Value = false;
        }
    }
}
