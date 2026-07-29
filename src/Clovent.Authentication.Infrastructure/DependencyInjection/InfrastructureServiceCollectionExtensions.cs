using Clovent.Authentication.Application;
using Clovent.Authentication.Infrastructure.Persistence;
using Clovent.Authentication.Infrastructure.Security;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Clovent.Authentication.Infrastructure.DependencyInjection;

/// <summary>
/// Authentication's own Infrastructure-layer registration, following the
/// AddApplication()/AddInfrastructure()/AddPersistence() convention. Holds
/// what isn't specifically about persistence: the credential hashers and
/// the <see cref="UnitOfWorkBehavior{TRequest,TResponse}"/> MediatR pipeline
/// registration (it depends on <see cref="IUnitOfWork"/>, registered by
/// <see cref="PersistenceServiceCollectionExtensions.AddPersistence"/>,
/// which a host must therefore call before this method).
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Registers <see cref="IPasswordHasher"/>/<see cref="IPinHasher"/> and the Unit-of-Work pipeline behavior.</summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">Accepted for signature consistency with the AddApplication()/AddInfrastructure()/AddPersistence() convention; not read today.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.TryAddSingleton<IPinHasher, Pbkdf2PinHasher>();

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));

        return services;
    }
}
