using Clovent.Authentication.Application;
using Clovent.Authentication.Credentials;
using Clovent.Authentication.Infrastructure.Persistence;
using Clovent.Desktop.Theming;
using Clovent.Identity.Infrastructure.Persistence;
using Clovent.Identity.Users;
using Clovent.Identity.Users.ValueObjects;
using Clovent.Platform.Bootstrap;
using Microsoft.Extensions.Options;

namespace Clovent.Desktop.Seed;

/// <summary>
/// Development-only convenience: creates one demo user ("admin") if no user with that username exists yet, so
/// <see cref="Forms.Identity.LoginForm"/> is demonstrable end-to-end without a
/// separate registration flow. Gated by <see cref="DesktopOptions.SeedDevelopmentUser"/> and
/// permanently disabled in non-DEBUG and Production builds.
/// </summary>
/// <remarks>
/// In all environments, existing user credentials must NEVER be reset, overwritten, or repaired by seeding.
/// If an "admin" or existing user is found, this startup task is a strict no-op.
/// </remarks>
#if !DEBUG
#pragma warning disable CS9113 // Primary constructor parameters are intentionally unused in Release builds where development seeding is disabled.
#endif
public sealed class DevelopmentUserSeedStartupTask(
    IUserRepository userRepository,
    IdentityDbContext identityDbContext,
    IUserCredentialsRepository credentialsRepository,
    AuthenticationDbContext authenticationDbContext,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider,
    IOptions<DesktopOptions> options,
    IOptions<Clovent.Platform.Configuration.PlatformOptions>? platformOptions = null) : IStartupTask
{
    private const string DemoUserName = "admin";
    private const string DemoPassword = "Admin123!";

    /// <inheritdoc/>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
#if !DEBUG
        return;
#else
        if (!options.Value.SeedDevelopmentUser ||
            string.Equals(platformOptions?.Value?.EnvironmentName, "Production", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var existing = await userRepository.GetByUserNameAsync(UserName.Create(DemoUserName), cancellationToken)
            ?? await userRepository.GetByEmailAsync(Email.Create("admin@clovent.local"), cancellationToken);
        if (existing is not null)
        {
            // Seed protection: Existing credentials must NEVER be overwritten or repaired.
            return;
        }

        var user = User.Create(
            Email.Create("admin@clovent.local"),
            UserName.Create(DemoUserName),
            DisplayName.Create("Administrator"));
        user.Activate();

        await userRepository.AddAsync(user, cancellationToken);
        await identityDbContext.SaveChangesAsync(cancellationToken);

        var now = timeProvider.GetUtcNow();
        var credentials = UserCredentials.Create(user.Id, now);
        credentials.SetPassword(PasswordHash.Create(passwordHasher.Hash(DemoPassword)), now);

        await credentialsRepository.AddAsync(credentials, cancellationToken);
        await authenticationDbContext.SaveChangesAsync(cancellationToken);
#endif
    }
}
#if !DEBUG
#pragma warning restore CS9113
#endif
