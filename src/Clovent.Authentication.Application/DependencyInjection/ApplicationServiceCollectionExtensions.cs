using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Authentication.Application.DependencyInjection;

/// <summary>
/// Authentication's own Application-layer registration - the
/// AddApplication()/AddInfrastructure()/AddPersistence() convention's first
/// piece for this module. Milestone 5's doc explicitly deferred this ("no
/// host exists yet to register into"); Milestone 9 ("Authentication
/// Integration") is the first milestone with a real host
/// (<c>Clovent.Desktop</c>) that actually needs to dispatch these commands.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Registers MediatR, scanning this assembly for every <c>IRequestHandler</c> already defined (Sessions, LoginAttempts, RefreshSessions, Credentials).</summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">Accepted for signature consistency with the AddApplication()/AddInfrastructure()/AddPersistence() convention; not read today.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceCollectionExtensions).Assembly));

        // SessionTerminationCascade is a plain Application-layer domain service
        // shared by ExpireSessionCommandHandler, LogOutSessionCommandHandler, and
        // RevokeSessionCommandHandler to enforce the cross-aggregate rule "when a
        // Session ends, its active RefreshSession must be invalidated". It is not
        // a MediatR interface implementor, so AddMediatR's assembly scan does not
        // register it automatically. Scoped lifetime is required because it depends
        // on IRefreshSessionRepository, which is registered Scoped.
        services.AddScoped<Sessions.SessionTerminationCascade>();

        return services;
    }
}
