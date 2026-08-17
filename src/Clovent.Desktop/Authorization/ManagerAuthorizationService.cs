using Clovent.Authentication.Application;
using Clovent.Authentication.Application.Credentials.Commands;
using Clovent.Authentication.Application.LoginAttempts.Commands;
using Clovent.Authentication.Credentials;
using Clovent.Authentication.LoginAttempts;
using Clovent.Identity.Application.Authorization;
using Clovent.Identity.Users;
using Clovent.Identity.Users.ValueObjects;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Authorization;

/// <summary>
/// The real <see cref="IManagerAuthorizationService"/>, built out of the
/// pieces <see cref="Login.LoginService"/> already uses rather than any new
/// credential machinery - see <see cref="IManagerAuthorizationService"/> for
/// why that reuse is the point.
/// </summary>
/// <remarks>
/// Creates one <see cref="IServiceScope"/> per call and resolves every scoped
/// dependency from it, for the same reason <see cref="Login.LoginService"/>
/// does: a WinForms app has no ambient request scope, so without an explicit
/// one the repositories and the <c>UnitOfWorkBehavior</c> that commits their
/// work could end up holding different <c>DbContext</c> instances. A private
/// scope also keeps this check off whichever screen invoked it, so a
/// challenge can never collide with that screen's own in-flight work.
/// </remarks>
public sealed class ManagerAuthorizationService(IServiceScopeFactory scopeFactory) : IManagerAuthorizationService
{
    private const string GenericFailureMessage = "Invalid manager username or password.";

    /// <inheritdoc/>
    public async Task<ManagerAuthorizationResult> AuthorizeAsync(
        string userName,
        string password,
        string featureCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureCode);

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            return ManagerAuthorizationResult.Denied("Enter the manager's username and password.");
        }

        using var scope = scopeFactory.CreateScope();
        var services = scope.ServiceProvider;
        var userRepository = services.GetRequiredService<IUserRepository>();
        var credentialsRepository = services.GetRequiredService<IUserCredentialsRepository>();
        var passwordHasher = services.GetRequiredService<IPasswordHasher>();
        var featurePolicy = services.GetRequiredService<IFeatureAuthorizationPolicy>();
        var mediator = services.GetRequiredService<IMediator>();

        var user = await ResolveUserAsync(userRepository, userName, cancellationToken);

        if (user is null)
        {
            await mediator.Send(new RecordLoginAttemptCommand(userName, null, LoginOutcome.UserNotFound), cancellationToken);
            return ManagerAuthorizationResult.Denied(GenericFailureMessage);
        }

        if (user.Status == UserStatus.Locked)
        {
            await mediator.Send(new RecordLoginAttemptCommand(userName, user.Id.Value, LoginOutcome.UserLocked), cancellationToken);
            return ManagerAuthorizationResult.Denied("That account is locked. Contact an administrator.");
        }

        if (user.Status != UserStatus.Active)
        {
            await mediator.Send(new RecordLoginAttemptCommand(userName, user.Id.Value, LoginOutcome.UserInactive), cancellationToken);
            return ManagerAuthorizationResult.Denied(GenericFailureMessage);
        }

        var credentials = await credentialsRepository.GetByUserIdAsync(user.Id, cancellationToken);
        var verified = credentials?.PasswordHash is not null && passwordHasher.Verify(password, credentials.PasswordHash.Value);

        if (!verified)
        {
            // Recorded exactly as a failed sign-in would be, so repeated
            // guesses at this dialog run into the same lockout policy rather
            // than offering an unmetered way to test passwords.
            await mediator.Send(new RecordLoginAttemptCommand(userName, user.Id.Value, LoginOutcome.InvalidCredentials), cancellationToken);
            if (credentials is not null)
            {
                await mediator.Send(new RecordCredentialCheckCommand(user.Id.Value, Succeeded: false), cancellationToken);
            }

            return ManagerAuthorizationResult.Denied(GenericFailureMessage);
        }

        await mediator.Send(new RecordCredentialCheckCommand(user.Id.Value, Succeeded: true), cancellationToken);
        await mediator.Send(new RecordLoginAttemptCommand(userName, user.Id.Value, LoginOutcome.Succeeded), cancellationToken);

        // A genuine credential is not authority on its own: the account behind
        // it must actually be entitled to the action. Without this, one
        // cashier could authorize an override with another cashier's password.
        if (!await featurePolicy.CanUseFeatureAsync(user.Id.Value, featureCode, cancellationToken))
        {
            return ManagerAuthorizationResult.Denied(
                $"{user.DisplayName.Value} is not authorized to approve this action.");
        }

        return ManagerAuthorizationResult.Approved(user.Id.Value, user.DisplayName.Value);
    }

    /// <summary>Resolves the submitted identifier as a username first, then as an email address - see <see cref="Login.LoginService"/> for the identical reasoning.</summary>
    private static async Task<User?> ResolveUserAsync(IUserRepository userRepository, string identifier, CancellationToken cancellationToken)
    {
        try
        {
            var name = UserName.Create(identifier);
            var byUserName = await userRepository.GetByUserNameAsync(name, cancellationToken);
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
}
