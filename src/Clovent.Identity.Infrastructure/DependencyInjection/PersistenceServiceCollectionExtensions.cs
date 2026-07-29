using Clovent.Identity.Application;
using Clovent.Identity.Branches;
using Clovent.Identity.Companies;
using Clovent.Identity.Infrastructure.Persistence;
using Clovent.Identity.Infrastructure.Repositories;
using Clovent.Identity.Organizations;
using Clovent.Identity.Permissions;
using Clovent.Identity.Roles;
using Clovent.Identity.Users;
using Clovent.Platform.Bootstrap;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Clovent.Identity.Infrastructure.DependencyInjection;

/// <summary>
/// Identity's own Persistence-layer registration, following the same
/// AddApplication()/AddInfrastructure()/AddPersistence() convention as every
/// other module - see <c>Clovent.Authentication.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions</c>
/// for the identical pattern.
/// </summary>
public static class PersistenceServiceCollectionExtensions
{
    /// <summary>The <c>ConnectionStrings</c> configuration key this module reads its SQL Server connection string from.</summary>
    public const string ConnectionStringName = "Identity";

    /// <summary>
    /// Registers <see cref="IdentityDbContext"/>,
    /// <see cref="IUserRepository"/>/<see cref="IRoleRepository"/>/<see cref="IPermissionRepository"/>/<see cref="IOrganizationRepository"/>/<see cref="ICompanyRepository"/>/<see cref="IBranchRepository"/>,
    /// the <see cref="IUnitOfWork"/> seam (Milestone 13's Organization/Company/Branch
    /// commands are the first Identity requests that need it), and an
    /// <see cref="IPersistenceInitializer"/> that applies migrations.
    /// </summary>
    /// <exception cref="InvalidOperationException">No <c>ConnectionStrings:Identity</c> value is configured.</exception>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Missing required connection string 'ConnectionStrings:{ConnectionStringName}'.");

        services.AddDbContext<IdentityDbContext>(options => options.UseSqlServer(connectionString));

        services.TryAddScoped<IUserRepository, UserRepository>();
        services.TryAddScoped<IRoleRepository, RoleRepository>();
        services.TryAddScoped<IPermissionRepository, PermissionRepository>();
        services.TryAddScoped<IOrganizationRepository, OrganizationRepository>();
        services.TryAddScoped<ICompanyRepository, CompanyRepository>();
        services.TryAddScoped<IBranchRepository, BranchRepository>();

        services.TryAddScoped<IUnitOfWork, UnitOfWork>();

        // Scoped, not Singleton - see Clovent.Authentication.Infrastructure's
        // identical reasoning: IdentityDbContext is Scoped, and
        // ApplicationBootstrapper.BuildAndInitializeAsync already resolves
        // every IPersistenceInitializer from a freshly-created scope.
        services.AddScoped<IPersistenceInitializer, IdentityPersistenceInitializer>();

        return services;
    }
}
