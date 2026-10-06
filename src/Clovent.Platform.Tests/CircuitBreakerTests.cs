using Clovent.Platform.CircuitBreakers;
using Xunit;

namespace Clovent.Platform.Tests;

public sealed class CircuitBreakerTests
{
    [Fact]
    public async Task CircuitBreaker_StartsClosed_AndExecutesSuccessfully()
    {
        var options = new CircuitBreakerOptions { FailureThreshold = 2 };
        var cb = new CircuitBreaker("TestService", options);

        Assert.Equal(CircuitBreakerState.Closed, cb.State);
        Assert.Equal(0, cb.FailureCount);

        var result = await cb.ExecuteAsync(() => Task.FromResult(42));
        Assert.Equal(42, result);
        Assert.Equal(CircuitBreakerState.Closed, cb.State);
    }

    [Fact]
    public async Task CircuitBreaker_Opens_WhenFailuresExceedThreshold()
    {
        var options = new CircuitBreakerOptions
        {
            FailureThreshold = 2,
            BreakDuration = TimeSpan.FromSeconds(5)
        };
        var cb = new CircuitBreaker("TestService", options);

        // First failure
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cb.ExecuteAsync<int>(() => throw new InvalidOperationException("Fail 1")));
        Assert.Equal(CircuitBreakerState.Closed, cb.State);
        Assert.Equal(1, cb.FailureCount);

        // Second failure -> trips circuit
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cb.ExecuteAsync<int>(() => throw new InvalidOperationException("Fail 2")));
        Assert.Equal(CircuitBreakerState.Open, cb.State);

        // Third call fails fast with CircuitBreakerOpenException
        var ex = await Assert.ThrowsAsync<CircuitBreakerOpenException>(() =>
            cb.ExecuteAsync(() => Task.FromResult(100)));
        Assert.Equal("TestService", ex.CircuitName);
    }

    [Fact]
    public async Task CircuitBreaker_TransitionsToHalfOpen_AndRecoversOnSuccess()
    {
        var options = new CircuitBreakerOptions
        {
            FailureThreshold = 1,
            BreakDuration = TimeSpan.FromMilliseconds(50)
        };
        var cb = new CircuitBreaker("TestService", options);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cb.ExecuteAsync<int>(() => throw new InvalidOperationException("Fail")));
        Assert.Equal(CircuitBreakerState.Open, cb.State);

        // Wait for cooldown
        await Task.Delay(70);

        // Trial attempt succeeds -> circuit resets to Closed
        var result = await cb.ExecuteAsync(() => Task.FromResult("OK"));
        Assert.Equal("OK", result);
        Assert.Equal(CircuitBreakerState.Closed, cb.State);
        Assert.Equal(0, cb.FailureCount);
    }

    [Fact]
    public void CircuitBreakerRegistry_ReturnsSameInstanceByName()
    {
        var registry = new CircuitBreakerRegistry();
        var cb1 = registry.GetOrCreate("QuickBooks");
        var cb2 = registry.GetOrCreate("QuickBooks");

        Assert.Same(cb1, cb2);
        Assert.Equal("QuickBooks", cb1.Name);
    }
}
