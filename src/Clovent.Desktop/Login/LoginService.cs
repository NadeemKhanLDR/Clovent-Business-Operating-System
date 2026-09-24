using Clovent.Authentication.Application;
using Clovent.Authentication.Application.Credentials.Commands;
using Clovent.Authentication.Application.LoginAttempts.Commands;
using Clovent.Authentication.Application.RefreshSessions.Commands;
using Clovent.Authentication.Application.Sessions.Commands;
using Clovent.Authentication.Credentials;
using Clovent.Authentication.LoginAttempts;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Users;
using Clovent.Identity.Users.ValueObjects;
using Clovent.Restaurant.Application.ActivityLogs.Commands;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Login;

/// <summary>
/// The real <see cref="ILoginService"/> - replaces Milestone 8's placeholder
/// registration only; <see cref="Clovent.Desktop.Forms.Identity.LoginForm"/> itself is unchanged.
/// Orchestrates Identity (resolve the submitted
/// identifier to a <see cref="User"/>), Authentication's Application layer
/// (via <see cref="IMediator"/> - <see cref="RecordLoginAttemptCommand"/>,
/// <see cref="RecordCredentialCheckCommand"/>, <see cref="StartSessionCommand"/>,
/// <see cref="IssueRefreshSessionCommand"/>), and credential verification
/// (<see cref="IPasswordHasher"/>/<see cref="IPinHasher"/>). On success,
/// establishes <see cref="ICurrentSession"/>.
/// </summary>
/// <remarks>
/// Creates one <see cref="IServiceScope"/> per <see cref="LoginAsync"/> call
/// and resolves every scoped dependency (repositories, <see cref="IMediator"/>,
/// and therefore the <c>AuthenticationDbContext</c>/<c>IdentityDbContext</c>
/// they use) from it. This matters: Authentication's <c>UnitOfWorkBehavior</c>
/// pipeline commits whichever <c>AuthenticationDbContext</c> instance was
/// resolved into the same DI scope as the command it wraps - without an
/// explicit scope here, a WinForms app has no ambient "current scope" the
/// way an ASP.NET Core request would, and each service could resolve a
/// different DbContext instance than the others used.
/// </remarks>
public sealed class LoginService(
    IServiceScopeFactory scopeFactory,
    ICurrentSession currentSession) : ILoginService
{
    private const string GenericFailureMessage = "Invalid username or password.";

    /// <inheritdoc/>
    public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Password) && string.IsNullOrWhiteSpace(request.Pin))
        {
            return LoginResult.Failure("Enter your password or PIN.");
        }

        using var scope = scopeFactory.CreateScope();
        var services = scope.ServiceProvider;
        var userRepository = services.GetRequiredService<IUserRepository>();
        var credentialsRepository = services.GetRequiredService<IUserCredentialsRepository>();
        var mediator = services.GetRequiredService<IMediator>();
        var passwordHasher = services.GetRequiredService<IPasswordHasher>();
        var pinHasher = services.GetRequiredService<IPinHasher>();

        // PIN-only sign-in (no username): the PIN must resolve the user, since
        // there is no identifier to look up. Salted hashes give no lookup key,
        // so every stored PIN hash is verified in turn; PINs are unique across
        // users (SetPinCommandHandler's uniqueness scan), so at most one can
        // match. The same generic failure message as every other bad
        // credential keeps this from revealing whether a PIN is in use.
        var pinOnly = string.IsNullOrWhiteSpace(request.Username) && !string.IsNullOrWhiteSpace(request.Pin);
        User? user;
        if (pinOnly)
        {
            user = await ResolveUserByPinAsync(credentialsRepository, userRepository, request.Pin!, pinHasher, cancellationToken);
        }
        else
        {
            user = await ResolveUserAsync(userRepository, request.Username, cancellationToken);
        }

        if (user is null)
        {
            var attemptedName = pinOnly ? "(pin)" : request.Username;
            await mediator.Send(new RecordLoginAttemptCommand(attemptedName, null, LoginOutcome.UserNotFound), cancellationToken);
            return LoginResult.Failure(GenericFailureMessage);
        }

        // Audit records need a non-empty identifier; PIN-only sign-in arrived
        // with none, so the resolved user's own username stands in for it in
        // every attempt record below.
        var attemptIdentifier = pinOnly ? user.UserName.Value : request.Username;

        if (user.Status == UserStatus.Locked)
        {
            await mediator.Send(new RecordLoginAttemptCommand(attemptIdentifier, user.Id.Value, LoginOutcome.UserLocked), cancellationToken);
            return LoginResult.Failure("Your account is locked. Contact an administrator.");
        }

        if (user.Status != UserStatus.Active)
        {
            await mediator.Send(new RecordLoginAttemptCommand(attemptIdentifier, user.Id.Value, LoginOutcome.UserInactive), cancellationToken);
            return LoginResult.Failure("Your account is not active yet.");
        }

        var credentials = await credentialsRepository.GetByUserIdAsync(user.Id, cancellationToken);
        var verified = credentials is not null && VerifyCredentials(credentials, request, passwordHasher, pinHasher);

        if (!verified)
        {
            await mediator.Send(new RecordLoginAttemptCommand(attemptIdentifier, user.Id.Value, LoginOutcome.InvalidCredentials), cancellationToken);
            if (credentials is not null)
            {
                await mediator.Send(new RecordCredentialCheckCommand(user.Id.Value, Succeeded: false), cancellationToken);
            }

            return LoginResult.Failure(GenericFailureMessage);
        }

        await mediator.Send(new RecordCredentialCheckCommand(user.Id.Value, Succeeded: true), cancellationToken);
        await mediator.Send(new RecordLoginAttemptCommand(attemptIdentifier, user.Id.Value, LoginOutcome.Succeeded), cancellationToken);

        var session = await mediator.Send(new StartSessionCommand(user.Id.Value), cancellationToken);

        if (request.RememberMe)
        {
            await mediator.Send(new IssueRefreshSessionCommand(session.SessionId), cancellationToken);
        }

        currentSession.SignIn(user.Id.Value, session.SessionId, user.DisplayName.Value);

        // Best-effort: the Restaurant activity log is a convenience audit
        // trail, not the authoritative record of this sign-in (that's
        // LoginAttempt, recorded above regardless of whether this call
        // succeeds) - see RestaurantPosView.LogActivityAsync's identical
        // reasoning for why a logging hiccup shouldn't turn a successful
        // sign-in into a failure.
        try
        {
            await mediator.Send(new RecordActivityCommand("Login", null, user.DisplayName.Value, Environment.MachineName), cancellationToken);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
        }

        return LoginResult.Success();
    }

    /// <summary>
    /// Resolves the submitted identifier as a username first, then as an
    /// email address - either value object's <c>Create</c> factory throws
    /// <see cref="ArgumentException"/> for a shape that doesn't match, which
    /// this treats as "try the other one" rather than propagating.
    /// </summary>
    private static async Task<User?> ResolveUserAsync(IUserRepository userRepository, string identifier, CancellationToken cancellationToken)
    {
        try
        {
            var userName = UserName.Create(identifier);
            var byUserName = await userRepository.GetByUserNameAsync(userName, cancellationToken);
            if (byUserName is not null)
            {
                return byUserName;
            }
        }
        catch (ArgumentException)
        {
            // Not a valid username shape - fall through and try email.
        }

        try
        {
            var email = Email.Create(identifier);
            return await userRepository.GetByEmailAsync(email, cancellationToken);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>
    /// Resolves the signing-in user for PIN-only sign-in: verifies the PIN
    /// against every stored PIN hash and returns the owning user, or
    /// <see langword="null"/> when no stored PIN matches.
    /// </summary>
    private static async Task<User?> ResolveUserByPinAsync(
        IUserCredentialsRepository credentialsRepository,
        IUserRepository userRepository,
        string pin,
        IPinHasher pinHasher,
        CancellationToken cancellationToken)
    {
        var all = await credentialsRepository.GetAllAsync(cancellationToken);
        foreach (var credentials in all)
        {
            if (credentials.PinHash is null || !pinHasher.Verify(pin, credentials.PinHash.Value))
                continue;

            return await userRepository.GetByIdAsync(credentials.UserId, cancellationToken);
        }

        return null;
    }

    private static bool VerifyCredentials(UserCredentials credentials, LoginRequest request, IPasswordHasher passwordHasher, IPinHasher pinHasher)
    {
        if (!string.IsNullOrWhiteSpace(request.Password) && credentials.PasswordHash is not null)
        {
            return passwordHasher.Verify(request.Password, credentials.PasswordHash.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Pin) && credentials.PinHash is not null)
        {
            return pinHasher.Verify(request.Pin, credentials.PinHash.Value);
        }

        return false;
    }
}
