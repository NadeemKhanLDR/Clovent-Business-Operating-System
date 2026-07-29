using Clovent.Authentication.Application;
using Clovent.Authentication.Credentials;
using Clovent.Authentication.Infrastructure.Persistence;
using Clovent.Authentication.Infrastructure.Repositories;
using Clovent.Authentication.LoginAttempts;
using Clovent.Authentication.RefreshSessions;
using Clovent.Authentication.Sessions;
using Clovent.Platform.Bootstrap;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Clovent.Authentication.Infrastructure.DependencyInjection;

/// <summary>
/// Authentication's own Persistence-layer registration, following the
/// AddApplication()/AddInfrastructure()/AddPersistence() convention
/// documented by <c>Clovent.Platform.DependencyInjection.PersistenceServiceCollectionExtensions</c>.
/// </summary>
public static class PersistenceServiceCollectionExtensions
{
    /// <summary>The <c>ConnectionStrings</c> configuration key this module reads its SQL Server connection string from.</summary>
    public const string ConnectionStringName = "Authentication";

    /// <summary>
    /// Registers <see cref="AuthenticationDbContext"/> (SQL Server, using the
    /// <c>ConnectionStrings:Authentication</c> configuration value - there is
    /// deliberately no hardcoded fallback, matching <c>PlatformOptions</c>'s
    /// fail-fast philosophy), the four repository implementations, the
    /// <see cref="IUnitOfWork"/> seam, and <see cref="IPersistenceInitializer"/>
    /// for applying migrations at startup.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">Configuration to read the connection string from.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    /// <exception cref="InvalidOperationException">No <c>ConnectionStrings:Authentication</c> value is configured.</exception>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Missing required connection string 'ConnectionStrings:{ConnectionStringName}'.");

        services.AddDbContext<AuthenticationDbContext>(options => options.UseSqlServer(connectionString));

        services.TryAddScoped<ISessionRepository, SessionRepository>();
        services.TryAddScoped<ILoginAttemptRepository, LoginAttemptRepository>();
        services.TryAddScoped<IRefreshSessionRepository, RefreshSessionRepository>();
        services.TryAddScoped<IUserCredentialsRepository, UserCredentialsRepository>();

        services.TryAddScoped<IUnitOfWork, UnitOfWork>();

        // Scoped, not Singleton: AuthenticationPersistenceInitializer depends on
        // AuthenticationDbContext, which AddDbContext registers as Scoped - a
        // Singleton here would capture one DbContext instance for the lifetime of
        // the host. ApplicationBootstrapper.BuildAndInitializeAsync already
        // resolves every IPersistenceInitializer from a freshly-created scope.
        services.AddScoped<IPersistenceInitializer, AuthenticationPersistenceInitializer>();

        return services;
    }
}
