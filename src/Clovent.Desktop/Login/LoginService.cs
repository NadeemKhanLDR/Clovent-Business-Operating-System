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

        var user = await ResolveUserAsync(userRepository, request.Username, cancellationToken);

        if (user is null)
        {
            await mediator.Send(new RecordLoginAttemptCommand(request.Username, null, LoginOutcome.UserNotFound), cancellationToken);
            return LoginResult.Failure(GenericFailureMessage);
        }

        if (user.Status == UserStatus.Locked)
        {
            await mediator.Send(new RecordLoginAttemptCommand(request.Username, user.Id.Value, LoginOutcome.UserLocked), cancellationToken);
            return LoginResult.Failure("Your account is locked. Contact an administrator.");
        }

        if (user.Status != UserStatus.Active)
        {
            await mediator.Send(new RecordLoginAttemptCommand(request.Username, user.Id.Value, LoginOutcome.UserInactive), cancellationToken);
            return LoginResult.Failure("Your account is not active yet.");
        }

        var credentials = await credentialsRepository.GetByUserIdAsync(user.Id, cancellationToken);
        var verified = credentials is not null && VerifyCredentials(credentials, request, passwordHasher, pinHasher);

        if (!verified)
        {
            await mediator.Send(new RecordLoginAttemptCommand(request.Username, user.Id.Value, LoginOutcome.InvalidCredentials), cancellationToken);
            if (credentials is not null)
            {
                await mediator.Send(new RecordCredentialCheckCommand(user.Id.Value, Succeeded: false), cancellationToken);
            }

            return LoginResult.Failure(GenericFailureMessage);
        }

        await mediator.Send(new RecordCredentialCheckCommand(user.Id.Value, Succeeded: true), cancellationToken);
        await mediator.Send(new RecordLoginAttemptCommand(request.Username, user.Id.Value, LoginOutcome.Succeeded), cancellationToken);

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
