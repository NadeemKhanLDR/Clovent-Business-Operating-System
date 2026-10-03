using Clovent.Authentication.Application;
using Clovent.Authentication.Infrastructure.Persistence;
using Clovent.Authentication.Infrastructure.Security;
using Clovent.Identity.Infrastructure.Persistence;
using Clovent.MasterData.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Commissioning.Services;

/// <summary>
/// Encapsulates isolated DI service collection composition for first-run commissioning.
/// Prevents UI forms from acting as secondary composition roots.
/// </summary>
public sealed class CommissioningBootstrapFactory : ICommissioningBootstrapFactory
{
    /// <inheritdoc/>
    public ServiceProvider CreateBootstrapServiceProvider(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

        services.AddDbContext<IdentityDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "Identity")));
        services.AddDbContext<AuthenticationDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "Authentication")));
        services.AddDbContext<MasterDataDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "MasterData")));

        return services.BuildServiceProvider();
    }
}
