using Clovent.Platform.Execution;
using Xunit;

namespace Clovent.Platform.Tests.Execution;

public sealed class ConcurrentScopeLeakageAndNestingTests
{
    private readonly IExecutionContextAccessor _accessor = new ExecutionContextAccessor();

    [Fact]
    public async Task ConcurrentAsyncScopes_DoNotLeakAcrossTasks()
    {
        var iterations = 20;
        var tasks = new Task[iterations];

        for (int i = 0; i < iterations; i++)
        {
            var orgId = Guid.NewGuid();
            var branchId = Guid.NewGuid();

            tasks[i] = Task.Run(async () =>
            {
                var context = new PlatformExecutionContext
                {
                    OrganizationId = orgId,
                    BranchId = branchId
                };

                using (_accessor.BeginScope(context))
                {
                    // Simulated async interleaved operations
                    for (int step = 0; step < 5; step++)
                    {
                        await Task.Delay(Random.Shared.Next(1, 10));
                        Assert.NotNull(_accessor.Current);
                        Assert.Equal(orgId, _accessor.Current.OrganizationId);
                        Assert.Equal(branchId, _accessor.Current.BranchId);
                    }
                }

                // Scope disposed for this async flow
                Assert.Null(_accessor.Current);
            });
        }

        await Task.WhenAll(tasks);
        Assert.Null(_accessor.Current);
    }

    [Fact]
    public void NestedScope_RestoresOuterContext_OnInnerException()
    {
        var outerOrg = Guid.NewGuid();
        var outerBranch = Guid.NewGuid();
        var innerOrg = Guid.NewGuid();
        var innerBranch = Guid.NewGuid();

        var outerContext = new PlatformExecutionContext
        {
            OrganizationId = outerOrg,
            BranchId = outerBranch
        };

        var innerContext = new PlatformExecutionContext
        {
            OrganizationId = innerOrg,
            BranchId = innerBranch
        };

        using (_accessor.BeginScope(outerContext))
        {
            Assert.Equal(outerOrg, _accessor.Current?.OrganizationId);

            try
            {
                using (_accessor.BeginScope(innerContext))
                {
                    Assert.Equal(innerOrg, _accessor.Current?.OrganizationId);
                    throw new InvalidOperationException("Simulated inner failure");
                }
            }
            catch (InvalidOperationException)
            {
                // Expected
            }

            // Outer context must be restored properly
            Assert.NotNull(_accessor.Current);
            Assert.Equal(outerOrg, _accessor.Current.OrganizationId);
            Assert.Equal(outerBranch, _accessor.Current.BranchId);
        }

        Assert.Null(_accessor.Current);
    }
}
