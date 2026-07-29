using Clovent.Identity.Application.Authorization;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Clovent.Identity.Application.DependencyInjection;

/// <summary>
/// Identity's own Application-layer registration, following the
/// AddApplication()/AddInfrastructure()/AddPersistence() convention. Milestone 10
/// ("Authorization") registered plain services since it is read-heavy
/// evaluation logic, not commands/queries; Milestone 13 ("Organization &amp;
/// Master Data Foundation") added a genuine CQRS surface for
/// Organization/Company/Branch, so this now also registers MediatR,
/// mirroring <c>Clovent.Authentication.Application</c>'s convention.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Registers <see cref="IAuthorizationService"/>, <see cref="IAuthorizationPolicyProvider"/>, the module/menu/feature policy wrappers, and MediatR (scanning this assembly for every <c>IRequestHandler</c>: Organizations, Companies, Branches). <see cref="IPermissionCache"/> is registered by Infrastructure's <c>AddInfrastructure</c>.</summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">Accepted for signature consistency with the AddApplication()/AddInfrastructure()/AddPersistence() convention; not read today.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddScoped<IAuthorizationService, AuthorizationService>();
        services.TryAddSingleton<IAuthorizationPolicyProvider, AuthorizationPolicyProvider>();

        services.TryAddScoped<IModuleAuthorizationPolicy, ModuleAuthorizationPolicy>();
        services.TryAddScoped<IMenuAuthorizationPolicy, MenuAuthorizationPolicy>();
        services.TryAddScoped<IFeatureAuthorizationPolicy, FeatureAuthorizationPolicy>();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceCollectionExtensions).Assembly));

        return services;
    }
}
