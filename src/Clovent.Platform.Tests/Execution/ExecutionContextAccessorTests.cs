using Clovent.Platform.Execution;
using Xunit;

namespace Clovent.Platform.Tests.Execution;

public sealed class ExecutionContextAccessorTests
{
    [Fact]
    public void Current_IsNull_ByDefault()
    {
        var accessor = new ExecutionContextAccessor();

        Assert.Null(accessor.Current);
    }

    [Fact]
    public void BeginScope_SetsCurrent_AndRestoresOnDispose()
    {
        var accessor = new ExecutionContextAccessor();
        var context = new PlatformExecutionContext { UserId = Guid.NewGuid() };

        using (accessor.BeginScope(context))
        {
            Assert.Same(context, accessor.Current);
        }

        Assert.Null(accessor.Current);
    }

    [Fact]
    public void BeginScope_Nested_RestoresOuterContext_OnInnerDispose()
    {
        var accessor = new ExecutionContextAccessor();
        var outer = new PlatformExecutionContext { UserId = Guid.NewGuid() };
        var inner = new PlatformExecutionContext { UserId = Guid.NewGuid() };

        using (accessor.BeginScope(outer))
        {
            using (accessor.BeginScope(inner))
            {
                Assert.Same(inner, accessor.Current);
            }

            Assert.Same(outer, accessor.Current);
        }

        Assert.Null(accessor.Current);
    }

    [Fact]
    public async Task BeginScope_DoesNotLeak_AcrossParallelAsyncFlows()
    {
        var accessor = new ExecutionContextAccessor();

        async Task<Guid?> RunInScopeAsync(Guid userId)
        {
            using (accessor.BeginScope(new PlatformExecutionContext { UserId = userId }))
            {
                // Yield so the two flows genuinely interleave rather than
                // running back-to-back on the same synchronous stack.
                await Task.Delay(10);
                return accessor.Current?.UserId;
            }
        }

        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        var resultA = Task.Run(() => RunInScopeAsync(userA));
        var resultB = Task.Run(() => RunInScopeAsync(userB));

        var results = await Task.WhenAll(resultA, resultB);

        Assert.Equal(userA, results[0]);
        Assert.Equal(userB, results[1]);
        Assert.Null(accessor.Current);
    }
}
