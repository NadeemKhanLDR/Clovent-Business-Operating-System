using Clovent.Platform.Execution;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Platform.DependencyInjection;

/// <summary>
/// Platform Foundation's own Infrastructure-layer registration. Every
/// future infrastructure project is expected to expose an equivalent
/// AddInfrastructure() in its own namespace, following this same naming
/// convention - never registering itself directly from a host's Program.cs.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddExecutionContextAccessor();

        return services;
    }
}
