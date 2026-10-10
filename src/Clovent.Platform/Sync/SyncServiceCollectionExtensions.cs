using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Platform.Sync;

/// <summary>Dependency injection extensions for CBOS Platform replication and delta-sync services.</summary>
public static class SyncServiceCollectionExtensions
{
    /// <summary>
    /// Registers production platform synchronization with authenticated HTTP transport,
    /// scope verification, network probing, and circuit breakers.
    /// In-memory transport and stores are never silently resolved in production.
    /// </summary>
    public static IServiceCollection AddPlatformSync(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddSingleton<INetworkConnectivityProbe, NetworkConnectivityProbe>();
        services.AddSingleton<SyncScopeContext>();

        services.AddOptions<SyncTransportOptions>();
        if (configuration != null)
        {
            services.Configure<SyncTransportOptions>(configuration.GetSection("Replication:Transport"));
        }

        services.AddSingleton<HttpDeltaSyncSender>();
        services.AddSingleton<IDeltaSyncTransport>(sp => sp.GetRequiredService<HttpDeltaSyncSender>());
        services.AddSingleton<ISyncPacketDispatcher, DeltaSyncDispatcher>();
        services.AddScoped<ISyncIngestionEngine, SyncIngestionEngine>();

        return services;
    }

    /// <summary>Registers test-only in-memory loopback transport and staging stores for unit and offline simulation testing.</summary>
    public static IServiceCollection AddTestPlatformSync(this IServiceCollection services)
    {
        services.AddSingleton<INetworkConnectivityProbe, NetworkConnectivityProbe>();
        services.AddSingleton<SyncScopeContext>();
        services.AddSingleton<IDeltaSyncTransport, InMemoryDeltaSyncTransport>();
        services.AddSingleton<ISyncPacketDispatcher, DeltaSyncDispatcher>();
        services.AddSingleton<ISyncIdempotencyStore, InMemorySyncIdempotencyStore>();
        services.AddSingleton<ISyncConflictStagingStore, InMemorySyncConflictStagingStore>();
        services.AddScoped<ISyncIngestionEngine, SyncIngestionEngine>();

        return services;
    }
}
