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
    /// <summary>
    /// Registers Platform Foundation's Infrastructure-layer services: the
    /// <see cref="IExecutionContextAccessor"/> singleton. <paramref name="configuration"/>
    /// is accepted for signature consistency with the
    /// AddInfrastructure()/AddApplication()/AddPersistence() convention and
    /// for future infrastructure registrations that need it; it is not read
    /// today.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">Configuration available to infrastructure registrations.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddExecutionContextAccessor();

        return services;
    }
}
