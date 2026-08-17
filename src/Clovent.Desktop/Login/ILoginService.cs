namespace Clovent.Desktop.Login;

/// <summary>
/// The one capability the Login form depends on. Owned by the UI layer
/// (the same Dependency Inversion pattern already used for
/// <c>Clovent.Authentication.Application.IIdentityUserService</c>/<c>IUnitOfWork</c>):
/// <see cref="Clovent.Desktop.Forms.Identity.LoginForm"/> calls only this interface and never contains
/// credential-checking logic itself. Milestone 8 registered a placeholder
/// implementation against it (deliberately no credential-checking logic,
/// per that milestone's brief); Milestone 9 ("Authentication Integration")
/// replaced only the DI registration with <see cref="LoginService"/>, the
/// real implementation that calls into <c>Clovent.Authentication.Application</c>
/// and <c>Clovent.Identity</c> - <see cref="Clovent.Desktop.Forms.Identity.LoginForm"/> itself did not change.
/// </summary>
public interface ILoginService
{
    /// <summary>Attempts to log in with the given credentials.</summary>
    Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// The credentials/options submitted from the Login form.
/// <see cref="Password"/> and <see cref="Pin"/> are both optional - the form
/// requires at least one of them, so a user may sign in with either.
/// </summary>
public sealed record LoginRequest(string Username, string? Password, string? Pin, bool RememberMe);

/// <summary>The outcome of a login attempt.</summary>
public sealed record LoginResult(bool Succeeded, string? ErrorMessage)
{
    /// <summary>A successful login.</summary>
    public static LoginResult Success() => new(true, null);

    /// <summary>A failed login, with a message safe to show the user (never raw exception detail).</summary>
    public static LoginResult Failure(string errorMessage) => new(false, errorMessage);
}
