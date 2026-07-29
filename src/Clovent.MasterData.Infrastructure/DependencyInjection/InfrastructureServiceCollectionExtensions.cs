using Clovent.MasterData.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.MasterData.Infrastructure.DependencyInjection;

/// <summary>
/// MasterData's own Infrastructure-layer registration, following the
/// AddApplication()/AddInfrastructure()/AddPersistence() convention. Holds
/// the <see cref="UnitOfWorkBehavior{TRequest,TResponse}"/> MediatR pipeline
/// registration - depends on <see cref="Clovent.MasterData.Application.IUnitOfWork"/>,
/// registered by <c>PersistenceServiceCollectionExtensions.AddPersistence</c>,
/// which a host must therefore call before this method - mirrors
/// <c>Clovent.Identity.Infrastructure</c>'s identical registration.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Registers the Unit-of-Work pipeline behavior.</summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">Accepted for signature consistency with the AddApplication()/AddInfrastructure()/AddPersistence() convention; not read today.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));

        return services;
    }
}
