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
    /// <see cref="IExecutionContextAccessor"/> singleton and the real system
    /// <see cref="TimeProvider"/> (<see cref="TimeProvider.System"/>) -
    /// every command handler across this solution that needs the current
    /// time takes <see cref="TimeProvider"/> as a constructor dependency
    /// rather than calling <see cref="DateTimeOffset.UtcNow"/> directly, so
    /// one real registration here backs all of them; tests substitute their
    /// own fake instead. <paramref name="configuration"/> is accepted for
    /// signature consistency with the
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
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
