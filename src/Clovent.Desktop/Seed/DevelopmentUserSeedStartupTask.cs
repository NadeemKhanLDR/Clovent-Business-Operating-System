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
/// Development-only convenience: creates one demo user ("admin", password
/// "Admin123!") if no user with that username exists yet, so
/// <see cref="Forms.Identity.LoginForm"/> is demonstrable end-to-end without a
/// separate registration flow (out of scope for every milestone in this
/// solution so far). Gated by <see cref="DesktopOptions.SeedDevelopmentUser"/> -
/// disabling that configuration value is how a non-development environment
/// opts out. Not a substitute for a real user-provisioning feature.
/// </summary>
/// <remarks>
/// If an "admin" user already exists, its credentials are checked against
/// <see cref="DemoPassword"/> and repaired via the normal
/// <see cref="UserCredentials.SetPassword"/> path if they no longer match -
/// e.g. after <see cref="DemoPassword"/> changed, or credentials were
/// otherwise seeded/overwritten with a different value. This never runs
/// outside <see cref="DesktopOptions.SeedDevelopmentUser"/>, never touches
/// any user other than the "admin" demo account, and goes through
/// <see cref="IPasswordHasher"/> exactly like every other password write -
/// no authentication bypass, no hardcoded verification shortcut.
/// </remarks>
public sealed class DevelopmentUserSeedStartupTask(
    IUserRepository userRepository,
    IdentityDbContext identityDbContext,
    IUserCredentialsRepository credentialsRepository,
    AuthenticationDbContext authenticationDbContext,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider,
    IOptions<DesktopOptions> options) : IStartupTask
{
    private const string DemoUserName = "admin";
    private const string DemoPassword = "Admin123!";

    /// <inheritdoc/>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!options.Value.SeedDevelopmentUser)
        {
            return;
        }

        var existing = await userRepository.GetByUserNameAsync(UserName.Create(DemoUserName), cancellationToken);
        if (existing is not null)
        {
            await RepairCredentialsIfMismatchedAsync(existing, cancellationToken);
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
    }

    /// <summary>
    /// Resets the demo admin's password hash if it doesn't verify against
    /// <see cref="DemoPassword"/> - e.g. left over from an earlier seed run
    /// with a different value. A no-op when the credentials already match,
    /// so this is safe to run on every startup.
    /// </summary>
    private async Task RepairCredentialsIfMismatchedAsync(User user, CancellationToken cancellationToken)
    {
        var credentials = await credentialsRepository.GetByUserIdAsync(user.Id, cancellationToken);
        if (credentials is null)
        {
            return;
        }

        if (credentials.PasswordHash is not null && passwordHasher.Verify(DemoPassword, credentials.PasswordHash.Value))
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        credentials.SetPassword(PasswordHash.Create(passwordHasher.Hash(DemoPassword)), now);
        credentials.ResetFailedAttempts();

        await authenticationDbContext.SaveChangesAsync(cancellationToken);
    }
}
