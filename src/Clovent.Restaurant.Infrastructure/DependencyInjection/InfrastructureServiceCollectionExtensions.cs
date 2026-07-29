using Clovent.Restaurant.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Restaurant.Infrastructure.DependencyInjection;

/// <summary>Restaurant's own Infrastructure-layer registration, following the AddApplication()/AddInfrastructure()/AddPersistence() convention. Holds the <see cref="UnitOfWorkBehavior{TRequest,TResponse}"/> MediatR pipeline registration.</summary>
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
