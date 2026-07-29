using Clovent.Identity.Application.Authorization;
using Clovent.Identity.Infrastructure.Caching;
using Clovent.Identity.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Clovent.Identity.Infrastructure.DependencyInjection;

/// <summary>
/// Identity's own Infrastructure-layer registration - the "Permission cache"
/// deliverable's concrete backend, plus (as of Milestone 13) the
/// <see cref="UnitOfWorkBehavior{TRequest,TResponse}"/> MediatR pipeline
/// registration (depends on <see cref="Clovent.Identity.Application.IUnitOfWork"/>,
/// registered by <c>PersistenceServiceCollectionExtensions.AddPersistence</c>,
/// which a host must therefore call before this method) - mirrors
/// <c>Clovent.Authentication.Infrastructure</c>'s identical registration.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Registers <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/>, <see cref="IPermissionCache"/>, and the Unit-of-Work pipeline behavior.</summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">Accepted for signature consistency with the AddApplication()/AddInfrastructure()/AddPersistence() convention; not read today.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.TryAddSingleton<IPermissionCache, MemoryPermissionCache>();

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));

        return services;
    }
}
