using Clovent.Platform.Sync;
using Clovent.Restaurant.Infrastructure.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Outbox;

public sealed class OutboxProcessorLifecycleTests
{
    [Fact]
    public void Dispose_UnsubscribesFromLongerLivedConnectivityProbe()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        var probe = new TrackingProbe();
        var processor = new OutboxProcessor(provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<OutboxProcessor>.Instance, probe);
        Assert.Equal(1, probe.SubscriberCount);

        processor.Dispose();
        processor.Dispose();

        Assert.Equal(0, probe.SubscriberCount);
        probe.SetConnected(true);
    }

    private sealed class TrackingProbe : INetworkConnectivityProbe
    {
        private EventHandler<bool>? _changed;
        public int SubscriberCount => _changed?.GetInvocationList().Length ?? 0;
        public bool IsConnected { get; private set; }
        public event EventHandler<bool>? ConnectivityChanged
        {
            add => _changed += value;
            remove => _changed -= value;
        }
        public Task<bool> CheckConnectivityAsync(CancellationToken cancellationToken = default) => Task.FromResult(IsConnected);
        public void SetConnected(bool isConnected)
        {
            IsConnected = isConnected;
            _changed?.Invoke(this, isConnected);
        }
    }
}
